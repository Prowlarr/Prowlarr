using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class HeadphonesSettingsValidator : AbstractValidator<HeadphonesSettings>
    {
        public HeadphonesSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(x => x.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
        }
    }

    public class HeadphonesSettings : IIndexerSettings
    {
        private static readonly HeadphonesSettingsValidator Validator = new ();

        public HeadphonesSettings()
        {
            ApiPath = "/api";
            ApiKey = "964d601959918a578a670984bdee9357";
        }

        public string ApiPath { get; set; }

        public string ApiKey { get; set; }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
