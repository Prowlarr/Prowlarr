using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.TorrentPotato
{
    public class TorrentPotato : TorrentIndexerBase<TorrentPotatoSettings>
    {
        public override string Name => "TorrentPotato";
        public override string[] IndexerUrls => new[] { "" };
        public override string Description => "A JSON based torrent provider previously developed for CouchPotato";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentPotato(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentPotatoRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentPotatoParser();
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                yield return GetDefinition("TorrentPotato", "A JSON based torrent provider previously developed for CouchPotato", "http://127.0.0.1");
            }
        }

        private IndexerDefinition GetDefinition(string name, string description, string baseUrl)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = name,
                Description = description,
                Implementation = GetType().Name,
                Settings = new TorrentPotatoSettings { BaseUrl = baseUrl },
                Protocol = DownloadProtocol.Torrent,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                SupportsPagination = SupportsPagination
            };
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.Movies);

            return caps;
        }
    }
}
