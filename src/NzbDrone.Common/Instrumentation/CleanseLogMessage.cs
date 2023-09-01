using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Instrumentation
{
    public class CleanseLogMessage
    {
        private static readonly Regex[] CleansingRules =
        {
            // Url
            new (@"(?<=[?&: ;])(apikey|api_key|(?:(?:access|api)[-_]?)?token|pass(?:key|wd)?|auth|authkey|user|u?id|api|[a-z_]*apikey|account|pid|pwd)=(?<secret>[^&=""]+?)(?=[ ""&=]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?<=[?& ;])[^=]*?(_?(?<!use|get_)token|username|passwo?rd)=(?<secret>[^&=]+?)(?= |&|$|;)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"rss(24h)?\.torrentleech\.org/(?!rss)(?<secret>[0-9a-z]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"torrentleech\.org/rss/download/[0-9]+/(?<secret>[0-9a-z]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"iptorrents\.com/[/a-z0-9?&;]*?(?:[?&;](u|tp)=(?<secret>[^&=;]+?))+(?= |;|&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"/fetch/[a-z0-9]{32}/(?<secret>[a-z0-9]{32})", RegexOptions.Compiled),
            new (@"getnzb.*?(?<=\?|&)(r)=(?<secret>[^&=]+?)(?= |&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"\b(\w*)?(_?(?<!use|get_)token|username|passwo?rd)=(?<secret>[^&=]+?)(?= |&|$|;)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?<=authkey = "")(?<secret>[^&=]+?)(?="")", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?<=beyond-hd\.[a-z]+/api/torrents/)(?<secret>[^&=][a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?<=beyond-hd\.[a-z]+/torrent/download/[\w\d-]+[.]\d+[.])(?<secret>[a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // UNIT3D
            new (@"(?<=[a-z0-9-]+\.[a-z]+/torrent/download/\d+\.)(?<secret>[^&=][a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Path
            new (@"""C:\\Users\\(?<secret>[^\""]+?)(\\|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"""/(home|Users)/(?<secret>[^/""]+?)(/|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Trackers Announce Keys; Designed for Qbit Json; should work for all in theory
            new (@"announce(\.php)?(/|%2f|%3fpasskey%3d)(?<secret>[a-z0-9]{16,})|(?<secret>[a-z0-9]{16,})(/|%2f)announce"),

            // NzbGet
            new (@"""Name""\s*:\s*""[^""]*(username|password)""\s*,\s*""Value""\s*:\s*""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Sabnzbd
            new (@"""[^""]*(username|password|api_?key|nzb_key)""\s*:\s*""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"""email_(account|to|from|pwd)""\s*:\s*""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // uTorrent
            new (@"\[""[a-z._]*(username|password)"",\d,""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"\[""(boss_key|boss_key_salt|proxy\.proxy)"",\d,""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Deluge
            new (@"auth.login\(""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // BroadcastheNet (;torrent_pass|torrents_notify_ is for MTV)
            new (@"""?method""?\s*:\s*""(getTorrents)"",\s*""?params""?\s*:\s*\[\s*""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"getTorrents\(""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?<=\?|&|;|=)(authkey|torrent_pass|torrents_notify)[_=](?<secret>[^&=]+?)(?=""|&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Plex
            new (@"(?<=\?|&)(X-Plex-Client-Identifier|X-Plex-Token)=(?<secret>[^&=]+?)(?= |&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Indexer Responses
            new (@"(?:avistaz|exoticaz|cinemaz|privatehd)\.[a-z]{2,3}/rss/download/(?<secret>[^&=]+?)/(?<secret>[^&=]+?)\.torrent", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"(?:animebytes)\.[a-z]{2,3}/torrent/[0-9]+/download/(?<secret>[^&=]+?)[""]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new (@"""(info_hash|token|((pass|rss)[- _]?key))"":""(?<secret>[^&=]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Applications
            new (@"""name"":""apikey"",""value"":""(?<secret>[^&=]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        private static readonly Regex CleanseRemoteIPRegex = new (@"(?:Auth-\w+(?<!Failure|Unauthorized) ip|from) (\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Compiled);

        public static string Cleanse(string message)
        {
            if (message.IsNullOrWhiteSpace())
            {
                return message;
            }

            foreach (var regex in CleansingRules)
            {
                message = regex.Replace(message, m =>
                {
                    var value = m.Value;
                    foreach (var capture in m.Groups["secret"].Captures.OfType<Capture>().Reverse())
                    {
                        value = value.Replace(capture.Index - m.Index, capture.Length, "(removed)");
                    }

                    return value;
                });
            }

            message = CleanseRemoteIPRegex.Replace(message, CleanseRemoteIP);

            return message;
        }

        private static string CleanseRemoteIP(Match match)
        {
            var group = match.Groups[1];
            var valueIP = group.Value;

            if (IPAddress.TryParse(valueIP, out var address) && !address.IsLocalAddress())
            {
                var prefix = match.Value.Substring(0, group.Index - match.Index);
                var postfix = match.Value.Substring(group.Index + group.Length - match.Index);
                var items = valueIP.Split('.');

                return $"{prefix}{items[0]}.*.*.{items[3]}{postfix}";
            }

            return match.Value;
        }
    }
}
