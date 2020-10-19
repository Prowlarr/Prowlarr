using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Tags
{
    public class TagDetails : ModelBase
    {
        public string Label { get; set; }
        public List<int> NotificationIds { get; set; }

        public bool InUse
        {
            get
            {
                return NotificationIds.Any();
            }
        }
    }
}
