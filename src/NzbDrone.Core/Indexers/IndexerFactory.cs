using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerFactory : IProviderFactory<IIndexer, IndexerDefinition>
    {
        List<IIndexer> Enabled(bool filterBlockedIndexers = true);
        List<IIndexer> AllProviders(bool filterBlockedIndexers = true);
        void DeleteIndexers(List<int> indexerIds);
    }

    public class IndexerFactory : ProviderFactory<IIndexer, IndexerDefinition>, IIndexerFactory
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly INewznabCapabilitiesProvider _newznabCapabilitiesProvider;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly Logger _logger;

        public IndexerFactory(IIndexerDefinitionUpdateService definitionService,
                              INewznabCapabilitiesProvider newznabCapabilitiesProvider,
                              IIndexerStatusService indexerStatusService,
                              IIndexerRepository providerRepository,
                              IEnumerable<IIndexer> providers,
                              IServiceProvider container,
                              IEventAggregator eventAggregator,
                              Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _definitionService = definitionService;
            _indexerStatusService = indexerStatusService;
            _newznabCapabilitiesProvider = newznabCapabilitiesProvider;
            _logger = logger;
        }

        public override List<IndexerDefinition> All()
        {
            var definitions = base.All();

            foreach (var definition in definitions)
            {
                if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
                {
                    MapCardigannDefinition(definition);
                }
            }

            return definitions;
        }

        public override IndexerDefinition Get(int id)
        {
            var definition = base.Get(id);

            if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
            {
                MapCardigannDefinition(definition);
            }

            return definition;
        }

        protected override List<IndexerDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        private void MapCardigannDefinition(IndexerDefinition definition)
        {
            var settings = (CardigannSettings)definition.Settings;
            var defFile = _definitionService.GetDefinition(settings.DefinitionFile);
            definition.ExtraFields = defFile.Settings;

            if (defFile.Login?.Captcha != null && !definition.ExtraFields.Any(x => x.Type == "cardigannCaptcha"))
            {
                definition.ExtraFields.Add(new SettingsField
                {
                    Name = "cardigannCaptcha",
                    Type = "cardigannCaptcha",
                    Label = "CAPTCHA"
                });
            }

            definition.BaseUrl = defFile.Links.First();
            definition.Description = defFile.Description;
            definition.Language = defFile.Language;
            definition.Encoding = Encoding.GetEncoding(defFile.Encoding);
            definition.Privacy = defFile.Type == "private" ? IndexerPrivacy.Private : IndexerPrivacy.Public;
            definition.Capabilities = new IndexerCapabilities();
            definition.Capabilities.ParseCardigannSearchModes(defFile.Caps.Modes);
            MapCardigannCategories(definition, defFile);
        }

        private void MapCardigannCategories(IndexerDefinition def, CardigannDefinition defFile)
        {
            if (defFile.Caps.Categories != null)
            {
                foreach (var category in defFile.Caps.Categories)
                {
                    var cat = NewznabStandardCategory.GetCatByName(category.Value);

                    if (cat == null)
                    {
                        continue;
                    }

                    def.Capabilities.Categories.AddCategoryMapping(category.Key, cat);
                }
            }

            if (defFile.Caps.Categorymappings != null)
            {
                foreach (var categorymapping in defFile.Caps.Categorymappings)
                {
                    IndexerCategory torznabCat = null;

                    if (categorymapping.cat != null)
                    {
                        torznabCat = NewznabStandardCategory.GetCatByName(categorymapping.cat);
                        if (torznabCat == null)
                        {
                            continue;
                        }
                    }

                    def.Capabilities.Categories.AddCategoryMapping(categorymapping.id, torznabCat, categorymapping.desc);

                    //if (categorymapping.Default)
                    //{
                    //    DefaultCategories.Add(categorymapping.id);
                    //}
                }
            }
        }

        public override IEnumerable<IndexerDefinition> GetDefaultDefinitions()
        {
            foreach (var provider in _providers)
            {
                var definitions = provider.DefaultDefinitions
                    .Where(v => v.Name != null && (v.Name != typeof(Cardigann.Cardigann).Name || v.Name != typeof(Newznab.Newznab).Name));

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
            definition.SupportsRss = provider.SupportsRss;
            definition.SupportsSearch = provider.SupportsSearch;
            definition.SupportsRedirect = provider.SupportsRedirect;

            //We want to use the definition Caps and Privacy for Cardigann instead of the provider.
            if (definition.Implementation != typeof(Cardigann.Cardigann).Name)
            {
                definition.BaseUrl = provider.BaseUrl;
                definition.Privacy = provider.Privacy;
                definition.Description = provider.Description;
                definition.Encoding = provider.Encoding;
                definition.Language = provider.Language;
                definition.Capabilities = provider.Capabilities;
            }
        }

        public List<IIndexer> Enabled(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = GetAvailableProviders().Where(n => ((IndexerDefinition)n.Definition).Enable);

            if (filterBlockedIndexers)
            {
                return FilterBlockedIndexers(enabledIndexers).ToList();
            }

            return enabledIndexers.ToList();
        }

        public List<IIndexer> AllProviders(bool filterBlockedIndexers = true)
        {
            var enabledIndexers = All().Select(GetInstance);

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

            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            if (definition.Implementation == typeof(Newznab.Newznab).Name)
            {
                var settings = (NewznabSettings)definition.Settings;
                settings.Categories = _newznabCapabilitiesProvider.GetCapabilities(settings)?.Categories.GetTorznabCategoryList() ?? null;
            }

            if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
            {
                MapCardigannDefinition(definition);
            }

            return base.Create(definition);
        }

        public override void Update(IndexerDefinition definition)
        {
            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            if (definition.Implementation == typeof(Newznab.Newznab).Name)
            {
                var settings = (NewznabSettings)definition.Settings;
                settings.Categories = _newznabCapabilitiesProvider.GetCapabilities(settings)?.Categories.GetTorznabCategoryList() ?? null;
            }

            if (definition.Implementation == typeof(Cardigann.Cardigann).Name)
            {
                MapCardigannDefinition(definition);
            }

            base.Update(definition);
        }
    }
}
