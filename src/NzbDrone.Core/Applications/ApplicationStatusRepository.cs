using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Status;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationStatusRepository : IProviderStatusRepository<ApplicationStatus>
    {
    }

    public class ApplicationStatusRepository : ProviderStatusRepository<ApplicationStatus>, IApplicationStatusRepository
    {
        public ApplicationStatusRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
