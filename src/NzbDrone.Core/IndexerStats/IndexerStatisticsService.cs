using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.IndexerStats
{
    public interface IIndexerStatisticsService
    {
        CombinedStatistics IndexerStatistics(DateTime start, DateTime end);
    }

    public class IndexerStatisticsService : IIndexerStatisticsService
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IHistoryService _historyService;

        public IndexerStatisticsService(IHistoryService historyService, IIndexerFactory indexerFactory)
        {
            _historyService = historyService;
            _indexerFactory = indexerFactory;
        }

        public CombinedStatistics IndexerStatistics(DateTime start, DateTime end)
        {
            var history = _historyService.Between(start, end);

            var groupedByIndexer = history.GroupBy(h => h.IndexerId);
            var groupedByUserAgent = history.GroupBy(h => h.Data.GetValueOrDefault("source") ?? "");
            var groupedByHost = history.GroupBy(h => h.Data.GetValueOrDefault("host") ?? "");

            var indexerStatsList = new List<IndexerStatistics>();
            var userAgentStatsList = new List<UserAgentStatistics>();
            var hostStatsList = new List<HostStatistics>();

            var indexers = _indexerFactory.All();

            foreach (var indexer in groupedByIndexer)
            {
                var indexerDef = indexers.SingleOrDefault(i => i.Id == indexer.Key);

                if (indexerDef == null)
                {
                    continue;
                }

                var indexerStats = new IndexerStatistics
                {
                    IndexerId = indexer.Key,
                    IndexerName = indexerDef.Name
                };

                var sortedEvents = indexer.OrderBy(v => v.Date)
                    .ThenBy(v => v.Id)
                    .ToArray();

                var temp = 0;
                var elapsedTimeEvents = sortedEvents
                    .Where(h => int.TryParse(h.Data.GetValueOrDefault("elapsedTime"), out temp) && h.Data.GetValueOrDefault("cached") != "1")
                    .Select(h => temp)
                    .ToArray();

                indexerStats.AverageResponseTime = elapsedTimeEvents.Any() ? (int)elapsedTimeEvents.Average() : 0;

                foreach (var historyEvent in sortedEvents)
                {
                    var failed = !historyEvent.Successful;
                    switch (historyEvent.EventType)
                    {
                        case HistoryEventType.IndexerQuery:
                            indexerStats.NumberOfQueries++;
                            if (failed)
                            {
                                indexerStats.NumberOfFailedQueries++;
                            }

                            break;
                        case HistoryEventType.IndexerAuth:
                            indexerStats.NumberOfAuthQueries++;
                            if (failed)
                            {
                                indexerStats.NumberOfFailedAuthQueries++;
                            }

                            break;
                        case HistoryEventType.ReleaseGrabbed:
                            indexerStats.NumberOfGrabs++;
                            if (failed)
                            {
                                indexerStats.NumberOfFailedGrabs++;
                            }

                            break;
                        case HistoryEventType.IndexerRss:
                            indexerStats.NumberOfRssQueries++;
                            if (failed)
                            {
                                indexerStats.NumberOfFailedRssQueries++;
                            }

                            break;
                        default:
                            break;
                    }
                }

                indexerStatsList.Add(indexerStats);
            }

            foreach (var indexer in groupedByUserAgent)
            {
                var indexerStats = new UserAgentStatistics
                {
                    UserAgent = indexer.Key
                };

                var sortedEvents = indexer.OrderBy(v => v.Date)
                                          .ThenBy(v => v.Id)
                                          .ToArray();

                foreach (var historyEvent in sortedEvents)
                {
                    switch (historyEvent.EventType)
                    {
                        case HistoryEventType.IndexerRss:
                        case HistoryEventType.IndexerQuery:
                            indexerStats.NumberOfQueries++;

                            break;
                        case HistoryEventType.ReleaseGrabbed:
                            indexerStats.NumberOfGrabs++;
                            break;
                        default:
                            break;
                    }
                }

                userAgentStatsList.Add(indexerStats);
            }

            foreach (var indexer in groupedByHost)
            {
                var indexerStats = new HostStatistics
                {
                    Host = indexer.Key
                };

                var sortedEvents = indexer.OrderBy(v => v.Date)
                                          .ThenBy(v => v.Id)
                                          .ToArray();

                foreach (var historyEvent in sortedEvents)
                {
                    switch (historyEvent.EventType)
                    {
                        case HistoryEventType.IndexerRss:
                        case HistoryEventType.IndexerQuery:
                            indexerStats.NumberOfQueries++;
                            break;
                        case HistoryEventType.ReleaseGrabbed:
                            indexerStats.NumberOfGrabs++;
                            break;
                        default:
                            break;
                    }
                }

                hostStatsList.Add(indexerStats);
            }

            return new CombinedStatistics
            {
                IndexerStatistics = indexerStatsList,
                UserAgentStatistics = userAgentStatsList,
                HostStatistics = hostStatsList
            };
        }
    }
}
