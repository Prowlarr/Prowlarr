using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotato : TorrentIndexerBase<TorrentPotatoSettings>
    {
        public override string Name => "TorrentPotato";
        public override string[] IndexerUrls => new string[] { "http://127.0.0.1" };
        public override string Description => "A JSON based torrent provider previously developed for CouchPotato";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public TorrentPotato(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
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
            return new TorrentPotatoRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentPotatoParser();
        }
    }
}
