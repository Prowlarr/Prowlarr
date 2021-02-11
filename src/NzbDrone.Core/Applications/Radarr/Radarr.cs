using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Radarr
{
    public class Radarr : ApplicationBase<RadarrSettings>
    {
        public override string Name => "Radarr";

        private readonly IRadarrV3Proxy _radarrV3Proxy;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IConfigFileProvider _configFileProvider;

        public Radarr(IRadarrV3Proxy radarrV3Proxy, IIndexerFactory indexerFactory, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _radarrV3Proxy = radarrV3Proxy;
            _indexerFactory = indexerFactory;
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
            var schema = _radarrV3Proxy.GetIndexerSchema(Settings);
            var newznab = schema.Where(i => i.Implementation == "Newznab").First();
            var torznab = schema.Where(i => i.Implementation == "Torznab").First();

            var radarrIndexer = BuildRadarrIndexer(indexer, indexer.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

            var remoteIndexer = _radarrV3Proxy.AddIndexer(radarrIndexer, Settings);
            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
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
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        public override void SyncIndexers()
        {
            // Pull Schema so we get the field mapping right
            var schema = _radarrV3Proxy.GetIndexerSchema(Settings);
            var newznab = schema.Where(i => i.Implementation == "Newznab").First();
            var torznab = schema.Where(i => i.Implementation == "Torznab").First();

            // Pull existing indexers from Radarr
            var indexers = _radarrV3Proxy.GetIndexers(Settings);

            //Pull all local indexers (TODO only those that support movie categories.)
            var prowlarrIndexers = _indexerFactory.Enabled();

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

                var radarrIndexer = BuildRadarrIndexer(definition, definition.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

                var remoteIndexer = _radarrV3Proxy.AddIndexer(radarrIndexer, Settings);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = definition.Id, RemoteIndexerId = remoteIndexer.Id });
            }

            //Delete Indexers that need Deleting.
        }

        private RadarrIndexer BuildRadarrIndexer(IndexerDefinition indexer, RadarrIndexer schema)
        {
            var radarrIndexer = new RadarrIndexer
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

            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            radarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return radarrIndexer;
        }
    }
}
