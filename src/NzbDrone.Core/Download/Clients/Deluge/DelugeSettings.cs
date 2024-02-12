using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Deluge
{
    public class DelugeSettingsValidator : AbstractValidator<DelugeSettings>
    {
        public DelugeSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);

            RuleFor(c => c.Category).Matches("^[-a-z0-9]*$").WithMessage("Allowed characters a-z, 0-9 and -");
        }
    }

    public class DelugeSettings : IProviderConfig
    {
        private static readonly DelugeSettingsValidator Validator = new DelugeSettingsValidator();

        public DelugeSettings()
        {
            Host = "localhost";
            Port = 8112;
            Password = "deluge";
            Category = "prowlarr";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "UseSsl", Type = FieldType.Checkbox, HelpText = "DownloadClientSettingsUseSslHelpText")]
        [FieldToken(TokenField.HelpText, "UseSsl", "clientName", "Deluge")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "UrlBase", Type = FieldType.Textbox, Advanced = true, HelpText = "DownloadClientDelugeSettingsUrlBaseHelpText")]
        [FieldToken(TokenField.HelpText, "UrlBase", "url", "http://[host]:[port]/[urlBase]/json")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(5, Label = "DefaultCategory", Type = FieldType.Textbox, HelpText = "DownloadClientSettingsDefaultCategoryHelpText")]
        public string Category { get; set; }

        [FieldDefinition(6, Label = "Priority", Type = FieldType.Select, SelectOptions = typeof(DelugePriority), HelpText = "DownloadClientSettingsPriorityItemHelpText")]
        public int Priority { get; set; }

        [FieldDefinition(7, Label = "DownloadClientSettingsAddPaused", Type = FieldType.Checkbox)]
        public bool AddPaused { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
