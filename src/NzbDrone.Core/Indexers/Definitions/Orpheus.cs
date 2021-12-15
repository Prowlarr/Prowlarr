using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Orpheus : Gazelle.Gazelle
    {
        public override string Name => "Orpheus";
        public override string[] IndexerUrls => new string[] { "https://orpheus.network/" };
        public override string Description => "Orpheus (APOLLO) is a Private Torrent Tracker for MUSIC";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public Orpheus(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                       },
                BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC, "Applications");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Books, "E-Books");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Other, "Comedy");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Books, "Comics");

            return caps;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OrpheusParser(Settings, Capabilities);
        }
    }

    public class OrpheusParser : GazelleParser
    {
        public OrpheusParser(GazelleSettings settings, IndexerCapabilities capabilities)
            : base(settings, capabilities)
        {
        }

        protected override string GetDownloadUrl(int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId);

            // Orpheus fails to download if usetoken=0 so we need to only add if we will use one
            if (_settings.UseFreeleechToken)
            {
                url = url.AddQueryParam("usetoken", "1");
            }

            return url.FullUri;
        }
    }
}
