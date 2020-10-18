using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        PagingSpec<MovieHistory> Paged(PagingSpec<MovieHistory> pagingSpec);
        MovieHistory MostRecentForMovie(int movieId);
        MovieHistory MostRecentForDownloadId(string downloadId);
        MovieHistory Get(int historyId);
        List<MovieHistory> Find(string downloadId, MovieHistoryEventType eventType);
        List<MovieHistory> FindByDownloadId(string downloadId);
        List<MovieHistory> GetByMovieId(int movieId, MovieHistoryEventType? eventType);
        void UpdateMany(List<MovieHistory> toUpdate);
        List<MovieHistory> Since(DateTime date, MovieHistoryEventType? eventType);
    }

    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public PagingSpec<MovieHistory> Paged(PagingSpec<MovieHistory> pagingSpec)
        {
            return _historyRepository.GetPaged(pagingSpec);
        }

        public MovieHistory MostRecentForMovie(int movieId)
        {
            return _historyRepository.MostRecentForMovie(movieId);
        }

        public MovieHistory MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public MovieHistory Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<MovieHistory> Find(string downloadId, MovieHistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<MovieHistory> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public List<MovieHistory> GetByMovieId(int movieId, MovieHistoryEventType? eventType)
        {
            return _historyRepository.GetByMovieId(movieId, eventType);
        }

        public void UpdateMany(List<MovieHistory> toUpdate)
        {
            _historyRepository.UpdateMany(toUpdate);
        }

        public List<MovieHistory> Since(DateTime date, MovieHistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }
    }
}
