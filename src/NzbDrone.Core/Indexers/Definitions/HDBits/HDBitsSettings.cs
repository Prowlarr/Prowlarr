using System;
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
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class HDBitsSettings : NoAuthTorrentBaseSettings
    {
        private static readonly HDBitsSettingsValidator Validator = new ();

        public HDBitsSettings()
        {
            Codecs = Array.Empty<int>();
            Mediums = Array.Empty<int>();
            Origins = Array.Empty<int>();
            FreeleechOnly = false;
            UseFilenames = true;
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "IndexerSettingsPasskey", HelpText = "IndexerHDBitsSettingsPasskeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(4, Label = "Codecs", Type = FieldType.Select, SelectOptions = typeof(HdBitsCodec), HelpText = "If unspecified, all options are used.", Advanced = true)]
        public IEnumerable<int> Codecs { get; set; }

        [FieldDefinition(5, Label = "Mediums", Type = FieldType.Select, SelectOptions = typeof(HdBitsMedium), HelpText = "If unspecified, all options are used.", Advanced = true)]
        public IEnumerable<int> Mediums { get; set; }

        [FieldDefinition(6, Label = "Origins", Type = FieldType.Select, SelectOptions = typeof(HdBitsOrigin), HelpText = "If unspecified, all options are used.", Advanced = true)]
        public IEnumerable<int> Origins { get; set; }

        [FieldDefinition(7, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Show freeleech releases only", Advanced = true)]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(8, Label = "Use Filenames", Type = FieldType.Checkbox, HelpText = "Check this option if you want to use torrent filenames as release titles")]
        public bool UseFilenames { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum HdBitsCodec
    {
        [FieldOption("H.264")]
        H264 = 1,
        [FieldOption("MPEG-2")]
        Mpeg2 = 2,
        [FieldOption("VC-1")]
        Vc1 = 3,
        [FieldOption("XviD")]
        Xvid = 4,
        [FieldOption("HEVC")]
        HEVC = 5
    }

    public enum HdBitsMedium
    {
        [FieldOption("Blu-ray/HD DVD")]
        Bluray = 1,
        [FieldOption("Encode")]
        Encode = 3,
        [FieldOption("Capture")]
        Capture = 4,
        [FieldOption("Remux")]
        Remux = 5,
        [FieldOption("WEB-DL")]
        WebDl = 6
    }

    public enum HdBitsOrigin
    {
        [FieldOption("Undefined")]
        Undefined = 0,
        [FieldOption("Internal")]
        Internal = 1
    }
}
