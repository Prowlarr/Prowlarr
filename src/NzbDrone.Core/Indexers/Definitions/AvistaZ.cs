using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AvistaZ : AvistazBase
    {
        public override string Name => "AvistaZ";
        public override string[] IndexerUrls => new[] { "https://avistaz.to/" };
        public override string Description => "Aka AsiaTorrents";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public AvistaZ(IIndexerRepository indexerRepository,
                       IIndexerHttpClient httpClient,
                       IEventAggregator eventAggregator,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AvistaZRequestGenerator
            {
                Settings = Settings,
                Capabilities = Capabilities,
                PageSize = PageSize,
                HttpClient = _httpClient,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AvistaZParser();
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

    public class AvistaZRequestGenerator : AvistazRequestGenerator
    {
        // AvistaZ has episodes without season. eg Running Man E323
        protected override string GetEpisodeSearchTerm(TvSearchCriteria searchCriteria)
        {
            return searchCriteria.Season is null or 0 && searchCriteria.Episode.IsNotNullOrWhiteSpace()
                ? $"E{searchCriteria.Episode}"
                : $"{searchCriteria.EpisodeSearchString}";
        }
    }

    public class AvistaZParser : AvistazParserBase
    {
        protected override string TimezoneOffset => "+01:00";
    }
}
