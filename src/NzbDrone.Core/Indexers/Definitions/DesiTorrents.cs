using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.UNIT3D;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    [Obsolete("Moved to YML for Cardigann v3")]
    public class DesiTorrents : Unit3dBase
    {
        public override string Name => "DesiTorrents";
        public override string Language => "en-US";
        public override string[] IndexerUrls => new[] { "https://desitorrents.tv/", "https://desitorrents.rocks/" };
        public override string Description => "Desitorrents is a  Private Torrent Tracker for BOLLYWOOD / TOLLYWOOD / GENERAL";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public DesiTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DesiTorrentsParser(Settings, Capabilities.Categories);
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
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.BooksEBook, "ebooks");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Games");

            return caps;
        }
    }

    public class DesiTorrentsParser : Unit3dParser
    {
        public DesiTorrentsParser(Unit3dSettings settings, IndexerCapabilitiesCategories categories)
            : base(settings, categories)
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
