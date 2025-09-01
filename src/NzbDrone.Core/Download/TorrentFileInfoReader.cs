using MonoTorrent;
using System;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Download
{
    public interface ITorrentFileInfoReader
    {
        string GetHashFromTorrentFile(byte[] fileContents);
    }

    public class TorrentFileInfoReader : ITorrentFileInfoReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string GetHashFromTorrentFile(byte[] fileContents)
        {
            try
            {
                var torrent = Torrent.Load(fileContents);
                var infoHash = torrent.InfoHash.ToHex();
                
                // Check if we got a valid infohash (not all zeros or empty)
                if (string.IsNullOrEmpty(infoHash) || infoHash.All(c => c == '0'))
                {
                    Logger.Debug("Torrent file contains invalid or missing infohash, returning empty string");
                    return string.Empty;
                }
                
                return infoHash;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to extract infohash from torrent file, returning empty string");
                return string.Empty;
            }
        }
    }
}
