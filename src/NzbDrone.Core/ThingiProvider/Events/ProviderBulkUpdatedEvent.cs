using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.ThingiProvider.Events
{
    public class ProviderBulkUpdatedEvent<TProvider> : IEvent
    {
        public IEnumerable<ProviderDefinition> Definitions { get; private set; }

        public ProviderBulkUpdatedEvent(IEnumerable<ProviderDefinition> definitions)
        {
            Definitions = definitions;
        }
    }
}
