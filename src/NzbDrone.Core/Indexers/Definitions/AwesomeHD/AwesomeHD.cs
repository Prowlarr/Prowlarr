using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHD : HttpIndexerBase<AwesomeHDSettings>
    {
        public override string Name => "AwesomeHD";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public override int PageSize => 50;

        public AwesomeHD(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AwesomeHDRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AwesomeHDRssParser(Settings);
        }
    }
}
