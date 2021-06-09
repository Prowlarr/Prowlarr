using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.UNIT3D;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Aither : Unit3dBase
    {
        public override string Name => "Aither";
        public override string BaseUrl => "https://aither.cc/";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public Aither(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
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
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TV/Sport, "Sport");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.PC, "Software/Apps");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.EBook, "Ebooks/Magazines");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.Audio/Audiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.Other, "Education");

            return caps;
        }
    }
}
