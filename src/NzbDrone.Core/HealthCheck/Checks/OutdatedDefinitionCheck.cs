using System.Linq;
using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderDeletedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderBulkDeletedEvent<IIndexer>))]
    public class OutdatedDefinitionCheck : HealthCheckBase
    {
        private readonly IIndexerDefinitionUpdateService _indexerDefinitionUpdateService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public OutdatedDefinitionCheck(IIndexerDefinitionUpdateService indexerDefinitionUpdateService, IIndexerFactory indexerFactory, ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
            _indexerDefinitionUpdateService = indexerDefinitionUpdateService;
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var blocklist = _indexerDefinitionUpdateService.GetBlocklist();

            var oldIndexers = _indexerFactory.AllProviders(false)
                    .Where(i => i.IsObsolete() || (i.Definition.Implementation == "Cardigann" && blocklist.Contains(((CardigannSettings)i.Definition.Settings).DefinitionFile))).ToList();

            if (oldIndexers.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            var healthType = HealthCheckResult.Warning;
            var healthMessage = string.Format(_localizationService.GetLocalizedString("IndexerObsoleteCheckMessage"),
                string.Join(", ", oldIndexers.Select(v => v.Definition.Name)));

            return new HealthCheck(GetType(),
                healthType,
                healthMessage,
                "#indexers-are-obsolete");
        }

        public override bool CheckOnSchedule => false;
    }
}
