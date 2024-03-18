using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Applications
{
    public class ApplicationIndexerSyncCommand : Command
    {
        public bool ForceSync { get; set; }

        public ApplicationIndexerSyncCommand()
        {
            ForceSync = false;
        }

        public override bool SendUpdatesToClient => true;

        public override string CompletionMessage => "Completed";
    }
}
