using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotato : HttpIndexerBase<TorrentPotatoSettings>
    {
        public override string Name => "TorrentPotato";
        public override string BaseUrl => "http://127.0.0.1";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public TorrentPotato(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        private IndexerDefinition GetDefinition(string name, TorrentPotatoSettings settings)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = name,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = DownloadProtocol.Torrent,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect
            };
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentPotatoRequestGenerator() { Settings = Settings, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentPotatoParser();
        }
    }
}
