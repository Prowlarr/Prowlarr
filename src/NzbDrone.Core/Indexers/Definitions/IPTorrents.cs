using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class IPTorrents : TorrentIndexerBase<IPTorrentsSettings>
    {
        public override string Name => "IPTorrents";
        public override string[] IndexerUrls => new[]
        {
            "https://iptorrents.com/",
            "https://iptorrents.me/",
            "https://nemo.iptorrents.com/",
            "https://ipt.getcrazy.me/",
            "https://ipt.findnemo.net/",
            "https://ipt.beelyrics.net/",
            "https://ipt.venom.global/",
            "https://ipt.workisboring.net/",
            "https://ipt.lol/",
            "https://ipt.cool/",
            "https://ipt.world/",
            "https://ipt.octopus.town/"
        };
        public override string Description => "IPTorrents (IPT) is a Private Torrent Tracker for 0DAY / GENERAL.";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public IPTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new IPTorrentsRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new IPTorrentsParser(Settings, Capabilities.Categories);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("lout.php"))
            {
                throw new IndexerAuthException("IPTorrents authentication with cookies failed.");
            }

            return false;
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            return FilterReleasesByQuery(cleanReleases, searchCriteria).ToList();
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

            caps.Categories.AddCategoryMapping(72, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(87, NewznabStandardCategory.Movies3D, "Movie/3D");
            caps.Categories.AddCategoryMapping(77, NewznabStandardCategory.MoviesSD, "Movie/480p");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.MoviesUHD, "Movie/4K");
            caps.Categories.AddCategoryMapping(89, NewznabStandardCategory.MoviesHD, "Movie/BD-R");
            caps.Categories.AddCategoryMapping(90, NewznabStandardCategory.MoviesSD, "Movie/BD-Rip");
            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.MoviesSD, "Movie/Cam");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesDVD, "Movie/DVD-R");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.MoviesBluRay, "Movie/HD/Bluray");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.Movies, "Movie/Kids");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.MoviesSD, "Movie/MP4");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.MoviesForeign, "Movie/Non-English");
            caps.Categories.AddCategoryMapping(68, NewznabStandardCategory.Movies, "Movie/Packs");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.MoviesWEBDL, "Movie/Web-DL");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesSD, "Movie/Xvid");
            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.Movies, "Movie/x265");

            caps.Categories.AddCategoryMapping(73, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TVDocumentary, "TV/Documentaries");
            caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(78, NewznabStandardCategory.TVSD, "TV/480p");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.TVHD, "TV/BD");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.TVSD, "TV/DVD-Rip");
            caps.Categories.AddCategoryMapping(66, NewznabStandardCategory.TVSD, "TV/Mobile");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.TVForeign, "TV/Non-English");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(83, NewznabStandardCategory.TVForeign, "TV/Packs/Non-English");
            caps.Categories.AddCategoryMapping(79, NewznabStandardCategory.TVSD, "TV/SD/x264");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.TVWEBDL, "TV/Web-DL");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping(99, NewznabStandardCategory.TVHD, "TV/x265");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVSD, "TV/Xvid");

            caps.Categories.AddCategoryMapping(74, NewznabStandardCategory.Console, "Games");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.ConsoleOther, "Games/Mixed");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.ConsoleNDS, "Games/Nintendo DS");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.PCISO, "Games/PC-ISO");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.PCGames, "Games/PC-Rip");
            caps.Categories.AddCategoryMapping(71, NewznabStandardCategory.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.ConsoleXBox360, "Games/Xbox-360");

            caps.Categories.AddCategoryMapping(75, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.AudioMP3, "Music/Audio");
            caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.AudioLossless, "Music/Flac");
            caps.Categories.AddCategoryMapping(93, NewznabStandardCategory.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.AudioVideo, "Music/Video");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.AudioVideo, "Podcast");

            caps.Categories.AddCategoryMapping(76, NewznabStandardCategory.Other, "Other/Miscellaneous");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PC0day, "Appz");
            caps.Categories.AddCategoryMapping(86, NewznabStandardCategory.PC0day, "Appz/Non-English");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.AudioAudiobook, "AudioBook");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.Books, "Books/Non-English");
            caps.Categories.AddCategoryMapping(94, NewznabStandardCategory.BooksComics, "Books/Comics");
            caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.BooksOther, "Books/Educational");
            caps.Categories.AddCategoryMapping(98, NewznabStandardCategory.Other, "Other/Fonts");
            caps.Categories.AddCategoryMapping(69, NewznabStandardCategory.PCMac, "Appz/Mac");
            caps.Categories.AddCategoryMapping(92, NewznabStandardCategory.BooksMags, "Books/Magazines & Newspapers");
            caps.Categories.AddCategoryMapping(58, NewznabStandardCategory.PCMobileOther, "Appz/Mobile");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.Other, "Other/Pics/Wallpapers");

            caps.Categories.AddCategoryMapping(88, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(85, NewznabStandardCategory.XXXOther, "XXX/Magazines");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.XXX, "XXX/Movie");
            caps.Categories.AddCategoryMapping(81, NewznabStandardCategory.XXX, "XXX/Movie/0Day");
            caps.Categories.AddCategoryMapping(91, NewznabStandardCategory.XXXPack, "XXX/Packs");
            caps.Categories.AddCategoryMapping(84, NewznabStandardCategory.XXXImageSet, "XXX/Pics/Wallpapers");

            return caps;
        }
    }

    public class IPTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        public IPTorrentsSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, SearchCriteriaBase searchCriteria, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "t";

            var qc = new NameValueCollection();

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories))
            {
                qc.Add(cat, string.Empty);
            }

            if (Settings.FreeLeechOnly)
            {
                qc.Add("free", "on");
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                // ipt uses sphinx, which supports boolean operators and grouping
                qc.Add("q", "+(" + imdbId + ")");
            }

            // changed from else if to if to support searching imdbid + season/episode in the same query
            if (!string.IsNullOrWhiteSpace(term))
            {
                // similar to above
                qc.Add("q", "+(" + term + ")");
            }

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                qc.Add("p", page.ToString());
            }

            if (qc.Count > 0)
            {
                searchUrl += $"?{qc.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            if (Settings.UserAgent.IsNotNullOrWhiteSpace())
            {
                request.HttpRequest.Headers.UserAgent = Settings.UserAgent;
            }

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SearchTerm}", searchCriteria, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class IPTorrentsParser : IParseIndexerResponse
    {
        private readonly IPTorrentsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public IPTorrentsParser(IPTorrentsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(indexerResponse.Content);

            var headerColumns = doc.QuerySelectorAll("table[id=\"torrents\"] > thead > tr > th").Select(x => x.TextContent.Trim()).ToList();
            var sizeIndex = FindColumnIndexOrDefault(headerColumns, "Sort by size", 5);
            var filesIndex = FindColumnIndexOrDefault(headerColumns, "Sort by files");

            var rows = doc.QuerySelectorAll("table[id=\"torrents\"] > tbody > tr");
            foreach (var row in rows)
            {
                var qTitleLink = row.QuerySelector("a.hv");

                //no results
                if (qTitleLink == null)
                {
                    continue;
                }

                var title = CleanTitle(qTitleLink.TextContent);
                var details = new Uri(_settings.BaseUrl + qTitleLink.GetAttribute("href").TrimStart('/'));

                var qLink = row.QuerySelector("a[href^=\"/download.php/\"]");
                var link = new Uri(_settings.BaseUrl + qLink.GetAttribute("href").TrimStart('/'));

                var descrSplit = row.QuerySelector("div.sub").TextContent.Split('|');
                var dateSplit = descrSplit.Last().Split(new[] { " by " }, StringSplitOptions.None);
                var publishDate = DateTimeUtil.FromTimeAgo(dateSplit.First());
                var description = descrSplit.Length > 1 ? "Tags: " + descrSplit.First().Trim() : "";

                var catIcon = row.QuerySelector("td:nth-of-type(1) a");
                if (catIcon == null)
                {
                    // Torrents - Category column == Text or Code
                    // release.Category = MapTrackerCatDescToNewznab(row.Cq().Find("td:eq(0)").Text()); // Works for "Text" but only contains the parent category
                    throw new Exception("Please, change the 'Torrents - Category column' option to 'Icons' in the website Settings. Wait a minute (cache) and then try again.");
                }

                // Torrents - Category column == Icons
                var cat = _categories.MapTrackerCatToNewznab(catIcon.GetAttribute("href").Substring(1));

                var size = ParseUtil.GetBytes(row.Children[sizeIndex].TextContent);

                int? files = null;

                if (filesIndex != -1)
                {
                    files = ParseUtil.CoerceInt(row.Children[filesIndex].TextContent.Replace("Go to files", ""));
                }

                var colIndex = row.Children.Length == 10 ? 7 : 6;

                var grabsIndex = FindColumnIndexOrDefault(headerColumns, "Sort by snatches", colIndex++);
                var seedersIndex = FindColumnIndexOrDefault(headerColumns, "Sort by seeders", colIndex++);
                var leechersIndex = FindColumnIndexOrDefault(headerColumns, "Sort by leechers", colIndex);

                var grabs = ParseUtil.CoerceInt(row.Children[grabsIndex].TextContent);
                var seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent);
                var leechers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent);

                var release = new TorrentInfo
                {
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    Title = title,
                    Description = description,
                    Categories = cat,
                    Size = size,
                    Files = files,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    PublishDate = publishDate,
                    DownloadVolumeFactor = row.QuerySelector("span.free") != null ? 0 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 1209600 // 336 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        private static int FindColumnIndexOrDefault(List<string> columns, string name, int defaultIndex = -1)
        {
            var index = columns.FindIndex(x => x.Equals(name, StringComparison.Ordinal));

            return index != -1 ? index : defaultIndex;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private static string CleanTitle(string title)
        {
            // drop invalid chars that seems to have cropped up in some titles. #6582
            title = Regex.Replace(title, @"[\u0000-\u0008\u000A-\u001F\u0100-\uFFFF]", string.Empty, RegexOptions.Compiled);
            title = Regex.Replace(title, @"[\(\[\{]REQ(UEST(ED)?)?[\)\]\}]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim(' ', '-', ':');
        }
    }

    public class IPTorrentsValidator : CookieBaseSettingsValidator<IPTorrentsSettings>
    {
        public IPTorrentsValidator()
        {
            RuleFor(c => c.UserAgent).NotEmpty();
        }
    }

    public class IPTorrentsSettings : CookieTorrentBaseSettings
    {
        private static readonly IPTorrentsValidator Validator = new ();

        [FieldDefinition(3, Label = "Cookie User-Agent", Type = FieldType.Textbox, HelpText = "User-Agent associated with cookie used from Browser")]
        public string UserAgent { get; set; }

        [FieldDefinition(4, Label = "FreeLeech Only", Type = FieldType.Checkbox, HelpText = "Search FreeLeech torrents only")]
        public bool FreeLeechOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
