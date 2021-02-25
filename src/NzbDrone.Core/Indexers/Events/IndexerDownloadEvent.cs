using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerDownloadEvent : IEvent
    {
        public int IndexerId { get; set; }
        public bool Successful { get; set; }
        public string Source { get; set; }
        public string Title { get; set; }
        public bool Redirect { get; set; }

        public IndexerDownloadEvent(int indexerId, bool successful, string source, string title, bool redirect = false)
        {
            IndexerId = indexerId;
            Successful = successful;
            Source = source;
            Title = title;
            Redirect = redirect;
        }
    }
}
