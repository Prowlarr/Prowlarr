using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerLimitService
    {
        bool AtDownloadLimit(IndexerDefinition indexer);
        bool AtQueryLimit(IndexerDefinition indexer);
        int CalculateRetryAfterDownloadLimit(IndexerDefinition indexer);
        int CalculateRetryAfterQueryLimit(IndexerDefinition indexer);
    }

    public class IndexerLimitService : IIndexerLimitService
    {
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;

        public IndexerLimitService(IHistoryService historyService,
                                   Logger logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public bool AtDownloadLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.HasValue)
            {
                var grabCount = _historyService.CountSince(indexer.Id, DateTime.Now.AddHours(-24), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed });
                var grabLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit;

                if (grabCount >= grabLimit)
                {
                    _logger.Info("Indexer {0} has performed {1} of possible {2} grabs in last 24 hours, exceeding the maximum grab limit", indexer.Name, grabCount, grabLimit);

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} grabs in last 24 hours, proceeding", indexer.Name, grabCount, grabLimit);
            }

            return false;
        }

        public bool AtQueryLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var queryCount = _historyService.CountSince(indexer.Id, DateTime.Now.AddHours(-24), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss });
                var queryLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit;

                if (queryCount >= queryLimit)
                {
                    _logger.Info("Indexer {0} has performed {1} of possible {2} queries in last 24 hours, exceeding the maximum query limit", indexer.Name, queryCount, queryLimit);

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} queries in last 24 hours, proceeding", indexer.Name, queryCount, queryLimit);
            }

            return false;
        }

        public int CalculateRetryAfterDownloadLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.HasValue)
            {
                var grabLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.GetValueOrDefault();

                var firstHistorySince = _historyService.FindFirstForIndexerSince(indexer.Id, DateTime.Now.AddHours(-24), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed }, grabLimit);

                if (firstHistorySince != null)
                {
                    return Convert.ToInt32(firstHistorySince.Date.ToLocalTime().AddHours(24).Subtract(DateTime.Now).TotalSeconds);
                }
            }

            return 0;
        }

        public int CalculateRetryAfterQueryLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var queryLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.GetValueOrDefault();

                var firstHistorySince = _historyService.FindFirstForIndexerSince(indexer.Id, DateTime.Now.AddHours(-24), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss }, queryLimit);

                if (firstHistorySince != null)
                {
                    return Convert.ToInt32(firstHistorySince.Date.ToLocalTime().AddHours(24).Subtract(DateTime.Now).TotalSeconds);
                }
            }

            return 0;
        }
    }
}
