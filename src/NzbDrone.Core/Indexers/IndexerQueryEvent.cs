using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Indexers
{
    public class IndexerQueryEvent : IEvent
    {
        public int IndexerId { get; set; }
        public string Query { get; set; }

        public IndexerQueryEvent(int indexerId, string query)
        {
            IndexerId = indexerId;
            Query = query;
        }
    }
}
