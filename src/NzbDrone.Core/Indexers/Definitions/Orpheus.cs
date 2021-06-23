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
        public override string BaseUrl => "https://orpheus.network/";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public Orpheus(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
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
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Movies, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TV, "Comedy");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Books, "Comics");

            return caps;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OrpheusParser(Settings, Capabilities, BaseUrl);
        }
    }

    public class OrpheusParser : GazelleParser
    {
        public OrpheusParser(GazelleSettings settings, IndexerCapabilities capabilities, string baseUrl)
            : base(settings, capabilities, baseUrl)
        {
        }

        protected override string GetDownloadUrl(int torrentId)
        {
            var url = new HttpUri(_baseUrl)
                .CombinePath("/ajax.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("useToken", _settings.UseFreeleechToken ? "1" : "0")
                .AddQueryParam("id", torrentId);

            return url.FullUri;
        }
    }
 }
