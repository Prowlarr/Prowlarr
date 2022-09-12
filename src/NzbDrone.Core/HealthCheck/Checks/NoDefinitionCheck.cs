using System.Linq;
using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderDeletedEvent<IIndexer>))]
    public class NoDefinitionCheck : HealthCheckBase
    {
        private readonly IIndexerDefinitionUpdateService _indexerDefinitionUpdateService;
        private readonly IIndexerFactory _indexerFactory;

        public NoDefinitionCheck(IIndexerDefinitionUpdateService indexerDefinitionUpdateService, IIndexerFactory indexerFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            _indexerDefinitionUpdateService = indexerDefinitionUpdateService;
            _indexerFactory = indexerFactory;
        }

        public override HealthCheck Check()
        {
            var currentDefs = _indexerDefinitionUpdateService.All();

            var noDefIndexers = _indexerFactory.AllProviders(false)
                    .Where(i => i.Definition.Implementation == "Cardigann" && !currentDefs.Any(d => d.File == ((IndexerDefinition)i.Definition).DefinitionFile)).ToList();

            if (noDefIndexers.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            var healthType = HealthCheckResult.Error;
            var healthMessage = string.Format(_localizationService.GetLocalizedString("IndexerNoDefCheckMessage"),
                string.Join(", ", noDefIndexers.Select(v => v.Definition.Name)));

            return new HealthCheck(GetType(),
                healthType,
                healthMessage,
                "#indexers-have-no-definition");
        }

        public override bool CheckOnSchedule => false;
    }
}
