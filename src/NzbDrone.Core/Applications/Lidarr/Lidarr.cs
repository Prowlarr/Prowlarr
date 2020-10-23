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
        private readonly IIndexerFactory _indexerFactory;
        private readonly IConfigFileProvider _configFileProvider;

        public Lidarr(ILidarrV1Proxy lidarrV1Proxy, IIndexerFactory indexerFactory, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _lidarrV1Proxy = lidarrV1Proxy;
            _indexerFactory = indexerFactory;
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
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        public override void SyncIndexers()
        {
            // Pull Schema so we get the field mapping right
            var schema = _lidarrV1Proxy.GetIndexerSchema(Settings);
            var newznab = schema.Where(i => i.Implementation == "Newznab").First();
            var torznab = schema.Where(i => i.Implementation == "Torznab").First();

            // Pull existing indexers from Lidarr
            var indexers = _lidarrV1Proxy.GetIndexers(Settings);

            //Pull all local indexers (TODO only those that support movie categories.)
            var prowlarrIndexers = _indexerFactory.GetAvailableProviders();

            //Pull mapping so we can check the mapping to see what already exists.
            var indexerMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            //Add new Indexers
            foreach (var indexer in prowlarrIndexers)
            {
                //Don't add if it already exists in our mappings for this app (TODO should we check that it exists remote?)
                if (indexerMappings.Any(x => x.IndexerId == indexer.Definition.Id))
                {
                    continue;
                }

                var definition = (IndexerDefinition)indexer.Definition;

                var lidarrIndexer = BuildLidarrIndexer(definition, definition.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

                var remoteIndexer = _lidarrV1Proxy.AddIndexer(lidarrIndexer, Settings);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = definition.Id, RemoteIndexerId = remoteIndexer.Id });
            }

            //Delete Indexers that need Deleting.
        }

        private LidarrIndexer BuildLidarrIndexer(IndexerDefinition indexer, LidarrIndexer schema)
        {
            var lidarrIndexer = new LidarrIndexer
            {
                Id = 0,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.EnableRss,
                EnableAutomaticSearch = indexer.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = schema.Fields,
            };

            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/1/";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            lidarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return lidarrIndexer;
        }
    }
}
