using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.TorrentRss
{
    public class TorrentRssIndexer : HttpIndexerBase<TorrentRssIndexerSettings>
    {
        public override string Name => "Torrent RSS Feed";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsSearch => false;
        public override int PageSize => 0;

        private readonly ITorrentRssParserFactory _torrentRssParserFactory;

        public TorrentRssIndexer(ITorrentRssParserFactory torrentRssParserFactory, IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
            _torrentRssParserFactory = torrentRssParserFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentRssIndexerRequestGenerator { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return _torrentRssParserFactory.GetParser(Settings);
        }
    }
}
