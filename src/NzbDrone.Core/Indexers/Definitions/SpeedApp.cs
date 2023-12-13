using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SpeedApp : SpeedAppBase
    {
        public override string Name => "SpeedApp.io";
        public override string[] IndexerUrls => new[] { "https://speedapp.io/" };
        public override string[] LegacyUrls => new[] { "https://speedapp.io" };
        public override string Description => "SpeedApp is a ROMANIAN Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string Language => "ro-RO";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public SpeedApp(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IIndexerRepository indexerRepository)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger, indexerRepository)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.ImdbId, TvSearchParam.Season, TvSearchParam.Ep,
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId,
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q,
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q,
                }
            };

            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesSD, "Movies: SD");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.MoviesSD, "Movies: SD Ro");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesHD, "Movies: HD");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.MoviesHD, "Movies: HD Ro");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesDVD, "Movies: DVD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesDVD, "Movies: DVD Ro");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.MoviesBluRay, "Movies: BluRay");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.MoviesBluRay, "Movies: BluRay Ro");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.Movies, "Movies: Ro");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.MoviesUHD, "Movies: 4K (2160p) Ro");
            caps.Categories.AddCategoryMapping(61, NewznabStandardCategory.MoviesUHD, "Movies: 4K (2160p)");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(66, NewznabStandardCategory.TV, "TV Packs Ro");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.TVSD, "TV Episodes");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.TVSD, "TV Episodes Ro");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.TVHD, "TV Episodes HD");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVHD, "TV Episodes HD Ro");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.TV, "TV Ro");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.PCGames, "Games: PC-ISO");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.Console, "Games: Console");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PC0day, "Applications");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.PC, "Applications: Linux");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.PCMac, "Applications: Mac");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.PCMobileOther, "Applications: Mobile");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.TV, "TV Cartoons");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVAnime, "TV Anime / Hentai");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.BooksEBook, "E-books");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Images");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.TVSport, "TV Sports");
            caps.Categories.AddCategoryMapping(58, NewznabStandardCategory.TVSport, "TV Sports Ro");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVDocumentary, "TV Documentary");
            caps.Categories.AddCategoryMapping(63, NewznabStandardCategory.TVDocumentary, "TV Documentary Ro");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.Other, "Tutorial");
            caps.Categories.AddCategoryMapping(67, NewznabStandardCategory.OtherMisc, "Miscellaneous");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXX, "XXX Movies");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.XXX, "XXX DVD");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.XXX, "XXX HD");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.XXXImageSet, "XXX Images");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.XXX, "XXX Packs");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.XXX, "XXX SD");

            return caps;
        }
    }
}
