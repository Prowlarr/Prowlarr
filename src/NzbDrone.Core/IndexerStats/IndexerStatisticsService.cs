using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.IndexerStats
{
    public interface IIndexerStatisticsService
    {
        List<IndexerStatistics> IndexerStatistics();
        List<UserAgentStatistics> UserAgentStatistics();
    }

    public class IndexerStatisticsService : IIndexerStatisticsService
    {
        private readonly IIndexerStatisticsRepository _indexerStatisticsRepository;

        public IndexerStatisticsService(IIndexerStatisticsRepository indexerStatisticsRepository)
        {
            _indexerStatisticsRepository = indexerStatisticsRepository;
        }

        public List<IndexerStatistics> IndexerStatistics()
        {
            var seasonStatistics = _indexerStatisticsRepository.IndexerStatistics();

            return seasonStatistics.ToList();
        }

        public List<UserAgentStatistics> UserAgentStatistics()
        {
            var seasonStatistics = _indexerStatisticsRepository.UserAgentStatistics();

            return seasonStatistics.ToList();
        }
    }
}
