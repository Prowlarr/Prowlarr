using System;

namespace NzbDrone.Core.Indexers.Definitions.TorrentPotato
{
    public class TorrentPotatoResponse
    {
        public int total_results { get; set; }
        public Result[] results { get; set; }
        public string error { get; set; }
    }

    public class Result
    {
        public string release_name { get; set; }
        public string torrent_id { get; set; }
        public string details_url { get; set; }
        public string download_url { get; set; }
        public string imdb_id { get; set; }
        public bool freeleech { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public int leechers { get; set; }
        public int seeders { get; set; }
        public DateTime publish_date { get; set; }
    }
}
