using Nancy;
using NzbDrone.Core.Indexers;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerModule : ProviderModuleBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        public static readonly IndexerResourceMapper ResourceMapper = new IndexerResourceMapper();

        private IIndexerFactory _indexerFactory { get; set; }

        public IndexerModule(IndexerFactory indexerFactory)
            : base(indexerFactory, "indexer", ResourceMapper)
        {
            _indexerFactory = indexerFactory;

            Get("{id}/newznab", x => GetNewznabResponse(x.id));
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }

            base.Validate(definition, includeWarnings);
        }

        private object GetNewznabResponse(int id)
        {
            var requestType = Request.Query.t;

            if (!requestType.HasValue)
            {
                throw new BadRequestException("Missing Function Parameter");
            }

            if (requestType.Value == "caps")
            {
                var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(id));
                Response response = indexer.Capabilities.ToXml();
                response.ContentType = "application/rss+xml";
                return response;
            }
            else
            {
                throw new BadRequestException("Function Not Available");
            }
        }
    }
}
