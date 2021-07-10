using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListSettingsValidator : AbstractValidator<FileListSettings>
    {
        public FileListSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Passkey).NotEmpty();
        }
    }

    public class FileListSettings : IIndexerSettings
    {
        private static readonly FileListSettingsValidator Validator = new FileListSettingsValidator();

        public FileListSettings()
        {
            BaseUrl = "https://filelist.io";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Site Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum FileListCategories
    {
        [FieldOption]
        Movie_SD = 1,
        [FieldOption]
        Movie_DVD = 2,
        [FieldOption]
        Movie_DVDRO = 3,
        [FieldOption]
        Movie_HD = 4,
        [FieldOption]
        Movie_HDRO = 19,
        [FieldOption]
        Movie_BluRay = 20,
        [FieldOption]
        Movie_BluRay4K = 26,
        [FieldOption]
        Movie_3D = 25,
        [FieldOption]
        Movie_4K = 6,
        [FieldOption]
        Xxx = 7
    }
}
