using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions;

public class XSpeeds : TorrentIndexerBase<UserPassTorrentBaseSettings>
{
    public override string Name => "XSpeeds";
    public override string[] IndexerUrls => new[] { "https://www.xspeeds.eu/" };
    public override string Description => "XSpeeds (XS) is a Private Torrent Tracker for MOVIES / TV / GENERAL";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    private string LandingUrl => Settings.BaseUrl + "login.php";
    private string LoginUrl => Settings.BaseUrl + "takelogin.php";

    public XSpeeds(IIndexerHttpClient httpClient,
        IEventAggregator eventAggregator,
        IIndexerStatusService indexerStatusService,
        IConfigService configService,
        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new XSpeedsRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new XSpeedsParser(Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var landingPage = await ExecuteAuth(new HttpRequest(LandingUrl));

        var requestBuilder = new HttpRequestBuilder(LoginUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true,
            Method = HttpMethod.Post
        };

        var authLoginRequest = requestBuilder
            .SetCookies(landingPage.GetCookies())
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", LandingUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector(".left_side table:nth-of-type(1) tr:nth-of-type(2)")?.TextContent.Trim().Replace("\n\t", " ");
            if (errorMessage.IsNullOrWhiteSpace())
            {
                errorMessage = dom.QuerySelector("div.notification-body")?.TextContent.Trim().Replace("\n\t", " ");
            }

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        var cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded.");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return !httpResponse.Content.Contains("logout.php");
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

        caps.Categories.AddCategoryMapping(92, NewznabStandardCategory.MoviesUHD, "4K Movies");
        caps.Categories.AddCategoryMapping(91, NewznabStandardCategory.TVUHD, "4K TV");
        caps.Categories.AddCategoryMapping(94, NewznabStandardCategory.TVUHD, "4K TV Boxsets");
        caps.Categories.AddCategoryMapping(70, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
        caps.Categories.AddCategoryMapping(66, NewznabStandardCategory.MoviesBluRay, "Blu-Ray");
        caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Books, "Books Magazines");
        caps.Categories.AddCategoryMapping(68, NewznabStandardCategory.MoviesOther, "Cams/TS");
        caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.TVDocumentary, "Documentaries");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesDVD, "DVDR");
        caps.Categories.AddCategoryMapping(72, NewznabStandardCategory.MoviesForeign, "Foreign");
        caps.Categories.AddCategoryMapping(74, NewznabStandardCategory.TVOther, "Kids");
        caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.PCMac, "Mac Games");
        caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVSport, "MMA");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.Movies, "Movie Boxsets");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.MoviesHD, "Movies HEVC");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.AudioVideo, "Music Videos");
        caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.ConsoleNDS, "NDS Games");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.Other, "Other");
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "PC Games");
        caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.Other, "Pictures");
        caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.ConsolePS4, "Playstation");
        caps.Categories.AddCategoryMapping(71, NewznabStandardCategory.TV, "PPV");
        caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.TV, "Soaps");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.TVSport, "Sports");
        caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.TVSport, "Sports FIFA World Cup");
        caps.Categories.AddCategoryMapping(86, NewznabStandardCategory.TVSport, "Sports MotorSports");
        caps.Categories.AddCategoryMapping(89, NewznabStandardCategory.TVSport, "Sports Olympics");
        caps.Categories.AddCategoryMapping(88, NewznabStandardCategory.TVSport, "Sports UK Football");
        caps.Categories.AddCategoryMapping(83, NewznabStandardCategory.Movies, "TOTM");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.TVSD, "TV Boxsets");
        caps.Categories.AddCategoryMapping(76, NewznabStandardCategory.TVHD, "TV HD Boxsets");
        caps.Categories.AddCategoryMapping(97, NewznabStandardCategory.TVHD, "TV HECV Boxsets");
        caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.TVHD, "TV HD");
        caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.TVHD, "TV HD HEVC");
        caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVSD, "TV SD");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.ConsoleWii, "Wii Games");
        caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.TVSport, "Wrestling");
        caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.ConsoleXBox, "Xbox Games");

        return caps;
    }
}

public class XSpeedsRequestGenerator : IIndexerRequestGenerator
{
    private readonly UserPassTorrentBaseSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public XSpeedsRequestGenerator(UserPassTorrentBaseSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
    {
        var categoryMapping = _capabilities.Categories.MapTorznabCapsToTrackers(categories);

        var parameters = new NameValueCollection
        {
            { "category", categoryMapping.FirstIfSingleOrDefault("0") }, // multi category search not supported
            { "include_dead_torrents", "yes" },
            { "sort", "added" },
            { "order", "desc" }
        };

        term = Regex.Replace(term, @"[ -._]+", " ").Trim();

        if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("do", "search");
            parameters.Set("keywords", term);
            parameters.Set("search_type", "t_name");
        }

        var searchUrl = _settings.BaseUrl + "browse.php";

        if (parameters.Count > 0)
        {
            searchUrl += $"?{parameters.GetQueryString()}";
        }

        var request = new IndexerRequest(searchUrl, HttpAccept.Html);

        yield return request;
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class XSpeedsParser : IParseIndexerResponse
{
    private readonly IndexerCapabilitiesCategories _categories;
    private readonly Regex _dateAddedRegex = new (@"\d{2}-\d{2}-\d{4} \d{2}:\d{2}", RegexOptions.Compiled);

    public XSpeedsParser(IndexerCapabilitiesCategories categories)
    {
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("table#sortabletable > tbody > tr:has(a[href*=\"details.php?id=\"])");
        foreach (var row in rows)
        {
            var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]");
            var title = qDetails?.TextContent.Trim();

            var infoUrl = qDetails?.GetAttribute("href");
            var downloadUrl = row.QuerySelector("a[href*=\"download.php\"]")?.GetAttribute("href");

            var description = row.QuerySelector(".tooltip-content > div:nth-of-type(2)")?.TextContent.Replace("|", ",").Replace(" ", "").Trim();

            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)")?.TextContent);
            var peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)")?.TextContent.Trim());

            var categoryLink = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
            var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Description = description,
                Categories = _categories.MapTrackerCatToNewznab(cat),
                Seeders = seeders,
                Peers = peers,
                Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(5)")?.TextContent.Trim()),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)")?.TextContent),
                UploadVolumeFactor = row.QuerySelector("img[title^=\"x2 Torrent\"]") != null ? 2 : 1,
                MinimumRatio = 0.8
            };

            var dateAddedMatch = _dateAddedRegex.Match(row.QuerySelector("td:nth-of-type(2) > div:last-child").TextContent.Trim());
            if (dateAddedMatch.Success)
            {
                release.PublishDate = DateTime.ParseExact(dateAddedMatch.Value, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
            }

            if (row.QuerySelector("img[title^=\"Free Torrent\"], img[title^=\"Sitewide Free Torrent\"]") != null)
            {
                release.DownloadVolumeFactor = 0;
            }
            else if (row.QuerySelector("img[title^=\"Silver Torrent\"]") != null)
            {
                release.DownloadVolumeFactor = 0.5;
            }
            else
            {
                release.DownloadVolumeFactor = 1;
            }

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
