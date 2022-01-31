using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(15)]
    public class IndexerVersions : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("IndexerDefinitionVersions")
                .WithColumn("DefinitionId").AsString().NotNullable().Unique()
                .WithColumn("File").AsString().NotNullable().Unique()
                .WithColumn("Sha").AsString().Nullable()
                .WithColumn("LastUpdated").AsDateTime().Nullable();
        }
    }
}
