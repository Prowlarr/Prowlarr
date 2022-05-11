using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class UserPassTorrentBaseSettings : IIndexerSettings
    {
        public class UserPassBaseSettingsValidator : AbstractValidator<UserPassTorrentBaseSettings>
        {
            public UserPassBaseSettingsValidator()
            {
                RuleFor(c => c.Username).NotEmpty();
                RuleFor(c => c.Password).NotEmpty();
            }
        }

        private static readonly UserPassBaseSettingsValidator Validator = new UserPassBaseSettingsValidator();

        public UserPassTorrentBaseSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
