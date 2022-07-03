using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class CookieTorrentBaseSettings : ITorrentIndexerSettings
    {
        public class CookieBaseSettingsValidator : AbstractValidator<CookieTorrentBaseSettings>
        {
            public CookieBaseSettingsValidator()
            {
                RuleFor(c => c.Cookie).NotEmpty();
            }
        }

        private static readonly CookieBaseSettingsValidator Validator = new CookieBaseSettingsValidator();

        public CookieTorrentBaseSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Site Cookie", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Cookie { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        [FieldDefinition(4)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new IndexerTorrentBaseSettings();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
