using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
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
            var filteredDefinitions = new List<IndexerDefinition>();

            foreach (var definition in definitions)
            {
                if (definition.Implementation == nameof(Cardigann))
                {
                    try
                    {
                        MapCardigannDefinition(definition);
                    }
                    catch (Exception ex)
                    {
                        // Skip indexer if we fail in Cardigann mapping
                        _logger.Debug(ex, "Indexer '{0}' has no definition", definition.Name);
                    }
                }

                filteredDefinitions.Add(definition);
            }

            return filteredDefinitions;
        }

        public override IndexerDefinition Get(int id)
        {
            var definition = base.Get(id);

            if (definition.Implementation == nameof(Cardigann))
            {
                try
                {
                    MapCardigannDefinition(definition);
                }
                catch
                {
                    throw new ModelNotFoundException(typeof(IndexerDefinition), id);
                }
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
            var defFile = _definitionService.GetCachedDefinition(settings.DefinitionFile);
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

            definition.IndexerUrls = defFile.Links.ToArray();
            definition.LegacyUrls = defFile.Legacylinks.ToArray();
            definition.Description = defFile.Description;
            definition.Language = defFile.Language;
            definition.Encoding = Encoding.GetEncoding(defFile.Encoding);
            definition.Privacy = defFile.Type switch
            {
                "private" => IndexerPrivacy.Private,
                "public" => IndexerPrivacy.Public,
                _ => IndexerPrivacy.SemiPrivate
            };
            definition.Capabilities = new IndexerCapabilities();
            definition.Capabilities.ParseCardigannSearchModes(defFile.Caps.Modes);
            definition.Capabilities.SupportsRawSearch = defFile.Caps.Allowrawsearch;
            MapCardigannCategories(definition, defFile);
        }

        private void MapCardigannCategories(IndexerDefinition def, CardigannDefinition defFile)
        {
            if (defFile.Caps.Categories != null && defFile.Caps.Categories.Any())
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

            if (defFile.Caps.Categorymappings != null && defFile.Caps.Categorymappings.Any())
            {
                foreach (var categoryMapping in defFile.Caps.Categorymappings)
                {
                    IndexerCategory torznabCat = null;

                    if (categoryMapping.Cat != null)
                    {
                        torznabCat = NewznabStandardCategory.GetCatByName(categoryMapping.Cat);

                        if (torznabCat == null)
                        {
                            continue;
                        }
                    }

                    def.Capabilities.Categories.AddCategoryMapping(categoryMapping.Id, torznabCat, categoryMapping.Desc);
                }
            }
        }

        public override IEnumerable<IndexerDefinition> GetDefaultDefinitions()
        {
            foreach (var provider in _providers)
            {
                if (provider.IsObsolete())
                {
                    continue;
                }

                var definitions = provider.DefaultDefinitions
                    .Where(v => v.Name != null && v.Name != nameof(Cardigann) && v.Name != nameof(Newznab.Newznab) && v.Name != nameof(Torznab.Torznab))
                    .Cast<IndexerDefinition>();

                foreach (var definition in definitions)
                {
                    SetProviderCharacteristics(provider, definition);
                    yield return definition;
                }
            }
        }

        public override IEnumerable<IndexerDefinition> GetPresetDefinitions(IndexerDefinition providerDefinition)
        {
            return Array.Empty<IndexerDefinition>();
        }

        public override void SetProviderCharacteristics(IIndexer provider, IndexerDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.Protocol = provider.Protocol;
            definition.SupportsRss = provider.SupportsRss;
            definition.SupportsSearch = provider.SupportsSearch;
            definition.SupportsRedirect = provider.SupportsRedirect;
            definition.SupportsPagination = provider.SupportsPagination;

            //We want to use the definition Caps and Privacy for Cardigann instead of the provider.
            if (definition.Implementation != nameof(Cardigann))
            {
                definition.IndexerUrls = provider.IndexerUrls;
                definition.LegacyUrls = provider.LegacyUrls;
                definition.Privacy = provider.Privacy;
                definition.Description ??= provider.Description;
                definition.Encoding = provider.Encoding;
                definition.Language ??= provider.Language;
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
                if (blockedIndexers.TryGetValue(indexer.Definition.Id, out var blockedIndexerStatus))
                {
                    _logger.Debug("Temporarily ignoring indexer {0} till {1} due to recent failures.", indexer.Definition.Name, blockedIndexerStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return indexer;
            }
        }

        public override ValidationResult Test(IndexerDefinition definition)
        {
            var result = base.Test(definition);

            if (definition.Id == 0)
            {
                return result;
            }

            if (result == null || result.IsValid)
            {
                _indexerStatusService.RecordSuccess(definition.Id);
            }
            else
            {
                _indexerStatusService.RecordFailure(definition.Id);
            }

            return result;
        }

        public override IndexerDefinition Create(IndexerDefinition definition)
        {
            definition.Added = DateTime.UtcNow;

            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            SetProviderCharacteristics(provider, definition);

            if (definition.Enable && definition.Implementation is nameof(Newznab.Newznab) or nameof(Torznab.Torznab))
            {
                var settings = (NewznabSettings)definition.Settings;
                settings.Categories = _newznabCapabilitiesProvider.GetCapabilities(settings, definition)?.Categories.GetTorznabCategoryList();
            }

            if (definition.Implementation == nameof(Cardigann))
            {
                MapCardigannDefinition(definition);
            }

            return base.Create(definition);
        }

        public override void Update(IndexerDefinition definition)
        {
            var provider = _providers.First(v => v.GetType().Name == definition.Implementation);

            SetProviderCharacteristics(provider, definition);

            if (definition.Enable && definition.Implementation is nameof(Newznab.Newznab) or nameof(Torznab.Torznab))
            {
                var settings = (NewznabSettings)definition.Settings;
                settings.Categories = _newznabCapabilitiesProvider.GetCapabilities(settings, definition)?.Categories.GetTorznabCategoryList();
            }

            if (definition.Implementation == nameof(Cardigann))
            {
                MapCardigannDefinition(definition);
            }

            base.Update(definition);
        }
    }
}
