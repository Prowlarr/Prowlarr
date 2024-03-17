using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Lidarr
{
    public class Lidarr : ApplicationBase<LidarrSettings>
    {
        public override string Name => "Lidarr";

        private readonly ILidarrV1Proxy _lidarrV1Proxy;
        private readonly ICached<List<LidarrIndexer>> _schemaCache;
        private readonly IConfigFileProvider _configFileProvider;

        public Lidarr(ICacheManager cacheManager, ILidarrV1Proxy lidarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _schemaCache = cacheManager.GetCache<List<LidarrIndexer>>(GetType());
            _lidarrV1Proxy = lidarrV1Proxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            var testIndexer = new IndexerDefinition
            {
                Id = 0,
                Name = "Test",
                Protocol = DownloadProtocol.Usenet,
                Capabilities = new IndexerCapabilities()
            };

            foreach (var cat in NewznabStandardCategory.AllCats)
            {
                testIndexer.Capabilities.Categories.AddCategoryMapping(1, cat);
            }

            try
            {
                failures.AddIfNotNull(_lidarrV1Proxy.TestConnection(BuildLidarrIndexer(testIndexer, DownloadProtocol.Usenet), Settings));
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ApiKey", "API Key is invalid"));
                        break;
                    case HttpStatusCode.BadRequest:
                        _logger.Warn(ex, "Prowlarr URL is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, Lidarr cannot connect to Prowlarr"));
                        break;
                    case HttpStatusCode.SeeOther:
                        _logger.Warn(ex, "Lidarr returned redirect and is invalid");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Lidarr URL is invalid, Prowlarr cannot connect to Lidarr - are you missing a URL base?"));
                        break;
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Lidarr. {ex.Message}"));
                        break;
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.Error(ex, "Unable to parse JSON response from application");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to parse JSON response from application. {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Lidarr. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _lidarrV1Proxy.GetIndexers(Settings)
                .Where(i => i.Implementation is "Newznab" or "Torznab");

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                var baseUrl = (string)indexer.Fields.FirstOrDefault(x => x.Name == "baseUrl")?.Value ?? string.Empty;

                if (!baseUrl.StartsWith(Settings.ProwlarrUrl.TrimEnd('/')) &&
                    (string)indexer.Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value != _configFileProvider.ApiKey)
                {
                    continue;
                }

                var match = AppIndexerRegex.Match(baseUrl);

                if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                {
                    // Add parsed mapping if it's mapped to a Indexer in this Prowlarr instance
                    mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerId = indexer.Id });
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var lidarrIndexer = BuildLidarrIndexer(indexer, indexer.Protocol);

            var remoteIndexer = _lidarrV1Proxy.AddIndexer(lidarrIndexer, Settings);

            if (remoteIndexer == null)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);

                return;
            }

            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                _lidarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var lidarrIndexer = BuildLidarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _lidarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!lidarrIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        lidarrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => lidarrIndexer.Fields.All(s => s.Name != f.Name)));

                        // Retain user tags not-affiliated with Prowlarr
                        lidarrIndexer.Tags.UnionWith(remoteIndexer.Tags);

                        // Retain user settings not-affiliated with Prowlarr
                        lidarrIndexer.DownloadClientId = remoteIndexer.DownloadClientId;

                        // Update the indexer if it still has categories that match
                        _lidarrV1Proxy.UpdateIndexer(lidarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _lidarrV1Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Lidarr", indexer.Name, indexer.Id);
                    lidarrIndexer.Id = 0;
                    var newRemoteIndexer = _lidarrV1Proxy.AddIndexer(lidarrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to Lidarr due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private LidarrIndexer BuildLidarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _lidarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new List<string> { "baseUrl", "apiPath", "apiKey", "categories", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime", "seedCriteria.discographySeedTime", "rejectBlocklistedTorrentHashesWhileGrabbing" };

            if (id == 0)
            {
                // Ensuring backward compatibility with older versions on first sync
                syncFields.AddRange(new List<string> { "earlyReleaseLimit", "additionalParameters" });
            }

            var newznab = schemas.First(i => i.Implementation == "Newznab");
            var torznab = schemas.First(i => i.Implementation == "Torznab");

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var lidarrIndexer = new LidarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<LidarrField>(),
                Tags = new HashSet<int>()
            };

            lidarrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.AppMinimumSeeders ?? indexer.AppProfile.Value.MinimumSeeders;
                lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedRatio;
                lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;

                if (lidarrIndexer.Fields.Any(x => x.Name == "seedCriteria.discographySeedTime"))
                {
                    lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.discographySeedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.PackSeedTime ?? ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
                }

                if (lidarrIndexer.Fields.Any(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing"))
                {
                    lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing").Value = Settings.SyncRejectBlocklistedTorrentHashesWhileGrabbing;
                }
            }

            return lidarrIndexer;
        }
    }
}
