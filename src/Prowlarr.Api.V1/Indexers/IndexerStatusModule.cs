using System.Collections.Generic;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;
using NzbDrone.SignalR;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatusModule : ProwlarrRestModuleWithSignalR<IndexerStatusResource, IndexerStatus>,
                                       IHandle<ProviderStatusChangedEvent<IIndexer>>
    {
        private readonly IIndexerStatusService _indexerStatusService;

        public IndexerStatusModule(IBroadcastSignalRMessage signalRBroadcaster, IIndexerStatusService indexerStatusService)
            : base(signalRBroadcaster)
        {
            _indexerStatusService = indexerStatusService;

            GetResourceAll = GetAll;
        }

        private List<IndexerStatusResource> GetAll()
        {
            return _indexerStatusService.GetBlockedProviders().ToResource();
        }

        public void Handle(ProviderStatusChangedEvent<IIndexer> message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
