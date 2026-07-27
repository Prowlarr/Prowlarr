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

namespace NzbDrone.Core.Applications.Sportarr
{
    public class Sportarr : ApplicationBase<SportarrSettings>
    {
        public override string Name => "Sportarr";

        private readonly ICached<List<SportarrIndexer>> _schemaCache;
        private readonly ISportarrV1Proxy _sportarrV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Sportarr(ICacheManager cacheManager, ISportarrV1Proxy sportarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _schemaCache = cacheManager.GetCache<List<SportarrIndexer>>(GetType());
            _sportarrV1Proxy = sportarrV1Proxy;
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
                failures.AddIfNotNull(_sportarrV1Proxy.TestConnection(BuildSportarrIndexer(testIndexer, testIndexer.Capabilities, DownloadProtocol.Usenet), Settings));
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
                        failures.AddIfNotNull(new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, Sportarr cannot connect to Prowlarr"));
                        break;
                    case HttpStatusCode.SeeOther:
                    case HttpStatusCode.TemporaryRedirect:
                        _logger.Warn(ex, "Sportarr returned redirect and is invalid");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Sportarr URL is invalid, Prowlarr cannot connect to Sportarr - are you missing a URL base?"));
                        break;
                    case HttpStatusCode.NotFound:
                        _logger.Warn(ex, "Sportarr not found");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Sportarr URL is invalid, Prowlarr cannot connect to Sportarr. Is Sportarr running and accessible? Sportarr version 4.0.1023 or later is required."));
                        break;
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Sportarr. {ex.Message}"));
                        break;
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.Error(ex, "Unable to parse JSON response from application");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to parse JSON response from application. Sportarr version 4.0.1023 or later is required. {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Sportarr. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _sportarrV1Proxy.GetIndexers(Settings)
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

            if (!indexerCapabilities.TvSearchAvailable && !indexerCapabilities.MovieSearchAvailable && !indexerCapabilities.SearchAvailable)
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to missing TV, movie or basic search support by the indexer", indexer.Name, indexer.Id);

                return;
            }

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var sportarrIndexer = BuildSportarrIndexer(indexer, indexerCapabilities, indexer.Protocol);

            var remoteIndexer = _sportarrV1Proxy.AddIndexer(sportarrIndexer, Settings);

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
                // Remove Indexer remotely and then remove the mapping
                _sportarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var indexerCapabilities = GetIndexerCapabilities(indexer);
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var sportarrIndexer = BuildSportarrIndexer(indexer, indexerCapabilities, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _sportarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!sportarrIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if ((indexerCapabilities.TvSearchAvailable || indexerCapabilities.MovieSearchAvailable || indexerCapabilities.SearchAvailable) &&
                        indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        sportarrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => sportarrIndexer.Fields.All(s => s.Name != f.Name)));

                        // Retain user tags not-affiliated with Prowlarr
                        sportarrIndexer.Tags.UnionWith(remoteIndexer.Tags);

                        // Update the indexer if it still has categories that match
                        _sportarrV1Proxy.UpdateIndexer(sportarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _sportarrV1Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if ((indexerCapabilities.TvSearchAvailable || indexerCapabilities.MovieSearchAvailable || indexerCapabilities.SearchAvailable) &&
                    indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Sportarr", indexer.Name, indexer.Id);
                    sportarrIndexer.Id = 0;
                    var newRemoteIndexer = _sportarrV1Proxy.AddIndexer(sportarrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to Sportarr due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private SportarrIndexer BuildSportarrIndexer(IndexerDefinition indexer, IndexerCapabilities indexerCapabilities, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _sportarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));

            // Sportarr keeps seed criteria in flat fields (seedRatio, seedTime,
            // seasonPackSeedTime), not nested seedCriteria.* names.
            var syncFields = new List<string> { "baseUrl", "apiPath", "apiKey", "categories", "minimumSeeders", "seedRatio", "seedTime", "seasonPackSeedTime", "rejectBlocklistedTorrentHashes" };

            if (id == 0)
            {
                syncFields.AddRange(new List<string> { "additionalParameters" });
            }

            var newznab = schemas.First(i => i.Implementation == "Newznab");
            var torznab = schemas.First(i => i.Implementation == "Torznab");

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var sportarrIndexer = new SportarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<SportarrField>(),
                Tags = new HashSet<int>()
            };

            sportarrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.AppMinimumSeeders ?? indexer.AppProfile.Value.MinimumSeeders;
                sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedRatio").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedRatio;
                sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
                sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "seasonPackSeedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.PackSeedTime ?? ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;

                if (sportarrIndexer.Fields.Any(x => x.Name == "rejectBlocklistedTorrentHashes"))
                {
                    sportarrIndexer.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashes").Value = Settings.SyncRejectBlocklistedTorrentHashesWhileGrabbing;
                }
            }

            return sportarrIndexer;
        }
    }
}
