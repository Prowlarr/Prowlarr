using NzbDrone.Core.IndexerProxies;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.IndexerProxies
{
    [V1ApiController]
    public class IndexerProxyController : ProviderControllerBase<IndexerProxyResource, IIndexerProxy, IndexerProxyDefinition>
    {
        public static readonly IndexerProxyResourceMapper ResourceMapper = new IndexerProxyResourceMapper();

        public IndexerProxyController(IndexerProxyFactory notificationFactory)
            : base(notificationFactory, "indexerProxy", ResourceMapper)
        {
        }
    }
}
