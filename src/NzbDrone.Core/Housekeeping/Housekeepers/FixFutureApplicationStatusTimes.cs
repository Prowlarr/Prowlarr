using NzbDrone.Core.Applications;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class FixFutureApplicationStatusTimes : FixFutureProviderStatusTimes<ApplicationStatus>, IHousekeepingTask
    {
        public FixFutureApplicationStatusTimes(IApplicationStatusRepository applicationStatusRepository)
            : base(applicationStatusRepository)
        {
        }
    }
}
