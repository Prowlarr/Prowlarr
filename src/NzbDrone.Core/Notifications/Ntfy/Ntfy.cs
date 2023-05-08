using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Ntfy
{
    public class Ntfy : NotificationBase<NtfySettings>
    {
        private readonly INtfyProxy _proxy;

        public Ntfy(INtfyProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "ntfy.sh";

        public override string Link => "https://ntfy.sh/";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendNotification(RELEASE_GRABBED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck message)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE_BRANDED, $"The following issue is now resolved: {previousCheck.Message}", Settings);
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
