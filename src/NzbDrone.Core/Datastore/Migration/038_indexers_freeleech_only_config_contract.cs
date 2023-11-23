using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(038)]
    public class indexers_freeleech_only_config_contract : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "HDSpaceSettings" }).Where(new { Implementation = "HDSpace" });
            Update.Table("Indexers").Set(new { ConfigContract = "ImmortalSeedSettings" }).Where(new { Implementation = "ImmortalSeed" });
            Update.Table("Indexers").Set(new { ConfigContract = "XSpeedsSettings" }).Where(new { Implementation = "XSpeeds" });
        }
    }
}
