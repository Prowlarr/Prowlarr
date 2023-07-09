using NzbDrone.Core.IndexerProxies;

namespace Prowlarr.Api.V1.IndexerProxies
{
    public class IndexerProxyBulkResource : ProviderBulkResource<IndexerProxyBulkResource>
    {
    }

    public class IndexerProxyBulkResourceMapper : ProviderBulkResourceMapper<IndexerProxyBulkResource, IndexerProxyDefinition>
    {
    }
}
