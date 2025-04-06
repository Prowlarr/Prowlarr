using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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

namespace NzbDrone.Core.Indexers.Definitions;

public class NorBits : TorrentIndexerBase<NorBitsSettings>
{
    public override string Name => "NorBits";
    public override string[] IndexerUrls => new[] { "https://norbits.net/" };
    public override string Description => "NorBits is a Norwegian Private site for MOVIES / TV / GENERAL";
    public override string Language => "nb-NO";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public NorBits(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new NorBitsRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new NorBitsParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true
        };

        var indexPage = await ExecuteAuth(requestBuilder.Build());

        var loginUrl = $"{Settings.BaseUrl.TrimEnd('/')}/login.php";

        var requestBuilder2 = new HttpRequestBuilder(loginUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true
        };

        var authLoginRequest = requestBuilder2
            .SetCookies(indexPage.GetCookies())
            .Build();

        // Get login page -- (not used, but simulation needed by tracker security's checks)
        await ExecuteAuth(authLoginRequest);

        var requestBuilder3 = new HttpRequestBuilder($"{Settings.BaseUrl.TrimEnd('/')}/takelogin.php")
        {
            LogResponseContent = true,
            AllowAutoRedirect = true,
            SuppressHttpError = true
        };

        var authLoginCheckRequest = requestBuilder3
            .Post()
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("code", Settings.TwoFactorAuthCode ?? string.Empty)
            .AddFormParameter("logout", "no")
            .AddFormParameter("returnto", "/")
            .SetCookies(indexPage.GetCookies())
            .SetHeader("Referer", loginUrl)
            .Build();

        var loginResponse = await ExecuteAuth(authLoginCheckRequest);

        if (!loginResponse.GetCookies().ContainsKey("uid"))
        {
            throw new IndexerAuthException("Login failed");
        }

        var cookies = loginResponse.GetCookies();
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

        caps.Categories.AddCategoryMapping("main_cat[]=1", NewznabStandardCategory.Movies, "Filmer");
        caps.Categories.AddCategoryMapping("main_cat[]=2", NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping("main_cat[]=3", NewznabStandardCategory.PC, "Programmer");
        caps.Categories.AddCategoryMapping("main_cat[]=4", NewznabStandardCategory.Console, "Spill");
        caps.Categories.AddCategoryMapping("main_cat[]=5", NewznabStandardCategory.Audio, "Musikk");
        caps.Categories.AddCategoryMapping("main_cat[]=6", NewznabStandardCategory.Books, "Tidsskrift");
        caps.Categories.AddCategoryMapping("main_cat[]=7", NewznabStandardCategory.AudioAudiobook, "Lydbøker");
        caps.Categories.AddCategoryMapping("main_cat[]=8", NewznabStandardCategory.AudioVideo, "Musikkvideoer");
        caps.Categories.AddCategoryMapping("main_cat[]=40", NewznabStandardCategory.AudioOther, "Podcasts");

        return caps;
    }
}

public class NorBitsRequestGenerator : IIndexerRequestGenerator
{
    private readonly NorBitsSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public NorBitsRequestGenerator(NorBitsSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
    {
        var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/browse.php";

        var parameters = new NameValueCollection
        {
            { "incldead", "1" },
            { "fullsearch", _settings.UseFullSearch ? "1" : "0" },
            { "scenerelease", "0" }
        };

        if (_settings.FreeLeechOnly)
        {
            parameters.Set("FL", "1");
        }

        var searchTerm = "search=";

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            searchTerm = "imdbsearch=" + imdbId;
        }
        else if (!string.IsNullOrWhiteSpace(term))
        {
            searchTerm = "search=" + term.UrlEncode(Encoding.UTF8);
        }

        searchUrl += "?" + searchTerm + "&" + parameters.GetQueryString();

        var categoriesList = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
        if (categoriesList.Any())
        {
            searchUrl += "&" + string.Join("&", categoriesList);
        }

        var request = new IndexerRequest(searchUrl, HttpAccept.Html);

        yield return request;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.FullImdbId));

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

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class NorBitsParser : IParseIndexerResponse
{
    private readonly NorBitsSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public NorBitsParser(NorBitsSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("#torrentTable > tbody > tr").Skip(1).ToCollection();

        foreach (var row in rows)
        {
            var link = _settings.BaseUrl + row.QuerySelector("td:nth-of-type(2) > a[href*=\"download.php?id=\"]")?.GetAttribute("href")?.TrimStart('/');
            var qDetails = row.QuerySelector("td:nth-of-type(2) > a[href*=\"details.php?id=\"]");

            var title = qDetails?.GetAttribute("title")?.Trim();
            var details = _settings.BaseUrl + qDetails?.GetAttribute("href")?.TrimStart('/');

            var catQuery = row.QuerySelector("td:nth-of-type(1) a[href*=\"main_cat[]\"]")?.GetAttribute("href")?.Split('?').Last().Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var category = catQuery?.FirstOrDefault(x => x.StartsWith("main_cat[]=", StringComparison.OrdinalIgnoreCase));

            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)")?.TextContent);
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)")?.TextContent);

            var release = new TorrentInfo
            {
                Guid = details,
                InfoUrl = details,
                DownloadUrl = link,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(category),
                Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(7)")?.TextContent),
                Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(3) > a")?.TextContent.Trim()),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)")?.FirstChild?.TextContent.Trim()),
                Seeders = seeders,
                Peers = seeders + leechers,
                PublishDate = DateTime.ParseExact(row.QuerySelector("td:nth-of-type(5)")?.TextContent.Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture),
                DownloadVolumeFactor = 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 172800 // 48 hours
            };

            var genres = row.QuerySelector("span.genres")?.TextContent;
            if (!string.IsNullOrEmpty(genres))
            {
                genres = genres.Trim().Replace("\xA0", " ").Replace("(", "").Replace(")", "").Replace(" | ", ",");
                release.Description = genres;
                release.Genres = genres.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var imdbId = ParseUtil.GetImdbId(row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href")?.TrimEnd('/')?.Split('/')?.LastOrDefault());
            release.ImdbId = imdbId ?? 0;

            if (row.QuerySelector("img[title=\"100% freeleech\"]") != null)
            {
                release.DownloadVolumeFactor = 0;
            }
            else if (row.QuerySelector("img[title=\"Halfleech\"]") != null)
            {
                release.DownloadVolumeFactor = 0.5;
            }
            else if (row.QuerySelector("img[title=\"90% Freeleech\"]") != null)
            {
                release.DownloadVolumeFactor = 0.1;
            }

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class NorBitsSettings : UserPassTorrentBaseSettings
{
    public NorBitsSettings()
    {
        UseFullSearch = false;
    }

    [FieldDefinition(4, Label = "2FA code", Type = FieldType.Textbox, HelpText = "Only fill in the <b>2FA code</b> box if you have enabled <b>2FA</b> on the NorBits Web Site. Otherwise just leave it empty.")]
    public string TwoFactorAuthCode { get; set; }

    [FieldDefinition(5, Label = "Use Full Search", Type = FieldType.Checkbox, HelpText = "Use Full Search from Site")]
    public bool UseFullSearch { get; set; }

    [FieldDefinition(6, Label = "FreeLeech Only", Type = FieldType.Checkbox, HelpText = "Search FreeLeech torrents only")]
    public bool FreeLeechOnly { get; set; }
}
