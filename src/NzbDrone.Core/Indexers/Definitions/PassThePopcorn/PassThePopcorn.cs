using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.PassThePopcorn
{
    public class PassThePopcorn : TorrentIndexerBase<PassThePopcornSettings>
    {
        public override string Name => "PassThePopcorn";
        public override string[] IndexerUrls => new[] { "https://passthepopcorn.me" };
        public override string Description => "PassThePopcorn (PTP) is a Private site for MOVIES / TV";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsPagination => true;
        public override int PageSize => 50;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        public PassThePopcorn(IIndexerHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PassThePopcornRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PassThePopcornParser(Settings);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = PageSize,
                LimitsMax = PageSize,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.FreeLeech,
                    PassThePopcornFlag.Golden,
                    PassThePopcornFlag.Approved
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Feature Film");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Movies, "Short Film");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TV, "Miniseries");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Movies, "Stand-up Comedy");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Movies, "Live Performance");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Movies, "Movie Collection");

            return caps;
        }
    }

    public class PassThePopcornFlag : IndexerFlag
    {
        public static IndexerFlag Golden => new ("golden", "Release follows Golden Popcorn quality rules");
        public static IndexerFlag Approved => new ("approved", "Release approved by PTP");
    }
}
