using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.IndexerStats;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerStatsController : Controller
    {
        private readonly IIndexerStatisticsService _indexerStatisticsService;

        public IndexerStatsController(IIndexerStatisticsService indexerStatisticsService)
        {
            _indexerStatisticsService = indexerStatisticsService;
        }

        [HttpGet]
        public IndexerStatsResource GetAll()
        {
            var indexerResource = new IndexerStatsResource
            {
                Indexers = _indexerStatisticsService.IndexerStatistics(),
                UserAgents = _indexerStatisticsService.UserAgentStatistics(),
                Hosts = _indexerStatisticsService.HostStatistics()
            };

            return indexerResource;
        }
    }
}
