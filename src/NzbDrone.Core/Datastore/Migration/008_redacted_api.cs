using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(8)]
    public class redacted_api : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Remove username/password fields for Redacted
            Execute.Sql("UPDATE Indexers Set Settings = JSON_REMOVE(Indexers.Settings, '$.username', '$.password') WHERE Implementation = 'Redacted'");

            // Add empty apikey and passkey field for Redacted
            Execute.Sql("UPDATE Indexers Set Settings = JSON_INSERT(Indexers.Settings, '$.apikey', '', '$.passkey', '') WHERE Implementation = 'Redacted'");

            // Swap the ConfigContract from GazelleSettings -> RedactedSettings
            Execute.Sql("UPDATE Indexers SET ConfigContract = Replace(ConfigContract, 'GazelleSettings', 'RedactedSettings') WHERE Implementation = 'Redacted';");
        }
    }
}
