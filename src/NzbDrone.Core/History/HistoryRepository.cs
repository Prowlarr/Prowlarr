using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<History>
    {
        History MostRecentForDownloadId(string downloadId);
        List<History> FindByDownloadId(string downloadId);
        List<History> FindDownloadHistory(int indexerId);
        List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType);
        void DeleteForIndexers(List<int> indexerIds);
        History MostRecentForIndexer(int indexerId);
        List<History> Since(DateTime date, HistoryEventType? eventType);
        void Cleanup(int days);
    }

    public class HistoryRepository : BasicRepository<History>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public History MostRecentForDownloadId(string downloadId)
        {
            return FindByDownloadId(downloadId)
                .OrderByDescending(h => h.Date)
                .FirstOrDefault();
        }

        public List<History> FindByDownloadId(string downloadId)
        {
            return Query(x => x.DownloadId == downloadId);
        }

        public List<History> FindDownloadHistory(int indexerId)
        {
            var allowed = new[] { HistoryEventType.ReleaseGrabbed };

            return Query(h => h.IndexerId == indexerId &&
                         allowed.Contains(h.EventType));
        }

        public List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType)
        {
            var query = Query(x => x.IndexerId == indexerId);

            if (eventType.HasValue)
            {
                query = query.Where(h => h.EventType == eventType).ToList();
            }

            return query.OrderByDescending(h => h.Date).ToList();
        }

        public void DeleteForIndexers(List<int> indexerIds)
        {
            Delete(c => indexerIds.Contains(c.IndexerId));
        }

        public void Cleanup(int days)
        {
            Delete(c => c.Date.AddDays(days) <= DateTime.Now);
        }

        public History MostRecentForIndexer(int indexerId)
        {
            return Query(x => x.IndexerId == indexerId)
                .OrderByDescending(h => h.Date)
                .FirstOrDefault();
        }

        public List<History> Since(DateTime date, HistoryEventType? eventType)
        {
            var builder = Builder().Where<History>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<History>(h => h.EventType == eventType);
            }

            return Query(builder).OrderBy(h => h.Date).ToList();
        }
    }
}
