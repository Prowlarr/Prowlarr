using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(10)]
    public class IndexerProxies : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("IndexerProxies")
                .WithColumn("Name").AsString()
                .WithColumn("Settings").AsString()
                .WithColumn("Implementation").AsString()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("Tags").AsString().Nullable();

            Alter.Table("Indexers").AddColumn("Tags").AsString().Nullable();
        }
    }
}
