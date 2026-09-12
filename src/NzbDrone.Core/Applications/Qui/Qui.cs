using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Qui
{
    public class Qui : ApplicationBase<QuiSettings>
    {
        public override string Name => "qui";

        private readonly IQuiProxy _quiProxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Qui(IQuiProxy quiProxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _quiProxy = quiProxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_quiProxy.TestConnection(Settings));

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _quiProxy.GetIndexers(Settings);

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                var baseUrl = Settings.ProwlarrUrl.TrimEnd('/');

                if (indexer.Backend == "prowlarr" &&
                    (indexer.BaseUrl?.TrimEnd('/').Equals(baseUrl, StringComparison.OrdinalIgnoreCase) == true ||
                     indexer.BaseUrl?.StartsWith(baseUrl + "/", StringComparison.OrdinalIgnoreCase) == true) &&
                    int.TryParse(indexer.IndexerId, out var indexerId))
                {
                    mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerId = indexer.Id });
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Protocol != DownloadProtocol.Torrent)
            {
                return;
            }

            var indexerCapabilities = GetIndexerCapabilities(indexer);

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var quiIndexer = BuildQuiIndexer(indexer, indexerCapabilities);

            var remoteIndexer = _quiProxy.AddIndexer(quiIndexer, Settings);

            if (remoteIndexer == null)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);

                return;
            }

            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = remoteIndexer.Id });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                _quiProxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            if (indexer.Protocol != DownloadProtocol.Torrent)
            {
                return;
            }

            var indexerCapabilities = GetIndexerCapabilities(indexer);
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            var quiIndexer = BuildQuiIndexer(indexer, indexerCapabilities, indexerMapping?.RemoteIndexerId ?? 0);

            var remoteIndexer = indexerMapping?.RemoteIndexerId > 0
                ? _quiProxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings)
                : null;

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!quiIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        _quiProxy.UpdateIndexer(quiIndexer, Settings);
                    }
                    else
                    {
                        _quiProxy.RemoveIndexer(remoteIndexer.Id, Settings);

                        if (indexerMapping != null)
                        {
                            _appIndexerMapService.Delete(indexerMapping.Id);
                        }
                    }
                }
            }
            else
            {
                if (indexerMapping != null)
                {
                    _appIndexerMapService.Delete(indexerMapping.Id);
                }

                if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to qui", indexer.Name, indexer.Id);
                    quiIndexer.Id = 0;
                    var newRemoteIndexer = _quiProxy.AddIndexer(quiIndexer, Settings);

                    if (newRemoteIndexer == null)
                    {
                        _logger.Debug("Failed to re-add {0} [{1}] to qui", indexer.Name, indexer.Id);
                        return;
                    }

                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to qui due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private QuiIndexer BuildQuiIndexer(IndexerDefinition indexer, IndexerCapabilities indexerCapabilities, int id = 0)
        {
            var supportedCategories = indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray());

            var capabilities = new List<string> { "search" };

            if (indexerCapabilities.TvSearchAvailable)
            {
                capabilities.Add("tv-search");
            }

            if (indexerCapabilities.MovieSearchAvailable)
            {
                capabilities.Add("movie-search");
            }

            if (indexerCapabilities.MusicSearchAvailable)
            {
                capabilities.Add("music-search");
            }

            if (indexerCapabilities.BookSearchAvailable)
            {
                capabilities.Add("book-search");
            }

            return new QuiIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                BaseUrl = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/",
                ApiKey = _configFileProvider.ApiKey,
                Backend = "prowlarr",
                Enabled = indexer.Enable,
                Priority = indexer.Priority,
                TimeoutSeconds = 30,
                LimitDefault = 100,
                LimitMax = 200,
                IndexerId = indexer.Id.ToString(),
                Capabilities = capabilities,
                Categories = supportedCategories.Select(c => new QuiCategory { CategoryId = c, CategoryName = NewznabStandardCategory.GetCatDesc(c) }).ToList()
            };
        }
    }
}
