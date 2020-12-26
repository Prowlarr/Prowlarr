using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.Messaging.Commands;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NzbDrone.Core.IndexerVersions
{
    public interface IIndexerDefinitionUpdateService
    {
        List<CardigannMetaDefinition> All();
        CardigannDefinition GetDefinition(string fileKey);
    }

    public class IndexerDefinitionUpdateService : IIndexerDefinitionUpdateService, IExecute<IndexerDefinitionUpdateCommand>
    {
        private const int DEFINITION_VERSION = 1;

        private readonly IHttpClient _httpClient;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly ICached<CardigannDefinition> _cache;
        private readonly Logger _logger;

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public IndexerDefinitionUpdateService(IHttpClient httpClient,
                                          IAppFolderInfo appFolderInfo,
                                          ICacheManager cacheManager,
                                          Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _cache = cacheManager.GetCache<CardigannDefinition>(typeof(CardigannDefinition), "definitions");
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<CardigannMetaDefinition> All()
        {
            var indexerList = new List<CardigannMetaDefinition>();

            try
            {
                var request = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}");
                var response = _httpClient.Get<List<CardigannMetaDefinition>>(request);
                indexerList = response.Resource;
                indexerList.AddRange(response.Resource);
            }
            catch
            {
                _logger.Error("Failed to Connect to Indexer Definition Server for Indexer listing");
            }

            return indexerList;
        }

        public CardigannDefinition GetDefinition(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            var definition = _cache.Get(file, () => LoadIndexerDef(file));

            return definition;
        }

        private CardigannDefinition GetHttpDefinition(string id)
        {
            var req = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}/{id}");
            var response = _httpClient.Get(req);
            return _deserializer.Deserialize<CardigannDefinition>(response.Content);
        }

        private CardigannDefinition LoadIndexerDef(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
            {
                throw new ArgumentNullException(nameof(fileKey));
            }

            var definitionFolder = Path.Combine(_appFolderInfo.StartUpFolder, "Definitions");

            var directoryInfo = new DirectoryInfo(definitionFolder);

            if (directoryInfo.Exists)
            {
                var files = directoryInfo.GetFiles($"{fileKey}.yml");

                if (files.Any())
                {
                    var file = files.First();
                    _logger.Trace("Loading Cardigann definition " + file.FullName);
                    try
                    {
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = _deserializer.Deserialize<CardigannDefinition>(definitionString);
                        return definition;
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error while parsing Cardigann definition {file.FullName}\n{e}");
                    }
                }
            }
            else
            {
                return GetHttpDefinition(fileKey);
            }

            throw new ArgumentOutOfRangeException(nameof(fileKey));
        }

        public void Execute(IndexerDefinitionUpdateCommand message)
        {
            UpdateLocalDefinitions();
        }

        private void UpdateLocalDefinitions()
        {
            var request = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}");
            var response = _httpClient.Get<List<CardigannMetaDefinition>>(request);

            foreach (var def in response.Resource)
            {
                try
                {
                    var startupFolder = _appFolderInfo.StartUpFolder;

                    var saveFile = Path.Combine(startupFolder, "Definitions", $"{def.File}.yml");

                    _httpClient.DownloadFile($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}/{def.File}", saveFile);

                    _cache.Remove(def.File);

                    _logger.Info("Updated definition: {0}", def.File);
                }
                catch (Exception ex)
                {
                    _logger.Error("Definition download failed: {0}, {1}", def.File, ex.Message);
                }
            }
        }
    }
}
