using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Applications.Listenarr
{
    public class Listenarr : ApplicationBase<ListenarrSettings>
    {
        public override string Name => "Listenarr";

        private readonly IListenarrV1Proxy _listenarrV1Proxy;
        private readonly ICached<List<ListenarrIndexer>> _schemaCache;
        private readonly IConfigFileProvider _configFileProvider;

        public Listenarr(ICacheManager cacheManager, IListenarrV1Proxy listenarrV1Proxy, IConfigFileProvider configFileProvider, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
            : base(appIndexerMapService, indexerFactory, logger)
        {
            _schemaCache = cacheManager.GetCache<List<ListenarrIndexer>>(GetType());
            _listenarrV1Proxy = listenarrV1Proxy;
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

            // Call the application's TestConnection directly, consistent with other application implementations.
            // Some applications do not expose a reliable system status endpoint, so we rely on the indexer test itself
            // to provide actionable feedback (auth, version, connectivity etc.).
            try
            {
                failures.AddIfNotNull(_listenarrV1Proxy.TestConnection(BuildListenarrIndexer(testIndexer, testIndexer.Capabilities, DownloadProtocol.Usenet), Settings));
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ApiKey", "API Key is invalid"));
                        break;
                    case HttpStatusCode.BadRequest:
                        _logger.Warn(ex, "Prowlarr URL is invalid");
                        failures.AddIfNotNull(new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, Listenarr cannot connect to Prowlarr"));
                        break;
                    case HttpStatusCode.SeeOther:
                    case HttpStatusCode.TemporaryRedirect:
                        _logger.Warn(ex, "Listenarr returned redirect and is invalid");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", "Listenarr URL is invalid, Prowlarr cannot connect to Listenarr - are you missing a URL base?"));
                        break;
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Listenarr. {ex.Message}"));
                        break;
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.Error(ex, "Unable to parse JSON response from application");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to parse JSON response from application. {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                failures.AddIfNotNull(new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to Listenarr. {ex.Message}"));
            }

            return new ValidationResult(failures);
        }

        public override List<AppIndexerMap> GetIndexerMappings()
        {
            var indexers = (_listenarrV1Proxy.GetIndexers(Settings) ?? new List<ListenarrIndexer>())
                .Where(i => i.Implementation is "Newznab" or "Torznab");

            var mappings = new List<AppIndexerMap>();

            foreach (var indexer in indexers)
            {
                var baseUrl = (string)indexer.Fields?.FirstOrDefault(x => x.Name == "baseUrl")?.Value ?? string.Empty;

                if (!baseUrl.StartsWith(Settings.ProwlarrUrl.TrimEnd('/')) &&
                    (string)indexer.Fields?.FirstOrDefault(x => x.Name == "apiKey")?.Value != _configFileProvider.ApiKey)
                {
                    continue;
                }

                var match = AppIndexerRegex.Match(baseUrl);

                if (match.Groups["indexer"].Success && int.TryParse(match.Groups["indexer"].Value, out var indexerId))
                {
                    // Add parsed mapping if it's mapped to a Indexer in this Prowlarr instance
                    mappings.Add(new AppIndexerMap { IndexerId = indexerId, RemoteIndexerId = indexer.Id });
                }
            }

            return mappings;
        }

        public override void AddIndexer(IndexerDefinition indexer)
        {
            var indexerCapabilities = GetIndexerCapabilities(indexer);

            if (!indexerCapabilities.MusicSearchAvailable && !indexerCapabilities.SearchAvailable)
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to missing music or basic search support by the indexer", indexer.Name, indexer.Id);

                return;
            }

            if (indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Empty())
            {
                _logger.Debug("Skipping add for indexer {0} [{1}] due to no app Sync Categories supported by the indexer", indexer.Name, indexer.Id);

                return;
            }

            _logger.Trace("Adding indexer {0} [{1}]", indexer.Name, indexer.Id);

            var listenarrIndexer = BuildListenarrIndexer(indexer, indexerCapabilities, indexer.Protocol);

            // If an existing remote indexer already points to this app indexer (matching baseUrl or name), insert mapping and skip adding
            try
            {
                var remoteIndexers = _listenarrV1Proxy.GetIndexers(Settings);
                var baseUrl = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";

                var match = remoteIndexers.FirstOrDefault(r =>
                    ((string)r.Fields.FirstOrDefault(f => f.Name == "baseUrl")?.Value ?? "").Equals(baseUrl, StringComparison.InvariantCultureIgnoreCase)
                    || (r.Name?.Contains(indexer.Name, StringComparison.InvariantCultureIgnoreCase) == true));

                if (match != null)
                {
                    _logger.Debug("Found existing remote indexer for {0} as {1} [{2}], inserting mapping", indexer.Name, match.Name, match.Id);
                    _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = match.Id });
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error while attempting to discover existing remote indexer for {0} [{1}]", indexer.Name, indexer.Id);
            }

            var remoteIndexer = _listenarrV1Proxy.AddIndexer(listenarrIndexer, Settings);

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
                //Remove Indexer remotely and then remove the mapping
                _listenarrV1Proxy.RemoveIndexer(indexerMapping.RemoteIndexerId, Settings);
                _appIndexerMapService.Delete(indexerMapping.Id);
            }
        }

        public override void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false)
        {
            _logger.Debug("Updating indexer {0} [{1}]", indexer.Name, indexer.Id);

            var indexerCapabilities = GetIndexerCapabilities(indexer);
            var appMappings = _appIndexerMapService.GetMappingsForApp(Definition.Id);
            var indexerMapping = appMappings.FirstOrDefault(m => m.IndexerId == indexer.Id);

            // If there is no mapping, treat this as an add instead of an update
            if (indexerMapping == null)
            {
                if ((indexerCapabilities.MusicSearchAvailable || indexerCapabilities.SearchAvailable) &&
                    indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("No mapping found for {0} [{1}], adding to Listenarr", indexer.Name, indexer.Id);

                    var listenarrIndexerToAdd = BuildListenarrIndexer(indexer, indexerCapabilities, indexer.Protocol);
                    listenarrIndexerToAdd.Id = 0;
                    ListenarrIndexer newRemoteIndexer = null;

                    try
                    {
                        newRemoteIndexer = _listenarrV1Proxy.AddIndexer(listenarrIndexerToAdd, Settings);
                    }
                    catch (HttpException ex)
                    {
                        _logger.Warn(ex, "Failed to add indexer {0} [{1}] to Listenarr: {2}", indexer.Name, indexer.Id, ex.Response?.StatusCode);
                    }

                    if (newRemoteIndexer != null)
                    {
                        _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                    }
                    else
                    {
                        // If add returned null or failed, try to discover existing remote indexer by baseUrl or name
                        try
                        {
                            var remoteIndexers = _listenarrV1Proxy.GetIndexers(Settings);
                            var baseUrl = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";

                            var match = remoteIndexers.FirstOrDefault(r =>
                                ((string)r.Fields.FirstOrDefault(f => f.Name == "baseUrl")?.Value ?? "").Equals(baseUrl, StringComparison.InvariantCultureIgnoreCase)
                                || (r.Name?.Contains(indexer.Name, StringComparison.InvariantCultureIgnoreCase) == true));

                            if (match != null)
                            {
                                _logger.Debug("Found existing remote indexer for {0} as {1} [{2}], inserting mapping", indexer.Name, match.Name, match.Id);
                                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = match.Id });
                            }
                            else
                            {
                                _logger.Debug("No remote indexer found for {0} after failing to add; skipping mapping", indexer.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Error while attempting to discover existing remote indexer for {0} [{1}]", indexer.Name, indexer.Id);
                        }
                    }
                }
                else
                {
                    _logger.Debug("No mapping found for {0} [{1}], skipping add due to indexer capabilities", indexer.Name, indexer.Id);
                }

                return;
            }

            // If mapping exists but contains an invalid remote id (0), remove and re-add if possible
            if (indexerMapping.RemoteIndexerId == 0)
            {
                _logger.Warn("Mapping for indexer {0} contains invalid remote id 0, removing mapping and re-adding if possible", indexer.Id);
                _appIndexerMapService.Delete(indexerMapping.Id);

                if ((indexerCapabilities.MusicSearchAvailable || indexerCapabilities.SearchAvailable) &&
                    indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    var listenarrIndexerToAdd = BuildListenarrIndexer(indexer, indexerCapabilities, indexer.Protocol);
                    listenarrIndexerToAdd.Id = 0;
                    ListenarrIndexer newRemoteIndexer = null;

                    try
                    {
                        newRemoteIndexer = _listenarrV1Proxy.AddIndexer(listenarrIndexerToAdd, Settings);
                    }
                    catch (HttpException ex)
                    {
                        _logger.Warn(ex, "Failed to add indexer {0} [{1}] to Listenarr: {2}", indexer.Name, indexer.Id, ex.Response?.StatusCode);
                    }

                    if (newRemoteIndexer != null)
                    {
                        _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                    }
                    else
                    {
                        try
                        {
                            var remoteIndexers = _listenarrV1Proxy.GetIndexers(Settings);
                            var baseUrl = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";

                            var match = remoteIndexers.FirstOrDefault(r =>
                                ((string)r.Fields.FirstOrDefault(f => f.Name == "baseUrl")?.Value ?? "").Equals(baseUrl, StringComparison.InvariantCultureIgnoreCase)
                                || (r.Name?.Contains(indexer.Name, StringComparison.InvariantCultureIgnoreCase) == true));

                            if (match != null)
                            {
                                _logger.Debug("Found existing remote indexer for {0} as {1} [{2}], inserting mapping", indexer.Name, match.Name, match.Id);
                                _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = match.Id });
                            }
                            else
                            {
                                _logger.Debug("No remote indexer found for {0} after failing to add; skipping mapping", indexer.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Error while attempting to discover existing remote indexer for {0} [{1}]", indexer.Name, indexer.Id);
                        }
                    }
                }
                else
                {
                    _logger.Debug("Skipping re-add due to indexer capabilities for {0} [{1}]", indexer.Name, indexer.Id);
                }

                return;
            }

            var listenarrIndexer = BuildListenarrIndexer(indexer, indexerCapabilities, indexer.Protocol, indexerMapping.RemoteIndexerId);

            var remoteIndexer = _listenarrV1Proxy.GetIndexer(indexerMapping.RemoteIndexerId, Settings);

            if (remoteIndexer != null)
            {
                _logger.Debug("Remote indexer {0} [{1}] found", remoteIndexer.Name, remoteIndexer.Id);

                if (!listenarrIndexer.Equals(remoteIndexer) || forceSync)
                {
                    _logger.Debug("Syncing remote indexer with current settings");

                    if ((indexerCapabilities.MusicSearchAvailable || indexerCapabilities.SearchAvailable) &&
                        indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                    {
                        // Retain user fields not-affiliated with Prowlarr
                        if (remoteIndexer.Fields != null)
                        {
                            listenarrIndexer.Fields.AddRange(remoteIndexer.Fields.Where(f => listenarrIndexer.Fields.All(s => s.Name != f.Name)));
                        }

                        // Retain user tags not-affiliated with Prowlarr
                        if (remoteIndexer.Tags != null)
                        {
                            listenarrIndexer.Tags.UnionWith(remoteIndexer.Tags);
                        }

                        // Retain user settings not-affiliated with Prowlarr
                        listenarrIndexer.DownloadClientId = remoteIndexer.DownloadClientId;

                        // Ensure ID is in sync with remote before updating
                        listenarrIndexer.Id = remoteIndexer.Id;

                        // Update the indexer if it still has categories that match
                        _listenarrV1Proxy.UpdateIndexer(listenarrIndexer, Settings);
                    }
                    else
                    {
                        // Else remove it, it no longer should be used
                        _listenarrV1Proxy.RemoveIndexer(remoteIndexer.Id, Settings);
                        _appIndexerMapService.Delete(indexerMapping.Id);
                    }
                }
            }
            else
            {
                _appIndexerMapService.Delete(indexerMapping.Id);

                if ((indexerCapabilities.MusicSearchAvailable || indexerCapabilities.SearchAvailable) &&
                    indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()).Any())
                {
                    _logger.Debug("Remote indexer not found, re-adding {0} [{1}] to Listenarr", indexer.Name, indexer.Id);
                    listenarrIndexer.Id = 0;
                    var newRemoteIndexer = _listenarrV1Proxy.AddIndexer(listenarrIndexer, Settings);

                    if (newRemoteIndexer != null)
                    {
                        _appIndexerMapService.Insert(new AppIndexerMap { AppId = Definition.Id, IndexerId = indexer.Id, RemoteIndexerId = newRemoteIndexer.Id });
                    }
                }
            }
        }

        private ListenarrIndexer BuildListenarrIndexer(IndexerDefinition indexer, IndexerCapabilities indexerCapabilities, DownloadProtocol protocol, int id = 0)
        {
            var cacheKey = $"{Settings.BaseUrl}";
            var schemas = _schemaCache.Get(cacheKey, () => _listenarrV1Proxy.GetIndexerSchema(Settings), TimeSpan.FromDays(7));
            var syncFields = new List<string> { "baseUrl", "apiPath", "apiKey", "categories", "minimumSeeders", "seedCriteria.seedRatio", "seedCriteria.seedTime", "seedCriteria.discographySeedTime", "rejectBlocklistedTorrentHashesWhileGrabbing" };

            // Validate schema presence and contents. Listenarr can sometimes return no schema or an unexpected shape
            if (schemas == null || !schemas.Any())
            {
                // Try refreshing schemas directly from proxy in case the cache is empty or stale
                try
                {
                    schemas = _listenarrV1Proxy.GetIndexerSchema(Settings);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to fetch indexer schemas from Listenarr at {0}", Settings.BaseUrl);
                    schemas = null;
                }
            }

            if (schemas == null || !schemas.Any())
            {
                _logger.Warn("No indexer schemas were returned from Listenarr at {0}", Settings.BaseUrl);
                throw new ApplicationException("Listenarr returned no indexer schemas. Ensure Listenarr exposes '/api/v1/indexer/schema' and that it returns a JSON array of schemas containing 'Newznab' or 'Torznab'.");
            }

            if (id == 0)
            {
                // Ensuring backward compatibility with older versions on first sync
                syncFields.AddRange(new List<string> { "additionalParameters" });
            }

            var newznab = schemas.FirstOrDefault(i => string.Equals(i.Implementation, "Newznab", StringComparison.InvariantCultureIgnoreCase) || (i.Implementations != null && i.Implementations.Any(s => string.Equals(s, "Newznab", StringComparison.InvariantCultureIgnoreCase))));
            var torznab = schemas.FirstOrDefault(i => string.Equals(i.Implementation, "Torznab", StringComparison.InvariantCultureIgnoreCase) || (i.Implementations != null && i.Implementations.Any(s => string.Equals(s, "Torznab", StringComparison.InvariantCultureIgnoreCase))));

            if (newznab == null && torznab == null)
            {
                _logger.Warn("Indexer schemas are missing 'Newznab' and 'Torznab' implementations from Listenarr at {0}", Settings.BaseUrl);
                throw new ApplicationException("Listenarr indexer schema must include at least one of 'Newznab' or 'Torznab' implementations.");
            }

            var schema = protocol == DownloadProtocol.Usenet ? newznab ?? torznab : torznab ?? newznab;

            if (schema == null)
            {
                _logger.Warn("No schema available for protocol {0} from Listenarr at {1}", protocol, Settings.BaseUrl);
                throw new ApplicationException($"Listenarr indexer schema does not contain a suitable implementation for protocol {protocol}.");
            }

            var listenarrIndexer = new ListenarrIndexer
            {
                Id = id,
                Name = $"{indexer.Name} (Prowlarr)",
                EnableRss = indexer.Enable && (indexer.AppProfile?.Value?.EnableRss ?? false),
                EnableAutomaticSearch = indexer.Enable && (indexer.AppProfile?.Value?.EnableAutomaticSearch ?? false),
                EnableInteractiveSearch = indexer.Enable && (indexer.AppProfile?.Value?.EnableInteractiveSearch ?? false),
                Priority = indexer.Priority,
                Implementation = indexer.Protocol == DownloadProtocol.Usenet ? "Newznab" : "Torznab",
                ConfigContract = schema.ConfigContract,
                Fields = new List<ListenarrField>(),
                Tags = new HashSet<int>()
            };

            listenarrIndexer.Fields.AddRange(schema.Fields.Where(x => syncFields.Contains(x.Name)));

            // Validate required fields exist to avoid NullReferenceExceptions when accessing their values
            var requiredFieldNames = new List<string> { "baseUrl", "apiPath", "apiKey", "categories" };
            var missing = requiredFieldNames.Where(f => listenarrIndexer.Fields.All(x => x.Name != f)).ToList();

            if (missing.Any())
            {
                _logger.Debug("Cached schema is missing required fields [{0}]. Attempting to refresh schema from proxy", string.Join(", ", missing));

                // Try a single direct refresh from the proxy in case cache is stale
                try
                {
                    var freshSchemas = _listenarrV1Proxy.GetIndexerSchema(Settings);

                    if (freshSchemas != null && freshSchemas.Any())
                    {
                        var freshNewznab = freshSchemas.FirstOrDefault(i => string.Equals(i.Implementation, "Newznab", StringComparison.InvariantCultureIgnoreCase) || (i.Implementations != null && i.Implementations.Any(s => string.Equals(s, "Newznab", StringComparison.InvariantCultureIgnoreCase))));
                        var freshTorznab = freshSchemas.FirstOrDefault(i => string.Equals(i.Implementation, "Torznab", StringComparison.InvariantCultureIgnoreCase) || (i.Implementations != null && i.Implementations.Any(s => string.Equals(s, "Torznab", StringComparison.InvariantCultureIgnoreCase))));

                        var freshSchema = protocol == DownloadProtocol.Usenet ? freshNewznab ?? freshTorznab : freshTorznab ?? freshNewznab;

                        if (freshSchema != null)
                        {
                            listenarrIndexer.Fields = freshSchema.Fields.Where(x => syncFields.Contains(x.Name)).ToList();
                            missing = requiredFieldNames.Where(f => listenarrIndexer.Fields.All(x => x.Name != f)).ToList();

                            if (!missing.Any())
                            {
                                _logger.Debug("Fresh schema contained required fields; proceeding with fresh schema");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to refresh indexer schemas from Listenarr at {0}", Settings.BaseUrl);
                }
            }

            if (missing.Any())
            {
                _logger.Warn("Indexer schema missing required fields [{0}] from Listenarr at {1}", string.Join(", ", missing), Settings.BaseUrl);
                throw new ApplicationException($"Listenarr indexer schema missing required fields: {string.Join(", ", missing)}. Ensure '/api/v1/indexer/schema' includes these fields.");
            }

            var field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "baseUrl");
            if (field != null)
            {
                field.Value = $"{Settings.ProwlarrUrl.TrimEnd('/')}/{indexer.Id}/";
            }

            field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiPath");
            if (field != null)
            {
                field.Value = "/api";
            }

            field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "apiKey");
            if (field != null)
            {
                field.Value = _configFileProvider.ApiKey;
            }

            field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "categories");
            if (field != null)
            {
                field.Value = JArray.FromObject(indexerCapabilities.Categories.SupportedCategories(Settings.SyncCategories.ToArray()));
            }

            if (indexer.Protocol == DownloadProtocol.Torrent)
            {
                var torrentSettings = indexer.Settings as ITorrentIndexerSettings;

                var appMinimumSeeders = torrentSettings?.TorrentBaseSettings.AppMinimumSeeders ?? indexer.AppProfile?.Value?.MinimumSeeders ?? 0;

                field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "minimumSeeders");
                if (field != null)
                {
                    field.Value = appMinimumSeeders;
                }

                field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio");
                if (field != null)
                {
                    field.Value = torrentSettings?.TorrentBaseSettings.SeedRatio;
                }

                field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime");
                if (field != null)
                {
                    field.Value = torrentSettings?.TorrentBaseSettings.SeedTime;
                }

                if (listenarrIndexer.Fields.Any(x => x.Name == "seedCriteria.discographySeedTime"))
                {
                    field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "seedCriteria.discographySeedTime");
                    if (field != null)
                    {
                        field.Value = torrentSettings?.TorrentBaseSettings.PackSeedTime ?? torrentSettings?.TorrentBaseSettings.SeedTime;
                    }
                }

                if (listenarrIndexer.Fields.Any(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing"))
                {
                    field = listenarrIndexer.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing");
                    if (field != null)
                    {
                        field.Value = Settings.SyncRejectBlocklistedTorrentHashesWhileGrabbing;
                    }
                }
            }

            return listenarrIndexer;
        }
    }
}
