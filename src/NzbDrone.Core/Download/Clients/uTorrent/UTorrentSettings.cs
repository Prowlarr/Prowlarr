using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.UTorrent
{
    public class UTorrentSettingsValidator : AbstractValidator<UTorrentSettings>
    {
        public UTorrentSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.UrlBase).ValidUrlBase().When(c => c.UrlBase.IsNotNullOrWhiteSpace());
            RuleFor(c => c.Category).NotEmpty();
        }
    }

    public class UTorrentSettings : IProviderConfig
    {
        private static readonly UTorrentSettingsValidator Validator = new UTorrentSettingsValidator();

        public UTorrentSettings()
        {
            Host = "localhost";
            Port = 8080;
            Category = "prowlarr";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Use secure connection when connecting to uTorrent")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Url Base", Type = FieldType.Textbox, Advanced = true, HelpText = "Adds a prefix to the uTorrent url, e.g. http://[host]:[port]/[urlBase]/api")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Default Category", Type = FieldType.Textbox, HelpText = "Default fallback category if no mapped category exists for a release. Adding a category specific to Prowlarr avoids conflicts with unrelated downloads, but it's optional")]
        public string Category { get; set; }

        [FieldDefinition(7, Label = "Recent Priority", Type = FieldType.Select, SelectOptions = typeof(UTorrentPriority), HelpText = "Priority to use when grabbing items")]
        public int Priority { get; set; }

        [FieldDefinition(8, Label = "Initial State", Type = FieldType.Select, SelectOptions = typeof(UTorrentState), HelpText = "Initial state for torrents added to uTorrent")]
        public int IntialState { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
