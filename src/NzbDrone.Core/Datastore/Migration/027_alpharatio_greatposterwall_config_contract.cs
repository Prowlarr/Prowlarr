using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration;

[Migration(027)]
public class alpharatio_greatposterwall_config_contract : NzbDroneMigrationBase
{
    protected override void MainDbUpgrade()
    {
        Update.Table("Indexers").Set(new { ConfigContract = "AlphaRatioSettings" }).Where(new { Implementation = "AlphaRatio" });
        Update.Table("Indexers").Set(new { ConfigContract = "GreatPosterWallSettings" }).Where(new { Implementation = "GreatPosterWall" });
    }
}
