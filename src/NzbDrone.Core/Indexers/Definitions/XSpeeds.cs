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
            using var dom = await parser.ParseDocumentAsync(response.Content);
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

        caps.Categories.AddCategoryMapping(70, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(113, NewznabStandardCategory.TVAnime, "Anime Boxsets");
        caps.Categories.AddCategoryMapping(112, NewznabStandardCategory.MoviesOther, "Anime Movies");
        caps.Categories.AddCategoryMapping(111, NewznabStandardCategory.MoviesOther, "Anime TV");
        caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
        caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Books, "Books Magazines");
        caps.Categories.AddCategoryMapping(68, NewznabStandardCategory.MoviesOther, "Cams/TS");
        caps.Categories.AddCategoryMapping(140, NewznabStandardCategory.TVDocumentary, "Documentary");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesDVD, "DVDR");
        caps.Categories.AddCategoryMapping(109, NewznabStandardCategory.MoviesBluRay, "Bluray Disc");
        caps.Categories.AddCategoryMapping(131, NewznabStandardCategory.TVSport, "Fighting");
        caps.Categories.AddCategoryMapping(134, NewznabStandardCategory.TVSport, "Fighting/Boxing");
        caps.Categories.AddCategoryMapping(133, NewznabStandardCategory.TVSport, "Fighting/MMA");
        caps.Categories.AddCategoryMapping(132, NewznabStandardCategory.TVSport, "Fighting/Wrestling");
        caps.Categories.AddCategoryMapping(72, NewznabStandardCategory.MoviesForeign, "Foreign");
        caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.TVForeign, "Foreign Boxsets");
        caps.Categories.AddCategoryMapping(114, NewznabStandardCategory.MoviesForeign, "Foreign Movies");
        caps.Categories.AddCategoryMapping(115, NewznabStandardCategory.TVForeign, "Foreign TV");
        caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.ConsoleOther, "Games Console");
        caps.Categories.AddCategoryMapping(105, NewznabStandardCategory.ConsoleOther, "Games Console/Nintendo");
        caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.ConsolePS4, "Games Console/Playstation");
        caps.Categories.AddCategoryMapping(106, NewznabStandardCategory.ConsoleXBox, "Games Console/XBOX");
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Games PC");
        caps.Categories.AddCategoryMapping(108, NewznabStandardCategory.PC, "Games PC/Linux");
        caps.Categories.AddCategoryMapping(107, NewznabStandardCategory.PCMac, "Games PC/Mac");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.Movies, "Movie Boxsets");
        caps.Categories.AddCategoryMapping(118, NewznabStandardCategory.MoviesUHD, "Movie Boxsets/Boxset 4K");
        caps.Categories.AddCategoryMapping(143, NewznabStandardCategory.MoviesHD, "Movie Boxsets/Boxset HD");
        caps.Categories.AddCategoryMapping(119, NewznabStandardCategory.MoviesHD, "Movie Boxsets/Boxset HEVC");
        caps.Categories.AddCategoryMapping(144, NewznabStandardCategory.MoviesSD, "Movie Boxsets/Boxset SD");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping(117, NewznabStandardCategory.MoviesUHD, "Movies 4K");
        caps.Categories.AddCategoryMapping(145, NewznabStandardCategory.MoviesHD, "Movies HD");
        caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.MoviesHD, "Movies HEVC");
        caps.Categories.AddCategoryMapping(146, NewznabStandardCategory.MoviesSD, "Movies SD");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping(135, NewznabStandardCategory.AudioLossless, "Music/FLAC");
        caps.Categories.AddCategoryMapping(136, NewznabStandardCategory.Audio, "Music Boxset");
        caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.AudioVideo, "Music Videos");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.Other, "Other");
        caps.Categories.AddCategoryMapping(125, NewznabStandardCategory.Other, "Other/Pictures");
        caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.TVOther, "Soaps");
        caps.Categories.AddCategoryMapping(83, NewznabStandardCategory.TVOther, "Specials");
        caps.Categories.AddCategoryMapping(139, NewznabStandardCategory.TV, "TOTM (Freeleech)");
        caps.Categories.AddCategoryMapping(138, NewznabStandardCategory.TV, "TOTW (x2 upload)");
        caps.Categories.AddCategoryMapping(139, NewznabStandardCategory.Movies, "TOTM (Freeleech)");
        caps.Categories.AddCategoryMapping(138, NewznabStandardCategory.Movies, "TOTW (x2 upload)");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.TVSport, "Sports");
        caps.Categories.AddCategoryMapping(88, NewznabStandardCategory.TVSport, "Sports/Football");
        caps.Categories.AddCategoryMapping(86, NewznabStandardCategory.TVSport, "Sports/MotorSports");
        caps.Categories.AddCategoryMapping(89, NewznabStandardCategory.TVSport, "Sports/Olympics");
        caps.Categories.AddCategoryMapping(126, NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping(127, NewznabStandardCategory.TVUHD, "TV 4K");
        caps.Categories.AddCategoryMapping(129, NewznabStandardCategory.TVHD, "TV HD");
        caps.Categories.AddCategoryMapping(130, NewznabStandardCategory.TVHD, "TV HEVC");
        caps.Categories.AddCategoryMapping(128, NewznabStandardCategory.TVSD, "TV SD");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.TVSD, "TV Boxsets");
        caps.Categories.AddCategoryMapping(120, NewznabStandardCategory.TVUHD, "Boxset TV 4K");
        caps.Categories.AddCategoryMapping(76, NewznabStandardCategory.TVHD, "Boxset TV HD");
        caps.Categories.AddCategoryMapping(97, NewznabStandardCategory.TVHD, "Boxset TV HEVC");
        caps.Categories.AddCategoryMapping(147, NewznabStandardCategory.TVSD, "Boxset TV SD");

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
        using var dom = parser.ParseDocument(indexerResponse.Content);

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
