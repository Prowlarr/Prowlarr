using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrentSettingsValidator : AbstractValidator<RTorrentSettings>
    {
        public RTorrentSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.Category).NotEmpty()
                                      .WithMessage("A category is recommended")
                                      .AsWarning();
        }
    }

    public class RTorrentSettings : IProviderConfig
    {
        private static readonly RTorrentSettingsValidator Validator = new RTorrentSettingsValidator();

        public RTorrentSettings()
        {
            Host = "localhost";
            Port = 8080;
            UrlBase = "RPC2";
            Category = "prowlarr";
            Priority = (int)RTorrentPriority.Normal;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "UseSsl", Type = FieldType.Checkbox, HelpText = "DownloadClientSettingsUseSslHelpText")]
        [FieldToken(TokenField.HelpText, "UseSsl", "clientName", "rTorrent")]
        public bool UseSsl { get; set; }

        [FieldDefinition(3, Label = "DownloadClientRTorrentSettingsUrlPath", Type = FieldType.Textbox, HelpText = "DownloadClientRTorrentSettingsUrlPathHelpText")]
        [FieldToken(TokenField.HelpText, "DownloadClientRTorrentSettingsUrlPath", "url", "http(s)://[host]:[port]/[urlPath]")]
        [FieldToken(TokenField.HelpText, "DownloadClientRTorrentSettingsUrlPath", "url2", "/plugins/rpc/rpc.php")]
        public string UrlBase { get; set; }

        [FieldDefinition(4, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(5, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(6, Label = "DefaultCategory", Type = FieldType.Textbox, HelpText = "DownloadClientSettingsDefaultCategoryHelpText")]
        public string Category { get; set; }

        [FieldDefinition(7, Label = "Directory", Type = FieldType.Textbox, Advanced = true, HelpText = "DownloadClientRTorrentSettingsDirectoryHelpText")]
        public string Directory { get; set; }

        [FieldDefinition(8, Label = "Priority", Type = FieldType.Select, SelectOptions = typeof(RTorrentPriority), HelpText = "DownloadClientSettingsPriorityItemHelpText")]
        public int Priority { get; set; }

        [FieldDefinition(9, Label = "DownloadClientRTorrentSettingsAddStopped", Type = FieldType.Checkbox, HelpText = "DownloadClientRTorrentSettingsAddStoppedHelpText")]
        public bool AddStopped { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
