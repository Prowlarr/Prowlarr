using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerProxies
{
    public interface IIndexerProxyFactory : IProviderFactory<IIndexerProxy, IndexerProxyDefinition>
    {
    }

    public class IndexerProxyFactory : ProviderFactory<IIndexerProxy, IndexerProxyDefinition>, IIndexerProxyFactory
    {
        public IndexerProxyFactory(IIndexerProxyRepository providerRepository, IEnumerable<IIndexerProxy> providers, IServiceProvider container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
        }

        protected override List<IndexerProxyDefinition> Active()
        {
            return All().Where(c => c.Enable && c.Settings.Validate().IsValid).ToList();
        }
    }
}
