using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public abstract class ApplicationBase<TSettings> : IApplication
        where TSettings : IProviderConfig, new()
    {
        private readonly IIndexerFactory _indexerFactory;

        protected readonly IAppIndexerMapService _appIndexerMapService;
        protected readonly Logger _logger;

        protected static readonly Regex AppIndexerRegex = new (@"\/(?<indexer>\d{1,3})(?:\/(?:api)?\/?)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        protected TSettings Settings => (TSettings)Definition.Settings;

        public ApplicationBase(IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
        {
            _appIndexerMapService = appIndexerMapService;
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                yield return new ApplicationDefinition
                {
                    SyncLevel = ApplicationSyncLevel.FullSync,
                    Implementation = GetType().Name,
                    Settings = config
                };
            }
        }

        public abstract void AddIndexer(IndexerDefinition indexer);
        public abstract void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false);
        public abstract void RemoveIndexer(int indexerId);
        public abstract List<AppIndexerMap> GetIndexerMappings();

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        protected IndexerCapabilities GetIndexerCapabilities(IndexerDefinition indexer)
        {
            try
            {
                return _indexerFactory.GetInstance(indexer).GetCapabilities();
            }
            catch (Exception)
            {
                return indexer.Capabilities;
            }
        }
    }
}
