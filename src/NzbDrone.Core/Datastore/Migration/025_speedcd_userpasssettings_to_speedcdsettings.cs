using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(025)]
    public class speedcd_userpasssettings_to_speedcdsettings : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "SpeedCDSettings" }).Where(new { Implementation = "SpeedCD" });
        }
    }
}
