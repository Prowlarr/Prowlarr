using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Migration
{
    public class JackettIndexerConfigDefintion
    {
        //TODO: Update to ensure json matches up with Prowlarrs
        public string Cookie { get; set; }
        public string Username { get; set; }
        public string MFA { get; set; }
        public string Email { get; set; }
        public string Pid { get; set; }
        public string Pin { get; set;  }
        public string Password { get; set; }
        public string Passkey { get; set; }
        public string ApiKey { get; set; }
        public string RssKey { get; set; }
        public string CaptchaText { get; set; }
        public string CaptchaCookie { get; set; }
        public string Filters { get; set; }
    }
}
