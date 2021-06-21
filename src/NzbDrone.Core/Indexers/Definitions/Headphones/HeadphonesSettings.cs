using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class HeadphonesSettingsValidator : AbstractValidator<HeadphonesSettings>
    {
        public HeadphonesSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class HeadphonesSettings : IProviderConfig
    {
        private static readonly HeadphonesSettingsValidator Validator = new HeadphonesSettingsValidator();

        public HeadphonesSettings()
        {
            ApiPath = "/api";
            ApiKey = "964d601959918a578a670984bdee9357";
        }

        public string ApiPath { get; set; }

        public string ApiKey { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "Site Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", HelpText = "Site Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
