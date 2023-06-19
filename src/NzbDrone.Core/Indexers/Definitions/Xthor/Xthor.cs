using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    [Obsolete("Moved to YML for Cardigann v5")]
    public class Xthor : TorrentIndexerBase<XthorSettings>
    {
        public override string Name => "Xthor";
        public override string[] IndexerUrls => new string[] { "https://api.xthor.tk/" };
        public override string Language => "fr-FR";
        public override string Description => "Xthor is a general Private torrent site";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1252");
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.5);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Xthor(IIndexerHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new XthorRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new XthorParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep },
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.TmdbId },
                MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q },
                BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q }
            };

            caps.Categories.AddCategoryMapping(118, NewznabStandardCategory.MoviesBluRay, "Films 2160p/Bluray");
            caps.Categories.AddCategoryMapping(119, NewznabStandardCategory.MoviesBluRay, "Films 2160p/Remux");
            caps.Categories.AddCategoryMapping(107, NewznabStandardCategory.MoviesUHD, "Films 2160p/x265");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesBluRay, "Films 1080p/BluRay");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesBluRay, "Films 1080p/Remux");
            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.MoviesHD, "Films 1080p/x265");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.MoviesHD, "Films 1080p/x264");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesHD, "Films 720p/x264");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesSD, "Films SD/x264");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Movies3D, "Films 3D");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesSD, "Films XviD");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesDVD, "Films DVD");
            caps.Categories.AddCategoryMapping(122, NewznabStandardCategory.MoviesHD, "Films HDTV");
            caps.Categories.AddCategoryMapping(94, NewznabStandardCategory.MoviesWEBDL, "Films WEBDL");
            caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.MoviesWEBDL, "Films WEBRiP");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TVDocumentary, "Films Documentaire");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.MoviesOther, "Films Animation");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.MoviesOther, "Films Spectacle");
            caps.Categories.AddCategoryMapping(125, NewznabStandardCategory.TVSport, "Films Sports");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.AudioVideo, "Films Concerts, Clips");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.MoviesOther, "Films VOSTFR");

            // TV / Series
            caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.TVOther, "Series BluRay");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TVOther, "Series Pack VF");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.TVHD, "Series HD VF");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVSD, "Series SD VF");
            caps.Categories.AddCategoryMapping(98, NewznabStandardCategory.TVOther, "Series Pack VOSTFR");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVHD, "Series HD VOSTFR");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVSD, "Series SD VOSTFR");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.TVAnime, "Series Packs Anime");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVAnime, "Series Animes");
            caps.Categories.AddCategoryMapping(110, NewznabStandardCategory.TVAnime, "Series Anime VOSTFR");
            caps.Categories.AddCategoryMapping(123, NewznabStandardCategory.TVOther, "Series Animation");
            caps.Categories.AddCategoryMapping(109, NewznabStandardCategory.TVDocumentary, "Series DOC");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.TVOther, "Series Sport");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVOther, "Series Emission TV");

            // XxX / MISC
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.XXX, "MISC XxX/Films");
            caps.Categories.AddCategoryMapping(105, NewznabStandardCategory.XXX, "MISC XxX/Séries");
            caps.Categories.AddCategoryMapping(114, NewznabStandardCategory.XXX, "MISC XxX/Lesbiennes");
            caps.Categories.AddCategoryMapping(115, NewznabStandardCategory.XXX, "MISC XxX/Gays");
            caps.Categories.AddCategoryMapping(113, NewznabStandardCategory.XXX, "MISC XxX/Hentai");
            caps.Categories.AddCategoryMapping(120, NewznabStandardCategory.XXX, "MISC XxX/Magazines");

            // Books / Livres
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.BooksEBook, "Livres Romans");
            caps.Categories.AddCategoryMapping(124, NewznabStandardCategory.AudioAudiobook, "Livres Audio Books");
            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.BooksMags, "Livres  Magazines");
            caps.Categories.AddCategoryMapping(99, NewznabStandardCategory.BooksOther, "Livres Bandes dessinées");
            caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.BooksEBook, "Livres Romans Jeunesse");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.BooksComics, "Livres Comics");
            caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.BooksOther, "Livres Mangas");

            // SOFTWARE / Logiciels
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCGames, "Logiciels Jeux PC");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.ConsolePS3, "Logiciels Playstation");
            caps.Categories.AddCategoryMapping(111, NewznabStandardCategory.PCMac, "Logiciels Jeux MAC");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.ConsoleXBox360, "Logiciels XboX");
            caps.Categories.AddCategoryMapping(112, NewznabStandardCategory.PC, "Logiciels Jeux Linux");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.ConsoleWii, "Logiciels Nintendo");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.ConsoleNDS, "Logiciels NDS");
            caps.Categories.AddCategoryMapping(117, NewznabStandardCategory.PC, "Logiciels ROM");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.PC, "Logiciels Applis PC");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PCMac, "Logiciels Applis Mac");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PCMobileAndroid, "Logiciels Smartphone");

            return caps;
        }
    }
}
