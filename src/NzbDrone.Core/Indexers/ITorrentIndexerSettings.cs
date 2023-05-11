namespace NzbDrone.Core.Indexers
{
    public interface ITorrentIndexerSettings : IIndexerSettings
    {
        IndexerTorrentBaseSettings TorrentBaseSettings { get; set; }
    }
}
