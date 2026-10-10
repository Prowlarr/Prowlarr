using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.CrossSeed
{
    public class CrossSeed : ApplicationBase<CrossSeedSettings>
    {
        public override string Name => "cross-seed";

        private readonly ICrossSeedProxy _crossSeedProxy;
        private readonly IConfigFileProvider _configFileProvider;

        public CrossSeed(ICrossSeedProxy crossSeedProxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _crossSeedProxy = crossSeedProxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                failures.AddIfNotNull(_crossSeedProxy.TestConnection(Settings));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to cross-seed. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _crossSeedProxy.GetIndexers(Settings);

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                if (indexer.ApiKey == _configFileProvider.ApiKey)
                {
                    if (TryExtractIndexerIdFromUrl(indexer.Url, out var indexerId))
                    {
                        // Add parsed mapping if it's mapped to an Indexer in this Prowlarr instance
                        mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerId = indexer.Id, RemoteIndexerName = indexer.Name });
                    }
                }
            }

            return mappings;
        }

        private bool TryExtractIndexerIdFromUrl(string url, out int indexerId)
        {
            indexerId = 0;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                var uri = new Uri(url);
                var pathSegments = uri.AbsolutePath.Trim('/').Split('/');

                // Expected format: /{indexerId}/api
                if (pathSegments.Length >= 2 && pathSegments[1] == "api")
                {
                    return int.TryParse(pathSegments[0], out indexerId);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to parse indexer URL: {0}", url);
            }

            return false;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            if (indexer.Protocol != DownloadProtocol.Torrent)
            {
                _logger.Debug("Skipping non-torrent indexer {0}", indexer.Name);
                return;
            }

            var crossSeedIndexer = BuildCrossSeedIndexer(indexer);

            try
            {
                var addedIndexer = _crossSeedProxy.AddIndexer(crossSeedIndexer, Settings);
                _logger.Debug("Added indexer {0} in cross-seed with ID {1}", indexer.Name, addedIndexer.Id);
                
                _appIndexerMapService.Insert(new AppIndexerMap 
                { 
                    AppId = Definition.Id, 
                    IndexerId = indexer.Id, 
                    RemoteIndexerId = addedIndexer.Id,
                    RemoteIndexerName = addedIndexer.Name
                });
            }
            catch (Exception ex)
            {
                _logger.Debug("Failed to add {0} [{1}]", indexer.Name, indexer.Id);
                throw;
            }
        }


        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            if (indexer.Protocol != DownloadProtocol.Torrent)
            {
                _logger.Debug("Skipping non-torrent indexer {0}", indexer.Name);
                return;
            }

            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            if (indexerMapping == null)
            {
                _logger.Debug("No existing mapping found for indexer {0}, adding as new", indexer.Name);
                AddIndexer(indexer);
                return;
            }

            var crossSeedIndexer = BuildCrossSeedIndexer(indexer);
            crossSeedIndexer.Id = indexerMapping.RemoteIndexerId;

            try
            {
                var remoteIndexer = _crossSeedProxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);
                if (remoteIndexer != null)
                {
                    _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                    if (!crossSeedIndexer.Equals(remoteIndexer) || forceSync)
                    {
                        _logger.Debug("Syncing remote indexer with current settings");
                        var updatedIndexer = _crossSeedProxy.UpdateIndexer(crossSeedIndexer, Settings);
                    }
                }
                else
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to cross-seed", indexer.Name, indexer.Id);
                    _appIndexerMapService.Delete(indexerMapping.Id);
                    AddIndexer(indexer);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to update indexer {0} in cross-seed", indexer.Name);
                
                // If update fails, the remote indexer might not exist anymore
                // Delete the mapping and re-add the indexer
                _logger.Debug("Update failed, removing stale mapping and re-adding indexer {0}", indexer.Name);
                _appIndexerMapService.Delete(indexerMapping.Id);
                AddIndexer(indexer);
            }
        }

        public override void RemoveIndexer(int indexerId)
        {
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexerId);
            
            if (indexerMapping != null)
            {
                try
                {
                    _crossSeedProxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                    _logger.Debug("Removed indexer {0} from cross-seed (remote ID: {1})", indexerId, indexerMapping.RemoteIndexerId);
                    
                    _appIndexerMapService.Delete(indexerMapping.Id);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Failed to remove indexer {0} from cross-seed", indexerId);
                    throw;
                }
            }
            else
            {
                _logger.Debug("No mapping found for indexer ID {0}, nothing to remove", indexerId);
            }
        }

        private CrossSeedIndexer BuildCrossSeedIndexer(IndexerDefinition indexer)
        {
            var prowlarrUrl = Settings.ProwlarrUrl?.TrimEnd('/') ?? _configFileProvider.UrlBase?.TrimEnd('/') ?? "http://localhost:9696";

            return new CrossSeedIndexer
            {
                Name = indexer.Name,
                Url = $"{prowlarrUrl}/{indexer.Id}/api",
                ApiKey = _configFileProvider.ApiKey,
                Enabled = indexer.Enable && (indexer.AppProfile?.Value?.EnableRss ?? true)
            };
        }
    }
}
