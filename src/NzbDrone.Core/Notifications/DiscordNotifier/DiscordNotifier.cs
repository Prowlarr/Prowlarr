using System.Collections.Generic;
using System.Collections.Specialized;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.DiscordNotifier
{
    public class DiscordNotifier : NotificationBase<DiscordNotifierSettings>
    {
        private readonly IDiscordNotifierProxy _proxy;

        public DiscordNotifier(IDiscordNotifierProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://discordnotifier.com";
        public override string Name => "Discord Notifier";
        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var variables = new StringDictionary();

            variables.Add("Radarr_EventType", "HealthIssue");
            variables.Add("Radarr_Health_Issue_Level", nameof(healthCheck.Type));
            variables.Add("Radarr_Health_Issue_Message", healthCheck.Message);
            variables.Add("Radarr_Health_Issue_Type", healthCheck.Source.Name);
            variables.Add("Radarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

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
