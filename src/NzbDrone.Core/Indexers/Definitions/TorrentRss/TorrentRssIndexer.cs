using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.TorrentRss
{
    public class TorrentRssIndexer : TorrentIndexerBase<TorrentRssIndexerSettings>
    {
        private readonly ITorrentRssParserFactory _torrentRssParserFactory;

        public override string Name => "Torrent RSS Feed";
        public override string[] IndexerUrls => new[] { "" };
        public override string Description => "Generic RSS Feed containing torrents";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override int PageSize => 0;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentRssIndexer(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, ITorrentRssParserFactory torrentRssParserFactory)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
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

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                yield return GetDefinition("showRSS", "showRSS is a service that allows you to keep track of your favorite TV shows", GetSettings("https://showrss.info/other/all.rss", allowZeroSize: true, defaultReleaseSize: 512));
                yield return GetDefinition("Torrent RSS Feed", "Generic RSS Feed containing torrents", GetSettings(""));
            }
        }

        private IndexerDefinition GetDefinition(string name, string description, TorrentRssIndexerSettings settings)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = name,
                Description = description,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = DownloadProtocol.Torrent,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                SupportsPagination = SupportsPagination,
                Capabilities = Capabilities
            };
        }

        private TorrentRssIndexerSettings GetSettings(string url, bool? allowZeroSize = null, double? defaultReleaseSize = null)
        {
            var settings = new TorrentRssIndexerSettings
            {
                BaseUrl = url,
                AllowZeroSize = allowZeroSize.GetValueOrDefault(false)
            };

            if (defaultReleaseSize.HasValue)
            {
                settings.DefaultReleaseSize = defaultReleaseSize;
            }

            return settings;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Other);

            return caps;
        }
    }
}
