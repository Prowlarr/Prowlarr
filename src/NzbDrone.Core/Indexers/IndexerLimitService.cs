using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerLimitService
    {
        bool AtDownloadLimit(IndexerDefinition indexer);
        bool AtQueryLimit(IndexerDefinition indexer);
    }

    public class IndexerLimitService : IIndexerLimitService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;

        public IndexerLimitService(IEventAggregator eventAggregator,
                                IHistoryService historyService,
                                Logger logger)
        {
            _eventAggregator = eventAggregator;
            _historyService = historyService;
            _logger = logger;
        }

        public bool AtDownloadLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit.HasValue)
            {
                var queryCount = _historyService.CountSince(indexer.Id, DateTime.Now.StartOfDay(), new List<HistoryEventType> { HistoryEventType.ReleaseGrabbed });

                if (queryCount > ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit)
                {
                    _logger.Info("Indexer {0} has exceeded maximum grab limit for today", indexer.Name);

                    return true;
                }
            }

            return false;
        }

        public bool AtQueryLimit(IndexerDefinition indexer)
        {
            if (indexer.Id > 0 && ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit.HasValue)
            {
                var queryCount = _historyService.CountSince(indexer.Id, DateTime.Now.StartOfDay(), new List<HistoryEventType> { HistoryEventType.IndexerQuery, HistoryEventType.IndexerRss });

                if (queryCount > ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit)
                {
                    _logger.Info("Indexer {0} has exceeded maximum query limit for today", indexer.Name);

                    return true;
                }
            }

            return false;
        }
    }
}
