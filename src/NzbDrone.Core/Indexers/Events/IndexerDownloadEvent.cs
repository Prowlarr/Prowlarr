using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerDownloadEvent : IEvent
    {
        public int IndexerId { get; set; }
        public bool Successful { get; set; }
        public string Source { get; set; }
        public string Host { get; set; }
        public string Title { get; set; }
        public bool Redirect { get; set; }
        public string Url { get; set; }

        public IndexerDownloadEvent(int indexerId, bool successful, string source, string host, string title, string url, bool redirect = false)
        {
            IndexerId = indexerId;
            Successful = successful;
            Source = source;
            Host = host;
            Title = title;
            Redirect = redirect;
            Url = url;
        }
    }
}
