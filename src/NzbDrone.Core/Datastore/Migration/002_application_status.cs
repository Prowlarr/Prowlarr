using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class ApplicationStatus : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("EnableAutomaticSearch").FromTable("Indexers");
            Delete.Column("EnableInteractiveSearch").FromTable("Indexers");

            Rename.Column("EnableRss").OnTable("Indexers").To("Enable");

            Create.TableForModel("ApplicationStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable();
        }
    }
}
