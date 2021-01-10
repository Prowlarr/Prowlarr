using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerModule : ProviderModuleBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        public static readonly IndexerResourceMapper ResourceMapper = new IndexerResourceMapper();

        private IIndexerFactory _indexerFactory { get; set; }
        private ISearchForNzb _nzbSearchService { get; set; }

        public IndexerModule(IndexerFactory indexerFactory, ISearchForNzb nzbSearchService)
            : base(indexerFactory, "indexer", ResourceMapper)
        {
            _indexerFactory = indexerFactory;
            _nzbSearchService = nzbSearchService;

            Get("{id}/newznab", x =>
            {
                var request = this.Bind<NewznabRequest>();
                return GetNewznabResponse(request);
            });
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }

            base.Validate(definition, includeWarnings);
        }

        private object GetNewznabResponse(NewznabRequest request)
        {
            var requestType = request.t;
            request.source = UserAgentParser.ParseSource(Request.Headers.UserAgent);

            if (requestType.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("Missing Function Parameter");
            }

            var indexer = _indexerFactory.Get(request.id);

            if (indexer == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var indexerInstance = _indexerFactory.GetInstance(indexer);

            switch (requestType)
            {
                case "caps":
                    Response response = indexerInstance.GetCapabilities().ToXml();
                    response.ContentType = "application/rss+xml";
                    return response;
                case "seach":
                case "tvsearch":
                case "music":
                case "book":
                case "movie":
                    Response searchResponse = _nzbSearchService.Search(request, new List<int> { indexer.Id }, false).ToXml(indexerInstance.Protocol);
                    searchResponse.ContentType = "application/rss+xml";
                    return searchResponse;
                default:
                    throw new BadRequestException("Function Not Available");
            }
        }
    }
}
