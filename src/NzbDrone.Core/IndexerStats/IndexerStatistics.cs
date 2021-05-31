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
        public int NumberOfRssQueries { get; set; }
        public int NumberOfAuthQueries { get; set; }
        public int NumberOfFailedQueries { get; set; }
        public int NumberOfFailedGrabs { get; set; }
        public int NumberOfFailedRssQueries { get; set; }
        public int NumberOfFailedAuthQueries { get; set; }
    }

    public class UserAgentStatistics : ResultSet
    {
        public string UserAgent { get; set; }
        public int NumberOfQueries { get; set; }
        public int NumberOfGrabs { get; set; }
    }

    public class HostStatistics : ResultSet
    {
        public string Host { get; set; }
        public int NumberOfQueries { get; set; }
        public int NumberOfGrabs { get; set; }
    }
}
