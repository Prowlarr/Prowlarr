using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleSettingsValidator : UserPassBaseSettingsValidator<GazelleSettings>
    {
        public GazelleSettingsValidator()
        : base()
        {
        }
    }

    public class GazelleSettings : UserPassTorrentBaseSettings
    {
        private static readonly GazelleSettingsValidator Validator = new ();

        public GazelleSettings()
        {
        }

        public string AuthKey;
        public string PassKey;

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Use freeleech tokens when available")]
        public bool UseFreeleechToken { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
