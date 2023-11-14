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
            FreeleechOnly = false;
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "API Key", HelpText = "Site API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(4, Label = "Codecs", Type = FieldType.Select, SelectOptions = typeof(HdBitsCodec), Advanced = true, HelpText = "If unspecified, all options are used.")]
        public IEnumerable<int> Codecs { get; set; }

        [FieldDefinition(5, Label = "Mediums", Type = FieldType.Select, SelectOptions = typeof(HdBitsMedium), Advanced = true, HelpText = "If unspecified, all options are used.")]
        public IEnumerable<int> Mediums { get; set; }

        [FieldDefinition(6, Label = "Freeleech Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "Show freeleech releases only")]
        public bool FreeleechOnly { get; set; }

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
}
