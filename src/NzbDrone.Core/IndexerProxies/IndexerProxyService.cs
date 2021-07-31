using System;
using NLog;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Messaging.Events;

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
