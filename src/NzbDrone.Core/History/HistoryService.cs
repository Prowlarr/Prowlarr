using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        List<History> Between(DateTime start, DateTime end);
        List<History> Since(DateTime date, HistoryEventType? eventType);
        int CountSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes);
        History FindFirstForIndexerSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes, int limit);
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

        public List<History> Between(DateTime start, DateTime end)
        {
            return _historyRepository.Between(start, end);
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
                Successful = message.QueryResult.Response?.StatusCode == HttpStatusCode.OK
            };

            if (message.Query is MovieSearchCriteria)
            {
                history.Data.Add("ImdbId", ((MovieSearchCriteria)message.Query).FullImdbId ?? string.Empty);
                history.Data.Add("TmdbId", ((MovieSearchCriteria)message.Query).TmdbId?.ToString() ?? string.Empty);
                history.Data.Add("TraktId", ((MovieSearchCriteria)message.Query).TraktId?.ToString() ?? string.Empty);
                history.Data.Add("Year", ((MovieSearchCriteria)message.Query).Year?.ToString() ?? string.Empty);
                history.Data.Add("Genre", ((MovieSearchCriteria)message.Query).Genre ?? string.Empty);
            }

            if (message.Query is TvSearchCriteria)
            {
                history.Data.Add("ImdbId", ((TvSearchCriteria)message.Query).FullImdbId ?? string.Empty);
                history.Data.Add("TvdbId", ((TvSearchCriteria)message.Query).TvdbId?.ToString() ?? string.Empty);
                history.Data.Add("TmdbId", ((TvSearchCriteria)message.Query).TmdbId?.ToString() ?? string.Empty);
                history.Data.Add("TraktId", ((TvSearchCriteria)message.Query).TraktId?.ToString() ?? string.Empty);
                history.Data.Add("RId", ((TvSearchCriteria)message.Query).RId?.ToString() ?? string.Empty);
                history.Data.Add("TvMazeId", ((TvSearchCriteria)message.Query).TvMazeId?.ToString() ?? string.Empty);
                history.Data.Add("Season", ((TvSearchCriteria)message.Query).Season?.ToString() ?? string.Empty);
                history.Data.Add("Episode", ((TvSearchCriteria)message.Query).Episode ?? string.Empty);
                history.Data.Add("Year", ((TvSearchCriteria)message.Query).Year?.ToString() ?? string.Empty);
                history.Data.Add("Genre", ((TvSearchCriteria)message.Query).Genre ?? string.Empty);
            }

            if (message.Query is MusicSearchCriteria)
            {
                history.Data.Add("Artist", ((MusicSearchCriteria)message.Query).Artist ?? string.Empty);
                history.Data.Add("Album", ((MusicSearchCriteria)message.Query).Album ?? string.Empty);
                history.Data.Add("Track", ((MusicSearchCriteria)message.Query).Track ?? string.Empty);
                history.Data.Add("Label", ((MusicSearchCriteria)message.Query).Label ?? string.Empty);
                history.Data.Add("Year", ((MusicSearchCriteria)message.Query).Year?.ToString() ?? string.Empty);
                history.Data.Add("Genre", ((MusicSearchCriteria)message.Query).Genre ?? string.Empty);
            }

            if (message.Query is BookSearchCriteria)
            {
                history.Data.Add("Author", ((BookSearchCriteria)message.Query).Author ?? string.Empty);
                history.Data.Add("BookTitle", ((BookSearchCriteria)message.Query).Title ?? string.Empty);
                history.Data.Add("Publisher", ((BookSearchCriteria)message.Query).Publisher ?? string.Empty);
                history.Data.Add("Year", ((BookSearchCriteria)message.Query).Year?.ToString() ?? string.Empty);
                history.Data.Add("Genre", ((BookSearchCriteria)message.Query).Genre ?? string.Empty);
            }

            history.Data.Add("ElapsedTime", message.QueryResult.Response?.ElapsedTime.ToString() ?? string.Empty);
            history.Data.Add("Query", message.Query.SearchTerm ?? string.Empty);
            history.Data.Add("QueryType", message.Query.SearchType ?? string.Empty);
            history.Data.Add("Categories", string.Join(",", message.Query.Categories) ?? string.Empty);
            history.Data.Add("Source", message.Query.Source ?? string.Empty);
            history.Data.Add("Host", message.Query.Host ?? string.Empty);
            history.Data.Add("QueryResults", message.QueryResult.Releases?.Count().ToString() ?? string.Empty);
            history.Data.Add("Url", message.QueryResult.Response?.Request.Url.FullUri ?? string.Empty);

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
            history.Data.Add("Url", message.Url ?? string.Empty);

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

        public int CountSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes)
        {
            return _historyRepository.CountSince(indexerId, date, eventTypes);
        }

        public History FindFirstForIndexerSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes, int limit)
        {
            return _historyRepository.FindFirstForIndexerSince(indexerId, date, eventTypes, limit);
        }
    }
}
