using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationsFactory : IProviderFactory<IApplications, ApplicationDefinition>
    {
    }

    public class ApplicationFactory : ProviderFactory<IApplications, ApplicationDefinition>, IApplicationsFactory
    {
        public ApplicationFactory(IApplicationsRepository providerRepository, IEnumerable<IApplications> providers, IContainer container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
        }
    }
}
