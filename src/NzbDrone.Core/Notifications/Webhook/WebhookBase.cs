using NzbDrone.Core.Configuration;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications.Webhook
{
    public abstract class WebhookBase<TSettings> : NotificationBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;

        protected WebhookBase(IConfigFileProvider configFileProvider, IConfigService configService)
        {
            _configFileProvider = configFileProvider;
            _configService = configService;
        }

        public WebhookGrabPayload BuildGrabPayload(GrabMessage message)
        {
            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Release = new WebhookRelease(message.Release),
                Trigger = message.GrabTrigger,
                Source = message.Source,
                Host = message.Host,
                DownloadClient = message.DownloadClientName,
                DownloadClientType = message.DownloadClientType,
                DownloadId = message.DownloadId
            };
        }

        protected WebhookHealthPayload BuildHealthPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.Health,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookHealthPayload BuildHealthRestoredPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.HealthRestored,
                InstanceName = _configFileProvider.InstanceName,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookApplicationUpdatePayload BuildApplicationUploadPayload(ApplicationUpdateMessage updateMessage)
        {
            return new WebhookApplicationUpdatePayload
            {
                EventType = WebhookEventType.ApplicationUpdate,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
                Message = updateMessage.Message,
                PreviousVersion = updateMessage.PreviousVersion.ToString(),
                NewVersion = updateMessage.NewVersion.ToString()
            };
        }

        protected WebhookPayload BuildTestPayload()
        {
            return new WebhookPayload
            {
                EventType = WebhookEventType.Test,
                InstanceName = _configFileProvider.InstanceName,
                ApplicationUrl = _configService.ApplicationUrl,
            };
        }
    }
}
