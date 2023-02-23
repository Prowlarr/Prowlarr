using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Gazelle;

public class GazelleSettingsValidator<T> : UserPassBaseSettingsValidator<T>
    where T : GazelleSettings
{
}

public class GazelleSettings : UserPassTorrentBaseSettings
{
    private static readonly GazelleSettingsValidator<GazelleSettings> Validator = new ();

    public string AuthKey { get; set; }
    public string PassKey { get; set; }

    [FieldDefinition(5, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Use freeleech tokens when available")]
    public bool UseFreeleechToken { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
