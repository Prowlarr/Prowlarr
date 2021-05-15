using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser
{
    public static class UserAgentParser
    {
        private static readonly List<string> UserAgentWhiteList = new List<string> { "Radarr", "Sonarr", "Readarr", "Lidarr", "Prowlarr" };

        private static readonly Regex AppSourceRegex = new Regex(@"(?<agent>.*)\/.*(\(.*\))?",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string ParseSource(string userAgent)
        {
            var match = AppSourceRegex.Match(userAgent);

            if (match.Groups["agent"].Success)
            {
                if (UserAgentWhiteList.Contains(match.Groups["agent"].Value))
                {
                    return match.Groups["agent"].Value;
                }
            }

            return "Other";
        }
    }
}
