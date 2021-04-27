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
        List<UserAgentStatistics> UserAgentStatistics();
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
            return Query(IndexerBuilder());
        }

        public List<UserAgentStatistics> UserAgentStatistics()
        {
            var time = DateTime.UtcNow;
            return UserAgentQuery(UserAgentBuilder());
        }

        private List<IndexerStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<IndexerStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private List<UserAgentStatistics> UserAgentQuery(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<UserAgentStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder IndexerBuilder() => new SqlBuilder()
            .Select(@"Indexers.Id AS IndexerId,
                     Indexers.Name AS IndexerName,
                     SUM(CASE WHEN EventType == 2 then 1 else 0 end) AS NumberOfQueries,
                     SUM(CASE WHEN EventType == 3 then 1 else 0 end) AS NumberOfRssQueries,
                     SUM(CASE WHEN EventType == 4 then 1 else 0 end) AS NumberOfAuthQueries,
                     SUM(CASE WHEN EventType == 1 then 1 else 0 end) AS NumberOfGrabs,
                     AVG(json_extract(History.Data,'$.elapsedTime')) AS AverageResponseTime")
            .Join<History.History, IndexerDefinition>((t, r) => t.IndexerId == r.Id)
            .GroupBy<IndexerDefinition>(x => x.Id);

        private SqlBuilder UserAgentBuilder() => new SqlBuilder()
            .Select(@"json_extract(History.Data,'$.source') AS UserAgent,
                     SUM(CASE WHEN EventType == 2 then 1 else 0 end) AS NumberOfQueries,
                     SUM(CASE WHEN EventType == 1 then 1 else 0 end) AS NumberOfGrabs,
                     SUM(CASE WHEN EventType == 3 then 1 else 0 end) AS NumberOfRssQueries")
            .GroupBy("UserAgent");
    }
}
