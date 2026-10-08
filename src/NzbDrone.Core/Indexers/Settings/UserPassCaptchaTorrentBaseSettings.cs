using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class UserPassCaptchaTorrentBaseSettings : ITorrentIndexerSettings
    {
        public class UserPassCaptchaBaseSettingsValidator : AbstractValidator<UserPassCaptchaTorrentBaseSettings>
        {
            public UserPassCaptchaBaseSettingsValidator()
            {
                RuleFor(c => c.Username).NotEmpty();
                RuleFor(c => c.Password).NotEmpty();
            }
        }

        private static readonly UserPassCaptchaBaseSettingsValidator Validator = new UserPassCaptchaBaseSettingsValidator();

        public UserPassCaptchaTorrentBaseSettings()
        {
            Username = "";
            Password = "";
            Captcha  = "";
            ExtraFieldData = new Dictionary<string, object>();
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Captcha", HelpText = "Site Captcha", Privacy = PrivacyLevel.Normal, Type = FieldType.Captcha)]
        public string Captcha { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        [FieldDefinition(6)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new IndexerTorrentBaseSettings();

        public Dictionary<string, object> ExtraFieldData { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
