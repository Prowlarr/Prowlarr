using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Signal
{
    public class Signal : NotificationBase<SignalSettings>
    {
        private readonly ISignalProxy _proxy;

        public Signal(ISignalProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Signal";
        public override string Link => "https://signal.org/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(RELEASE_GRABBED_TITLE_BRANDED, grabMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, healthCheck.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE_BRANDED, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
