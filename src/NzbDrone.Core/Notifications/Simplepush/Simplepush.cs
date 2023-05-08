using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Simplepush
{
    public class Simplepush : NotificationBase<SimplepushSettings>
    {
        private readonly ISimplepushProxy _proxy;

        public Simplepush(ISimplepushProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Simplepush";
        public override string Link => "https://simplepush.io/";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendNotification(RELEASE_GRABBED_TITLE, message.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
