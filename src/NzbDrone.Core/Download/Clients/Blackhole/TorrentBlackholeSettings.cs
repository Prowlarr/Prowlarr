using System.ComponentModel;
using FluentValidation;
using Newtonsoft.Json;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.Blackhole
{
    public class TorrentBlackholeSettingsValidator : AbstractValidator<TorrentBlackholeSettings>
    {
        public TorrentBlackholeSettingsValidator()
        {
            //Todo: Validate that the path actually exists
            RuleFor(c => c.TorrentFolder).IsValidPath();
            RuleFor(c => c.MagnetFileExtension).NotEmpty();
        }
    }

    public class TorrentBlackholeSettings : IProviderConfig
    {
        public TorrentBlackholeSettings()
        {
            MagnetFileExtension = ".magnet";
        }

        private static readonly TorrentBlackholeSettingsValidator Validator = new TorrentBlackholeSettingsValidator();

        [FieldDefinition(0, Label = "TorrentBlackholeTorrentFolder", Type = FieldType.Path, HelpText = "BlackholeFolderHelpText")]
        [FieldToken(TokenField.HelpText, "TorrentBlackholeTorrentFolder", "extension", ".torrent")]
        public string TorrentFolder { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [FieldDefinition(1, Label = "TorrentBlackholeSaveMagnetFiles", Type = FieldType.Checkbox, HelpText = "TorrentBlackholeSaveMagnetFilesHelpText")]
        public bool SaveMagnetFiles { get; set; }

        [FieldDefinition(2, Label = "TorrentBlackholeSaveMagnetFilesExtension", Type = FieldType.Textbox, HelpText = "TorrentBlackholeSaveMagnetFilesExtensionHelpText")]
        public string MagnetFileExtension { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
