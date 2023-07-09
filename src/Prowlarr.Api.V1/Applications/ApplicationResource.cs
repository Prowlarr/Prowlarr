using NzbDrone.Core.Applications;

namespace Prowlarr.Api.V1.Applications
{
    public class ApplicationResource : ProviderResource<ApplicationResource>
    {
        public ApplicationSyncLevel SyncLevel { get; set; }
        public string TestCommand { get; set; }
    }

    public class ApplicationResourceMapper : ProviderResourceMapper<ApplicationResource, ApplicationDefinition>
    {
        public override ApplicationResource ToResource(ApplicationDefinition definition)
        {
            if (definition == null)
            {
                return default;
            }

            var resource = base.ToResource(definition);

            resource.SyncLevel = definition.SyncLevel;

            return resource;
        }

        public override ApplicationDefinition ToModel(ApplicationResource resource)
        {
            if (resource == null)
            {
                return default;
            }

            var definition = base.ToModel(resource);

            definition.SyncLevel = resource.SyncLevel;

            return definition;
        }
    }
}
