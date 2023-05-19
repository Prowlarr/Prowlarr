namespace NzbDrone.Core.Indexers.Definitions.TorrentRss
{
    public class TorrentRssIndexerParserSettings
    {
        public bool UseEZTVFormat { get; set; }
        public bool ParseSeedersInDescription { get; set; }
        public bool UseEnclosureUrl { get; set; }
        public bool UseEnclosureLength { get; set; }
        public bool ParseSizeInDescription { get; set; }
        public string SizeElementName { get; set; }
    }
}
