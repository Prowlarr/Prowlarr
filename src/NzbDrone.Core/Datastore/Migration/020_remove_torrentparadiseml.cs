using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(020)]
    public class remove_torrentparadiseml : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Remove, 017 incorrectly removes this using "TorrentParadiseMI"
            Delete.FromTable("Indexers").Row(new { Implementation = "TorrentParadiseMl" });
        }
    }
}
