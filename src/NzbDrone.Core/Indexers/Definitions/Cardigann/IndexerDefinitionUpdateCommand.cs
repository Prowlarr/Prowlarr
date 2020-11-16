using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class IndexerDefinitionUpdateCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
