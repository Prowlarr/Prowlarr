using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public class Unit3dResponse
    {
        public List<Unit3dTorrent> Data { get; set; }
        public Unit3dLinks Links { get; set; }
    }

    public class Unit3dTorrent
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public Unit3dTorrentAttributes Attributes { get; set; }
    }

    public class Unit3dTorrentAttributes
    {
        public string Name { get; set; }

        [JsonProperty(PropertyName = "release_year")]
        public int ReleaseYear { get; set; }
        public string Category { get; set; }
        public string Encode { get; set; }
        public string Resolution { get; set; }
        public long Size { get; set; }

        [JsonProperty(PropertyName = "num_file")]
        public int Files { get; set; }

        [JsonProperty(PropertyName = "times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty(PropertyName = "download_link")]
        public string DownloadLink { get; set; }

        [JsonProperty(PropertyName = "details_link")]
        public string DetailsLink { get; set; }

        [JsonProperty(PropertyName = "imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty(PropertyName = "tmdb_id")]
        public string TmdbId { get; set; }

        [JsonProperty(PropertyName = "tvdb_id")]
        public string TvdbId { get; set; }

        [JsonProperty(PropertyName = "igdb_id")]
        public string IgdbId { get; set; }

        [JsonProperty(PropertyName = "mal_id")]
        public string MalId { get; set; }

        [JsonProperty(PropertyName = "double_upload")]
        public bool DoubleUpload { get; set; }
        public bool Freeleech { get; set; }
        public string Uploader { get; set; }
    }

    public class Unit3dLinks
    {
        public string first { get; set; }
        public string last { get; set; }
        public string prev { get; set; }
        public string next { get; set; }
        public string self { get; set; }
    }
}
