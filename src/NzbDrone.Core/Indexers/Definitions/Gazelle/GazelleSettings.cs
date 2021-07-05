using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleSettingsValidator : AbstractValidator<GazelleSettings>
    {
        public GazelleSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class GazelleSettings : IIndexerSettings
    {
        private static readonly GazelleSettingsValidator Validator = new GazelleSettingsValidator();

        public GazelleSettings()
        {
        }

        public string AuthKey;
        public string PassKey;

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, HelpText = "Password", Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Use Freeleech Token")]
        public bool UseFreeleechToken { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
