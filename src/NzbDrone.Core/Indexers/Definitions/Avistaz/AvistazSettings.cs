using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
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

    public class AvistazSettings : IProviderConfig
    {
        private static readonly AvistazSettingsValidator Validator = new AvistazSettingsValidator();

        public AvistazSettings()
        {
            Token = "";
        }

        public string Token { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "Site Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", HelpText = "Site Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(3, Label = "PID", HelpText = "PID from My Account or My Profile page")]
        public string Pid { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
