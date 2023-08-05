using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public class LazyLibrarian : ApplicationBase<LazyLibrarianSettings>
    {
        public override string Name => "LazyLibrarian";

        private readonly ILazyLibrarianV1Proxy _lazyLibrarianV1Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public LazyLibrarian(ILazyLibrarianV1Proxy lazyLibrarianV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _lazyLibrarianV1Proxy = lazyLibrarianV1Proxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                failures.AddIfNotNull(_lazyLibrarianV1Proxy.TestConnection(Settings));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to LazyLibrarian. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _lazyLibrarianV1Proxy.GetIndexers(Settings);

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                if (indexer.Apikey == _configFileProvider.ApiKey)
                {
                    var match = AppIndexerRegex.Match(indexer.Host);

                    if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                    {
                        // Add parsed mapping if it's mapped to a Indexer in this Prowlarr instance
                        mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerName = $"{indexer.Type},{indexer.Name}" });
                    }
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Trace("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var lazyLibrarianIndexer = BuildLazyLibrarianIndexer(indexer, indexer.Protocol);

            var remoteIndexer = _lazyLibrarianV1Proxy.AddIndexer(lazyLibrarianIndexer, Settings);

            if (remoteIndexer == null)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);

                return;
            }

            _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerName = $"{remoteIndexer.Type},{remoteIndexer.Name}" });
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);

            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);

            if (indexerMapping != null)
            {
                //Remove Indexer remotely and then remove the mapping
                var indexerProps = indexerMapping.RemoteIndexerName.Split(",");
                _lazyLibrarianV1Proxy.RemoveIndexer(indexerProps[1], (LazyLibrarianProviderType)Enum.Parse(typeof(LazyLibrarianProviderType), indexerProps[0]), Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);
            var indexerProps = indexerMapping.RemoteIndexerName.Split(",");

            var lazyLibrarianIndexer = BuildLazyLibrarianIndexer(indexer, indexer.Protocol, indexerProps[1]);

            //Use the old remote id to find the indexer on LazyLibrarian incase the update was from a name change in Prowlarr
            var remoteIndexer = _lazyLibrarianV1Proxy.GetIndexer(indexerProps[1], lazyLibrarianIndexer.Type, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} found", remoteIndexer.Name);

                if (!lazyLibrarianIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    _lazyLibrarianV1Proxy.UpdateIndexer(lazyLibrarianIndexer, Settings);
                    indexerMapping.RemoteIndexerName = $"{lazyLibrarianIndexer.Type},{lazyLibrarianIndexer.Altername}";
                    _appIndexerMapService.Update(indexerMapping);
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to LazyLibrarian", indexer.Name, indexer.Id);
                    var newRemoteIndexer = _lazyLibrarianV1Proxy.AddIndexer(lazyLibrarianIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerName = $"{newRemoteIndexer.Type},{newRemoteIndexer.Name}" });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to LazyLibrarian due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private LazyLibrarianIndexer BuildLazyLibrarianIndexer(IndexerDefinition indexer, DownloadProtocol protocol, string originalName = null)
        {
            var schema = protocol == DownloadProtocol.Usenet ? LazyLibrarianProviderType.Newznab : LazyLibrarianProviderType.Torznab;

            var lazyLibrarianIndexer = new LazyLibrarianIndexer
            {
                Name = originalName ?? $"{indexer.Name} (Prowlarr)",
                Altername = $"{indexer.Name} (Prowlarr)",
                Host = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/api",
                Apikey = _configFileProvider.ApiKey,
                Categories = string.Join(",", indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray())),
                Enabled = indexer.Enable,
                Type = schema,
                Priority = indexer.Priority
            };

            return lazyLibrarianIndexer;
        }
    }
}
