using NzbDrone.Common.Messaging;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers
{
    public class IndexerQueryEvent : IEvent
    {
        public int IndexerId { get; set; }
        public SearchCriteriaBase Query { get; set; }
        public long Time { get; set; }
        public bool Successful { get; set; }
        public int? Results { get; set; }

        public IndexerQueryEvent(int indexerId, SearchCriteriaBase query, long time, bool successful, int? results = null)
        {
            IndexerId = indexerId;
            Query = query;
            Time = time;
            Successful = successful;
            Results = results;
        }
    }
}
