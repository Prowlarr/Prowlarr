using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class Cardigann : TorrentIndexerBase<CardigannSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly ICached<CardigannRequestGenerator> _generatorCache;

        public override string Name => "Cardigann";
        public override string[] IndexerUrls => new string[] { "" };
        public override string Description => "";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        // Page size is different per indexer, setting to 1 ensures we don't break out of paging logic
        // thinking its a partial page and instead all search_path requests are run for each indexer
        public override int PageSize => 1;

        public override TimeSpan RateLimit
        {
            get
            {
                var definition = _definitionService.GetCachedDefinition(Settings.DefinitionFile);

                if (definition.RequestDelay.HasValue && definition.RequestDelay.Value > base.RateLimit.TotalSeconds)
                {
                    return TimeSpan.FromSeconds(definition.RequestDelay.Value);
                }

                return base.RateLimit;
            }
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            var generator = _generatorCache.Get(Settings.DefinitionFile, () =>
                new CardigannRequestGenerator(_configService,
                    _definitionService.GetCachedDefinition(Settings.DefinitionFile),
                    _logger,
                    RateLimit)
                {
                    HttpClient = _httpClient,
                    Definition = Definition,
                    Settings = Settings
                });

            generator = (CardigannRequestGenerator)SetCookieFunctions(generator);

            generator.Settings = Settings;

            _generatorCache.ClearExpired();

            return generator;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_configService,
                _definitionService.GetCachedDefinition(Settings.DefinitionFile),
                _logger)
            {
                Settings = Settings
            };
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            if (_definitionService.GetCachedDefinition(Settings.DefinitionFile).Search?.Rows?.Filters?.Any(x => x.Name == "andmatch") ?? false)
            {
                cleanReleases = FilterReleasesByQuery(releases, searchCriteria).ToList();
            }

            return cleanReleases;
        }

        protected override IDictionary<string, string> GetCookies()
        {
            if (Settings.ExtraFieldData.TryGetValue("cookie", out var cookies))
            {
                return CookieUtil.CookieHeaderToDictionary((string)cookies);
            }

            return base.GetCookies();
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var def in _definitionService.All())
                {
                    yield return GetDefinition(def);
                }
            }
        }

        public Cardigann(IIndexerDefinitionUpdateService definitionService,
                         IIndexerHttpClient httpClient,
                         IEventAggregator eventAggregator,
                         IIndexerStatusService indexerStatusService,
                         IConfigService configService,
                         ICacheManager cacheManager,
                         Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _definitionService = definitionService;
            _generatorCache = cacheManager.GetRollingCache<CardigannRequestGenerator>(GetType(), "CardigannGeneratorCache", TimeSpan.FromMinutes(5));
        }

        private IndexerDefinition GetDefinition(CardigannMetaDefinition definition)
        {
            var defaultSettings = new List<SettingsField>
            {
                new () { Name = "username", Label = "Username", Type = "text" },
                new () { Name = "password", Label = "Password", Type = "password" }
            };

            var settings = definition.Settings ?? defaultSettings;

            if (definition.Login?.Captcha != null)
            {
                settings.Add(new SettingsField
                {
                    Name = "cardigannCaptcha",
                    Type = "cardigannCaptcha",
                    Label = "CAPTCHA"
                });
            }

            return new IndexerDefinition
            {
                Enable = true,
                Name = definition.Name,
                Language = definition.Language,
                Description = definition.Description,
                Implementation = GetType().Name,
                IndexerUrls = definition.Links.ToArray(),
                LegacyUrls = definition.Legacylinks.ToArray(),
                Settings = new CardigannSettings { DefinitionFile = definition.File },
                Protocol = DownloadProtocol.Torrent,
                Privacy = definition.Type switch
                {
                    "private" => IndexerPrivacy.Private,
                    "public" => IndexerPrivacy.Public,
                    _ => IndexerPrivacy.SemiPrivate
                },
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                SupportsPagination = SupportsPagination,
                Capabilities = ParseCardigannCapabilities(definition),
                ExtraFields = settings
            };
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            SetCookieFunctions(generator);

            generator.Settings = Settings;

            return generator.CheckIfLoginIsNeeded(httpResponse);
        }

        protected override async Task DoLogin()
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            SetCookieFunctions(generator);

            generator.Settings = Settings;

            await generator.DoLogin();
        }

        protected override async Task<HttpRequest> GetDownloadRequest(Uri link)
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            var request = await generator.DownloadRequest(link);

            request.AllowAutoRedirect = true;

            return request;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await base.Test(failures);
            if (failures.HasErrors())
            {
                return;
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "checkCaptcha")
            {
                var generator = (CardigannRequestGenerator)GetRequestGenerator();

                var result = generator.GetConfigurationForSetup(false).GetAwaiter().GetResult();
                return new
                {
                    captchaRequest = result
                };
            }

            if (action == "getUrls")
            {
                var devices = ((IndexerDefinition)Definition).IndexerUrls;

                return new
                {
                    options = devices.Select(d => new { Value = d, Name = d })
                };
            }

            return null;
        }

        private IndexerCapabilities ParseCardigannCapabilities(CardigannMetaDefinition definition)
        {
            var capabilities = new IndexerCapabilities();

            if (definition.Caps == null)
            {
                return capabilities;
            }

            capabilities.ParseCardigannSearchModes(definition.Caps.Modes);
            capabilities.SupportsRawSearch = definition.Caps.Allowrawsearch;

            if (definition.Caps.Categories != null && definition.Caps.Categories.Any())
            {
                foreach (var category in definition.Caps.Categories)
                {
                    var cat = NewznabStandardCategory.GetCatByName(category.Value);

                    if (cat == null)
                    {
                        continue;
                    }

                    capabilities.Categories.AddCategoryMapping(category.Key, cat);
                }
            }

            if (definition.Caps.Categorymappings != null && definition.Caps.Categorymappings.Any())
            {
                foreach (var categoryMapping in definition.Caps.Categorymappings)
                {
                    IndexerCategory torznabCat = null;

                    if (categoryMapping.Cat != null)
                    {
                        torznabCat = NewznabStandardCategory.GetCatByName(categoryMapping.Cat);

                        if (torznabCat == null)
                        {
                            continue;
                        }
                    }

                    capabilities.Categories.AddCategoryMapping(categoryMapping.Id, torznabCat, categoryMapping.Desc);
                }
            }

            return capabilities;
        }
    }
}
