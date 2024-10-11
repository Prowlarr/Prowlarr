using System;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.SignalR;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.IndexerProxies
{
    [V1ApiController]
    public class IndexerProxyController : ProviderControllerBase<IndexerProxyResource, IndexerProxyBulkResource, IIndexerProxy, IndexerProxyDefinition>
    {
        public static readonly IndexerProxyResourceMapper ResourceMapper = new ();
        public static readonly IndexerProxyBulkResourceMapper BulkResourceMapper = new ();

        public IndexerProxyController(IBroadcastSignalRMessage signalRBroadcaster, IndexerProxyFactory notificationFactory)
            : base(signalRBroadcaster, notificationFactory, "indexerProxy", ResourceMapper, BulkResourceMapper)
        {
        }

        [NonAction]
        public override ActionResult<IndexerProxyResource> UpdateProvider([FromBody] IndexerProxyBulkResource providerResource)
        {
            throw new NotImplementedException();
        }

        [NonAction]
        public override object DeleteProviders([FromBody] IndexerProxyBulkResource resource)
        {
            throw new NotImplementedException();
        }
    }
}
