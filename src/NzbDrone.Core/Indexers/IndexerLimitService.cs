using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerLimitService
    {
        bool AtDownloadLimit(IndexerDefinition indexer);
        bool AtQueryLimit(IndexerDefinition indexer);
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
                var grabCount = _historyService.CountSince(indexer.Id, DateTime.Now.StartOfDay(), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed });
                var grabLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit;

                if (grabCount > grabLimit)
                {
                    _logger.Info("Indexer {0} has exceeded maximum grab limit for today", indexer.Name);

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} grabs for today, proceeding", indexer.Name, grabCount, grabLimit);
            }

            return false;
        }

        public bool AtQueryLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var queryCount = _historyService.CountSince(indexer.Id, DateTime.Now.StartOfDay(), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss });
                var queryLimit = ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit;

                if (queryCount > queryLimit)
                {
                    _logger.Info("Indexer {0} has exceeded maximum query limit for today", indexer.Name);

                    return true;
                }

                _logger.Debug("Indexer {0} has performed {1} of possible {2} queries for today, proceeding", indexer.Name, queryCount, queryLimit);
            }

            return false;
        }
    }
}
