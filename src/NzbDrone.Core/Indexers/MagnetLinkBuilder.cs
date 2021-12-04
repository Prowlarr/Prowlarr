using System;
using System.Collections.Generic;
using System.Linq;
using MonoTorrent;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers
{
    public static class MagnetLinkBuilder
    {
        private static readonly List<string> _trackers = new List<string>
        {
            "udp://tracker.opentrackr.org:1337/announce",
            "http://p4p.arenabg.com:1337/announce",
            "udp://9.rarbg.com:2810/announce",
            "udp://tracker.openbittorrent.com:6969/announce",
            "udp://www.torrent.eu.org:451/announce",
            "udp://tracker.torrent.eu.org:451/announce",
            "udp://retracker.lanta-net.ru:2710/announce",
            "udp://open.stealth.si:80/announce",
            "udp://exodus.desync.com:6969/announce",
            "http://openbittorrent.com:80/announce",
            "udp://opentracker.i2p.rocks:6969/announce",
            "udp://opentor.org:2710/announce",
            "udp://ipv4.tracker.harry.lu:80/announce",
            "udp://tracker.uw0.xyz:6969/announce",
            "udp://tracker.moeking.me:6969/announce",
            "udp://tracker.dler.org:6969/announce",
            "udp://explodie.org:6969/announce",
            "udp://bt2.archive.org:6969/announce",
            "udp://bt1.archive.org:6969/announce",
            "https://trakx.herokuapp.com:443/announce",
        };

        public static string BuildPublicMagnetLink(string infoHash, string releaseTitle)
        {
            return new MagnetLink(InfoHash.FromHex(infoHash), releaseTitle, _trackers).ToV1String();
        }

        public static string GetInfoHashFromMagnet(string magnet)
        {
            try
            {
                var xt = ParseUtil.GetArgumentFromQueryString(magnet.ToString(), "xt");
                return xt.Split(':').Last(); // remove prefix urn:btih:
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
