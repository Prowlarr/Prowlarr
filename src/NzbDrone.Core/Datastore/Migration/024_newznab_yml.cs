using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(24)]
    public class newznab_yml : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { Implementation = "GenericNewznab", ConfigContract = "GenericNewznabSettings" }).Where(new { Implementation = "Newznab" });
        }
    }
}
