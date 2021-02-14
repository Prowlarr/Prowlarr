using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public abstract class Gazelle : HttpIndexerBase<GazelleSettings>
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override string BaseUrl => "";
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Gazelle(IHttpClient httpClient,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GazelleRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities,
                BaseUrl = BaseUrl
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GazelleParser(Settings, Capabilities, BaseUrl);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            return caps;
        }
    }
}
