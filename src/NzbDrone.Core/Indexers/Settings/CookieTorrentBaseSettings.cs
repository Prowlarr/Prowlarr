using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class CookieBaseSettingsValidator<T> : NoAuthSettingsValidator<T>
        where T : CookieTorrentBaseSettings
    {
        public CookieBaseSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class CookieTorrentBaseSettings : NoAuthTorrentBaseSettings
    {
        private static readonly CookieBaseSettingsValidator<CookieTorrentBaseSettings> Validator = new ();

        public CookieTorrentBaseSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Site Cookie", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Cookie { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
