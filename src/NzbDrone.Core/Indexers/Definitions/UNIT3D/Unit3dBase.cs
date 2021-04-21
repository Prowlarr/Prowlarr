using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public abstract class Unit3dBase : HttpIndexerBase<Unit3dSettings>
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override string BaseUrl => "";
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Unit3dBase(IHttpClient httpClient,
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
                Capabilities = Capabilities,
                BaseUrl = BaseUrl
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new Unit3dParser(Capabilities.Categories, BaseUrl);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            return caps;
        }
    }
}
