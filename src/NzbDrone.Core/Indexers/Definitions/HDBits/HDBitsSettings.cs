using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.HDBits
{
    public class HDBitsSettingsValidator : NoAuthSettingsValidator<HDBitsSettings>
    {
        public HDBitsSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class HDBitsSettings : NoAuthTorrentBaseSettings
    {
        private static readonly HDBitsSettingsValidator Validator = new ();

        public HDBitsSettings()
        {
            Codecs = System.Array.Empty<int>();
            Mediums = System.Array.Empty<int>();
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "API Key", HelpText = "Site API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(4, Label = "Codecs", Type = FieldType.TagSelect, SelectOptions = typeof(HdBitsCodec), Advanced = true, HelpText = "Options: h264, Mpeg2, VC1, Xvid. If unspecified, all options are used.")]
        public IEnumerable<int> Codecs { get; set; }

        [FieldDefinition(5, Label = "Mediums", Type = FieldType.TagSelect, SelectOptions = typeof(HdBitsMedium), Advanced = true, HelpText = "Options: BluRay, Encode, Capture, Remux, WebDL. If unspecified, all options are used.")]
        public IEnumerable<int> Mediums { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum HdBitsCategory
    {
        Movie = 1,
        Tv = 2,
        Documentary = 3,
        Music = 4,
        Sport = 5,
        Audio = 6,
        Xxx = 7,
        MiscDemo = 8
    }

    public enum HdBitsCodec
    {
        H264 = 1,
        Mpeg2 = 2,
        Vc1 = 3,
        Xvid = 4,
        HEVC = 5
    }

    public enum HdBitsMedium
    {
        Bluray = 1,
        Encode = 3,
        Capture = 4,
        Remux = 5,
        WebDl = 6
    }
}
