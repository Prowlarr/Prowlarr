using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHDSettingsValidator : AbstractValidator<AwesomeHDSettings>
    {
        public AwesomeHDSettingsValidator()
        {
            RuleFor(c => c.Passkey).NotEmpty();
        }
    }

    public class AwesomeHDSettings : IProviderConfig
    {
        private static readonly AwesomeHDSettingsValidator Validator = new AwesomeHDSettingsValidator();

        public AwesomeHDSettings()
        {
        }

        [FieldDefinition(1, Label = "Passkey", Privacy = PrivacyLevel.ApiKey)]
        public string Passkey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
