using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.HealthCheck
{
    /// <summary>
    /// Published when a health check that is already failing fails again with different indexer
    /// tags, e.g. an indexer with another tag becomes unavailable while the first still is.
    /// <see cref="HealthCheckFailedEvent"/> is only published when a check starts failing, so this
    /// lets tag-scoped notifications react to indexers joining or leaving an ongoing failure.
    /// </summary>
    public class HealthCheckUpdatedEvent : IEvent
    {
        public HealthCheck PreviousCheck { get; private set; }
        public HealthCheck HealthCheck { get; private set; }
        public bool IsInStartupGracePeriod { get; private set; }

        /// <summary>
        /// Creates the event for a failing check whose indexer tags changed.
        /// </summary>
        /// <param name="previousCheck">The failing result reported before this run.</param>
        /// <param name="healthCheck">The failing result reported by this run.</param>
        /// <param name="isInStartupGracePeriod">Whether the application is still in its startup grace period.</param>
        public HealthCheckUpdatedEvent(HealthCheck previousCheck, HealthCheck healthCheck, bool isInStartupGracePeriod)
        {
            PreviousCheck = previousCheck;
            HealthCheck = healthCheck;
            IsInStartupGracePeriod = isInStartupGracePeriod;
        }
    }
}
