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

    [FieldDefinition(5, Type = FieldType.Select, Label = "Use Freeleech Tokens", SelectOptions = typeof(GazelleFreeleechTokenAction), HelpText = "When to use freeleech tokens")]
    public int UseFreeleechToken { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}

public enum GazelleFreeleechTokenAction
{
    [FieldOption(Label = "Never", Hint = "Do not use tokens")]
    Never = 0,

    [FieldOption(Label = "Preferred", Hint = "Use token if possible")]
    Preferred = 1,

    [FieldOption(Label = "Required", Hint = "Abort download if unable to use token")]
    Required = 2,
}
