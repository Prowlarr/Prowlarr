using System.Collections.Generic;
using System.Collections.Specialized;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Notifiarr
{
    public class Notifiarr : NotificationBase<NotifiarrSettings>
    {
        private readonly INotifiarrProxy _proxy;

        public Notifiarr(INotifiarrProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://notifiarr.com";
        public override string Name => "Notifiarr";
        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var variables = new StringDictionary();

            variables.Add("Prowlarr_EventType", "HealthIssue");
            variables.Add("Prowlarr_Health_Issue_Level", healthCheck.Type.ToString() ?? string.Empty);
            variables.Add("Prowlarr_Health_Issue_Message", healthCheck.Message);
            variables.Add("Prowlarr_Health_Issue_Type", healthCheck.Source.Name);
            variables.Add("Prowlarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var variables = new StringDictionary();

            variables.Add("Prowlarr_EventType", "ApplicationUpdate");
            variables.Add("Prowlarr_Update_Message", updateMessage.Message);
            variables.Add("Prowlarr_Update_NewVersion", updateMessage.NewVersion.ToString());
            variables.Add("Prowlarr_Update_PreviousVersion", updateMessage.PreviousVersion.ToString());

            _proxy.SendNotification(variables, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
