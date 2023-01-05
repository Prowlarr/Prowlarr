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
            RuleFor(c => c.FreeleechSize).GreaterThanOrEqualTo(0);
        }
    }

    public class GazelleSettings : UserPassTorrentBaseSettings
    {
        private static readonly GazelleSettingsValidator Validator = new ();

        public GazelleSettings()
        {
            UseFreeleechToken = false;
            FreeleechSize = 0;
        }

        public string AuthKey;
        public string PassKey;

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Use freeleech tokens when available")]
        public bool UseFreeleechToken { get; set; }

        [FieldDefinition(5, Type = FieldType.Number, Label = "Freeleech Torrent Size", Unit = "bytes", Advanced = true, HelpText = "Only use freeleech tokens for torrents above a given size")]
        public long FreeleechSize { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
