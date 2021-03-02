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

            failures.AddIfNotNull(_sonarrV3Proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
            {
                var schema = _schemaCache.Get(Definition.Settings.ToJson(), () => _sonarrV3Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
                var newznab = schema.Where(i => i.Implementation == "Newznab").First();
                var torznab = schema.Where(i => i.Implementation == "Torznab").First();

                var sonarrIndexer = BuildSonarrIndexer(indexer, indexer.Protocol == DownloadProtocol.Usenet ? newznab : torznab);

                var remoteIndexer = _sonarrV3Proxy.AddIndexer(sonarrIndexer, Settings);
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
                _sonarrV3Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer)
        {
            //Use the Id mapping here to delete the correct indexer
            throw new System.NotImplementedException();
        }

        private SonarrIndexer BuildSonarrIndexer(IndexerDefinition indexer, SonarrIndexer schema)
        {
            var sonarrIndexer = new SonarrIndexer
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

            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl}/api/v1/indexer/{indexer.Id}/";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/newznab";
            sonarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;

            return sonarrIndexer;
        }
    }
}
