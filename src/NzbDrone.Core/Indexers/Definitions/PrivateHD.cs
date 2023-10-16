using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class PrivateHD : AvistazBase
    {
        public override string Name => "PrivateHD";
        public override string[] IndexerUrls => new[] { "https://privatehd.to/" };
        public override string Description => "PrivateHD (PHD) is a Private Torrent Tracker for HD MOVIES / TV and the sister-site of AvistaZ, CinemaZ, ExoticaZ, and AnimeTorrents";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public PrivateHD(IIndexerRepository indexerRepository,
                         IIndexerHttpClient httpClient,
                         IEventAggregator eventAggregator,
                         IIndexerStatusService indexerStatusService,
                         IConfigService configService,
                         Logger logger)
            : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = PageSize,
                LimitsMax = PageSize,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId, MovieSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies);
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesUHD);
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesHD);
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVUHD);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD);

            return caps;
        }
    }
}
