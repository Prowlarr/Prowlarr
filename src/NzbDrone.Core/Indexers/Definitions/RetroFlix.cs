using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class RetroFlix : SpeedAppBase
    {
        public override string Name => "RetroFlix";
        public override string[] IndexerUrls => new string[] { "https://retroflix.club/" };
        public override string[] LegacyUrls => new string[] { "https://retroflix.net/" };
        public override string Description => "Private Torrent Tracker for Classic Movies / TV / General Releases";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.1);
        protected override int MinimumSeedTime => 432000; // 120 hours

        public RetroFlix(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IIndexerRepository indexerRepository)
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
                },
            };

            caps.Categories.AddCategoryMapping(401, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(402, NewznabStandardCategory.TV, "TV Series");
            caps.Categories.AddCategoryMapping(406, NewznabStandardCategory.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(407, NewznabStandardCategory.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(409, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(408, NewznabStandardCategory.Audio, "HQ Audio");

            return caps;
        }
    }
}
