using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(11)]
    public class app_indexer_remote_name : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ApplicationIndexerMapping").AddColumn("RemoteIndexerName").AsString().Nullable();
        }
    }
}
