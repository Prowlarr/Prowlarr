using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Migration
{
    public class JackettIndexerConfigDefintion
    {
        public string id { get; set; }
        public string type { get; set; } //DO NOT THINK THIS IS REQUIRED
        public string name { get; set; } //Again doubt it's needed
        public string value { get; set; }

        //BELOW ARE THE DIFFERENT THINGS FROM NIT
        //TODO: Update to ensure json matches up with Prowlarrs
        public string Cookie { get; set; }
        public string Username { get; set; }
        public string 2FA { get; set; }
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
