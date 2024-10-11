using NzbDrone.Core.Applications;
using NzbDrone.SignalR;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Applications
{
    [V1ApiController("applications")]
    public class ApplicationController : ProviderControllerBase<ApplicationResource, ApplicationBulkResource, IApplication, ApplicationDefinition>
    {
        public static readonly ApplicationResourceMapper ResourceMapper = new ();
        public static readonly ApplicationBulkResourceMapper BulkResourceMapper = new ();

        public ApplicationController(IBroadcastSignalRMessage signalRBroadcaster, ApplicationFactory applicationsFactory)
            : base(signalRBroadcaster, applicationsFactory, "applications", ResourceMapper, BulkResourceMapper)
        {
        }
    }
}
