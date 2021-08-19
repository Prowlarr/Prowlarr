using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
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
        List<string> GetBlocklist();
    }

    public class IndexerDefinitionUpdateService : IIndexerDefinitionUpdateService, IExecute<IndexerDefinitionUpdateCommand>
    {
        private const int DEFINITION_VERSION = 1;
        private readonly List<string> _defintionBlocklist = new List<string>()
        {
            "aither",
            "animeworld",
            "blutopia",
            "beyond-hd",
            "beyond-hd-oneurl",
            "danishbytes",
            "hdbits",
            "shareisland"
        };

        private readonly IHttpClient _httpClient;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly ICached<CardigannDefinition> _cache;
        private readonly Logger _logger;

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public IndexerDefinitionUpdateService(IHttpClient httpClient,
                                          IAppFolderInfo appFolderInfo,
                                          IDiskProvider diskProvider,
                                          ICacheManager cacheManager,
                                          Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
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
                indexerList = response.Resource.Where(i => !_defintionBlocklist.Contains(i.File)).ToList();

                var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions", "Custom");

                var directoryInfo = new DirectoryInfo(definitionFolder);

                if (directoryInfo.Exists)
                {
                    var files = directoryInfo.GetFiles($"*.yml");

                    foreach (var file in files)
                    {
                        _logger.Debug("Loading Custom Cardigann definition " + file.FullName);

                        try
                        {
                            var definitionString = File.ReadAllText(file.FullName);
                            var definition = _deserializer.Deserialize<CardigannMetaDefinition>(definitionString);

                            definition.File = Path.GetFileNameWithoutExtension(file.Name);

                            if (indexerList.Any(i => i.File == definition.File || i.Name == definition.Name))
                            {
                                _logger.Warn("Custom Cardigann definition {0} does not have unique file name or Indexer name", file.FullName);
                                continue;
                            }

                            indexerList.Add(definition);
                        }
                        catch (Exception e)
                        {
                            _logger.Error($"Error while parsing custom Cardigann definition {file.FullName}\n{e}");
                        }
                    }
                }
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

        public List<string> GetBlocklist()
        {
            return _defintionBlocklist;
        }

        private CardigannDefinition GetHttpDefinition(string id)
        {
            var req = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}/{id}");
            var response = _httpClient.Get(req);
            var definition = _deserializer.Deserialize<CardigannDefinition>(response.Content);
            return CleanIndexerDefinition(definition);
        }

        private CardigannDefinition LoadIndexerDef(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
            {
                throw new ArgumentNullException(nameof(fileKey));
            }

            EnsureDefinitionsFolder();

            var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions");

            var directoryInfo = new DirectoryInfo(definitionFolder);

            if (directoryInfo.Exists)
            {
                var files = directoryInfo.GetFiles($"{fileKey}.yml", SearchOption.AllDirectories);

                if (files.Any())
                {
                    var file = files.First();
                    _logger.Trace("Loading Cardigann definition " + file.FullName);
                    try
                    {
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = _deserializer.Deserialize<CardigannDefinition>(definitionString);

                        return CleanIndexerDefinition(definition);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error while parsing Cardigann definition {file.FullName}\n{e}");
                    }
                }
            }

            return GetHttpDefinition(fileKey);
        }

        private CardigannDefinition CleanIndexerDefinition(CardigannDefinition definition)
        {
            if (definition.Settings == null)
            {
                definition.Settings = new List<SettingsField>
                            {
                                new SettingsField { Name = "username", Label = "Username", Type = "text" },
                                new SettingsField { Name = "password", Label = "Password", Type = "password" }
                            };
            }

            if (definition.Encoding == null)
            {
                definition.Encoding = "UTF-8";
            }

            if (definition.Login != null && definition.Login.Method == null)
            {
                definition.Login.Method = "form";
            }

            if (definition.Search.Paths == null)
            {
                definition.Search.Paths = new List<SearchPathBlock>();
            }

            // convert definitions with a single search Path to a Paths entry
            if (definition.Search.Path != null)
            {
                definition.Search.Paths.Add(new SearchPathBlock
                {
                    Path = definition.Search.Path,
                    Inheritinputs = true
                });
            }

            return definition;
        }

        public void Execute(IndexerDefinitionUpdateCommand message)
        {
            UpdateLocalDefinitions();
        }

        private void EnsureDefinitionsFolder()
        {
            var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions");

            _diskProvider.CreateFolder(definitionFolder);
        }

        private void UpdateLocalDefinitions()
        {
            var request = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}");
            var response = _httpClient.Get<List<CardigannMetaDefinition>>(request);

            foreach (var def in response.Resource)
            {
                try
                {
                    var startupFolder = _appFolderInfo.AppDataFolder;

                    EnsureDefinitionsFolder();

                    var saveFile = Path.Combine(startupFolder, "Definitions", $"{def.File}.yml");

                    _httpClient.DownloadFile($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}/{def.File}", saveFile);

                    _cache.Remove(def.File);

                    _logger.Debug("Updated definition: {0}", def.File);
                }
                catch (Exception ex)
                {
                    _logger.Error("Definition download failed: {0}, {1}", def.File, ex.Message);
                }
            }
        }
    }
}
