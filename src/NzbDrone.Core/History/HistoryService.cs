using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
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
                                  IHandle<IndexerQueryEvent>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
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

        public void Handle(IndexerQueryEvent message)
        {
            var history = new History
            {
                Date = DateTime.UtcNow,
                IndexerId = message.IndexerId,
                EventType = HistoryEventType.IndexerQuery
            };

            history.Data.Add("ElapsedTime", message.Time.ToString());
            history.Data.Add("Query", message.Query.SceneTitles.FirstOrDefault() ?? string.Empty);
            history.Data.Add("Successful", message.Successful.ToString());
            history.Data.Add("QueryResults", message.Results.HasValue ? message.Results.ToString() : null);

            _historyRepository.Insert(history);
        }

        public void Handle(ProviderDeletedEvent<IIndexer> message)
        {
            _historyRepository.DeleteForIndexers(new List<int> { message.ProviderId });
        }
    }
}
