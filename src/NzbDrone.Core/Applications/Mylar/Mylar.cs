using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Mylar
{
    public class Mylar : ApplicationBase<MylarSettings>
    {
        public override string Name => "Mylar";

        private readonly IMylarV3Proxy _mylarV3Proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Mylar(IMylarV3Proxy mylarV3Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, Logger logger)
            : base(appIndexerMapService, logger)
        {
            _mylarV3Proxy = mylarV3Proxy;
            _configFileProvider = configFileProvider;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                failures.AddIfNotNull(_mylarV3Proxy.TestConnection(Settings));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Mylar. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = _mylarV3Proxy.GetIndexers(Settings);

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

            var mylarIndexer = BuildMylarIndexer(indexer, indexer.Protocol);

            var remoteIndexer = _mylarV3Proxy.AddIndexer(mylarIndexer, Settings);

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
                _mylarV3Proxy.RemoveIndexer(indexerProps[1], (MylarProviderType)Enum.Parse(typeof(MylarProviderType), indexerProps[0]), Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);
            var indexerProps = indexerMapping.RemoteIndexerName.Split(",");

            var mylarIndexer = BuildMylarIndexer(indexer, indexer.Protocol, indexerProps[1]);

            //Use the old remote id to find the indexer on Mylar incase the update was from a name change in Prowlarr
            var remoteIndexer = _mylarV3Proxy.GetIndexer(indexerProps[1], mylarIndexer.Type, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} found", remoteIndexer.Name);

                if (!mylarIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    _mylarV3Proxy.UpdateIndexer(mylarIndexer, Settings);
                    indexerMapping.RemoteIndexerName = $"{mylarIndexer.Type},{mylarIndexer.Altername}";
                    _appIndexerMapService.Update(indexerMapping);
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if (indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Mylar", indexer.Name, indexer.Id);
                    var newRemoteIndexer = _mylarV3Proxy.AddIndexer(mylarIndexer, Settings);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerName = $"{newRemoteIndexer.Type},{newRemoteIndexer.Name}" });
                }
                else
                {
                    _logger.Debug("Remote indexer not found for {0} [{1}], skipping re-add to Mylar due to indexer capabilities", indexer.Name, indexer.Id);
                }
            }
        }

        private MylarIndexer BuildMylarIndexer(IndexerDefinition indexer, DownloadProtocol protocol, string originalName = null)
        {
            var schema = protocol == DownloadProtocol.Usenet ? MylarProviderType.Newznab : MylarProviderType.Torznab;

            var mylarIndexer = new MylarIndexer
            {
                Name = originalName ?? $"{indexer.Name} (Prowlarr)",
                Altername = $"{indexer.Name} (Prowlarr)",
                Host = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/api",
                Apikey = _configFileProvider.ApiKey,
                Categories = string.Join(",", indexer.Capabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray())),
                Enabled = indexer.Enable,
                Type = schema,
            };

            return mylarIndexer;
        }
    }
}
