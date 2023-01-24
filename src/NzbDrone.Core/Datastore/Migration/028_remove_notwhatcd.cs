using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration;

[Migration(028)]
public class remove_notwhatcd : NzbDroneMigrationBase
{
    protected override void MainDbUpgrade()
    {
        // Remove, site dead
        Delete.FromTable("Indexers").Row(new { Implementation = "NotWhatCD" });
    }
}
