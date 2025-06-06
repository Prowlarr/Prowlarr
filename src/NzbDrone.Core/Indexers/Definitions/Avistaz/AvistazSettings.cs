using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazSettingsValidator : NoAuthSettingsValidator<AvistazSettings>
    {
        public AvistazSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Pid).NotEmpty();
        }
    }

    public class AvistazSettings : NoAuthTorrentBaseSettings
    {
        private static readonly AvistazSettingsValidator Validator = new();

        public AvistazSettings()
        {
            Token = "";
            FreeleechOnly = false;
        }

        public string Token { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "IndexerAvistazSettingsUsernameHelpText", HelpTextWarning = "IndexerAvistazSettingsUsernameHelpTextWarning", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "IndexerAvistazSettingsPasswordHelpText", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "PID", HelpText = "IndexerAvistazSettingsPidHelpText", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Pid { get; set; }

        [FieldDefinition(5, Label = "IndexerSettingsFreeleechOnly", Type = FieldType.Checkbox, HelpText = "IndexerAvistazSettingsFreeleechOnlyHelpText")]
        public bool FreeleechOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
