using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(043)]
    public class newznab_indexers_enable_redirect : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { Redirect = true }).Where(new { Implementation = "Newznab", Redirect = false });
        }
    }
}
