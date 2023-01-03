using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcornSettingsValidator : NoAuthSettingsValidator<PassThePopcornSettings>
    {
        public PassThePopcornSettingsValidator()
        : base()
        {
            RuleFor(c => c.APIUser).NotEmpty();
            RuleFor(c => c.APIKey).NotEmpty();
        }
    }

    public class PassThePopcornSettings : NoAuthTorrentBaseSettings
    {
        private static readonly PassThePopcornSettingsValidator Validator = new ();

        public PassThePopcornSettings()
        {
        }

        [FieldDefinition(2, Label = "API User", HelpText = "These settings are found in your PassThePopcorn security settings (Edit Profile > Security).", Privacy = PrivacyLevel.UserName)]
        public string APIUser { get; set; }

        [FieldDefinition(3, Label = "API Key", HelpText = "Site API Key", Privacy = PrivacyLevel.ApiKey)]
        public string APIKey { get; set; }

        [FieldDefinition(4, Label = "Freeleech Only", HelpText = "Return only freeleech torrents", Type = FieldType.Checkbox)]
        public bool FreeleechOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
