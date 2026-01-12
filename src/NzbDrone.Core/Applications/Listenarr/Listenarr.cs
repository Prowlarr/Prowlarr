using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.Applications.Listenarr
{
    public class Listenarr : ApplicationBase<ListenarrSettings>
    {
        public override string Name => "Listenarr";

        private readonly IListenarrV1Proxy _listenarrV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly Lazy<ITagService> _tagService;

        public Listenarr(
            IListenarrV1Proxy listenarrV1Proxy,
            IAppIndexerMapService appIndexerMapService,
            IIndexerFactory indexerFactory,
            IConfigFileProvider configFileProvider,
            Lazy<ITagService> tagService,
            Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _listenarrV1Proxy = listenarrV1Proxy;
            _configFileProvider = configFileProvider;
            _tagService = tagService;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var status = _listenarrV1Proxy.GetStatus(Settings);

                if (status == null)
                {
                    failures.Add(new ValidationFailure(string.Empty, "Unable to connect to Listenarr"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.Add(new ValidationFailure(string.Empty, "Unable to send test message"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _listenarrV1Proxy.GetIndexers(Settings)?.Where(i => i.Implementation is "Newznab" or "Torznab");
            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers ?? Enumerable.Empty<ListenarrIndexer>())
            {
                var baseUrl = indexer.Fields?.FirstOrDefault(x => x.Name == "baseUrl")?.Value?.ToString() ?? indexer.BaseUrl ?? string.Empty;

                if (!baseUrl.StartsWith(Settings.ProwlarrUrl.TrimEnd('/')) &&
                    (string)indexer.Fields?.FirstOrDefault(x => x.Name == "apiKey")?.Value != _configFileProvider.ApiKey)
                {
                    continue;
                }

                var match = AppIndexerRegex.Match(baseUrl);

                if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                {
                    mappings.Add(new AppIndexerMap
                    {
                        IndexerId = indexerId,
                        RemoteIndexerId = indexer.Id
                    });
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            var indexerCapabilities = GetIndexerCapabilities(indexer);

            if (!indexerCapabilities.SearchAvailable)
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to missing search support by the indexer", indexer.Name, indexer.Id);
                return;
            }

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);
                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var listenarrIndexer = BuildListenarrIndexer(indexer, Settings);

            var remoteIndexer = _listenarrV1Proxy.AddIndexer(listenarrIndexer, Settings);

            if (remoteIndexer == null)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);
                return;
            }

            _appIndexerMapService.Insert(new AppIndexerMap
            {
                AppId = Definition.Id,
                IndexerId = indexer.Id,
                RemoteIndexerId = remoteIndexer.Id
            });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var mapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (mapping != null)
            {
                _listenarrV1Proxy.RemoveIndexer(mapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(mapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var indexerCapabilities = GetIndexerCapabilities(indexer);
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var mapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            if (mapping != null)
            {
                var listenarrIndexer = BuildListenarrIndexer(indexer, Settings, mapping.RemoteIndexerId);
                _listenarrV1Proxy.UpdateIndexer(listenarrIndexer, Settings);
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getTags")
            {
                var tags = _tagService.Value.All().Select(t => new { Value = t.Id, Name = t.Label });
                return new { options = tags };
            }

            return base.RequestAction(action, query);
        }

        private ListenarrIndexer BuildListenarrIndexer(IndexerDefinition indexer, ListenarrSettings settings, int remoteId = 0)
        {
            var listenarrIndexer = new ListenarrIndexer
            {
                Id = remoteId,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && indexer.AppProfile.Value.EnableRss,
                EnableAutomaticSearch = indexer.Enable && indexer.AppProfile.Value.EnableAutomaticSearch,
                EnableInteractiveSearch = indexer.Enable && indexer.AppProfile.Value.EnableInteractiveSearch,
                Priority = indexer.Priority,
                ConfigContract = "NewznabSettings",
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                Protocol = indexer.Protocol == DownloadProtocol.Usenet ? "usenet" : "torrent",
                Fields = new List<ListenarrField>
                {
                    new ListenarrField
                    {
                        Name = "baseUrl",
                        Value = $"{settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/api"
                    },
                    new ListenarrField
                    {
                        Name = "apiKey",
                        Value = _configFileProvider.ApiKey
                    },
                    new ListenarrField
                    {
                        Name = "categories",
                        Value = Array.Empty<int>()
                    }
                }
            };

            return listenarrIndexer;
        }
    }
}
