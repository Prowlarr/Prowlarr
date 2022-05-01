using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazSettingsValidator : AbstractValidator<AvistazSettings>
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
        private static readonly AvistazSettingsValidator Validator = new AvistazSettingsValidator();

        public AvistazSettings()
        {
            Token = "";
        }

        public string Token { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "PID", HelpText = "PID from My Account or My Profile page")]
        public string Pid { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
