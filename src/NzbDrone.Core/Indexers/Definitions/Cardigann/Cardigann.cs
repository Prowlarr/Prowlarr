using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class Cardigann : HttpIndexerBase<CardigannSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly ICached<CardigannRequestGenerator> _generatorCache;

        public override string Name => "Cardigann";
        public override string BaseUrl => "";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override int PageSize => 100;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return _generatorCache.Get(Settings.DefinitionFile, () =>
                new CardigannRequestGenerator(_configService,
                    _definitionService.GetDefinition(Settings.DefinitionFile),
                    _logger)
                {
                    HttpClient = _httpClient,
                    Settings = Settings
                });
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_configService,
                _definitionService.GetDefinition(Settings.DefinitionFile),
                _logger)
            {
                Settings = Settings
            };
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
                         IHttpClient httpClient,
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
                new SettingsField { Name = "username", Label = "Username", Type = "text" },
                new SettingsField { Name = "password", Label = "Password", Type = "password" }
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
                Implementation = GetType().Name,
                Settings = new CardigannSettings { DefinitionFile = definition.File },
                Protocol = DownloadProtocol.Torrent,
                Privacy = definition.Type == "private" ? IndexerPrivacy.Private : IndexerPrivacy.Public,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                Capabilities = new IndexerCapabilities(),
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

            return null;
        }
    }
}
