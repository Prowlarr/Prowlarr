using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
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

        public override string[] IndexerUrls => new string[]
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
            "https://ipt.world/"
        };
        public override string Description => "IPTorrents (IPT) is a Private Torrent Tracker for 0DAY / GENERAL.";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public IPTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new IPTorrentsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new IPTorrentsParser(Settings, Capabilities.Categories);
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

        public IPTorrentsRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "t";

            var qc = new NameValueCollection();

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

            if (Settings.FreeLeechOnly)
            {
                qc.Add("free", "on");
            }

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                qc.Add(cat, string.Empty);
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpException(indexerResponse.HttpResponse);
            }

            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);

            var rows = doc.QuerySelectorAll("table[id='torrents'] > tbody > tr");
            foreach (var row in rows)
            {
                var qTitleLink = row.QuerySelector("a.hv");

                //no results
                if (qTitleLink == null)
                {
                    continue;
                }

                // drop invalid char that seems to have cropped up in some titles. #6582
                var title = qTitleLink.TextContent.Trim().Replace("\u000f", "");
                var details = new Uri(_settings.BaseUrl + qTitleLink.GetAttribute("href").TrimStart('/'));

                var qLink = row.QuerySelector("a[href^=\"/download.php/\"]");
                var link = new Uri(_settings.BaseUrl + qLink.GetAttribute("href").TrimStart('/'));

                var descrSplit = row.QuerySelector("div.sub").TextContent.Split('|');
                var dateSplit = descrSplit.Last().Split(new[] { " by " }, StringSplitOptions.None);
                var publishDate = DateTimeUtil.FromTimeAgo(dateSplit.First());
                var description = descrSplit.Length > 1 ? "Tags: " + descrSplit.First().Trim() : "";
                description += dateSplit.Length > 1 ? " Uploaded by: " + dateSplit.Last().Trim() : "";

                var catIcon = row.QuerySelector("td:nth-of-type(1) a");
                if (catIcon == null)
                {
                    // Torrents - Category column == Text or Code
                    // release.Category = MapTrackerCatDescToNewznab(row.Cq().Find("td:eq(0)").Text()); // Works for "Text" but only contains the parent category
                    throw new Exception("Please, change the 'Torrents - Category column' option to 'Icons' in the website Settings. Wait a minute (cache) and then try again.");
                }

                // Torrents - Category column == Icons
                var cat = _categories.MapTrackerCatToNewznab(catIcon.GetAttribute("href").Substring(1));

                var size = ParseUtil.GetBytes(row.Children[5].TextContent);

                var colIndex = 6;
                int? files = null;

                if (row.Children.Length == 10)
                {
                    files = ParseUtil.CoerceInt(row.Children[colIndex].TextContent.Replace("Go to files", ""));
                    colIndex++;
                }

                var grabs = ParseUtil.CoerceInt(row.Children[colIndex++].TextContent);
                var seeders = ParseUtil.CoerceInt(row.Children[colIndex++].TextContent);
                var leechers = ParseUtil.CoerceInt(row.Children[colIndex].TextContent);
                var dlVolumeFactor = row.QuerySelector("span.free") != null ? 0 : 1;

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = cat,
                    Size = size,
                    Files = files,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    DownloadVolumeFactor = dlVolumeFactor,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 1209600 // 336 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class IPTorrentsSettingsValidator : AbstractValidator<IPTorrentsSettings>
    {
        public IPTorrentsSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class IPTorrentsSettings : IIndexerSettings
    {
        private static readonly IPTorrentsSettingsValidator Validator = new IPTorrentsSettingsValidator();

        public IPTorrentsSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Enter the cookie for the site. Example: `cf_clearance=0f7e7f10c62fd069323da10dcad545b828a44b6-1622730685-9-100; uid=123456789; pass=passhashwillbehere`", HelpLink = "https://wiki.servarr.com/prowlarr/faq#finding-cookies")]
        public string Cookie { get; set; }

        [FieldDefinition(3, Label = "FreeLeech Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "Search Freeleech torrents only")]
        public bool FreeLeechOnly { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
