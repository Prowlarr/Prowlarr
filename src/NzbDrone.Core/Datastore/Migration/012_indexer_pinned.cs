using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(12)]
    public class IndexerPinned : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers")
                .AddColumn("Pinned").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
