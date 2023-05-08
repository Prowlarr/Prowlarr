using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Prowl
{
    public class Prowl : NotificationBase<ProwlSettings>
    {
        private readonly IProwlProxy _prowlProxy;

        public Prowl(IProwlProxy prowlProxy)
        {
            _prowlProxy = prowlProxy;
        }

        public override string Link => "https://www.prowlapp.com/";
        public override string Name => "Prowl";

        public override void OnGrab(GrabMessage message)
        {
            _prowlProxy.SendNotification(RELEASE_GRABBED_TITLE, message.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _prowlProxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousMessage)
        {
            _prowlProxy.SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousMessage.Message}", Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _prowlProxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_prowlProxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
