using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(030)]
    public class animetorrents_use_custom_config_contract : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "AnimeTorrentsSettings" }).Where(new { Implementation = "AnimeTorrents" });
        }
    }
}
