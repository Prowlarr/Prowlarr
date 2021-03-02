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

namespace NzbDrone.Core.Applications.Readarr
{
    public class Readarr : ApplicationBase<ReadarrSettings>
    {
        public override string Name => "Readarr";

        private readonly ICached<List<ReadarrIndexer>> _schemaCache;
        private readonly IReadarrV1Proxy _readarrV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Readarr(ICacheManager cacheManager, IReadarrV1Proxy readarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _schemaCache = cacheManager.GetCache<List<ReadarrIndexer>>(GetType());
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
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
            {
                var readarrIndexer = BuildReadarrIndexer(indexer, indexer.Protocol);

                var remoteIndexer = _readarrV1Proxy.AddIndexer(readarrIndexer, Settings);
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
                _readarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            _logger.Debug("Updating indexer {0}[{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var readarrIndexer = BuildReadarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _readarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!readarrIndexer.Equals(remoteIndexer))
                {
                    _readarrV1Proxy.UpdateIndexer(readarrIndexer, Settings);
                }
            }
            else
            {
                _logger.Debug("Remote indexer not found, re-adding indexer to Readarr");
                readarrIndexer.Id = 0;

                var newRemoteIndexer = _readarrV1Proxy.AddIndexer(readarrIndexer, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
            }
        }

        private ReadarrIndexer BuildReadarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var schemas = _schemaCache.Get(Definition.Settings.ToJson(), () => _readarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var readarrIndexer = new ReadarrIndexer
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

            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return readarrIndexer;
        }
    }
}
