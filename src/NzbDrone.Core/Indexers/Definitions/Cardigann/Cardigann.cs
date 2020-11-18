using System.Collections.Generic;
using System.Linq;
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

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override int PageSize => 100;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new CardigannRequestGenerator(_definitionService.GetDefinition(Settings.DefinitionFile),
                                                 Settings,
                                                 _logger);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_definitionService.GetDefinition(Settings.DefinitionFile),
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
            return new IndexerDefinition
            {
                EnableRss = false,
                EnableAutomaticSearch = false,
                EnableInteractiveSearch = false,
                Name = definition.Name,
                Implementation = GetType().Name,
                Settings = new CardigannSettings { DefinitionFile = definition.File },
                Protocol = DownloadProtocol.Torrent,
                Privacy = definition.Type == "private" ? IndexerPrivacy.Private : IndexerPrivacy.Public,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                Capabilities = new IndexerCapabilities(),
                ExtraFields = definition.Settings
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            base.Test(failures);
            if (failures.HasErrors())
            {
                return;
            }
        }
    }
}
