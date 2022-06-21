using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazRelease
    {
        public string Url { get; set; }
        public string Download { get; set; }
        public Dictionary<string, string> Category { get; set; }

        [JsonProperty(PropertyName = "movie_tv")]
        public AvistazIdInfo MovieTvinfo { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "file_name")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "info_hash")]
        public string InfoHash { get; set; }
        public int? Leech { get; set; }
        public int? Completed { get; set; }
        public int? Seed { get; set; }

        [JsonProperty(PropertyName = "file_size")]
        public long? FileSize { get; set; }

        [JsonProperty(PropertyName = "file_count")]
        public int? FileCount { get; set; }

        [JsonProperty(PropertyName = "download_multiply")]
        public double? DownloadMultiply { get; set; }

        [JsonProperty(PropertyName = "upload_multiply")]
        public double? UploadMultiply { get; set; }

        [JsonProperty(PropertyName = "video_quality")]
        public string VideoQuality { get; set; }
        public string Type { get; set; }
    }

    public class AvistazResponse
    {
        public List<AvistazRelease> Data { get; set; }
    }

    public class AvistazErrorResponse
    {
        public string Message { get; set; }
    }

    public class AvistazIdInfo
    {
        public string Tmdb { get; set; }
        public string Tvdb { get; set; }
        public string Imdb { get; set; }
        public string Title { get; set; }

        [JsonProperty(PropertyName = "tv_episode")]
        public string TvEpisode { get; set; }

        [JsonProperty(PropertyName = "tv_season")]
        public string TVSeason { get; set; }

        [JsonProperty(PropertyName = "tv_full_season")]
        public bool TVFullSeason { get; set; }
    }

    public class AvistazAuthResponse
    {
        public string Token { get; set; }
        public string Expiry { get; set; }
    }
}
