using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListSettingsValidator : AbstractValidator<FileListSettings>
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

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Site Passkey (This is the alphanumeric string in the tracker url shown in your download client)", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        [FieldDefinition(4, Label = "Freeleech Only", HelpText = "Search Freeleech torrents only", Type = FieldType.Checkbox)]
        public bool FreeleechOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
