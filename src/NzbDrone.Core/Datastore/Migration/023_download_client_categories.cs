using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(023)]
    public class download_client_categories : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DownloadClients")
                .AddColumn("Categories").AsString().WithDefaultValue("[]");
        }
    }
}
