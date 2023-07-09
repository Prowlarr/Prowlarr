using NzbDrone.Core.Indexers;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerController : ProviderControllerBase<IndexerResource, IndexerBulkResource, IIndexer, IndexerDefinition>
    {
        public IndexerController(IndexerFactory indexerFactory, IndexerResourceMapper resourceMapper, IndexerBulkResourceMapper bulkResourceMapper)
            : base(indexerFactory, "indexer", resourceMapper, bulkResourceMapper)
        {
        }
    }
}
