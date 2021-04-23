using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
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
        List<string> GetBlacklist();
    }

    public class IndexerDefinitionUpdateService : IIndexerDefinitionUpdateService, IExecute<IndexerDefinitionUpdateCommand>
    {
        private const int DEFINITION_VERSION = 1;
        private readonly List<string> _defintionBlacklist = new List<string>() { "blutopia", "beyond-hd", "beyond-hd-oneurl" };

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
                indexerList = response.Resource.Where(i => !_defintionBlacklist.Contains(i.File)).ToList();
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

        public List<string> GetBlacklist()
        {
            return _defintionBlacklist;
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

            EnsureDefinitionsFolder();

            var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions");

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

                        //defaults
                        if (definition.Settings == null)
                        {
                            definition.Settings = new List<SettingsField>
                            {
                                new SettingsField { Name = "username", Label = "Username", Type = "text" },
                                new SettingsField { Name = "password", Label = "Password", Type = "password" }
                            };
                        }

                        if (definition.Login != null && definition.Login.Method == null)
                        {
                            definition.Login.Method = "form";
                        }

                        return definition;
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error while parsing Cardigann definition {file.FullName}\n{e}");
                    }
                }
            }

            return GetHttpDefinition(fileKey);
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
