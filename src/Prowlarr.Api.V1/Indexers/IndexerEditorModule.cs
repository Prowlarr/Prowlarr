using Nancy;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerEditorModule : ProwlarrV1Module
    {
        private readonly IIndexerFactory _movieService;
        private readonly IManageCommandQueue _commandQueueManager;

        public IndexerEditorModule(IIndexerFactory movieService, IManageCommandQueue commandQueueManager)
            : base("/indexer/editor")
        {
            _movieService = movieService;
            _commandQueueManager = commandQueueManager;
            Delete("/", movie => DeleteIndexers());
        }

        private object DeleteIndexers()
        {
            var resource = Request.Body.FromJson<IndexerEditorResource>();

            _movieService.DeleteIndexers(resource.IndexerIds);

            return new object();
        }
    }
}
