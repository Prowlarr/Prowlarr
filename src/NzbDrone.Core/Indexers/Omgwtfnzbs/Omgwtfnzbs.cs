using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.Omgwtfnzbs
{
    public class Omgwtfnzbs : HttpApplicationBase<OmgwtfnzbsSettings>
    {
        public override string Name => "omgwtfnzbs";

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public Omgwtfnzbs(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new OmgwtfnzbsRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OmgwtfnzbsRssParser();
        }
    }
}
