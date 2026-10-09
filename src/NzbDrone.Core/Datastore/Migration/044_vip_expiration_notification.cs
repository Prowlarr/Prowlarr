using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(044)]
    public class vip_expiration_notification : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnVipExpiration").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
