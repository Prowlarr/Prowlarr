using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetSettingsValidator : AbstractValidator<BroadcastheNetSettings>
    {
        public BroadcastheNetSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class BroadcastheNetSettings : IProviderConfig
    {
        private static readonly BroadcastheNetSettingsValidator Validator = new BroadcastheNetSettingsValidator();

        public BroadcastheNetSettings()
        {
        }

        [FieldDefinition(1, Label = "API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
