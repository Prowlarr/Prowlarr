using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerFactory : IProviderFactory<IIndexer, IndexerDefinition>
    {
        List<IIndexer> RssEnabled(bool filterBlockedIndexers = true);
        List<IIndexer> AutomaticSearchEnabled(bool filterBlockedIndexers = true);
        List<IIndexer> InteractiveSearchEnabled(bool filterBlockedIndexers = true);
        void DeleteIndexers(List<int> indexerIds);
    }

    public class IndexerFactory : ProviderFactory<IIndexer, IndexerDefinition>, IIndexerFactory
    {
        private readonly ICardigannDefinitionService _definitionService;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly Logger _logger;

        public IndexerFactory(ICardigannDefinitionService definitionService,
                              IIndexerStatusService indexerStatusService,
                              IIndexerRepository providerRepository,
                              IEnumerable<IIndexer> providers,
                              IContainer container,
                              IEventAggregator eventAggregator,
                              Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _definitionService = definitionService;
            _indexerStatusService = indexerStatusService;
            _logger = logger;
        }

        public override List<IndexerDefinition> All()
        {
            var definitions = base.All();
            var metaDefs = _definitionService.All().ToDictionary(x => x.File);

            foreach (var definition in definitions)
            {
                if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
                {
                    var settings = (CardigannSettings)definition.Settings;
                    definition.ExtraFields = metaDefs[settings.DefinitionFile].Settings;
                }
            }

            return definitions;
        }

        public override IndexerDefinition Get(int id)
        {
            var definition = base.Get(id);
            var metaDefs = _definitionService.All().ToDictionary(x => x.File);

            if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
            {
                var settings = (CardigannSettings)definition.Settings;
                definition.ExtraFields = metaDefs[settings.DefinitionFile].Settings;
            }

            return definition;
        }

        protected override List<IndexerDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        public override IEnumerable<IndexerDefinition> GetDefaultDefinitions()
        {
            foreach (var provider in _providers)
            {
                var definitions = provider.DefaultDefinitions
                    .Where(v => v.Name != null && v.Name != provider.GetType().Name);

                foreach (IndexerDefinition definition in definitions)
                {
                    SetProviderCharacteristics(provider, definition);
                    yield return definition;
                }
            }
        }

        public override IEnumerable<IndexerDefinition> GetPresetDefinitions(IndexerDefinition providerDefinition)
        {
            return new List<IndexerDefinition>();
        }

        public override void SetProviderCharacteristics(IIndexer provider, IndexerDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.Protocol = provider.Protocol;
            definition.Privacy = provider.Privacy;
            definition.SupportsRss = provider.SupportsRss;
            definition.SupportsSearch = provider.SupportsSearch;
            definition.Capabilities = provider.Capabilities;
        }

        public List<IIndexer> RssEnabled(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = GetAvailableProviders().Where(n => ((IndexerDefinition)n.Definition).EnableRss);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        public List<IIndexer> AutomaticSearchEnabled(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = GetAvailableProviders().Where(n => ((IndexerDefinition)n.Definition).EnableAutomaticSearch);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        public List<IIndexer> InteractiveSearchEnabled(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = GetAvailableProviders().Where(n => ((IndexerDefinition)n.Definition).EnableInteractiveSearch);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        private IEnumerable<IIndexer> FilterBlockedIndexers(IEnumerable<IIndexer> indexers)
        {
            var blockedIndexers = _indexerStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var indexer in indexers)
            {
                IndexerStatus blockedIndexerStatus;
                if (blockedIndexers.TryGetValue(indexer.Definition.Id, out blockedIndexerStatus))
                {
                    _logger.Debug("Temporarily ignoring indexer {0} till {1} due to recent failures.", indexer.Definition.Name, blockedIndexerStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return indexer;
            }
        }

        public void DeleteIndexers(List<int> indexerIds)
        {
            var indexersToDelete = _providerRepository.Get(indexerIds).ToList();

            _providerRepository.DeleteMany(indexerIds);

            foreach (var indexer in indexersToDelete)
            {
                _logger.Info("Deleted indexer {0}", indexer.Name);
            }
        }

        public override ValidationResult Test(IndexerDefinition definition)
        {
            var result = base.Test(definition);

            if ((result == null || result.IsValid) && definition.Id != 0)
            {
                _indexerStatusService.RecordSuccess(definition.Id);
            }

            return result;
        }

        public override IndexerDefinition Create(IndexerDefinition definition)
        {
            definition.Added = DateTime.UtcNow;

            return base.Create(definition);
        }
    }
}
