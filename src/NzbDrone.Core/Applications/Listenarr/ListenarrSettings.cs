using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Applications.Listenarr
{
    public class ListenarrSettingsValidator : AbstractValidator<ListenarrSettings>
    {
        public ListenarrSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).IsValidUrl();
            RuleFor(c => c.ProwlarrUrl).IsValidUrl();
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class ListenarrSettings : IApplicationSettings
    {
        private static readonly ListenarrSettingsValidator Validator = new ListenarrSettingsValidator();

        public ListenarrSettings()
        {
            ProwlarrUrl = "http://localhost:9696";
            BaseUrl = "http://localhost:5000";
            SyncLevel = (int)ApplicationSyncLevel.FullSync; // default reasonable behavior
        }

        [FieldDefinition(0, Label = "Prowlarr Server", HelpText = "URL of Prowlarr server as Listenarr sees it, including http:// or https://, and port if needed")]
        public string ProwlarrUrl { get; set; }

        [FieldDefinition(1, Label = "Listenarr Server", HelpText = "URL of Listenarr server, including http:// or https://, and port if needed")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "API Key for Listenarr")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, SelectOptions = typeof(ApplicationSyncLevel), Label = "Sync Level", HelpText = "How should Prowlarr sync indexers to Listenarr")]
        public int SyncLevel { get; set; }

        [FieldDefinition(4, Label = "Sync Categories", Advanced = true, HelpText = "Sync audiobook categories to Listenarr (must match Listenarr's category schema)")]
        public IEnumerable<int> SyncCategories { get; set; }

        [FieldDefinition(5, Type = FieldType.TagSelect, SelectOptionsProviderAction = "getTags", Label = "Tags", HelpText = "Only add indexers with these tags to Listenarr")]
        public IEnumerable<int> Tags { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
