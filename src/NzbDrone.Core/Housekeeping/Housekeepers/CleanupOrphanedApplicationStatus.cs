using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedApplicationStatus : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedApplicationStatus(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ApplicationStatus""
                             WHERE ""Id"" IN (
                             SELECT ""ApplicationStatus"".""Id"" FROM ""ApplicationStatus""
                             LEFT OUTER JOIN ""Applications""
                             ON ""ApplicationStatus"".""ProviderId"" = ""Applications"".""Id""
                             WHERE ""Applications"".""Id"" IS NULL)");
        }
    }
}
