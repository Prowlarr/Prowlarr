using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;
using NzbDrone.SignalR;
using Prowlarr.Http;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerStatusController : RestControllerWithSignalR<IndexerStatusResource, IndexerStatus>,
                                       IHandle<ProviderStatusChangedEvent<IIndexer>>
    {
        private readonly IIndexerStatusService _indexerStatusService;

        public IndexerStatusController(IBroadcastSignalRMessage signalRBroadcaster, IIndexerStatusService indexerStatusService)
            : base(signalRBroadcaster)
        {
            _indexerStatusService = indexerStatusService;
        }

        [NonAction]
        public override IndexerStatusResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Produces("application/json")]
        public List<IndexerStatusResource> GetAll()
        {
            return _indexerStatusService.GetBlockedProviders().ToResource();
        }

        [NonAction]
        public void Handle(ProviderStatusChangedEvent<IIndexer> message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
