using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazRelease
    {
        public string Url { get; init; }
        public string Download { get; init; }
        public Dictionary<string, string> Category { get; init; }

        [JsonPropertyName("movie_tv")]
        public AvistazIdInfo MovieTvinfo { get; init; }

        [JsonPropertyName("created_at_iso")]
        public string CreatedAtIso { get; init; }

        [JsonPropertyName("file_name")]
        public string FileName { get; init; }

        [JsonPropertyName("release_title")]
        public string ReleaseTitle { get; init; }

        [JsonPropertyName("info_hash")]
        public string InfoHash { get; init; }

        public int? Leech { get; init; }
        public int? Completed { get; init; }
        public int? Seed { get; init; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; init; }

        [JsonPropertyName("file_count")]
        public int? FileCount { get; init; }

        [JsonPropertyName("download_multiply")]
        public double? DownloadMultiply { get; init; }

        [JsonPropertyName("upload_multiply")]
        public double? UploadMultiply { get; init; }

        [JsonPropertyName("video_quality")]
        public string VideoQuality { get; init; }

        public string Type { get; init; }

        public string Format { get; init; }

        public IReadOnlyCollection<AvistazLanguage> Audio { get; init; }
        public IReadOnlyCollection<AvistazLanguage> Subtitle { get; init; }
    }

    public class AvistazLanguage
    {
        public int Id { get; init; }
        public string Language { get; init; }
    }

    public class AvistazResponse
    {
        public IReadOnlyCollection<AvistazRelease> Data { get; init; }
    }

    public class AvistazErrorResponse
    {
        public string Message { get; init; }
    }

    public class AvistazIdInfo
    {
        public string Tmdb { get; init; }
        public string Tvdb { get; init; }
        public string Imdb { get; init; }
    }

    public class AvistazAuthResponse
    {
        public string Token { get; init; }
    }
}
