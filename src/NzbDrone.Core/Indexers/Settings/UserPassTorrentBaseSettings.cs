using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class UserPassBaseSettingsValidator<T> : NoAuthSettingsValidator<T>
        where T : UserPassTorrentBaseSettings
    {
        public UserPassBaseSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class UserPassTorrentBaseSettings : NoAuthTorrentBaseSettings
    {
        private static readonly UserPassBaseSettingsValidator<UserPassTorrentBaseSettings> Validator = new ();

        public UserPassTorrentBaseSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
