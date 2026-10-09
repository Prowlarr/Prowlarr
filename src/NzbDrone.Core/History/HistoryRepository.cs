using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<History>
    {
        History MostRecentForDownloadId(string downloadId);
        List<History> FindByDownloadId(string downloadId);
        List<History> FindDownloadHistory(int indexerId);
        List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType, int? limit);
        void DeleteForIndexers(List<int> indexerIds);
        History MostRecentForIndexer(int indexerId);
        List<History> Between(DateTime start, DateTime end, List<int> indexerIds);
        List<History> Since(DateTime date, HistoryEventType? eventType);
        void Cleanup(int days);
        int CountSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes);
        History FindFirstForIndexerSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes, int limit);
    }

    public class HistoryRepository : BasicRepository<History>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public History MostRecentForDownloadId(string downloadId)
        {
            var builder = Builder()
                .Where<History>(x => x.DownloadId == downloadId)
                .OrderBy<History>(h => h.Date, SortDirection.Descending)
                .Take(1);

            return Query(builder).FirstOrDefault();
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

        public List<History> GetByIndexerId(int indexerId, HistoryEventType? eventType, int? limit)
        {
            var builder = Builder().Where<History>(x => x.IndexerId == indexerId);

            if (eventType.HasValue)
            {
                builder.Where<History>(h => h.EventType == eventType);
            }

            builder.OrderBy<History>(h => h.Date, SortDirection.Descending);

            if (limit.HasValue)
            {
                builder.Take(limit.Value);
            }

            return Query(builder);
        }

        public void DeleteForIndexers(List<int> indexerIds)
        {
            Delete(c => indexerIds.Contains(c.IndexerId));
        }

        public void Cleanup(int days)
        {
            var cleanDate = DateTime.Now.AddDays(-1 * days);

            Delete(c => c.Date <= cleanDate);
        }

        public History MostRecentForIndexer(int indexerId)
        {
            var builder = Builder()
                .Where<History>(x => x.IndexerId == indexerId)
                .OrderBy<History>(h => h.Date, SortDirection.Descending)
                .Take(1);

            return Query(builder).FirstOrDefault();
        }

        public List<History> Between(DateTime start, DateTime end, List<int> indexerIds)
        {
            var builder = Builder()
                .Where<History>(x => x.Date >= start && x.Date <= end)
                .Where<History>(x => indexerIds.Contains(x.IndexerId))
                .OrderBy<History>(h => h.Date);

            return Query(builder);
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

        public int CountSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes)
        {
            var intEvents = eventTypes.Select(t => (int)t).ToList();

            var builder = new SqlBuilder(_database.DatabaseType)
                .SelectCount()
                .Where<History>(x => x.IndexerId == indexerId)
                .Where<History>(x => x.Date >= date)
                .Where<History>(x => intEvents.Contains((int)x.EventType));

            var sql = builder.AddPageCountTemplate(typeof(History));

            using (var conn = _database.OpenConnection())
            {
                return conn.ExecuteScalar<int>(sql.RawSql, sql.Parameters);
            }
        }

        public History FindFirstForIndexerSince(int indexerId, DateTime date, List<HistoryEventType> eventTypes, int limit)
        {
            var intEvents = eventTypes.Select(t => (int)t).ToList();

            var builder = Builder()
                .Where<History>(x => x.IndexerId == indexerId)
                .Where<History>(x => x.Date >= date)
                .Where<History>(x => intEvents.Contains((int)x.EventType));

            if (limit > 0)
            {
                builder.OrderBy<History>(h => h.Date, SortDirection.Descending).Take(limit);
            }

            return Query(builder).MinBy(h => h.Date);
        }
    }
}
