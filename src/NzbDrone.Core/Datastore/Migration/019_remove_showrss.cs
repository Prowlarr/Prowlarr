using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(019)]
    public class remove_showrss : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Remove, YML version exists
            Delete.FromTable("Indexers").Row(new { Implementation = "ShowRSS" });
        }
    }
}
