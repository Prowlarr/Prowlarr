using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerCapsEvent : IEvent
    {
        public int IndexerId { get; set; }
        public bool Successful { get; set; }

        public IndexerCapsEvent(int indexerId, bool successful)
        {
            IndexerId = indexerId;
            Successful = successful;
        }
    }
}
