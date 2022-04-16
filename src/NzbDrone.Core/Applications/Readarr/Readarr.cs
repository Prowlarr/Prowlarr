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
                failures.AddIfNotNull(_readarrV1Proxy.TestConnection(BuildReadarrIndexer(testIndexer, DownloadProtocol.Usenet), Settings));
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Unable to complete application test, cannot connect to Readarr"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _readarrV1Proxy.GetIndexers(Settings)
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
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var readarrIndexer = BuildReadarrIndexer(indexer, indexer.Protocol, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = _readarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer found, syncing with current settings");

                if (!readarrIndexer.Equals(remoteIndexer))
                {
                    if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        // Update the indexer if it still has categories that match
                        _readarrV1Proxy.UpdateIndexer(readarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _readarrV1Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} to Readarr", indexer.Name);
                    readarrIndexer.Id = 0;
                    var newRemoteIndexer = _readarrV1Proxy.AddIndexer(readarrIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0}, skipping re-add to Readarr due to indexer capabilities", indexer.Name);
                }
            }
        }

        private ReadarrIndexer BuildReadarrIndexer(IndexerDefinition indexer, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _readarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));

            var newznab = schemas.Where(i => i.Implementation == "Newznab").First();
            var torznab = schemas.Where(i => i.Implementation == "Torznab").First();

            var schema = protocol == DownloadProtocol.Usenet ? newznab : torznab;

            var readarrIndexer = new ReadarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = schema.Fields,
            };

            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath").Value = "/api";
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey").Value = _configFileProvider.ApiKey;
            readarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories").Value = JArray.FromObject(indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));

            return readarrIndexer;
        }
    }
}
