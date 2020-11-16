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

            GetResourceAll = GetAll;
        }

        private List<IndexerStatsResource> GetAll()
        {
            return _indexerStatisticsService.IndexerStatistics().ToResource();
        }
    }
}
