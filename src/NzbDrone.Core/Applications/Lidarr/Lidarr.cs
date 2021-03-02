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

            failures.AddIfNotNull(_lidarrV1Proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
            {
                var lidarrIndexer = BuildLidarrIndexer(indexer, indexer.Protocol);

                var remoteIndexer = _lidarrV1Proxy.AddIndexer(lidarrIndexer, Settings);
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
                _lidarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            _logger.Debug("Updating indexer {0}[{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var readarrIndexer = BuildLidarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _lidarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!readarrIndexer.Equals(remoteIndexer))
                {
                    _lidarrV1Proxy.UpdateIndexer(readarrIndexer, Settings);
                }
            }
            else
            {
                _logger.Debug("Remote indexer not found, re-adding indexer to Lidarr");
                readarrIndexer.Id = 0;

                var newRemoteIndexer = _lidarrV1Proxy.AddIndexer(readarrIndexer, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
            }
        }

        private LidarrIndexer BuildLidarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var schemas = _schemaCache.Get(Definition.Settings.ToJson(), () => _lidarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var lidarrIndexer = new LidarrIndexer
            {
                Id = 0,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable,
                EnableAutomaticSearch = indexer.Enable,
                EnableInteractiveSearch = indexer.Enable,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = schema.Fields,
            };

            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return lidarrIndexer;
        }
    }
}
