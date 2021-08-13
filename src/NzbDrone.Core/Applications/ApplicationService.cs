using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration.Events;
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
                                      IHandleAsync<ProviderBulkUpdatedEvent<IIndexer>>,
                                      IHandleAsync<ApiKeyChangedEvent>,
                                      IExecute<ApplicationIndexerSyncCommand>
    {
        private readonly IApplicationFactory _applicationsFactory;
        private readonly IAppIndexerMapService _appIndexerMapService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IApplicationStatusService _applicationStatusService;
        private readonly Logger _logger;

        public ApplicationService(IApplicationFactory applicationsFactory, IApplicationStatusService applicationStatusService, IAppIndexerMapService appIndexerMapService, IIndexerFactory indexerFactory, Logger logger)
        {
            _applicationsFactory = applicationsFactory;
            _applicationStatusService = applicationStatusService;
            _appIndexerMapService = appIndexerMapService;
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public void HandleAsync(ProviderAddedEvent<IApplication> message)
        {
            var appDefinition = (ApplicationDefinition)message.Definition;

            if (appDefinition.Enable)
            {
                var app = _applicationsFactory.GetInstance(appDefinition);
                var indexers = _indexerFactory.Enabled().Select(i => (IndexerDefinition)i.Definition).ToList();

                SyncIndexers(new List<IApplication> { app }, indexers);
            }
        }

        public void HandleAsync(ProviderAddedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled();

            foreach (var app in enabledApps)
            {
                ExecuteAction(a => a.AddIndexer((IndexerDefinition)message.Definition), app);
            }
        }

        public void HandleAsync(ProviderDeletedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled();

            foreach (var app in enabledApps)
            {
                ExecuteAction(a => a.RemoveIndexer(message.ProviderId), app);
            }
        }

        public void HandleAsync(ProviderUpdatedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled()
                                                  .Where(n => ((ApplicationDefinition)n.Definition).SyncLevel == ApplicationSyncLevel.FullSync)
                                                  .ToList();

            SyncIndexers(enabledApps, new List<IndexerDefinition> { (IndexerDefinition)message.Definition });
        }

        public void HandleAsync(ApiKeyChangedEvent message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled();

            var indexers = _indexerFactory.AllProviders().Select(i => (IndexerDefinition)i.Definition).ToList();

            SyncIndexers(enabledApps, indexers, true);
        }

        public void HandleAsync(ProviderBulkUpdatedEvent<IIndexer> message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled();

            var indexers = message.Definitions.Select(d => (IndexerDefinition)d).ToList();

            SyncIndexers(enabledApps, indexers);
        }

        public void Execute(ApplicationIndexerSyncCommand message)
        {
            var enabledApps = _applicationsFactory.SyncEnabled();

            var indexers = _indexerFactory.AllProviders().Select(i => (IndexerDefinition)i.Definition).ToList();

            SyncIndexers(enabledApps, indexers, true);
        }

        private void SyncIndexers(List<IApplication> applications, List<IndexerDefinition> indexers, bool removeRemote = false)
        {
            foreach (var app in applications)
            {
                var indexerMappings = _appIndexerMapService.GetMappingsForApp(app.Definition.Id);

                //Get Dictionary of Remote Indexers point to Prowlarr and what they are mapped to
                var remoteMappings = ExecuteAction(a => a.GetIndexerMappings(), app);

                if (remoteMappings == null)
                {
                    continue;
                }

                //Add mappings if not already in db, these were setup manually in the app or orphaned by a table wipe
                foreach (var mapping in remoteMappings)
                {
                    if (!indexerMappings.Any(m => (m.RemoteIndexerId > 0 && m.RemoteIndexerId == mapping.RemoteIndexerId) || (m.RemoteIndexerName.IsNotNullOrWhiteSpace() && m.RemoteIndexerName == mapping.RemoteIndexerName)))
                    {
                        var addMapping = new AppIndexerMap
                        {
                            AppId = app.Definition.Id,
                            RemoteIndexerId = mapping.RemoteIndexerId,
                            RemoteIndexerName = mapping.RemoteIndexerName,
                            IndexerId = mapping.IndexerId
                        };

                        _appIndexerMapService.Insert(addMapping);
                        indexerMappings.Add(addMapping);
                    }
                }

                foreach (var indexer in indexers)
                {
                    var definition = indexer;

                    if (indexerMappings.Any(x => x.IndexerId == definition.Id))
                    {
                        if (((ApplicationDefinition)app.Definition).SyncLevel == ApplicationSyncLevel.FullSync)
                        {
                            ExecuteAction(a => a.UpdateIndexer(definition), app);
                        }
                    }
                    else
                    {
                        if (indexer.Enable)
                        {
                            ExecuteAction(a => a.AddIndexer(definition), app);
                        }
                    }
                }

                if (removeRemote)
                {
                    foreach (var mapping in indexerMappings)
                    {
                        if (!indexers.Any(x => x.Id == mapping.IndexerId))
                        {
                            _logger.Info("Indexer with the ID {0} was found within {1} but is no longer defined within Prowlarr, this is being removed.", mapping.IndexerId, app.Name);
                            ExecuteAction(a => a.RemoveIndexer(mapping.IndexerId), app);
                        }
                    }
                }
            }
        }

        private void ExecuteAction(Action<IApplication> applicationAction, IApplication application)
        {
            try
            {
                applicationAction(application);
                _applicationStatusService.RecordSuccess(application.Definition.Id);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _applicationStatusService.RecordConnectionFailure(application.Definition.Id);
                }
                else
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("timed out"))
                {
                    _logger.Warn("{0} server is currently unavailable. {1}", this, webException.Message);
                }
                else
                {
                    _logger.Warn("{0} {1}", this, webException.Message);
                }
            }
            catch (TooManyRequestsException ex)
            {
                if (ex.RetryAfter != TimeSpan.Zero)
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id, TimeSpan.FromHours(1));
                }

                _logger.Warn("API Request Limit reached for {0}", this);
            }
            catch (HttpException ex)
            {
                _applicationStatusService.RecordFailure(application.Definition.Id);
                _logger.Warn("{0} {1}", this, ex.Message);
            }
            catch (Exception ex)
            {
                _applicationStatusService.RecordFailure(application.Definition.Id);
                _logger.Error(ex, "An error occurred while talking to remote application.");
            }
        }

        private TResult ExecuteAction<TResult>(Func<IApplication, TResult> applicationAction, IApplication application)
        {
            TResult result;

            try
            {
                result = applicationAction(application);
                _applicationStatusService.RecordSuccess(application.Definition.Id);
                return result;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _applicationStatusService.RecordConnectionFailure(application.Definition.Id);
                }
                else
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("timed out"))
                {
                    _logger.Warn("{0} server is currently unavailable. {1}", this, webException.Message);
                }
                else
                {
                    _logger.Warn("{0} {1}", this, webException.Message);
                }
            }
            catch (TooManyRequestsException ex)
            {
                if (ex.RetryAfter != TimeSpan.Zero)
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _applicationStatusService.RecordFailure(application.Definition.Id, TimeSpan.FromHours(1));
                }

                _logger.Warn("API Request Limit reached for {0}", this);
            }
            catch (HttpException ex)
            {
                _applicationStatusService.RecordFailure(application.Definition.Id);
                _logger.Warn("{0} {1}", this, ex.Message);
            }
            catch (Exception ex)
            {
                _applicationStatusService.RecordFailure(application.Definition.Id);
                _logger.Error(ex, "An error occurred while talking to remote application.");
            }

            return default(TResult);
        }
    }
}
