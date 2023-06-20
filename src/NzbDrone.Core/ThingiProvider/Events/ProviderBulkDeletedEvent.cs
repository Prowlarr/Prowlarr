using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.ThingiProvider.Events
{
    public class ProviderBulkDeletedEvent<TProvider> : IEvent
    {
        public IEnumerable<int> ProviderIds { get; private set; }

        public ProviderBulkDeletedEvent(IEnumerable<int> ids)
        {
            ProviderIds = ids;
        }
    }
}
