using NzbDrone.Core.Applications;

namespace Prowlarr.Api.V1.Application
{
    public class ApplicationModule : ProviderModuleBase<ApplicationResource, IApplication, ApplicationDefinition>
    {
        public static readonly ApplicationResourceMapper ResourceMapper = new ApplicationResourceMapper();

        public ApplicationModule(ApplicationFactory applicationsFactory)
            : base(applicationsFactory, "applications", ResourceMapper)
        {
        }
    }
}
