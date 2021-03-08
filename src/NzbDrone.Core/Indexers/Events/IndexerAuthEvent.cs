using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers.Events
{
    public class IndexerAuthEvent : IEvent
    {
        public int IndexerId { get; set; }
        public bool Successful { get; set; }
        public long Time { get; set; }

        public IndexerAuthEvent(int indexerId, bool successful, long time)
        {
            IndexerId = indexerId;
            Successful = successful;
            Time = time;
        }
    }
}
