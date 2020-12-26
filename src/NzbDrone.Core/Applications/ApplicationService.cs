using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.Applications
{
    public class ApplicationService : IHandleAsync<ProviderAddedEvent<IIndexer>>,
                                      IHandleAsync<ProviderDeletedEvent<IIndexer>>,
                                      IHandleAsync<ProviderAddedEvent<IApplication>>,
                                      IHandleAsync<ProviderUpdatedEvent<IIndexer>>,
                                      IExecute<ApplicationIndexerSyncCommand>
    {
        private readonly IApplicationFactory _applicationsFactory;
        private readonly Logger _logger;

        public ApplicationService(IApplicationFactory applicationsFactory, Logger logger)
        {
            _applicationsFactory = applicationsFactory;
            _logger = logger;
        }

        // Sync Indexers on App Add if Sync Enabled
        public void HandleAsync(ProviderAddedEvent<IApplication> message)
        {
            var appDefinition = (ApplicationDefinition)message.Definition;

            if (message.Definition.Enable)
            {
                var app = _applicationsFactory.GetInstance(appDefinition);

                app.SyncIndexers();
            }
        }

        public void HandleAsync(ProviderAddedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only apps with Sync enabled
            foreach (var app in enabledApps)
            {
                app.AddIndexer((IndexerDefinition)message.Definition);
            }
        }

        public void HandleAsync(ProviderDeletedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only remove indexers when Sync is Full
            foreach (var app in enabledApps)
            {
                app.RemoveIndexer(message.ProviderId);
            }
        }

        public void HandleAsync(ProviderUpdatedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            // TODO: Only upate indexers when Sync is Full
            foreach (var app in enabledApps)
            {
                app.UpdateIndexer((IndexerDefinition)message.Definition);
            }
        }

        public void Execute(ApplicationIndexerSyncCommand message)
        {
            var enabledApps = _applicationsFactory.GetAvailableProviders();

            foreach (var app in enabledApps)
            {
                app.SyncIndexers();
            }
        }
    }
}
