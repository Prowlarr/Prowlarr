using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        PagingSpec<History> Paged(PagingSpec<History> pagingSpec);
        History MostRecentForIndexer(int indexerId);
        History MostRecentForDownloadId(string downloadId);
        History Get(int historyId);
        List<History> Find(string downloadId, HistoryEventType eventType);
        List<History> FindByDownloadId(string downloadId);
        List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType);
        void UpdateMany(List<History> toUpdate);
        List<History> Since(DateTime date, HistoryEventType? eventType);
    }

    public class HistoryService : IHistoryService,
                                  IHandle<ProviderDeletedEvent<IIndexer>>,
                                  IHandle<IndexerQueryEvent>,
                                  IHandle<IndexerDownloadEvent>,
                                  IHandle<IndexerAuthEvent>,
                                  IExecute<CleanUpHistoryCommand>,
                                  IExecute<ClearHistoryCommand>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, IConfigService configService, Logger logger)
        {
            _historyRepository = historyRepository;
            _configService = configService;
            _logger = logger;
        }

        public PagingSpec<History> Paged(PagingSpec<History> pagingSpec)
        {
            return _historyRepository.GetPaged(pagingSpec);
        }

        public History MostRecentForIndexer(int indexerId)
        {
            return _historyRepository.MostRecentForIndexer(indexerId);
        }

        public History MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public History Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<History> Find(string downloadId, HistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<History> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType)
        {
            return _historyRepository.GetByIndexerId(indexerId, eventType);
        }

        public void UpdateMany(List<History> toUpdate)
        {
            _historyRepository.UpdateMany(toUpdate);
        }

        public List<History> Since(DateTime date, HistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }

        public void Cleanup()
        {
            var cleanupDays = _configService.HistoryCleanupDays;

            if (cleanupDays == 0)
            {
                _logger.Info("Automatic cleanup of History is disabled");
                return;
            }

            _logger.Info("Removing items older than {0} days from history", cleanupDays);

            _historyRepository.Cleanup(cleanupDays);

            _logger.Debug("History has been cleaned up.");
        }

        public void Handle(IndexerQueryEvent message)
        {
            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.IndexerId,
                EventType = message.Query.RssSearch ? HistoryEventType.IndexerRss : HistoryEventType.IndexerQuery,
                Successful = message.Successful
            };

            if (message.Query is MovieSearchCriteria)
            {
                history.Data.Add("ImdbId", ((MovieSearchCriteria)message.Query).FullImdbId ?? string.Empty);
                history.Data.Add("TmdbId", ((MovieSearchCriteria)message.Query).TmdbId?.ToString() ?? string.Empty);
                history.Data.Add("TraktId", ((MovieSearchCriteria)message.Query).TraktId?.ToString() ?? string.Empty);
            }

            if (message.Query is TvSearchCriteria)
            {
                history.Data.Add("ImdbId", ((TvSearchCriteria)message.Query).FullImdbId ?? string.Empty);
                history.Data.Add("TvdbId", ((TvSearchCriteria)message.Query).TvdbId?.ToString() ?? string.Empty);
                history.Data.Add("TraktId", ((TvSearchCriteria)message.Query).TraktId?.ToString() ?? string.Empty);
                history.Data.Add("RId", ((TvSearchCriteria)message.Query).RId?.ToString() ?? string.Empty);
                history.Data.Add("TvMazeId", ((TvSearchCriteria)message.Query).TvMazeId?.ToString() ?? string.Empty);
                history.Data.Add("Season", ((TvSearchCriteria)message.Query).Season?.ToString() ?? string.Empty);
                history.Data.Add("Episode", ((TvSearchCriteria)message.Query).Episode ?? string.Empty);
            }

            if (message.Query is MusicSearchCriteria)
            {
                history.Data.Add("Artist", ((MusicSearchCriteria)message.Query).Artist ?? string.Empty);
                history.Data.Add("Album", ((MusicSearchCriteria)message.Query).Album ?? string.Empty);
            }

            if (message.Query is BookSearchCriteria)
            {
                history.Data.Add("Author", ((BookSearchCriteria)message.Query).Author ?? string.Empty);
                history.Data.Add("BookTitle", ((BookSearchCriteria)message.Query).Title ?? string.Empty);
            }

            history.Data.Add("ElapsedTime", message.Time.ToString());
            history.Data.Add("Query", message.Query.SearchTerm ?? string.Empty);
            history.Data.Add("QueryType", message.Query.SearchType ?? string.Empty);
            history.Data.Add("Categories", string.Join(",", message.Query.Categories) ?? string.Empty);
            history.Data.Add("Source", message.Query.Source ?? string.Empty);
            history.Data.Add("Host", message.Query.Host ?? string.Empty);
            history.Data.Add("QueryResults", message.Results.HasValue ? message.Results.ToString() : null);

            _historyRepository.Insert(history);
        }

        public void Handle(IndexerDownloadEvent message)
        {
            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.IndexerId,
                EventType = HistoryEventType.ReleaseGrabbed,
                Successful = message.Successful
            };

            history.Data.Add("Source", message.Source ?? string.Empty);
            history.Data.Add("Host", message.Host ?? string.Empty);
            history.Data.Add("GrabMethod", message.Redirect ? "Redirect" : "Proxy");
            history.Data.Add("Title", message.Title);

            _historyRepository.Insert(history);
        }

        public void Handle(IndexerAuthEvent message)
        {
            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.IndexerId,
                EventType = HistoryEventType.IndexerAuth,
                Successful = message.Successful
            };

            history.Data.Add("ElapsedTime", message.Time.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(ProviderDeletedEvent<IIndexer> message)
        {
            _historyRepository.DeleteForIndexers(new List<int> { message.ProviderId });
        }

        public void Execute(CleanUpHistoryCommand message)
        {
            Cleanup();
        }

        public void Execute(ClearHistoryCommand message)
        {
            _historyRepository.Purge(vacuum: true);
        }
    }
}
