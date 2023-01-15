using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.QBittorrent
{
    public class QBittorrentSettingsValidator : AbstractValidator<QBittorrentSettings>
    {
        public QBittorrentSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.UrlBase).ValidUrlBase().When(c => c.UrlBase.IsNotNullOrWhiteSpace());

            RuleFor(c => c.Category).Matches(@"^([^\\\/](\/?[^\\\/])*)?$").WithMessage(@"Can not contain '\', '//', or start/end with '/'");
        }
    }

    public class QBittorrentSettings : IProviderConfig
    {
        private static readonly QBittorrentSettingsValidator Validator = new QBittorrentSettingsValidator();

        public QBittorrentSettings()
        {
            Host = "localhost";
            Port = 8080;
            Category = "prowlarr";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Use a secure connection. See Options -> Web UI -> 'Use HTTPS instead of HTTP' in qBittorrent.")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "Url Base", Type = FieldType.Textbox, Advanced = true, HelpText = "Adds a prefix to the qBittorrent url, e.g. http://[host]:[port]/[urlBase]/api")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "Default Category", Type = FieldType.Textbox, HelpText = "Default fallback category if no mapped category exists for a release. Adding a category specific to Prowlarr avoids conflicts with unrelated downloads, but it's optional")]
        public string Category { get; set; }

        [FieldDefinition(7, Label = "Priority", Type = FieldType.Select, SelectOptions = typeof(QBittorrentPriority), HelpText = "Priority to use when grabbing items")]
        public int Priority { get; set; }

        [FieldDefinition(8, Label = "Initial State", Type = FieldType.Select, SelectOptions = typeof(QBittorrentState), HelpText = "Initial state for torrents added to qBittorrent")]
        public int InitialState { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
