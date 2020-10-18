using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void ProcessQueue();
        bool SupportsOnHealthIssue { get; }
    }
}
