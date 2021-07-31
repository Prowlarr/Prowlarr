using System.Linq;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerProxies
{
    public class IndexerProxyDefinition : ProviderDefinition
    {
        public override bool Enable => Tags.Any();
    }
}
