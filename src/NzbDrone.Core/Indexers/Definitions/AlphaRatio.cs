using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AlphaRatio : Gazelle.Gazelle
    {
        public override string Name => "AlphaRatio";
        public override string BaseUrl => "https://alpharatio.cc/";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public AlphaRatio(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AlphaRatioRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities,
                BaseUrl = BaseUrl
            };
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
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVSD, "TvSD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVHD, "TvHD");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVUHD, "TvUHD");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVSD, "TvDVDRip");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSD, "TvPackSD");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TVHD, "TvPackHD");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVUHD, "TvPackUHD");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesSD, "MovieSD");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.MoviesHD, "MovieHD");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesUHD, "MovieUHD");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.MoviesSD, "MoviePackSD");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.MoviesHD, "MoviePackHD");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.MoviesUHD, "MoviePackUHD");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.XXX, "MovieXXX");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.MoviesBluRay, "Bluray");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVAnime, "AnimeSD");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVAnime, "AnimeHD");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.PCGames, "GamesPC");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.ConsoleXBox, "GamesxBox");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.ConsolePS4, "GamesPS");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.ConsoleWii, "GamesNin");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PC0day, "AppsWindows");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PCMac, "AppsMAC");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PC0day, "AppsLinux");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCMobileOther, "AppsMobile");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.XXX, "0dayXXX");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "eBook");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.AudioAudiobook, "AudioBook");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.AudioOther, "Music");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.Other, "Misc");

            return caps;
        }
    }

    public class AlphaRatioRequestGenerator : Gazelle.GazelleRequestGenerator
    {
        protected override bool ImdbInTags => true;
    }
}
