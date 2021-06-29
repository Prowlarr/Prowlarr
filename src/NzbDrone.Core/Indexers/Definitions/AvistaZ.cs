using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AvistaZ : AvistazBase
    {
        public override string Name => "AvistaZ";
        public override string[] IndexerUrls => new string[] { "https://avistaz.to/" };
        public override string Description => "Aka AsiaTorrents";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public AvistaZ(IIndexerRepository indexerRepository, IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AvistazRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities
            };
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
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
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio);

            return caps;
        }
    }
}
