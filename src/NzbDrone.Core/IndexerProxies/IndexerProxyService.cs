using NLog;

namespace NzbDrone.Core.IndexerProxies
{
    public class IndexerProxyService
    {
        private readonly IIndexerProxyFactory _indexerProxyFactory;
        private readonly Logger _logger;

        public IndexerProxyService(IIndexerProxyFactory indexerProxyFactory, Logger logger)
        {
            _indexerProxyFactory = indexerProxyFactory;
            _logger = logger;
        }
    }
}
