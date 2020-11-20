using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Applications
{
    public class ApplicationIndexerSyncCommand : Command
    {
        public override bool SendUpdatesToClient => true;

        public override string CompletionMessage => null;
    }
}
