using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(13)]
    public class desi_gazelle_to_unit3d : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "Unit3dSettings", Enable = 0 }).Where(new { Implementation = "DesiTorrents" });
        }
    }
}
