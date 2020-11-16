using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerVersions
{
    public class IndexerDefinitionUpdateCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
