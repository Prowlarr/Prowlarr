using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(017)]
    public class indexer_cleanup : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            //Remove v3 yml transfers
            Delete.FromTable("Indexers").Row(new { Implementation = "Aither" });
            Delete.FromTable("Indexers").Row(new { Implementation = "Anilibria" });
            Delete.FromTable("Indexers").Row(new { Implementation = "AnimeWorld" });
            Delete.FromTable("Indexers").Row(new { Implementation = "LatTeam" });
            Delete.FromTable("Indexers").Row(new { Implementation = "Blutopia" });
            Delete.FromTable("Indexers").Row(new { Implementation = "DanishBytes" });
            Delete.FromTable("Indexers").Row(new { Implementation = "DesiTorrents" });
            Delete.FromTable("Indexers").Row(new { Implementation = "DigitalCore" });
            Delete.FromTable("Indexers").Row(new { Implementation = "InternetArchive" });
            Delete.FromTable("Indexers").Row(new { Implementation = "Milkie" });
            Delete.FromTable("Indexers").Row(new { Implementation = "ShareIsland" });
            Delete.FromTable("Indexers").Row(new { Implementation = "SuperBits" });
            Delete.FromTable("Indexers").Row(new { Implementation = "ThePirateBay" });
            Delete.FromTable("Indexers").Row(new { Implementation = "TorrentLeech" });
            Delete.FromTable("Indexers").Row(new { Implementation = "TorrentSeeds" });
            Delete.FromTable("Indexers").Row(new { Implementation = "TorrentParadiseMI" });
            Delete.FromTable("Indexers").Row(new { Implementation = "YTS" });

            //Change settings to shared classes
            Update.Table("Indexers").Set(new { ConfigContract = "NoAuthTorrentBaseSettings" }).Where(new { Implementation = "Animedia" });
            Update.Table("Indexers").Set(new { ConfigContract = "NoAuthTorrentBaseSettings" }).Where(new { Implementation = "Shizaproject" });
            Update.Table("Indexers").Set(new { ConfigContract = "NoAuthTorrentBaseSettings" }).Where(new { Implementation = "ShowRSS" });
            Update.Table("Indexers").Set(new { ConfigContract = "NoAuthTorrentBaseSettings" }).Where(new { Implementation = "SubsPlease" });
            Update.Table("Indexers").Set(new { ConfigContract = "NoAuthTorrentBaseSettings" }).Where(new { Implementation = "TorrentsCSV" });

            //Change settings to shared classes
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "Anidub" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "AnimeTorrents" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "Anthelion" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "BB" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "HDSpace" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "HDTorrents" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "ImmortalSeed" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "RevolutionTT" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "SpeedCD" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "TVVault" });
            Update.Table("Indexers").Set(new { ConfigContract = "UserPassTorrentBaseSettings" }).Where(new { Implementation = "ZonaQ" });

            //Change settings to shared classes
            Update.Table("Indexers").Set(new { ConfigContract = "CookieTorrentBaseSettings" }).Where(new { Implementation = "TorrentDay" });
            Update.Table("Indexers").Set(new { ConfigContract = "CookieTorrentBaseSettings" }).Where(new { Implementation = "MoreThanTV" });
            Update.Table("Indexers").Set(new { ConfigContract = "CookieTorrentBaseSettings" }).Where(new { Implementation = "BitHDTV" });
        }
    }
}
