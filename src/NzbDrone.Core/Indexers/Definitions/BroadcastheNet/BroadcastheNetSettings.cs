using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetSettingsValidator : AbstractValidator<BroadcastheNetSettings>
    {
        public BroadcastheNetSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class BroadcastheNetSettings : IIndexerSettings
    {
        private static readonly BroadcastheNetSettingsValidator Validator = new BroadcastheNetSettingsValidator();

        public BroadcastheNetSettings()
        {
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
