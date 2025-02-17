using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.HDBits
{
    public class HDBits : TorrentIndexerBase<HDBitsSettings>
    {
        public override string Name => "HDBits";
        public override string[] IndexerUrls => new[] { "https://hdbits.org/" };
        public override string[] LegacyUrls => new[] { "https://hdbits.org" };
        public override string Description => "Best HD Tracker";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRedirect => true;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public HDBits(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDBitsRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDBitsParser(Settings, Capabilities.Categories);
        }

        private static IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.Internal,
                    IndexerFlag.Exclusive,
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movie");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Audio, "Audio Track");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.Other, "Misc/Demo");

            return caps;
        }
    }
}
