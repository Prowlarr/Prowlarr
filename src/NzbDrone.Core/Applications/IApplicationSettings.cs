using System.Collections.Generic;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationSettings : IProviderConfig
    {
        IEnumerable<int> SyncCategories { get; set; }
    }
}
