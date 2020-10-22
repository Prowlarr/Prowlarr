using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.Applications
{
    public class ApplicationService : IHandle<ProviderAddedEvent<IIndexer>>, IHandle<ProviderDeletedEvent<IIndexer>>, IHandle<ProviderAddedEvent<IApplication>>, IHandle<ProviderUpdatedEvent<IIndexer>>
    {
        private readonly IApplicationFactory _applicationsFactory;
        private readonly Logger _logger;

        public ApplicationService(IApplicationFactory applicationsFactory, Logger logger)
        {
            _applicationsFactory = applicationsFactory;
            _logger = logger;
        }

        // Sync Indexers on App Add if Sync Enabled
        public void Handle(ProviderAddedEvent<IApplication> message)
        {
            var appDefinition = (ApplicationDefinition)message.Definition;

            if (message.Definition.Enable)
            {
                var app = _applicationsFactory.GetInstance(appDefinition);

                app.SyncIndexers();
            }
        }

        public void Handle(ProviderAddedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only apps with Sync enabled
            foreach (var app in enabledApps)
            {
                app.AddIndexer((IndexerDefinition)message.Definition);
            }
        }

        public void Handle(ProviderDeletedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only remove indexers when Sync is Full
            foreach (var app in enabledApps)
            {
                app.RemoveIndexer(message.ProviderId);
            }
        }

        public void Handle(ProviderUpdatedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only upate indexers when Sync is Full
            foreach (var app in enabledApps)
            {
                app.UpdateIndexer((IndexerDefinition)message.Definition);
            }
        }
    }
}
