using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<HealthCheckFailedEvent>,
          IHandle<HealthCheckRestoredEvent>,
          IHandleAsync<HealthCheckCompleteEvent>,
          IHandle<UpdateInstalledEvent>,
          IHandle<IndexerDownloadEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly INotificationStatusService _notificationStatusService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, INotificationStatusService notificationStatusService, IIndexerFactory indexerFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _notificationStatusService = notificationStatusService;
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            return healthCheck.Type switch
            {
                HealthCheckResult.Error => true,
                HealthCheckResult.Warning when includeWarnings => true,
                _ => false
            };
        }

        private bool ShouldHandleOnGrab(GrabMessage message, bool includeManual)
        {
            return message.GrabTrigger switch
            {
                GrabTrigger.Api => true,
                GrabTrigger.Manual when includeManual => true,
                _ => false
            };
        }

        private string GetMessage(ReleaseInfo release, GrabTrigger grabTrigger, string source, string downloadClient)
        {
            var message = string.Format("{0} grabbed by {1} from {2}",
                                    release.Title,
                                    source,
                                    release.Indexer);

            if (grabTrigger == GrabTrigger.Manual)
            {
                message = string.Format("{0} manually grabbed in Prowlarr from {1}",
                                    release.Title,
                                    release.Indexer);
            }

            if (downloadClient.IsNotNullOrWhiteSpace())
            {
                message += $" and sent to {downloadClient}";
            }

            return message;
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            // Don't send health check notifications during the start up grace period,
            // once that duration expires they they'll be retested and fired off if necessary.
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings) &&
                        ShouldHandleHealthCheck(notification.Definition, message.HealthCheck))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckRestoredEvent message)
        {
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthRestoredEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.PreviousCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings) &&
                        ShouldHandleHealthCheck(notification.Definition, message.PreviousCheck))
                    {
                        notification.OnHealthRestored(message.PreviousCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthRestored notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(HealthCheckCompleteEvent message)
        {
            ProcessQueue();
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Prowlarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IndexerDownloadEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Release = message.Release,
                Source = message.Source,
                Host = message.Host,
                Successful = message.Successful,
                DownloadClientName = message.DownloadClientName,
                DownloadClientType = message.DownloadClient,
                DownloadId = message.DownloadId,
                Redirect = message.Redirect,
                GrabTrigger = message.GrabTrigger,
                Message = GetMessage(message.Release, message.GrabTrigger, message.Source, message.DownloadClientName)
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (ShouldHandleIndexer(notification.Definition, (IndexerDefinition)message.Indexer.Definition) &&
                        ShouldHandleOnGrab(grabMessage, ((NotificationDefinition)notification.Definition).IncludeManualGrabs))
                    {
                        notification.OnGrab(grabMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        private bool ShouldHandleIndexer(ProviderDefinition definition, ProviderDefinition indexer)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");

                return true;
            }

            if (definition.Tags.Intersect(indexer.Tags).Any())
            {
                _logger.Debug("Notification and indexer have one or more intersecting tags.");

                return true;
            }

            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent.", definition.Name, indexer.Name);

            return false;
        }

        private bool ShouldHandleHealthCheck(ProviderDefinition definition, HealthCheck.HealthCheck healthCheck)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");

                return true;
            }

            // Health checks that aren't tied to specific indexers (e.g. system checks) are not
            // tag-scoped, so tagged notifications still receive them rather than silently missing
            // application-wide health issues.
            if (healthCheck.RelatedProviders?.Any() != true)
            {
                _logger.Debug("Health check is not associated with any indexer, notification will be sent.");

                return true;
            }

            var relatedTags = _indexerFactory.All()
                .Where(i => healthCheck.RelatedProviders.Contains(i.Id))
                .SelectMany(i => i.Tags)
                .ToHashSet();

            if (definition.Tags.Intersect(relatedTags).Any())
            {
                _logger.Debug("Notification and health check indexer(s) have one or more intersecting tags.");

                return true;
            }

            _logger.Debug("{0} does not have any intersecting tags with the health check indexer(s). Notification will not be sent.", definition.Name);

            return false;
        }
    }
}
