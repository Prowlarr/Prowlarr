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

            var oldIndexers = _indexerFactory.All().Where(i => i.Implementation == "Cardigann" && blocklist.Contains(((CardigannSettings)i.Settings).DefinitionFile)).ToList();

            if (oldIndexers.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                string.Format(_localizationService.GetLocalizedString("IndexerObsoleteCheckMessage"),
                string.Join(", ", oldIndexers.Select(v => v.Name))),
                "#indexers-are-obsolete");
        }

        public override bool CheckOnSchedule => false;
    }
}
