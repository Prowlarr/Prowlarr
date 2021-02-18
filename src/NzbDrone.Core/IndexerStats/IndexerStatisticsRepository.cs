using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.IndexerStats
{
    public interface IIndexerStatisticsRepository
    {
        List<IndexerStatistics> IndexerStatistics();
    }

    public class IndexerStatisticsRepository : IIndexerStatisticsRepository
    {
        private const string _selectTemplate = "SELECT /**select**/ FROM History /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public IndexerStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<IndexerStatistics> IndexerStatistics()
        {
            var time = DateTime.UtcNow;
            return Query(Builder());
        }

        public List<IndexerStatistics> IndexerStatistics(int indexerId)
        {
            var time = DateTime.UtcNow;
            return Query(Builder().Where<IndexerDefinition>(x => x.Id == indexerId));
        }

        private List<IndexerStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<IndexerStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder Builder() => new SqlBuilder()
            .Select(@"Indexers.Id AS IndexerId,
                     Indexers.Name AS IndexerName,
                     SUM(CASE WHEN EventType == 2 then 1 else 0 end) AS NumberOfQueries,
                     SUM(CASE WHEN EventType == 1 then 1 else 0 end) AS NumberOfGrabs,
                     AVG(json_extract(History.Data,'$.elapsedTime')) AS AverageResponseTime")
            .Join<History.History, IndexerDefinition>((t, r) => t.IndexerId == r.Id)
            .GroupBy<IndexerDefinition>(x => x.Id);
    }
}
