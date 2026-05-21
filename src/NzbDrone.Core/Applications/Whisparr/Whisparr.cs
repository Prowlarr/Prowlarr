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

namespace NzbDrone.Core.Applications.Whisparr
{
    public class Whisparr : ApplicationBase<WhisparrSettings>
    {
        public override string Name => "Whisparr";

        private readonly IWhisparrV3Proxy _whisparrV3Proxy;
        private readonly ICached<List<WhisparrIndexer>> _schemaCache;
        private readonly IConfigFileProvider _configFileProvider;

        public Whisparr(ICacheManager cacheManager, IWhisparrV3Proxy whisparrV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _schemaCache = cacheManager.GetCache<List<WhisparrIndexer>>(GetType());
            _whisparrV3Proxy = whisparrV3Proxy;
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
                failures.AddIfNotNull(_whisparrV3Proxy.TestConnection(BuildWhisparrIndexer(testIndexer, testIndexer.Capabilities, DownloadProtocol.Usenet), Settings));
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
                        failures.AddIfNotNull(new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, Whisparr cannot connect to Prowlarr"));
                        break;
                    case HttpStatusCode.SeeOther:
                    case HttpStatusCode.TemporaryRedirect:
                        _logger.Warn(ex, "Whisparr returned redirect and is invalid");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Whisparr URL is invalid, Prowlarr cannot connect to Whisparr - are you missing a URL base?"));
                        break;
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Whisparr. {ex.Message}"));
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
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Whisparr. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _whisparrV3Proxy.GetIndexers(Settings)
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
            var indexerCapabilities = GetIndexerCapabilities(indexer);

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var whisparrIndexer = BuildWhisparrIndexer(indexer, indexerCapabilities, indexer.Protocol);

            var remoteIndexer = _whisparrV3Proxy.AddIndexer(whisparrIndexer, Settings);

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
                _whisparrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var indexerCapabilities = GetIndexerCapabilities(indexer);
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var whisparrIndexer = BuildWhisparrIndexer(indexer, indexerCapabilities, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _whisparrV3Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!whisparrIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        whisparrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => whisparrIndexer.Fields.All(s => s.Name != f.Name)));

                        // Retain user tags not-affiliated with Prowlarr
                        whisparrIndexer.Tags.UnionWith(remoteIndexer.Tags);

                        // Retain user settings not-affiliated with Prowlarr
                        whisparrIndexer.DownloadClientId = remoteIndexer.DownloadClientId;

                        // Update the indexer if it still has categories that match
                        _whisparrV3Proxy.UpdateIndexer(whisparrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _whisparrV3Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Whisparr", indexer.Name, indexer.Id);
                    whisparrIndexer.Id = 0;
                    var newRemoteIndexer = _whisparrV3Proxy.AddIndexer(whisparrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to Whisparr due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private WhisparrIndexer BuildWhisparrIndexer(IndexerDefinition indexer, IndexerCapabilities indexerCapabilities, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _whisparrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new[] { "baseUrl", "apiPath", "apiKey", "categories", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime", "seedCriteria.seasonPackSeedTime", "rejectBlocklistedTorrentHashesWhileGrabbing" };

            var newznab = schemas.First(i => i.Implementation == "Newznab");
            var torznab = schemas.First(i => i.Implementation == "Torznab");

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var whisparrIndexer = new WhisparrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<WhisparrField>(),
                Tags = new HashSet<int>()
            };

            whisparrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.AppMinimumSeeders ?? indexer.AppProfile.Value.MinimumSeeders;
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedRatio;
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;

                if (whisparrIndexer.Fields.Any(x => x.Name == "seedCriteria.seasonPackSeedTime"))
                {
                    whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.PackSeedTime ?? ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
                }

                if (whisparrIndexer.Fields.Any(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing"))
                {
                    whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing").Value = Settings.SyncRejectBlocklistedTorrentHashesWhileGrabbing;
                }
            }

            return whisparrIndexer;
        }
    }
}
