using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Radarr
{
    public class Radarr : ApplicationBase<RadarrSettings>
    {
        public override string Name => "Radarr";

        private readonly IRadarrV3Proxy _radarrV3Proxy;
        private readonly ICached<List<RadarrIndexer>> _schemaCache;
        private readonly IConfigFileProvider _configFileProvider;

        public Radarr(ICacheManager cacheManager, IRadarrV3Proxy radarrV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _schemaCache = cacheManager.GetCache<List<RadarrIndexer>>(GetType());
            _radarrV3Proxy = radarrV3Proxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_radarrV3Proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
            {
                var radarrIndexer = BuildRadarrIndexer(indexer, indexer.Protocol);

                var remoteIndexer = _radarrV3Proxy.AddIndexer(radarrIndexer, Settings);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
            }
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                _radarrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            _logger.Debug("Updating indexer {0}[{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var radarrIndexer = BuildRadarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _radarrV3Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!radarrIndexer.Equals(remoteIndexer))
                {
                    _radarrV3Proxy.UpdateIndexer(radarrIndexer, Settings);
                }
            }
            else
            {
                _logger.Debug("Remote indexer not found, re-adding indexer to Radarr");
                radarrIndexer.Id = 0;

                var newRemoteIndexer = _radarrV3Proxy.AddIndexer(radarrIndexer, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
            }
        }

        private RadarrIndexer BuildRadarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var schemas = _schemaCache.Get(Definition.Settings.ToJson(), () => _radarrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var radarrIndexer = new RadarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable,
                EnableAutomaticSearch = indexer.Enable,
                EnableInteractiveSearch = indexer.Enable,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = schema.Fields,
            };

            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return radarrIndexer;
        }
    }
}
