using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.UNIT3D;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ShareIsland : Unit3dBase
    {
        public override string Name => "ShareIsland";
        public override string[] IndexerUrls => new string[] { "https://shareisland.org/" };
        public override string Description => "A general italian tracker.";
        public override string Language => "it-IT";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public ShareIsland(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movie");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "Serie TV");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.BooksEBook, "Ebook");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.BooksMags, "Riviste e Giornali");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PC, "Software");

            return caps;
        }
    }
}
