using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerProxies
{
    public interface IIndexerProxyRepository : IProviderRepository<IndexerProxyDefinition>
    {
        void UpdateSettings(IndexerProxyDefinition model);
    }

    public class IndexerProxyRepository : ProviderRepository<IndexerProxyDefinition>, IIndexerProxyRepository
    {
        public IndexerProxyRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void UpdateSettings(IndexerProxyDefinition model)
        {
            SetFields(model, m => m.Settings);
        }
    }
}
