using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazSettingsValidator : AbstractValidator<AvistazSettings>
    {
        public AvistazSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Pid).NotEmpty();
        }
    }

    public class AvistazSettings : IIndexerSettings
    {
        private static readonly AvistazSettingsValidator Validator = new AvistazSettingsValidator();

        public AvistazSettings()
        {
            Token = "";
        }

        public string Token { get; set; }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, HelpText = "Password", Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "PID", HelpText = "PID from My Account or My Profile page")]
        public string Pid { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
