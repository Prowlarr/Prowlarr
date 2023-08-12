using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerDownloadEvent : IEvent
    {
        public ReleaseInfo Release { get; set; }
        public bool Successful { get; set; }
        public string Source { get; set; }
        public string Host { get; set; }
        public string Title { get; set; }
        public bool Redirect { get; set; }
        public string Url { get; set; }
        public int DownloadClientId { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientName { get; set; }
        public string DownloadId { get; set; }
        public IIndexer Indexer { get; set; }
        public GrabTrigger GrabTrigger { get; set; }

        public IndexerDownloadEvent(ReleaseInfo release, bool successful, string source, string host, string title, string url)
        {
            Release = release;
            Successful = successful;
            Source = source;
            Host = host;
            Title = title;
            Url = url;
        }
    }

    public enum GrabTrigger
    {
        Api,
        Manual
    }
}
