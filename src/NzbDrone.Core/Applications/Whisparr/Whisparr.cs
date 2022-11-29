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

namespace NzbDrone.Core.Applications.Whisparr
{
    public class Whisparr : ApplicationBase<WhisparrSettings>
    {
        public override string Name => "Whisparr";

        private readonly IWhisparrV3Proxy _whisparrV3Proxy;
        private readonly ICached<List<WhisparrIndexer>> _schemaCache;
        private readonly IConfigFileProvider _configFileProvider;

        public Whisparr(ICacheManager cacheManager, IWhisparrV3Proxy whisparrV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
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
                failures.AddIfNotNull(_whisparrV3Proxy.TestConnection(BuildWhisparrIndexer(testIndexer, DownloadProtocol.Usenet), Settings));
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Unable to complete application test, cannot connect to Whisparr"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _whisparrV3Proxy.GetIndexers(Settings)
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
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
            {
                var radarrIndexer = BuildWhisparrIndexer(indexer, indexer.Protocol);

                var remoteIndexer = _whisparrV3Proxy.AddIndexer(radarrIndexer, Settings);
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
                _whisparrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var whisparrIndexer = BuildWhisparrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _whisparrV3Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!whisparrIndexer.Equals(remoteIndexer))
                {
                    if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        whisparrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => !whisparrIndexer.Fields.Any(s => s.Name == f.Name)));

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

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} to Whisparr", indexer.Name);
                    whisparrIndexer.Id = 0;
                    var newRemoteIndexer = _whisparrV3Proxy.AddIndexer(whisparrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0}, skipping re-add to Radarr due to indexer capabilities", indexer.Name);
                }
            }
        }

        private WhisparrIndexer BuildWhisparrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _whisparrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new string[] { "baseUrl", "apiPath", "apiKey", "categories", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime" };

            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

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
                Fields = new List<WhisparrField>()
            };

            whisparrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value = indexer.AppProfile.Value.MinimumSeeders;
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedRatio;
                whisparrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value = ((ITorrentIndexerSettings)indexer.Settings).TorrentBaseSettings.SeedTime;
            }

            return whisparrIndexer;
        }
    }
}
