using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerProxies
{
    public interface IIndexerProxySettings : IProviderConfig
    {
        string Host { get; set; }
    }
}
