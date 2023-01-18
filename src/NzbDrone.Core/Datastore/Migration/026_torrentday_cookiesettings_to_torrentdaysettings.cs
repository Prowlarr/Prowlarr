using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(026)]
    public class torrentday_cookiesettings_to_torrentdaysettings : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("Indexers").Set(new { ConfigContract = "TorrentDaySettings" }).Where(new { Implementation = "TorrentDay" });
        }
    }
}
