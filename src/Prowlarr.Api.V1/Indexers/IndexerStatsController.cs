using System;
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
        public IndexerStatsResource GetAll(DateTime? startDate, DateTime? endDate)
        {
            var statsStartDate = startDate ?? DateTime.MinValue;
            var statsEndDate = endDate ?? DateTime.Now;

            var indexerStats = _indexerStatisticsService.IndexerStatistics(statsStartDate, statsEndDate);

            var indexerResource = new IndexerStatsResource
            {
                Indexers = indexerStats.IndexerStatistics,
                UserAgents = indexerStats.UserAgentStatistics,
                Hosts = indexerStats.HostStatistics
            };

            return indexerResource;
        }
    }
}
