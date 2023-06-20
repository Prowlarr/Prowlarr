using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(029)]
    public class add_on_grab_to_notifications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnGrab").AsBoolean().WithDefaultValue(false);
            Alter.Table("Notifications").AddColumn("IncludeManualGrabs").AsBoolean().WithDefaultValue(false).NotNullable();
        }
    }
}
