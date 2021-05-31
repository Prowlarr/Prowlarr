using System.Collections.Generic;
using NzbDrone.Core.IndexerStats;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatsResource : RestResource
    {
        public List<IndexerStatistics> Indexers { get; set; }
        public List<UserAgentStatistics> UserAgents { get; set; }
        public List<HostStatistics> Hosts { get; set; }
    }
}
