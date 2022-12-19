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
                RuleFor(x => x.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
                RuleFor(x => x.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
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

        [FieldDefinition(10)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        [FieldDefinition(11)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new IndexerTorrentBaseSettings();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
