using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(9)]
    public class app_profiles_applications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("AppSyncProfiles").AddColumn("ApplicationIds").AsString().Nullable();

            Execute.WithConnection(AddApplications);

            Alter.Table("Indexers").AddColumn("AppProfileIds").AsString().Nullable();

            Execute.WithConnection(MigrateAppProfileId);

            Delete.Column("AppProfileId").FromTable("Indexers");
        }

        private void AddApplications(IDbConnection conn, IDbTransaction tran)
        {
            var appIdsQuery = conn.Query<int>("SELECT Id FROM Applications").ToList();
            var updateSql = "UPDATE AppSyncProfiles SET ApplicationIds = @Ids";
            conn.Execute(updateSql, new { Ids = JsonSerializer.Serialize(appIdsQuery) }, transaction: tran);
        }

        private void MigrateAppProfileId(IDbConnection conn, IDbTransaction tran)
        {
            var appProfileId = conn.Query<int>("SELECT AppProfileId FROM Indexers").ToList();
            var updateSql = "UPDATE Indexers SET AppProfileIds = @Ids";
            conn.Execute(updateSql, new { Ids = JsonSerializer.Serialize(appProfileId) }, transaction: tran);
        }
    }
}
