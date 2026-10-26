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
        int CalculateIntervalLimitMinutes(IndexerDefinition indexer);
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
            if (indexer is { Id: > 0 } && ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.HasValue)
            {
                var intervalLimitMinutes = CalculateIntervalLimitMinutes(indexer);
                var grabCount = _historyService.CountSince(indexer.Id, DateTime.Now.AddMinutes(-1 * intervalLimitMinutes), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed });
                var grabLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit;

                if (grabCount >= grabLimit)
                {
                    _logger.Info("Indexer {0} has performed {1} of possible {2} grabs in last {3}, exceeding the maximum grab limit", indexer.Name, grabCount, grabLimit, FormatIntervalLimit(intervalLimitMinutes));

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} grabs in last {3}, proceeding", indexer.Name, grabCount, grabLimit, FormatIntervalLimit(intervalLimitMinutes));
            }

            return false;
        }

        public bool AtQueryLimit(IndexerDefinition indexer)
        {
            if (indexer is { Id: > 0 } && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var intervalLimitMinutes = CalculateIntervalLimitMinutes(indexer);
                var queryCount = _historyService.CountSince(indexer.Id, DateTime.Now.AddMinutes(-1 * intervalLimitMinutes), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss });
                var queryLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit;

                if (queryCount >= queryLimit)
                {
                    _logger.Info("Indexer {0} has performed {1} of possible {2} queries in last {3}, exceeding the maximum query limit", indexer.Name, queryCount, queryLimit, FormatIntervalLimit(intervalLimitMinutes));

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} queries in last {3}, proceeding", indexer.Name, queryCount, queryLimit, FormatIntervalLimit(intervalLimitMinutes));
            }

            return false;
        }

        public int CalculateRetryAfterDownloadLimit(IndexerDefinition indexer)
        {
            if (indexer is { Id: > 0 } && ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.HasValue)
            {
                var intervalLimitMinutes = CalculateIntervalLimitMinutes(indexer);
                var grabLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.GetValueOrDefault();

                var firstHistorySince = _historyService.FindFirstForIndexerSince(indexer.Id, DateTime.Now.AddMinutes(-1 * intervalLimitMinutes), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed }, grabLimit);

                if (firstHistorySince != null)
                {
                    return Convert.ToInt32(firstHistorySince.Date.ToLocalTime().AddMinutes(intervalLimitMinutes).Subtract(DateTime.Now).TotalSeconds);
                }
            }

            return 0;
        }

        public int CalculateRetryAfterQueryLimit(IndexerDefinition indexer)
        {
            if (indexer is { Id: > 0 } && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var intervalLimitMinutes = CalculateIntervalLimitMinutes(indexer);
                var queryLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.GetValueOrDefault();

                var firstHistorySince = _historyService.FindFirstForIndexerSince(indexer.Id, DateTime.Now.AddMinutes(-1 * intervalLimitMinutes), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss }, queryLimit);

                if (firstHistorySince != null)
                {
                    return Convert.ToInt32(firstHistorySince.Date.ToLocalTime().AddMinutes(intervalLimitMinutes).Subtract(DateTime.Now).TotalSeconds);
                }
            }

            return 0;
        }

        public int CalculateIntervalLimitMinutes(IndexerDefinition indexer)
        {
            if (indexer is { Id: > 0 })
            {
                return ((IIndexerSettings)indexer.Settings).BaseSettings.LimitsUnit switch
                {
                    (int)IndexerLimitsUnit.Minute => 1,
                    (int)IndexerLimitsUnit.Hour => 60,
                    _ => 1440
                };
            }

            // Fallback to limits per day
            return 1440;
        }

        public static string FormatIntervalLimit(int minutes)
        {
            return minutes switch
            {
                1440 => "1 day",
                60 => "1 hour",
                1 => "1 minute",
                _ => $"{minutes} minute(s)"
            };
        }
    }
}
