using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Gazelle;

public class GazelleUserPassOrCookieValidator<T> : NoAuthSettingsValidator<T>
    where T : GazelleUserPassOrCookieSettings
{
    public GazelleUserPassOrCookieValidator()
    {
        RuleFor(c => c.Username).NotEmpty().When(c => c.Cookie.IsNullOrWhiteSpace());
        RuleFor(c => c.Password).NotEmpty().When(c => c.Cookie.IsNullOrWhiteSpace());
        RuleFor(c => c.Cookie).NotEmpty().When(c => c.Username.IsNullOrWhiteSpace() && c.Password.IsNullOrWhiteSpace());
    }
}

public class GazelleUserPassOrCookieSettings : GazelleSettings
{
    private static readonly GazelleUserPassOrCookieValidator<GazelleUserPassOrCookieSettings> Validator = new ();

    [FieldDefinition(4, Label = "Cookie", HelpText = "Use the Cookie field only if 2FA is enabled for your account, leave it empty otherwise.", HelpLink = "https://wiki.servarr.com/useful-tools#finding-cookies", Privacy = PrivacyLevel.Password)]
    public string Cookie { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
