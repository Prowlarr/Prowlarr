using NzbDrone.Core.Indexers;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerController : ProviderControllerBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        public IndexerController(IndexerFactory indexerFactory, IndexerResourceMapper resourceMapper)
            : base(indexerFactory, "indexer", resourceMapper)
        {
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }

            base.Validate(definition, includeWarnings);
        }
    }
}
