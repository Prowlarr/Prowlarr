using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotificationFactory : IProviderFactory<INotification, NotificationDefinition>
    {
        List<INotification> OnGrabEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnHealthIssueEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnHealthRestoredEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnApplicationUpdateEnabled(bool filterBlockedNotifications = true);
    }

    public class NotificationFactory : ProviderFactory<INotification, NotificationDefinition>, INotificationFactory
    {
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationFactory(INotificationStatusService notificationStatusService, INotificationRepository providerRepository, IEnumerable<INotification> providers, IServiceProvider container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _notificationStatusService = notificationStatusService;
            _logger = logger;
        }

        protected override List<NotificationDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        public List<INotification> OnGrabEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab).ToList();
        }

        public List<INotification> OnHealthIssueEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue).ToList();
        }

        public List<INotification> OnHealthRestoredEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthRestored)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthRestored).ToList();
        }

        public List<INotification> OnApplicationUpdateEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate).ToList();
        }

        private IEnumerable<INotification> FilterBlockedNotifications(IEnumerable<INotification> notifications)
        {
            var blockedNotifications = _notificationStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var notification in notifications)
            {
                if (blockedNotifications.TryGetValue(notification.Definition.Id, out var notificationStatus))
                {
                    _logger.Debug("Temporarily ignoring notification {0} till {1} due to recent failures.", notification.Definition.Name, notificationStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return notification;
            }
        }

        public override void SetProviderCharacteristics(INotification provider, NotificationDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.SupportsOnGrab = provider.SupportsOnGrab;
            definition.SupportsOnHealthIssue = provider.SupportsOnHealthIssue;
            definition.SupportsOnHealthRestored = provider.SupportsOnHealthRestored;
            definition.SupportsOnApplicationUpdate = provider.SupportsOnApplicationUpdate;
        }

        public override ValidationResult Test(NotificationDefinition definition)
        {
            var result = base.Test(definition);

            if (definition.Id == 0)
            {
                return result;
            }

            if (result == null || result.IsValid)
            {
                _notificationStatusService.RecordSuccess(definition.Id);
            }
            else
            {
                _notificationStatusService.RecordFailure(definition.Id);
            }

            return result;
        }
    }
}
