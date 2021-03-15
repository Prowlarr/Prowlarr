using NzbDrone.Core.Applications;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Application
{
    [V1ApiController("applications")]
    public class ApplicationController : ProviderControllerBase<ApplicationResource, IApplication, ApplicationDefinition>
    {
        public static readonly ApplicationResourceMapper ResourceMapper = new ApplicationResourceMapper();

        public ApplicationController(ApplicationFactory applicationsFactory)
            : base(applicationsFactory, "applications", ResourceMapper)
        {
        }
    }
}
