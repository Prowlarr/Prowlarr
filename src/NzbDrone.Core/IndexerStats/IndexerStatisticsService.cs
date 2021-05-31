using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.IndexerStats
{
    public interface IIndexerStatisticsService
    {
        List<IndexerStatistics> IndexerStatistics();
        List<UserAgentStatistics> UserAgentStatistics();
        List<HostStatistics> HostStatistics();
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
            var indexerStatistics = _indexerStatisticsRepository.IndexerStatistics();

            return indexerStatistics.ToList();
        }

        public List<UserAgentStatistics> UserAgentStatistics()
        {
            var userAgentStatistics = _indexerStatisticsRepository.UserAgentStatistics();

            return userAgentStatistics.ToList();
        }

        public List<HostStatistics> HostStatistics()
        {
            var hostStatistics = _indexerStatisticsRepository.HostStatistics();

            return hostStatistics.ToList();
        }
    }
}
