using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(043)]
    public class newznab_indexers_enable_redirect : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(UpdateNewznabRedirectSetting);
        }

        private void UpdateNewznabRedirectSetting(IDbConnection conn, IDbTransaction tran)
        {
            var updateSql = "UPDATE \"Indexers\" SET \"Redirect\" = @Redirect WHERE \"Implementation\" = 'Newznab' AND \"Redirect\" = false";
            conn.Execute(updateSql, new { Redirect = true }, transaction: tran);
        }
    }
}
