using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class Newznab : UsenetIndexerBase<NewznabSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;

        public override string Name => "Newznab";
        public override string[] IndexerUrls => new string[] { "" };
        public override string Description => "Newznab is an API search specification for Usenet";
        public override bool FollowRedirect => true;
        public override bool SupportsRedirect => true;

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            var defFile = _definitionService.GetCachedDefinition(Settings.DefinitionFile);

            return new NewznabRequestGenerator()
            {
                PageSize = PageSize,
                Settings = Settings,
                Definition = defFile
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            var defFile = _definitionService.GetCachedDefinition(Settings.DefinitionFile);
            var capabilities = new IndexerCapabilities();
            capabilities.ParseYmlSearchModes(defFile.Caps.Modes);
            capabilities.SupportsRawSearch = defFile.Caps.Allowrawsearch;
            capabilities.MapYmlCategories(defFile);

            return new GenericNewznabRssParser(capabilities.Categories.GetTorznabCategoryList());
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var def in _definitionService.AllForImplementation(GetType().Name))
                {
                    yield return GetDefinition(def);
                }
            }
        }

        public Newznab(IIndexerDefinitionUpdateService definitionService, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
            _definitionService = definitionService;
        }

        private IndexerDefinition GetDefinition(IndexerMetaDefinition definition)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = definition.Name,
                Language = definition.Language,
                Description = definition.Description,
                Implementation = GetType().Name,
                IndexerUrls = definition.Links.ToArray(),
                LegacyUrls = definition.Legacylinks.ToArray(),
                Settings = new NewznabSettings { DefinitionFile = definition.File },
                Protocol = DownloadProtocol.Usenet,
                Privacy = definition.Type switch
                {
                    "private" => IndexerPrivacy.Private,
                    "public" => IndexerPrivacy.Public,
                    _ => IndexerPrivacy.SemiPrivate
                },
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                Capabilities = new IndexerCapabilities()
            };
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
    }
}
