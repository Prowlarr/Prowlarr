using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazRelease
    {
        public string Url { get; set; }
        public string Download { get; set; }
        public Dictionary<string, string> Category { get; set; }

        [JsonPropertyName("movie_tv")]
        public AvistazIdInfo MovieTvinfo { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("info_hash")]
        public string InfoHash { get; set; }
        public int? Leech { get; set; }
        public int? Completed { get; set; }
        public int? Seed { get; set; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        [JsonPropertyName("file_count")]
        public int? FileCount { get; set; }

        [JsonPropertyName("download_multiply")]
        public double? DownloadMultiply { get; set; }

        [JsonPropertyName("upload_multiply")]
        public double? UploadMultiply { get; set; }

        [JsonPropertyName("video_quality")]
        public string VideoQuality { get; set; }
        public string Type { get; set; }
        public List<AvistazLanguage> Audio { get; set; }
        public List<AvistazLanguage> Subtitle { get; set; }
    }

    public class AvistazLanguage
    {
        public int Id { get; set; }
        public string Language { get; set; }
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
    }

    public class AvistazAuthResponse
    {
        public string Token { get; set; }
    }
}
