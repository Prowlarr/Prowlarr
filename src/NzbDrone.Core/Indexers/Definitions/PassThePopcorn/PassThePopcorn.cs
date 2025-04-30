using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

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
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(4);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        private readonly ICached<IndexerQueryResult> _queryResultCache;

        public PassThePopcorn(IIndexerHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            ICacheManager cacheManager,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _queryResultCache = cacheManager.GetCache<IndexerQueryResult>(GetType(), "QueryResults");
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

        protected string BuildQueryResultCacheKey(IndexerRequest request)
        {
            return HashUtil.ComputeSha256Hash($"{request.HttpRequest.Url.FullUri}::{Settings.ToJson()}");
        }

        protected override async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var cacheKey = BuildQueryResultCacheKey(request);
            var queryResult = _queryResultCache.Find(cacheKey);

            if (queryResult != null)
            {
                queryResult.Cached = true;

                return queryResult;
            }

            _queryResultCache.ClearExpired();

            queryResult = await base.FetchPage(request, parser);
            _queryResultCache.Set(cacheKey, queryResult, TimeSpan.FromMinutes(5));

            return queryResult;
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            return cleanReleases.Select(r => (ReleaseInfo)r.Clone()).ToList();
        }
    }

    public class PassThePopcornFlag : IndexerFlag
    {
        public static IndexerFlag Golden => new ("golden", "Release follows Golden Popcorn quality rules");
        public static IndexerFlag Approved => new ("approved", "Release approved by PTP staff");
    }
}
