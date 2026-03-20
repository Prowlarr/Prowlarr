using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NzbDrone.Core.IndexerVersions
{
    public interface IIndexerDefinitionUpdateService
    {
        List<CardigannMetaDefinition> All();
        CardigannDefinition GetCachedDefinition(string fileKey);
        List<string> GetBlocklist();
    }

    public class IndexerDefinitionUpdateService : IIndexerDefinitionUpdateService, IExecute<IndexerDefinitionUpdateCommand>, IHandle<ApplicationStartedEvent>
    {
        /* Update Service will fall back if version # does not exist for an indexer  per Ta */

        private const string DEFINITION_BRANCH = "master";
        private const int DEFINITION_VERSION = 11;
        private const string DANISHBYTES_API_DEFINITION = "danishbytes-api";
        private const string DANISHBYTES_URL = "https://danishbytes.club/";
        private const string NORDICBYTES_URL = "https://nordicbytes.org/";

        // Used when moving yml to C#
        private readonly List<string> _definitionBlocklist = new()
        {
            "aither",
            "animeworld",
            "audiobookbay",
            "beyond-hd-oneurl",
            "beyond-hd",
            "blutopia",
            "brsociety",
            "danishbytes",
            "datascene",
            "desitorrents",
            "hdbits",
            "lat-team",
            "mteamtp",
            "mteamtp2fa",
            "reelflix",
            "shareisland",
            "skipthecommercials",
            "tellytorrent"
        };

        private readonly IHttpClient _httpClient;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IIndexerDefinitionVersionService _versionService;
        private readonly ICached<CardigannDefinition> _cache;
        private readonly Logger _logger;

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public IndexerDefinitionUpdateService(IHttpClient httpClient,
                                          IAppFolderInfo appFolderInfo,
                                          IDiskProvider diskProvider,
                                          IIndexerDefinitionVersionService versionService,
                                          ICacheManager cacheManager,
                                          Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _versionService = versionService;
            _cache = cacheManager.GetCache<CardigannDefinition>(typeof(CardigannDefinition), "definitions");
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<CardigannMetaDefinition> All()
        {
            var indexerList = new List<CardigannMetaDefinition>();

            try
            {
                // Grab latest def list from server or fallback to disk
                try
                {
                    var request = new HttpRequest($"https://indexers.prowlarr.com/{DEFINITION_BRANCH}/{DEFINITION_VERSION}");
                    var response = _httpClient.Get<List<CardigannMetaDefinition>>(request);
                    indexerList = response.Resource.Where(i => !_definitionBlocklist.Contains(i.File)).ToList();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error while getting indexer definitions, fallback to reading from disk.");

                    var definitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions");

                    indexerList = ReadDefinitionsFromDisk(indexerList, definitionFolder);
                }

                //Check for custom definitions
                var customDefinitionFolder = Path.Combine(_appFolderInfo.AppDataFolder, "Definitions", "Custom");

                indexerList = ReadDefinitionsFromDisk(indexerList, customDefinitionFolder);
                indexerList = indexerList.Select(UpdateDanishBytesMetaDefinition).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to Connect to Indexer Definition Server for Indexer listing");
            }

            return indexerList;
        }

        public CardigannDefinition GetCachedDefinition(string fileKey)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
            {
                throw new ArgumentNullException(nameof(fileKey));
            }

            var definition = _cache.Get(fileKey, () => GetUncachedDefinition(fileKey));

            return definition;
        }

        public List<string> GetBlocklist()
        {
            return _definitionBlocklist;
        }

        private List<CardigannMetaDefinition> ReadDefinitionsFromDisk(List<CardigannMetaDefinition> defs, string path, SearchOption options = SearchOption.TopDirectoryOnly)
        {
            var indexerList = defs;

            var directoryInfo = new DirectoryInfo(path);

            if (directoryInfo.Exists)
            {
                var files = directoryInfo.GetFiles("*.yml", options);

                foreach (var file in files)
                {
                    _logger.Debug("Loading definition {0}", file.FullName);

                    try
                    {
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = _deserializer.Deserialize<CardigannMetaDefinition>(definitionString);

                        definition.File = Path.GetFileNameWithoutExtension(file.Name);

                        if (indexerList.Any(i => i.File == definition.File || i.Name == definition.Name))
                        {
                            _logger.Warn("Definition {0} does not have unique file name or Indexer name", file.FullName);
                            continue;
                        }

                        indexerList.Add(definition);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error while parsing Cardigann definition {0}", file.FullName);
                    }
                }
            }

            return indexerList;
        }

        private CardigannDefinition GetUncachedDefinition(string fileKey)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
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
                    _logger.Trace("Loading Cardigann definition {0}", file.FullName);

                    try
                    {
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = _deserializer.Deserialize<CardigannDefinition>(definitionString);

                        return CleanIndexerDefinition(definition);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error while parsing Cardigann definition {0}", file.FullName);
                    }
                }
            }

            var dbDefs = _versionService.All();

            //Check to ensure it's in versioned defs before we go to web
            if (dbDefs.Count > 0 && dbDefs.All(x => x.File != fileKey))
            {
                throw new ArgumentNullException(nameof(fileKey));
            }

            //No definition was returned locally, go to the web
            return GetHttpDefinition(fileKey);
        }

        private CardigannDefinition GetHttpDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            CardigannDefinition definition;

            try
            {
                var request = new HttpRequest($"https://indexers.prowlarr.com/{DEFINITION_BRANCH}/{DEFINITION_VERSION}/{id}");
                var response = _httpClient.Get(request);

                definition = _deserializer.Deserialize<CardigannDefinition>(response.Content);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Indexer definition for '{id}' does not exist.", ex);
            }

            return CleanIndexerDefinition(definition);
        }

        private CardigannDefinition CleanIndexerDefinition(CardigannDefinition definition)
        {
            if (definition.Settings == null)
            {
                definition.Settings = new List<SettingsField>
                {
                    new() { Name = "username", Label = "Username", Type = "text" },
                    new() { Name = "password", Label = "Password", Type = "password" }
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

            if (string.Equals(definition.Id, DANISHBYTES_API_DEFINITION, StringComparison.OrdinalIgnoreCase))
            {
                definition.Links = UpdateDanishBytesLinks(definition.Links);
                UpdateDanishBytesHelpLinks(definition.Settings);
            }

            return definition;
        }

        private CardigannMetaDefinition UpdateDanishBytesMetaDefinition(CardigannMetaDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            if (!string.Equals(definition.Id, DANISHBYTES_API_DEFINITION, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(definition.File, DANISHBYTES_API_DEFINITION, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }

            definition.Links = UpdateDanishBytesLinks(definition.Links);
            UpdateDanishBytesHelpLinks(definition.Settings);

            return definition;
        }

        private List<string> UpdateDanishBytesLinks(List<string> links)
        {
            var updatedLinks = (links ?? new List<string>())
                .Where(link => !string.Equals(link, NORDICBYTES_URL, StringComparison.OrdinalIgnoreCase))
                .ToList();

            updatedLinks.Insert(0, NORDICBYTES_URL);

            return updatedLinks;
        }

        private void UpdateDanishBytesHelpLinks(List<SettingsField> settings)
        {
            if (settings == null)
            {
                return;
            }

            foreach (var setting in settings.Where(s => !string.IsNullOrWhiteSpace(s.Default) && (s.Name == "info_apikey" || s.Name == "info_rsskey")))
            {
                setting.Default = setting.Default.Replace(DANISHBYTES_URL, NORDICBYTES_URL, StringComparison.OrdinalIgnoreCase);
                setting.Default = setting.Default.Replace("DanishBytes", "NordicBytes", StringComparison.OrdinalIgnoreCase);
            }
        }

        public void Handle(ApplicationStartedEvent message)
        {
            // Sync indexers on app start
            UpdateLocalDefinitions();
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
            var startupFolder = _appFolderInfo.AppDataFolder;

            try
            {
                EnsureDefinitionsFolder();

                var definitionsFolder = Path.Combine(startupFolder, "Definitions");
                var saveFile = Path.Combine(definitionsFolder, "indexers.zip");

                _httpClient.DownloadFile($"https://indexers.prowlarr.com/{DEFINITION_BRANCH}/{DEFINITION_VERSION}/package.zip", saveFile);

                using (var archive = ZipFile.OpenRead(saveFile))
                {
                    archive.ExtractToDirectory(definitionsFolder, true);
                }

                _diskProvider.DeleteFile(saveFile);

                _cache.Clear();

                _logger.Debug("Updated indexer definitions");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Definition update failed");
            }
        }
    }
}
