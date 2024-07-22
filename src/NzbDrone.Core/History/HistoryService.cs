using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
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
            var response = message.QueryResult.Response;

            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.IndexerId,
                EventType = message.Query.IsRssSearch ? HistoryEventType.IndexerRss : HistoryEventType.IndexerQuery,
                Successful = response?.StatusCode == HttpStatusCode.OK || (response is { Request: { SuppressHttpError: true, SuppressHttpErrorStatusCodes: not null } } && response.Request.SuppressHttpErrorStatusCodes.Contains(response.StatusCode))
            };

            if (message.Query is MovieSearchCriteria movieSearchCriteria)
            {
                history.Data.Add("ImdbId", movieSearchCriteria.FullImdbId);
                history.Data.Add("TmdbId", movieSearchCriteria.TmdbId?.ToString());
                history.Data.Add("TraktId", movieSearchCriteria.TraktId?.ToString());
                history.Data.Add("Year", movieSearchCriteria.Year?.ToString());
                history.Data.Add("Genre", movieSearchCriteria.Genre);
            }

            if (message.Query is TvSearchCriteria tvSearchCriteria)
            {
                history.Data.Add("ImdbId", tvSearchCriteria.FullImdbId);
                history.Data.Add("TvdbId", tvSearchCriteria.TvdbId?.ToString());
                history.Data.Add("TmdbId", tvSearchCriteria.TmdbId?.ToString());
                history.Data.Add("TraktId", tvSearchCriteria.TraktId?.ToString());
                history.Data.Add("RId", tvSearchCriteria.RId?.ToString());
                history.Data.Add("TvMazeId", tvSearchCriteria.TvMazeId?.ToString());
                history.Data.Add("Season", tvSearchCriteria.Season?.ToString());
                history.Data.Add("Episode", tvSearchCriteria.Episode);
                history.Data.Add("Year", tvSearchCriteria.Year?.ToString());
                history.Data.Add("Genre", tvSearchCriteria.Genre);
            }

            if (message.Query is MusicSearchCriteria musicSearchCriteria)
            {
                history.Data.Add("Artist", musicSearchCriteria.Artist);
                history.Data.Add("Album", musicSearchCriteria.Album);
                history.Data.Add("Track", musicSearchCriteria.Track);
                history.Data.Add("Label", musicSearchCriteria.Label);
                history.Data.Add("Year", musicSearchCriteria.Year?.ToString());
                history.Data.Add("Genre", musicSearchCriteria.Genre);
            }

            if (message.Query is BookSearchCriteria bookSearchCriteria)
            {
                history.Data.Add("Author", bookSearchCriteria.Author);
                history.Data.Add("Title", bookSearchCriteria.Title);
                history.Data.Add("Publisher", bookSearchCriteria.Publisher);
                history.Data.Add("Year", bookSearchCriteria.Year?.ToString());
                history.Data.Add("Genre", bookSearchCriteria.Genre);
            }

            history.Data.Add("Limit", message.Query.Limit?.ToString());
            history.Data.Add("Offset", message.Query.Offset?.ToString());

            // Clean empty data
            history.Data = history.Data.Where(d => d.Value != null).ToDictionary(x => x.Key, x => x.Value);

            history.Data.Add("ElapsedTime", message.QueryResult.Cached ? "0" : message.QueryResult.Response?.ElapsedTime.ToString() ?? string.Empty);
            history.Data.Add("Query", message.Query.SearchTerm ?? string.Empty);
            history.Data.Add("QueryType", message.Query.SearchType ?? string.Empty);
            history.Data.Add("Categories", string.Join(",", message.Query.Categories ?? Array.Empty<int>()));
            history.Data.Add("Source", message.Query.Source ?? string.Empty);
            history.Data.Add("Host", message.Query.Host ?? string.Empty);
            history.Data.Add("QueryResults", message.QueryResult.Releases?.Count.ToString() ?? string.Empty);
            history.Data.Add("Url", message.QueryResult.Response?.Request.Url.FullUri ?? string.Empty);
            history.Data.Add("Cached", message.QueryResult.Cached ? "1" : "0");

            _historyRepository.Insert(history);
        }

        public void Handle(IndexerDownloadEvent message)
        {
            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.Release.IndexerId,
                EventType = HistoryEventType.ReleaseGrabbed,
                Successful = message.Successful
            };

            history.Data.Add("Source", message.Source ?? string.Empty);
            history.Data.Add("Host", message.Host ?? string.Empty);
            history.Data.Add("GrabMethod", message.Redirect ? "Redirect" : "Proxy");
            history.Data.Add("GrabTitle", message.Title);
            history.Data.Add("Url", message.Url ?? string.Empty);

            if (message.ElapsedTime > 0)
            {
                history.Data.Add("ElapsedTime", message.ElapsedTime.ToString());
            }

            if (message.Release.InfoUrl.IsNotNullOrWhiteSpace())
            {
                history.Data.Add("InfoUrl", message.Release.InfoUrl);
            }

            if (message.DownloadClient.IsNotNullOrWhiteSpace() || message.DownloadClientName.IsNotNullOrWhiteSpace())
            {
                history.Data.Add("DownloadClient", message.DownloadClient ?? string.Empty);
                history.Data.Add("DownloadClientName", message.DownloadClientName ?? string.Empty);
            }

            if (message.Release.PublishDate != DateTime.MinValue)
            {
                history.Data.Add("PublishedDate", message.Release.PublishDate.ToString("s") + "Z");
            }

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

            history.Data.Add("ElapsedTime", message.ElapsedTime.ToString());

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
