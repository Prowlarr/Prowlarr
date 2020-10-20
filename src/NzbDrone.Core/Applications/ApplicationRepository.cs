using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationsRepository : IProviderRepository<ApplicationDefinition>
    {
        void UpdateSettings(ApplicationDefinition model);
    }

    public class ApplicationRepository : ProviderRepository<ApplicationDefinition>, IApplicationsRepository
    {
        public ApplicationRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void UpdateSettings(ApplicationDefinition model)
        {
            SetFields(model, m => m.Settings);
        }
    }
}
