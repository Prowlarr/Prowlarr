using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class Cardigann : HttpIndexerBase<CardigannSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;

        public override string Name => "Cardigann";
        public override string BaseUrl => "";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override int PageSize => 100;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new CardigannRequestGenerator(_configService,
                                                 _definitionService.GetDefinition(Settings.DefinitionFile),
                                                 Settings,
                                                 _logger)
            {
                HttpClient = _httpClient
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_configService,
                                       _definitionService.GetDefinition(Settings.DefinitionFile),
                                       Settings,
                                       _logger);
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
                         IIndexerStatusService indexerStatusService,
                         IConfigService configService,
                         Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
            _definitionService = definitionService;
        }

        private IndexerDefinition GetDefinition(CardigannMetaDefinition definition)
        {
            var defaultSettings = new List<SettingsField>
            {
                new SettingsField { Name = "username", Label = "Username", Type = "text" },
                new SettingsField { Name = "password", Label = "Password", Type = "password" }
            };

            var settings = definition.Settings ?? defaultSettings;

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

            return generator.CheckIfLoginIsNeeded(httpResponse);
        }

        protected override async Task DoLogin()
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            SetCookieFunctions(generator);

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
    }
}
