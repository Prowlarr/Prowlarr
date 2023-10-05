using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentDay : TorrentIndexerBase<TorrentDaySettings>
    {
        public override string Name => "TorrentDay";
        public override string[] IndexerUrls => new[]
        {
            "https://torrentday.cool/",
            "https://tday.love/",
            "https://secure.torrentday.com/",
            "https://classic.torrentday.com/",
            "https://www.torrentday.com/",
            "https://torrentday.it/",
            "https://td.findnemo.net/",
            "https://td.getcrazy.me/",
            "https://td.venom.global/",
            "https://td.workisboring.net/"
        };
        public override string Description => "TorrentDay (TD) is a Private site for TV / MOVIES / GENERAL";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentDay(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentDayRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentDayParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        private IndexerCapabilities SetCapabilities()
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

            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.MoviesUHD, "Movie/4K");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.MoviesSD, "Movies/480p");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray-Full");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.MoviesSD, "Movies/MP4");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.MoviesForeign, "Movies/Non-English");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Movies, "Movies/Packs");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.MoviesSD, "Movies/SD/x264");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Movies, "Movies/x265");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Movies/XviD");

            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVSD, "TV/480p");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVHD, "TV/Bluray");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.TVSD, "TV/DVD-Rip");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.TVSD, "TV/Mobile");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.TVForeign, "TV/Non-English");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TVSD, "TV/SD/x264");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.TVUHD, "TV/x265");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD, "TV/XviD");

            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "PC/Games");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.ConsolePS3, "PS");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.ConsolePSP, "PSP");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.ConsoleNDS, "Nintendo");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.ConsoleXBox, "Xbox");

            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.AudioMP3, "Music/Audio");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Audio, "Music/Flac");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.AudioForeign, "Music/Non-English");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.AudioVideo, "Music/Video");

            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.AudioAudiobook, "Audio Books");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.BooksForeign, "Books/Non-English");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.TVDocumentary, "Educational");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Other, "Fonts");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.AudioOther, "Podcast");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.PC, "Softwa/Packs");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.PC, "Software");

            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.XXX, "XXX/0Day");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXX, "XXX/Movies");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXPack, "XXX/Packs");

            return caps;
        }
    }

    public class TorrentDayRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentDaySettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "t.json";

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (cats.Count == 0)
            {
                cats = Capabilities.Categories.GetTrackerCategories();
            }

            var catStr = string.Join(";", cats);
            searchUrl = searchUrl + "?" + catStr;

            if (Settings.FreeLeechOnly)
            {
                searchUrl += ";free";
            }

            searchUrl += ";q=";

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                searchUrl += imdbId + " ".UrlEncode(Encoding.UTF8);
            }

            searchUrl += term.UrlEncode(Encoding.UTF8);

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentDayParser : IParseIndexerResponse
    {
        private readonly TorrentDaySettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentDayParser(TorrentDaySettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var rows = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);

            foreach (var row in rows)
            {
                var title = (string)row.name;

                var torrentId = (long)row.t;
                var details = new Uri(_settings.BaseUrl + "details.php?id=" + torrentId);
                var seeders = (int)row.seeders;
                var imdbId = (string)row["imdb-id"];
                var downloadMultiplier = (double?)row["download-multiplier"] ?? 1;
                var link = new Uri(_settings.BaseUrl + "download.php/" + torrentId + "/" + torrentId + ".torrent");
                var publishDate = DateTimeUtil.UnixTimestampToDateTime((long)row.ctime).ToLocalTime();
                var imdb = ParseUtil.GetImdbId(imdbId) ?? 0;

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = _categories.MapTrackerCatToNewznab(row.c.ToString()),
                    Size = (long)row.size,
                    Files = (int)row.files,
                    Grabs = (int)row.completed,
                    Seeders = seeders,
                    Peers = seeders + (int)row.leechers,
                    ImdbId = imdb,
                    DownloadVolumeFactor = downloadMultiplier,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentDaySettings : CookieTorrentBaseSettings
    {
        [FieldDefinition(3, Label = "FreeLeech Only", Type = FieldType.Checkbox, HelpText = "Search Freeleech torrents only")]
        public bool FreeLeechOnly { get; set; }
    }
}
