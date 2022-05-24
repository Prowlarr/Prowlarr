using NzbDrone.Core.Notifications;

namespace Prowlarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
        public bool OnGrab { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnHealthRestored { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool IncludeManualGrabs { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool SupportsOnHealthRestored { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.IncludeManualGrabs = definition.IncludeManualGrabs;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnHealthRestored = definition.OnHealthRestored;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.SupportsOnHealthRestored = definition.SupportsOnHealthRestored;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource, NotificationDefinition existingDefinition)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource, existingDefinition);

            definition.OnGrab = resource.OnGrab;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.IncludeManualGrabs = resource.IncludeManualGrabs;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnHealthRestored = resource.OnHealthRestored;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.SupportsOnHealthRestored = resource.SupportsOnHealthRestored;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;

            return definition;
        }
    }
}
