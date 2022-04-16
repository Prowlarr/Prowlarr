using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(016)]
    public class cleanup_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Config").Row(new { Key = "movieinfolanguage" });
            Delete.FromTable("Config").Row(new { Key = "downloadclientworkingfolders" });
        }
    }
}
