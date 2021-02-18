using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.IndexerStats
{
    public class IndexerStatistics : ResultSet
    {
        public int IndexerId { get; set; }
        public string IndexerName { get; set; }
        public int AverageResponseTime { get; set; }
        public int NumberOfQueries { get; set; }
        public int NumberOfGrabs { get; set; }
    }
}
