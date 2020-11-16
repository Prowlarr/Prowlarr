using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class IndexerDefinitionUpdateCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
