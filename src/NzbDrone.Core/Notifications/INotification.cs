using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnHealthRestored(HealthCheck.HealthCheck previousCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnHealthRestored { get; }
        bool SupportsOnApplicationUpdate { get; }
    }
}
