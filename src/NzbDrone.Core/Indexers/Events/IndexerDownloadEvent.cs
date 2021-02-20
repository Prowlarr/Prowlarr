using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerDownloadEvent : IEvent
    {
        public int IndexerId { get; set; }
        public bool Successful { get; set; }
        public string Source { get; set; }

        public IndexerDownloadEvent(int indexerId, bool successful, string source)
        {
            IndexerId = indexerId;
            Successful = successful;
            Source = source;
        }
    }
}
