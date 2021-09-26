using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.UNIT3D;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class LatTeam : Unit3dBase
    {
        public override string Name => "Lat-Team";
        public override string Language => "es";
        public override string[] IndexerUrls => new[] { "https://lat-team.com/" };
        public override string Description => "Lat-Team is a Private Torrent Tracker for HD MOVIES / TV";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public LatTeam(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Peliculas");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesOther, "Retro Pelicula");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV Series");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVOther, "Retro Serie TV");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVForeign, "Telenovelas y Teleseries");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Musica");

            return caps;
        }
    }
}
