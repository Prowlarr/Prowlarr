using System;
using System.Collections.Generic;
using System.Linq;
using MonoTorrent;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers
{
    public static class MagnetLinkBuilder
    {
        private static readonly List<string> Trackers = new ()
        {
            "udp://tracker.opentrackr.org:1337/announce",
            "https://tracker2.ctix.cn:443/announce",
            "https://tracker1.520.jp:443/announce",
            "udp://opentracker.i2p.rocks:6969/announce",
            "udp://open.tracker.cl:1337/announce",
            "udp://open.demonii.com:1337/announce",
            "udp://tracker.openbittorrent.com:6969/announce",
            "http://tracker.openbittorrent.com:80/announce",
            "udp://open.stealth.si:80/announce",
            "udp://exodus.desync.com:6969/announce",
            "udp://tracker.torrent.eu.org:451/announce",
            "udp://tracker1.bt.moack.co.kr:80/announce",
            "udp://tracker-udp.gbitt.info:80/announce",
            "udp://explodie.org:6969/announce",
            "https://tracker.gbitt.info:443/announce",
            "http://tracker.gbitt.info:80/announce",
            "http://bt.endpot.com:80/announce",
            "udp://tracker.tiny-vps.com:6969/announce",
            "udp://tracker.auctor.tv:6969/announce",
            "udp://tk1.trackerservers.com:8080/announce"
        };

        public static string BuildPublicMagnetLink(string infoHash, string releaseTitle)
        {
            return new MagnetLink(InfoHash.FromHex(infoHash), releaseTitle, Trackers).ToV1String();
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
