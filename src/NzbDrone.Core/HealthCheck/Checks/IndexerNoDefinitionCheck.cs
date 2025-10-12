using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderDeletedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderBulkDeletedEvent<IIndexer>))]
    public class IndexerNoDefinitionCheck : HealthCheckBase
    {
        private readonly IIndexerDefinitionUpdateService _indexerDefinitionUpdateService;
        private readonly IIndexerFactory _indexerFactory;

        public IndexerNoDefinitionCheck(IIndexerDefinitionUpdateService indexerDefinitionUpdateService, IIndexerFactory indexerFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            _indexerDefinitionUpdateService = indexerDefinitionUpdateService;
            _indexerFactory = indexerFactory;
        }

        public override HealthCheck Check()
        {
            var currentDefinitions = _indexerDefinitionUpdateService.All();
            var noDefinitionIndexers = _indexerFactory.AllProviders(false)
                    .Where(i => i.Definition.Implementation == "Cardigann" && currentDefinitions.All(d => d.File != ((CardigannSettings)i.Definition.Settings).DefinitionFile))
                    .ToList();

            if (noDefinitionIndexers.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Error,
                HealthCheckReason.IndexerNoDefinition,
                _localizationService.GetLocalizedString("IndexerNoDefinitionCheckHealthCheckMessage", new Dictionary<string, object>
                {
                    { "indexerNames", string.Join(", ", noDefinitionIndexers.Select(v => v.Definition.Name).ToArray()) }
                }),
                "#indexers-have-no-definition");
        }

        public override bool CheckOnSchedule => false;
    }
}
