using System.Text;
using NzbDrone.Core.Download.Clients;

namespace NzbDrone.Core.Parser.Model
{
    public class TorrentInfo : ReleaseInfo
    {
        public string MagnetUrl { get; set; }
        public string InfoHash { get; set; }
        public int? Seeders { get; set; }
        public int? Peers { get; set; }
        public double? MinimumRatio { get; set; }
        public long? MinimumSeedTime { get; set; }
        public double? DownloadVolumeFactor { get; set; }
        public double? UploadVolumeFactor { get; set; }
        public bool? Scene { get; set; }

        public TorrentSeedConfiguration SeedConfiguration { get; set; }

        public static int? GetSeeders(ReleaseInfo release)
        {
            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return null;
            }

            return torrentInfo.Seeders;
        }

        public static int? GetPeers(ReleaseInfo release)
        {
            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return null;
            }

            return torrentInfo.Peers;
        }

        public override string ToString(string format)
        {
            var stringBuilder = new StringBuilder(base.ToString(format));
            switch (format.ToUpperInvariant())
            {
                case "L": // Long format
                    stringBuilder.AppendLine("MagnetUrl: " + MagnetUrl ?? "Empty");
                    stringBuilder.AppendLine("InfoHash: " + InfoHash ?? "Empty");
                    stringBuilder.AppendLine("Seeders: " + Seeders ?? "Empty");
                    stringBuilder.AppendLine("Peers: " + Peers ?? "Empty");
                    break;
            }

            return stringBuilder.ToString();
        }
    }
}
