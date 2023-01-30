using Newtonsoft.Json;

namespace NzbDrone.Core.Indexers.Definitions.FileList;

public class FileListTorrent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public int Leechers { get; set; }
    public int Seeders { get; set; }
    [JsonProperty(PropertyName = "times_completed")]
    public uint TimesCompleted { get; set; }
    public uint Comments { get; set; }
    public uint Files { get; set; }
    [JsonProperty(PropertyName = "imdb")]
    public string ImdbId { get; set; }
    public bool Internal { get; set; }
    [JsonProperty(PropertyName = "freeleech")]
    public bool FreeLeech { get; set; }
    [JsonProperty(PropertyName = "doubleup")]
    public bool DoubleUp { get; set; }
    [JsonProperty(PropertyName = "upload_date")]
    public string UploadDate { get; set; }
    public string Category { get; set; }
    [JsonProperty(PropertyName = "small_description")]
    public string SmallDescription { get; set; }
}
