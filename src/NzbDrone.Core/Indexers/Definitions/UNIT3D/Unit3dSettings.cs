using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public class Unit3dSettingsValidator : AbstractValidator<Unit3dSettings>
    {
        public Unit3dSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class Unit3dSettings : IIndexerSettings
    {
        private static readonly Unit3dSettingsValidator Validator = new Unit3dSettingsValidator();

        public Unit3dSettings()
        {
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", HelpText = "Site API Key generated in My Security", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
