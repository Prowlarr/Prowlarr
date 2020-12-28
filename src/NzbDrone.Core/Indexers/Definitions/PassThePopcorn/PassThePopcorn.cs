using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcorn : HttpIndexerBase<PassThePopcornSettings>
    {
        public override string Name => "PassThePopcorn";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        public override int PageSize => 50;

        public PassThePopcorn(IHttpClient httpClient,
            ICacheManager cacheManager,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PassThePopcornRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
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

            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.Movies, "Feature Film");
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesForeign);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesOther);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesSD);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesHD);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.Movies3D);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesBluRay);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesDVD);
            caps.Categories.AddCategoryMapping(1.ToString(), NewznabStandardCategory.MoviesWEBDL);
            caps.Categories.AddCategoryMapping(2.ToString(), NewznabStandardCategory.Movies, "Short Film");
            caps.Categories.AddCategoryMapping(3.ToString(), NewznabStandardCategory.TV, "Miniseries");
            caps.Categories.AddCategoryMapping(4.ToString(), NewznabStandardCategory.TV, "Stand-up Comedy");
            caps.Categories.AddCategoryMapping(5.ToString(), NewznabStandardCategory.TV, "Live Performance");

            return caps;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PassThePopcornParser(Settings, _logger);
        }
    }
}
