using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class DesiTorrents : Gazelle.Gazelle
    {
        public override string Name => "DesiTorrents";
        public override string[] IndexerUrls => new string[] { "https://desitorrents.tv/" };
        public override string Description => "Desitorrents is a  Private Torrent Tracker for BOLLYWOOD / TOLLYWOOD / GENERAL";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public DesiTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DesiTorrentsParser(Settings, Capabilities);
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "Tv shows");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.BooksEBook, "ebooks");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Games");

            return caps;
        }
    }

    public class DesiTorrentsParser : Gazelle.GazelleParser
    {
        public DesiTorrentsParser(GazelleSettings settings, IndexerCapabilities capabilities)
            : base(settings, capabilities)
        {
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = base.ParseResponse(indexerResponse);

            foreach (TorrentInfo release in releases)
            {
                release.MinimumRatio = 0.6;
                release.MinimumSeedTime = 259200;
            }

            return releases;
        }
    }
}
