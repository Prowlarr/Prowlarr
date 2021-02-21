using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Lidarr
{
    public class Lidarr : ApplicationBase<LidarrSettings>
    {
        public override string Name => "Lidarr";

        private readonly ILidarrV1Proxy _lidarrV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Lidarr(ILidarrV1Proxy lidarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
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
            var schema = _lidarrV1Proxy.GetIndexerSchema(Settings);
            var newznab = schema.Where(i => i.Implementation == "Newznab").First();
            var torznab = schema.Where(i => i.Implementation == "Torznab").First();

            var lidarrIndexer = BuildLidarrIndexer(indexer, indexer.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

            var remoteIndexer = _lidarrV1Proxy.AddIndexer(lidarrIndexer, Settings);
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

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        private LidarrIndexer BuildLidarrIndexer(IndexerDefinition indexer, LidarrIndexer schema)
        {
            var lidarrIndexer = new LidarrIndexer
            {
                Id = 0,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = true,
                EnableAutomaticSearch = true,
                EnableInteractiveSearch = true,
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
