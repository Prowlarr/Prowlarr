using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public class LazyLibrarianSettingsValidator : AbstractValidator<LazyLibrarianSettings>
    {
        public LazyLibrarianSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).IsValidUrl();
            RuleFor(c => c.ProwlarrUrl).IsValidUrl();
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.SyncCategories).NotEmpty();
        }
    }

    public class LazyLibrarianSettings : IApplicationSettings
    {
        private static readonly LazyLibrarianSettingsValidator Validator = new ();

        public LazyLibrarianSettings()
        {
            ProwlarrUrl = "http://localhost:9696";
            BaseUrl = "http://localhost:5299";
            SyncCategories = new[]
            {
                NewznabStandardCategory.AudioAudiobook.Id,
                NewznabStandardCategory.Books.Id,
                NewznabStandardCategory.BooksComics.Id,
                NewznabStandardCategory.BooksEBook.Id,
                NewznabStandardCategory.BooksForeign.Id,
                NewznabStandardCategory.BooksMags.Id,
                NewznabStandardCategory.BooksOther.Id,
                NewznabStandardCategory.BooksTechnical.Id,
            };
        }

        [FieldDefinition(0, Label = "Prowlarr Server", HelpText = "Prowlarr server URL as LazyLibrarian sees it, including http(s)://, port, and urlbase if needed", Placeholder = "http://localhost:9696")]
        public string ProwlarrUrl { get; set; }

        [FieldDefinition(1, Label = "LazyLibrarian Server", HelpText = "URL used to connect to LazyLibrarian server, including http(s)://, port, and urlbase if required", Placeholder = "http://localhost:5299")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "ApiKey", Privacy = PrivacyLevel.ApiKey, HelpText = "The ApiKey generated by LazyLibrarian in Settings/Web Interface")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "Sync Categories", Type = FieldType.Select, SelectOptions = typeof(NewznabCategoryFieldConverter), Advanced = true, HelpText = "Only Indexers that support these categories will be synced")]
        public IEnumerable<int> SyncCategories { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
