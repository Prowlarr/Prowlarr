using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationFactory : IProviderFactory<IApplication, ApplicationDefinition>
    {
    }

    public class ApplicationFactory : ProviderFactory<IApplication, ApplicationDefinition>, IApplicationFactory
    {
        public ApplicationFactory(IApplicationsRepository providerRepository, IEnumerable<IApplication> providers, IContainer container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
        }
    }
}
