using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public class Unit3dSettingsValidator : NoAuthSettingsValidator<Unit3dSettings>
    {
        public Unit3dSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class Unit3dSettings : NoAuthTorrentBaseSettings
    {
        private static readonly Unit3dSettingsValidator Validator = new ();

        [FieldDefinition(2, Label = "ApiKey", HelpText = "Site API Key generated in My Security", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
