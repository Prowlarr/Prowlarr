using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnHealthIssue { get; set; }
        public bool OnHealthRestored { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool OnGrab { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool IncludeManualGrabs { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool SupportsOnHealthRestored { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }

        public override bool Enable => OnHealthIssue || OnHealthRestored || OnApplicationUpdate || OnGrab;
    }
}
