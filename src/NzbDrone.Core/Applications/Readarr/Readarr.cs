using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Readarr
{
    public class Readarr : ApplicationBase<ReadarrSettings>
    {
        public override string Name => "Readarr";

        private readonly IReadarrV1Proxy _readarrV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Readarr(IReadarrV1Proxy readarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _readarrV1Proxy = readarrV1Proxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_readarrV1Proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            var schema = _readarrV1Proxy.GetIndexerSchema(Settings);
            var newznab = schema.Where(i => i.Implementation == "Newznab").First();
            var torznab = schema.Where(i => i.Implementation == "Torznab").First();

            var readarrIndexer = BuildReadarrIndexer(indexer, indexer.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

            var remoteIndexer = _readarrV1Proxy.AddIndexer(readarrIndexer, Settings);
            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                _readarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        private ReadarrIndexer BuildReadarrIndexer(IndexerDefinition indexer, ReadarrIndexer schema)
        {
            var readarrIndexer = new ReadarrIndexer
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

            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return readarrIndexer;
        }
    }
}
