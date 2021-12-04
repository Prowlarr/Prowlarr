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

            var indexerResource = new IndexerStatsResource
            {
                Indexers = _indexerStatisticsService.IndexerStatistics(statsStartDate, statsEndDate).IndexerStatistics,
                UserAgents = _indexerStatisticsService.IndexerStatistics(statsStartDate, statsEndDate).UserAgentStatistics,
                Hosts = _indexerStatisticsService.IndexerStatistics(statsStartDate, statsEndDate).HostStatistics
            };

            return indexerResource;
        }
    }
}
