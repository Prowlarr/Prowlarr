using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NzbDrone.Core.IndexerStats;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatsModule : ProwlarrRestModule<IndexerStatsResource>
    {
        private readonly IIndexerStatisticsService _indexerStatisticsService;

        public IndexerStatsModule(IIndexerStatisticsService indexerStatisticsService)
        {
            _indexerStatisticsService = indexerStatisticsService;

            Get("/", x =>
            {
                return GetAll();
            });
        }

        private IndexerStatsResource GetAll()
        {
            var indexerResource = new IndexerStatsResource
            {
                Indexers = _indexerStatisticsService.IndexerStatistics(),
                UserAgents = _indexerStatisticsService.UserAgentStatistics(),
            };

            return indexerResource;
        }
    }
}
