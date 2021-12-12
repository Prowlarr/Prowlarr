using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.UNIT3D;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    [Obsolete("Moved to YML for Cardigann v3")]
    public class AnimeWorld : Unit3dBase
    {
        public override string Name => "AnimeWorld";
        public override string[] IndexerUrls => new string[] { "https://animeworld.cx/" };
        public override string Description => "AnimeWorld (AW) is a GERMAN Private site for ANIME / MANGA / HENTAI";
        public override string Language => "de-DE";

        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public AnimeWorld(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
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
                BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Anime Movie");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Anime Musik/OST");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Anime Spiele");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.XXX, "Hentai");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Spiele Linux");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "Sonstiges");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.Movies, "Filme");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TV, "Serien");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.PCGames, "Spiele");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.Audio, "Musik");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.BooksComics, "Mangas");

            return caps;
        }
    }
}
