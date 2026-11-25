using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Applications.CrossSeed
{
    public class CrossSeedSettingsValidator : AbstractValidator<CrossSeedSettings>
    {
        public CrossSeedSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).IsValidUrl();
            RuleFor(c => c.ProwlarrUrl).IsValidUrl();
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class CrossSeedSettings : IApplicationSettings
    {
        private static readonly CrossSeedSettingsValidator Validator = new();

        public CrossSeedSettings()
        {
            ProwlarrUrl = "http://localhost:9696";
            BaseUrl = "http://localhost:2468";
            SyncCategories = new[] { 2000, 2010, 2020, 2030, 2040, 2045, 2050, 2060, 2070, 2080, 2090 };
        }

        [FieldDefinition(0, Label = "Prowlarr Server", HelpText = "Prowlarr server URL as cross-seed sees it, including http(s)://, port, and urlbase if needed", Placeholder = "http://localhost:9696")]
        public string ProwlarrUrl { get; set; }

        [FieldDefinition(1, Label = "cross-seed Server", HelpText = "URL used to connect to cross-seed server, including http(s)://, port, and urlbase if required", Placeholder = "http://localhost:2468")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "The API key configured in cross-seed")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "Sync Categories", Type = FieldType.Select, SelectOptions = typeof(NewznabCategoryFieldConverter), HelpText = "Only Indexers that support these categories will be synced (cross-seed only supports torrents)", Advanced = true)]
        public IEnumerable<int> SyncCategories { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
