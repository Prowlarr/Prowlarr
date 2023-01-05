using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetSettingsValidator : NoAuthSettingsValidator<BroadcastheNetSettings>
    {
        public BroadcastheNetSettingsValidator()
        : base()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class BroadcastheNetSettings : NoAuthTorrentBaseSettings
    {
        private static readonly BroadcastheNetSettingsValidator Validator = new BroadcastheNetSettingsValidator();

        public BroadcastheNetSettings()
        {
        }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
