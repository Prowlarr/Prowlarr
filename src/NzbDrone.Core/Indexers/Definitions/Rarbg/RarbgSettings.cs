using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class RarbgSettings : NoAuthTorrentBaseSettings
    {
        public RarbgSettings()
        {
            RankedOnly = false;
        }

        [FieldDefinition(2, Type = FieldType.Checkbox, Label = "Ranked Only", HelpText = "Only include ranked results.")]
        public bool RankedOnly { get; set; }

        [FieldDefinition(3, Type = FieldType.Captcha, Label = "CAPTCHA Token", HelpText = "CAPTCHA Clearance token used to handle CloudFlare Anti-DDOS measures on shared-ip VPNs.")]
        public string CaptchaToken { get; set; }
    }
}
