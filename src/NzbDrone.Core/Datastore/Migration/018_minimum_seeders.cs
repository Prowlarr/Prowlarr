using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(18)]
    public class minimum_seeders : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("AppSyncProfiles")
                .AddColumn("MinimumSeeders").AsInt32().NotNullable().WithDefaultValue(1);
        }
    }
}
