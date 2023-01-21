using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Apprise
{
    public class Apprise : NotificationBase<AppriseSettings>
    {
        public override string Name => "Apprise";

        public override string Link => "https://github.com/caronc/apprise";

        private readonly IAppriseProxy _proxy;

        public Apprise(IAppriseProxy proxy)
        {
            _proxy = proxy;
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(Settings, HEALTH_ISSUE_TITLE_BRANDED, $"{healthCheck.Message}");
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(Settings, APPLICATION_UPDATE_TITLE_BRANDED, $"{updateMessage.Message}");
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
