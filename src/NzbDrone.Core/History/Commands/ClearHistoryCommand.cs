using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.History
{
    public class ClearHistoryCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
