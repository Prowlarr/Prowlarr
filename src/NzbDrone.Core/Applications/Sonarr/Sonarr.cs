using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Sonarr
{
    public class Sonarr : ApplicationBase<SonarrSettings>
    {
        public override string Name => "Sonarr";

        private readonly ICached<List<SonarrIndexer>> _schemaCache;
        private readonly ISonarrV3Proxy _sonarrV3Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Sonarr(ICacheManager cacheManager, ISonarrV3Proxy sonarrV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _schemaCache = cacheManager.GetCache<List<SonarrIndexer>>(GetType());
            _sonarrV3Proxy = sonarrV3Proxy;
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
                failures.AddIfNotNull(_sonarrV3Proxy.TestConnection(BuildSonarrIndexer(testIndexer, DownloadProtocol.Usenet), Settings));
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Unable to complete application test, cannot connect to Sonarr"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _sonarrV3Proxy.GetIndexers(Settings)
                                         .Where(i => i.Implementation == "Newznab" || i.Implementation == "Torznab");

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                if ((string)indexer.Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value == _configFileProvider.ApiKey)
                {
                    var match = AppIndexerRegex.Match((string)indexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value);

                    if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                    {
                        //Add parsed mapping if it's mapped to a Indexer in this Prowlarr instance
                        mappings.Add(new AppIndexerMap { RemoteIndexerId = indexer.Id, IndexerId = indexerId });
                    }
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any() || indexer.Capabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Any())
            {
                var sonarrIndexer = BuildSonarrIndexer(indexer, indexer.Protocol);

                var remoteIndexer = _sonarrV3Proxy.AddIndexer(sonarrIndexer, Settings);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
            }

            _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                _sonarrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var sonarrIndexer = BuildSonarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _sonarrV3Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!sonarrIndexer.Equals(remoteIndexer))
                {
                    if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any() || indexer.Capabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        sonarrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => !sonarrIndexer.Fields.Any(s => s.Name == f.Name)));

                        // Retain user settings not-affiliated with Prowlarr
                        sonarrIndexer.DownloadClientId = remoteIndexer.DownloadClientId;
                        sonarrIndexer.SeasonSearchMaximumSingleEpisodeAge = remoteIndexer.SeasonSearchMaximumSingleEpisodeAge;

                        // Update the indexer if it still has categories that match
                        _sonarrV3Proxy.UpdateIndexer(sonarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _sonarrV3Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any() || indexer.Capabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} to Sonarr", indexer.Name);
                    sonarrIndexer.Id = 0;
                    var newRemoteIndexer = _sonarrV3Proxy.AddIndexer(sonarrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0}, skipping re-add to Sonarr due to indexer capabilities", indexer.Name);
                }
            }
        }

        private SonarrIndexer BuildSonarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _sonarrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new string[] { "baseUrl", "apiPath", "apiKey", "categories", "animeCategories", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime", "seedCriteria.seasonPackSeedTime" };

            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var sonarrIndexer = new SonarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<SonarrField>()
            };

            sonarrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "animeCategories").Value = JArray.FromObject(indexer.Capabilities.Categories.SupportedCategories(Settings.AnimeSyncCategories.ToArray()));

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.AppMinimumSeeders ?? indexer.AppProfile.Value.MinimumSeeders;
                sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedRatio;
                sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
                sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.PackSeedTime ?? ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
            }

            return sonarrIndexer;
        }
    }
}
