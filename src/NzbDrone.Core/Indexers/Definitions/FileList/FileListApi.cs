using System.Text.Json.Serialization;

namespace NzbDrone.Core.Indexers.Definitions.FileList;

public class FileListTorrent
{
    public uint Id { get; set; }

    public string Name { get; set; }

    public long Size { get; set; }

    public int Leechers { get; set; }

    public int Seeders { get; set; }

    [JsonPropertyName("times_completed")]
    public uint TimesCompleted { get; set; }

    public uint Files { get; set; }

    [JsonPropertyName("imdb")]
    public string ImdbId { get; set; }

    public bool Internal { get; set; }

    [JsonPropertyName("freeleech")]
    public bool FreeLeech { get; set; }

    [JsonPropertyName("doubleup")]
    public bool DoubleUp { get; set; }

    [JsonPropertyName("upload_date")]
    public string UploadDate { get; set; }

    public string Category { get; set; }

    [JsonPropertyName("small_description")]
    public string SmallDescription { get; set; }
}

public class FileListErrorResponse
{
    public string Error { get; set; }
}
