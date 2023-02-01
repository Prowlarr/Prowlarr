using System;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class CookieBaseSettingsValidator<T> : AbstractValidator<T>
        where T : CookieTorrentBaseSettings
    {
        public CookieBaseSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
            RuleFor(c => c.Cookie)
                .Must(c => !c.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
                .When(c => c.Cookie.IsNotNullOrWhiteSpace())
                .WithMessage("Should not start with \"Cookie:\".");
            RuleFor(c => c.Cookie)
                .Must(c => !c.StartsWith("Set-Cookie:", StringComparison.OrdinalIgnoreCase))
                .When(c => c.Cookie.IsNotNullOrWhiteSpace())
                .WithMessage("Should not start with \"Set-Cookie:\".");
            RuleFor(x => x.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(x => x.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
        }
    }

    public class CookieTorrentBaseSettings : ITorrentIndexerSettings
    {
        private static readonly CookieBaseSettingsValidator<CookieTorrentBaseSettings> Validator = new ();

        public CookieTorrentBaseSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Site Cookie", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Cookie { get; set; }

        [FieldDefinition(10)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        [FieldDefinition(11)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new ();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
