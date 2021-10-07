using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class SceneTime : TorrentIndexerBase<SceneTimeSettings>
    {
        public override string Name => "SceneTime";
        public override string[] IndexerUrls => new[] { "https://www.scenetime.com/" };
        public override string Description => "Always on time";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SceneTime(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SceneTimeRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SceneTimeParser(Settings, Capabilities.Categories);
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
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                                   },
                MovieSearchParams = new List<MovieSearchParam>
                                   {
                                       MovieSearchParam.Q
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

            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.XXX, "Movies Adult");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.Movies3D, "Movies 3D");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.MoviesOther, "Movies CAM-TS");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.MoviesUHD, "Movies UHD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVUHD, "TV UHD");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(77, NewznabStandardCategory.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Games PC ISO");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.ConsoleXBox, "Games XBOX");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.ConsoleWii, "Games Wii");
            caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.ConsoleNDS, "Games Nintendo DS");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.ConsolePS4, "Games/PS");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.ConsoleOther, "Games Dreamcast");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.PCMac, "Mac/Linux");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.PC0day, "Apps");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PCMobileOther, "Mobile Apps");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Books, "Books and Magazines");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.BooksComics, "Books Comic");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.Audio, "Music Pack");

            caps.Flags = new List<IndexerFlag>
            {
                IndexerFlag.FreeLeech
            };

            return caps;
        }
    }

    public class SceneTimeRequestGenerator : IIndexerRequestGenerator
    {
        public SceneTimeSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public SceneTimeRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var qc = new NameValueCollection
            {
                { "cata", "yes" },
                { "sec", "jax" }
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in catList)
            {
                qc.Add("c" + cat, "1");
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", term);
            }

            if (Settings.FreeLeechOnly)
            {
                qc.Add("freeleech", "on");
            }

            var searchUrl = string.Format("{0}/browse.php?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

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

    public class SceneTimeParser : IParseIndexerResponse
    {
        private readonly SceneTimeSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SceneTimeParser(SceneTimeSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var table = dom.QuerySelector("table.movehere");
            if (table == null)
            {
                return torrentInfos; // no results
            }

            var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head")
                                     .Select(x => x.TextContent).ToList();
            var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
            var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
            var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
            var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
            var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

            var rows = dom.QuerySelectorAll("tr.browse");

            foreach (var row in rows)
            {
                // TODO convert to initializer
                var qDescCol = row.Children[nameIndex];
                var qLink = qDescCol.QuerySelector("a");
                var details = _settings.BaseUrl + "/" + qLink.GetAttribute("href");
                var torrentId = qLink.GetAttribute("href").Split('=')[1];
                var sizeStr = row.Children[sizeIndex].TextContent;
                var seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());

                var catId = "82"; // default
                var qCatLink = row.Children[categoryIndex].QuerySelector("a");
                if (qCatLink != null)
                {
                    catId = new Regex(@"\?cat=(\d*)").Match(qCatLink.GetAttribute("href")).Groups[1].ToString().Trim();
                }

                var release = new TorrentInfo
                {
                    Title = qLink.TextContent,
                    InfoUrl = details,
                    Categories = _categories.MapTrackerCatToNewznab(catId),
                    DownloadUrl = string.Format("{0}/download.php/{1}/download.torrent", _settings.BaseUrl, torrentId),
                    Guid = details,
                    PublishDate = DateTimeUtil.FromTimeAgo(qDescCol.ChildNodes.Last().TextContent),
                    Size = ParseUtil.GetBytes(sizeStr),
                    Seeders = seeders,
                    Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + seeders,
                    DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200 // 72 hours
                };

                release.Categories = _categories.MapTrackerCatToNewznab(catId);

                //TODO Do something with this filtering
                //if (!query.MatchQueryStringAND(release.Title))
                //    continue;
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SceneTimeSettingsValidator : AbstractValidator<SceneTimeSettings>
    {
        public SceneTimeSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class SceneTimeSettings : IIndexerSettings
    {
        private static readonly SceneTimeSettingsValidator Validator = new SceneTimeSettingsValidator();

        public SceneTimeSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Login cookie from website")]
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
