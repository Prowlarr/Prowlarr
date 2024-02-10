using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.FileList;

public class FileListSettingsValidator : NoAuthSettingsValidator<FileListSettings>
{
    public FileListSettingsValidator()
    {
        RuleFor(c => c.Username).NotEmpty();
        RuleFor(c => c.Passkey).NotEmpty();
    }
}

public class FileListSettings : NoAuthTorrentBaseSettings
{
    private static readonly FileListSettingsValidator Validator = new ();

    public FileListSettings()
    {
        BaseSettings.QueryLimit = 150;
        BaseSettings.LimitsUnit = (int)IndexerLimitsUnit.Hour;
    }

    [FieldDefinition(2, Label = "Username", HelpText = "IndexerFileListSettingsUsernameHelpText", Privacy = PrivacyLevel.UserName)]
    public string Username { get; set; }

    [FieldDefinition(3, Label = "IndexerSettingsPasskey", HelpText = "IndexerFileListSettingsPasskeyHelpText", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
    public string Passkey { get; set; }

    [FieldDefinition(4, Label = "IndexerSettingsFreeleechOnly", HelpText = "IndexerFileListSettingsFreeleechOnlyHelpText", Type = FieldType.Checkbox)]
    public bool FreeleechOnly { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
