using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public abstract class Unit3dBase : TorrentIndexerBase<Unit3dSettings>
    {
        public override string[] IndexerUrls => new string[] { "" };
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Unit3dBase(IIndexerHttpClient httpClient,
                       IEventAggregator eventAggregator,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new Unit3dRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new Unit3dParser(Settings, Capabilities.Categories);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            return caps;
        }
    }
}
