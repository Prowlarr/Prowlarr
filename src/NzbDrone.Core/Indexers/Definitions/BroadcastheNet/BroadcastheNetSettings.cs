using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetSettingsValidator : NoAuthSettingsValidator<BroadcastheNetSettings>
    {
        public BroadcastheNetSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class BroadcastheNetSettings : NoAuthTorrentBaseSettings
    {
        private static readonly BroadcastheNetSettingsValidator Validator = new ();

        public BroadcastheNetSettings()
        {
            BaseSettings.QueryLimit = 150;
            BaseSettings.LimitsUnit = (int)IndexerLimitsUnit.Hour;
        }

        [FieldDefinition(2, Label = "ApiKey", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
