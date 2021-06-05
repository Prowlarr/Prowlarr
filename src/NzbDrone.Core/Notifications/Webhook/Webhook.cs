using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class Webhook : NotificationBase<WebhookSettings>
    {
        private readonly IWebhookProxy _proxy;

        public Webhook(IWebhookProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://wikijs.servarr.com/prowlarr/settings#connect";

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var payload = new WebhookHealthPayload
                          {
                              EventType = WebhookEventType.Health,
                              Level = healthCheck.Type,
                              Message = healthCheck.Message,
                              Type = healthCheck.Source.Name,
                              WikiUrl = healthCheck.WikiUrl?.ToString()
                          };

            _proxy.SendWebhook(payload, Settings);
        }

        public override string Name => "Webhook";

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(SendWebhookTest());

            return new ValidationResult(failures);
        }

        private ValidationFailure SendWebhookTest()
        {
            try
            {
                var payload = new WebhookHealthPayload
                {
                    EventType = WebhookEventType.Test
                };

                _proxy.SendWebhook(payload, Settings);
            }
            catch (WebhookException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
