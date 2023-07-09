using NzbDrone.Core.Applications;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Applications
{
    [V1ApiController("applications")]
    public class ApplicationController : ProviderControllerBase<ApplicationResource, ApplicationBulkResource, IApplication, ApplicationDefinition>
    {
        public static readonly ApplicationResourceMapper ResourceMapper = new ();
        public static readonly ApplicationBulkResourceMapper BulkResourceMapper = new ();

        public ApplicationController(ApplicationFactory applicationsFactory)
            : base(applicationsFactory, "applications", ResourceMapper, BulkResourceMapper)
        {
        }
    }
}
