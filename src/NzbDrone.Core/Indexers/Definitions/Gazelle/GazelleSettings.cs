using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleSettings : UserPassTorrentBaseSettings
    {
        public GazelleSettings()
        {
        }

        public string AuthKey;
        public string PassKey;

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Use Freeleech Token", HelpText = "Use Freeleech Token")]
        public bool UseFreeleechToken { get; set; }
    }
}
