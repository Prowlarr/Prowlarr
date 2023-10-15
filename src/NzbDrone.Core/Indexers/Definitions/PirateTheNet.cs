using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
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
[Obsolete("PirateTheNet has shutdown 2023-10-14")]
public class PirateTheNet : TorrentIndexerBase<UserPassTorrentBaseSettings>
{
    public override string Name => "PirateTheNet";
    public override string[] IndexerUrls => new[] { "https://piratethenet.org/" };
    public override string[] LegacyUrls => new[] { "http://piratethenet.org/" };
    public override string Description => "PirateTheNet (PTN) is a ratioless movie tracker.";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    private string LoginUrl => Settings.BaseUrl + "takelogin.php";
    private string CaptchaUrl => Settings.BaseUrl + "simpleCaptcha.php?numImages=1";

    public PirateTheNet(IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new PirateTheNetRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new PirateTheNetParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var captchaPage = await ExecuteAuth(new HttpRequest(CaptchaUrl));

        var captchaResponse = JsonConvert.DeserializeAnonymousType(captchaPage.Content, new
        {
            images = new[] { new { hash = string.Empty } }
        });

        var requestBuilder = new HttpRequestBuilder(LoginUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true,
            Method = HttpMethod.Post
        };

        var authLoginRequest = requestBuilder
            .SetCookies(captchaPage.GetCookies())
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("captchaSelection", captchaResponse.images[0].hash)
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", LoginUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            throw new IndexerAuthException("Login Failed.");
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
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            }
        };

        caps.Categories.AddCategoryMapping("1080P", NewznabStandardCategory.MoviesHD, "1080P");
        caps.Categories.AddCategoryMapping("2160P", NewznabStandardCategory.MoviesHD, "2160P");
        caps.Categories.AddCategoryMapping("720P", NewznabStandardCategory.MoviesHD, "720P");
        caps.Categories.AddCategoryMapping("BDRip", NewznabStandardCategory.MoviesSD, "BDRip");
        caps.Categories.AddCategoryMapping("BluRay", NewznabStandardCategory.MoviesBluRay, "BluRay");
        caps.Categories.AddCategoryMapping("BRRip", NewznabStandardCategory.MoviesSD, "BRRip");
        caps.Categories.AddCategoryMapping("DVDR", NewznabStandardCategory.MoviesDVD, "DVDR");
        caps.Categories.AddCategoryMapping("DVDRip", NewznabStandardCategory.MoviesSD, "DVDRip");
        caps.Categories.AddCategoryMapping("FLAC", NewznabStandardCategory.AudioLossless, "FLAC OST");
        caps.Categories.AddCategoryMapping("MP3", NewznabStandardCategory.AudioMP3, "MP3 OST");
        caps.Categories.AddCategoryMapping("MP4", NewznabStandardCategory.MoviesOther, "MP4");
        caps.Categories.AddCategoryMapping("Packs", NewznabStandardCategory.MoviesOther, "Packs");
        caps.Categories.AddCategoryMapping("R5", NewznabStandardCategory.MoviesDVD, "R5 / SCR");
        caps.Categories.AddCategoryMapping("Remux", NewznabStandardCategory.MoviesOther, "Remux");
        caps.Categories.AddCategoryMapping("TVRip", NewznabStandardCategory.MoviesOther, "TVRip");
        caps.Categories.AddCategoryMapping("WebRip", NewznabStandardCategory.MoviesWEBDL, "WebRip");

        return caps;
    }
}

public class PirateTheNetRequestGenerator : IIndexerRequestGenerator
{
    private readonly UserPassTorrentBaseSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public PirateTheNetRequestGenerator(UserPassTorrentBaseSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
    {
        var parameters = new NameValueCollection
        {
            { "action", "torrentstable" },
            { "viewtype", "0" },
            { "visiblecategories", "Action,Adventure,Animation,Biography,Comedy,Crime,Documentary,Drama,Family,Fantasy,History,Horror,Kids,Music,Mystery,Packs,Romance,Sci-Fi,Short,Sports,Thriller,War,Western" },
            { "page", "1" },
            { "visibility", "showall" },
            { "compression", "showall" },
            { "sort", "added" },
            { "order", "DESC" },
            { "titleonly", "true" },
            { "packs", "showall" },
            { "bookmarks", "showall" },
            { "subscriptions", "showall" },
            { "skw", "showall" }
        };

        if (imdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Set("advancedsearchparameters", $"[imdb={imdbId}]");
        }
        else if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("searchstring", term);
        }

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
        if (queryCats.Any())
        {
            parameters.Set("hiddenqualities", string.Join(",", queryCats));
        }

        var searchUrl = _settings.BaseUrl + "torrentsutils.php";

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

public class PirateTheNetParser : IParseIndexerResponse
{
    private readonly UserPassTorrentBaseSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public PirateTheNetParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("table.main > tbody > tr");
        foreach (var row in rows.Skip(1))
        {
            var qDetails = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)");
            var title = qDetails?.GetAttribute("alt")?.Trim();

            var infoUrl = _settings.BaseUrl + qDetails?.GetAttribute("href")?.TrimStart('/');
            var downloadUrl = _settings.BaseUrl + row.QuerySelector("td > a:has(img[alt=\"Download Torrent\"])")?.GetAttribute("href")?.TrimStart('/');

            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)")?.TextContent);
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)")?.TextContent);

            var cat = row.QuerySelector("td:nth-of-type(1) > a > img")?.GetAttribute("src")?.Split('/').Last().Split('.').First() ?? "packs";

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(cat),
                Seeders = seeders,
                Peers = seeders + leechers,
                Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(7)")?.TextContent.Trim()),
                Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(4)")?.TextContent),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent),
                DownloadVolumeFactor = 0, // ratioless
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 259200, // 72 hours
            };

            var added = row.QuerySelector("td:nth-of-type(6) > nobr")?.TextContent.Trim();
            if (added.StartsWith("Today "))
            {
                release.PublishDate = DateTime.Now.Date + DateTime.ParseExact(added.Split(" ", 2).Last(), "hh:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            }
            else if (added.StartsWith("Yesterday "))
            {
                release.PublishDate = DateTime.Now.AddDays(-1).Date + DateTime.ParseExact(added.Split(" ", 2).Last(), "hh:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            }
            else
            {
                release.PublishDate = DateTime.ParseExact(added, "MMM d yyyy hh:mm tt", CultureInfo.InvariantCulture);
            }

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
