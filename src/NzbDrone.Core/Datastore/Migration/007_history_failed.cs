using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(7)]
    public class history_failed : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("History")
                .AddColumn("Successful").AsBoolean().NotNullable().WithDefaultValue(true);

            // Postgres added after this, not needed
            IfDatabase("sqlite").Execute.Sql("UPDATE \"History\" SET \"Successful\" = (json_extract(\"History\".\"Data\",'$.successful') == 'True' );");
        }
    }
}
