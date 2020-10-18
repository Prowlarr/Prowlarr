using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotato : HttpIndexerBase<TorrentPotatoSettings>
    {
        public override string Name => "TorrentPotato";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public TorrentPotato(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        private IndexerDefinition GetDefinition(string name, TorrentPotatoSettings settings)
        {
            return new IndexerDefinition
            {
                EnableRss = false,
                EnableAutomaticSearch = false,
                EnableInteractiveSearch = false,
                Name = name,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = DownloadProtocol.Torrent,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch
            };
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentPotatoRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentPotatoParser();
        }
    }
}
