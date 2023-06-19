using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
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

public class Uniotaku : TorrentIndexerBase<UniotakuSettings>
{
    public override string Name => "UniOtaku";
    public override string[] IndexerUrls => new[] { "https://tracker.uniotaku.com/" };
    public override string Description => "UniOtaku is a BRAZILIAN Semi-Private Torrent Tracker for ANIME";
    public override string Language => "pt-BR";
    public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public Uniotaku(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new UniotakuRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new UniotakuParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var loginUrl = Settings.BaseUrl + "account-login.php";
        var requestBuilder = new HttpRequestBuilder(loginUrl);

        var cookies = Cookies;
        Cookies = null;

        var authLoginRequest = requestBuilder
            .Post()
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("manter", "1")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", Settings.BaseUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector(".login-content span.text-red")?.TextContent.Trim();

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return !httpResponse.GetCookies().ContainsKey("uid") || !httpResponse.GetCookies().ContainsKey("pass");
    }

    public override async Task<byte[]> Download(Uri link)
    {
        var request = new HttpRequestBuilder(link.ToString())
            .SetCookies(GetCookies() ?? new Dictionary<string, string>())
            .Accept(HttpAccept.Html)
            .Build();

        var response = await _httpClient.ExecuteProxiedAsync(request, Definition);

        var parser = new HtmlParser();
        var dom = parser.ParseDocument(response.Content);
        var downloadLink = dom.QuerySelector("a[href^=\"download.php?id=\"]")?.GetAttribute("href")?.Trim();

        if (downloadLink == null)
        {
            throw new Exception($"Failed to fetch download link from {link}");
        }

        return await base.Download(new Uri(Settings.BaseUrl + downloadLink));
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

        caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.MoviesOther, "Filme");
        caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.TVAnime, "OVA");
        caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.BooksComics, "Mangá");
        caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.TVOther, "Dorama");
        caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.Audio, "OST");
        caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.TVAnime, "Anime Completo");
        caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.BooksComics, "Mangá Completo");
        caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.TVOther, "Dorama Completo");
        caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.XXX, "Hentai");
        caps.Categories.AddCategoryMapping(56, NewznabStandardCategory.XXXOther, "H Doujinshi");
        caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.TVOther, "Tokusatsu");

        return caps;
    }
}

public class UniotakuRequestGenerator : IIndexerRequestGenerator
{
    private readonly UniotakuSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public UniotakuRequestGenerator(UniotakuSettings settings, IndexerCapabilities capabilities)
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

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

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
        if (!string.IsNullOrWhiteSpace(term))
        {
            term = "%" + Regex.Replace(term, @"[ -._]+", "%").Trim() + "%";
        }

        var categoryMapping = _capabilities.Categories.MapTorznabCapsToTrackers(categories);

        var parameters = new NameValueCollection
        {
            { "categoria", categoryMapping.FirstIfSingleOrDefault("0") },
            { "grupo", "0" },
            { "status", _settings.FreeleechOnly ? "1" : "0" },
            { "ordenar", "0" },
            { "start", "0" },
            { "length", "100" },
            { "search[value]", term ?? string.Empty },
            { "search[regex]", "false" },
        };

        var searchUrl = $"{_settings.BaseUrl}torrents_.php?{parameters.GetQueryString()}";

        yield return new IndexerRequest(searchUrl, HttpAccept.Html);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class UniotakuParser : IParseIndexerResponse
{
    private readonly UniotakuSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public UniotakuParser(UniotakuSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();

        var jsonContent = JObject.Parse(indexerResponse.Content);

        var publishDate = DateTime.Now;
        foreach (var item in jsonContent.Value<JArray>("data"))
        {
            var detailsDom = parser.ParseDocument(item.SelectToken("[0]").Value<string>());
            var categoryDom = parser.ParseDocument(item.SelectToken("[1]").Value<string>());
            var groupDom = parser.ParseDocument(item.SelectToken("[7]").Value<string>());

            var qTitleLink = detailsDom.QuerySelector("a[href^=\"torrents-details.php?id=\"]");
            var title = qTitleLink?.TextContent.Trim();
            var infoUrl = _settings.BaseUrl + qTitleLink?.GetAttribute("href");

            var category = categoryDom.QuerySelector("img[alt]")?.GetAttribute("alt")?.Trim() ?? "Anime";

            var releaseGroup = groupDom.QuerySelector("a[href*=\"teams-view.php?id=\"]")?.TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(releaseGroup))
            {
                title += $" [{releaseGroup}]";
            }

            var seeders = item.SelectToken("[3]")?.Value<int>();
            var leechers = item.SelectToken("[4]")?.Value<int>();

            publishDate = publishDate.AddMinutes(-1);

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = infoUrl,
                Title = title,
                Categories = _categories.MapTrackerCatDescToNewznab(category),
                Size = ParseUtil.GetBytes(item.SelectToken("[6]")?.Value<string>()),
                Grabs = item.SelectToken("[5]")?.Value<int>(),
                Seeders = seeders,
                Peers = seeders + leechers,
                PublishDate = publishDate,
                DownloadVolumeFactor =
                    detailsDom.QuerySelector("img[src*=\"images/free.gif\"]") != null ? 0 :
                    detailsDom.QuerySelector("img[src*=\"images/silverdownload.gif\"]") != null ? 0.5 : 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 0.7
            };

            releaseInfos.Add(release);
        }

        return releaseInfos;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class UniotakuSettings : UserPassTorrentBaseSettings
{
    public UniotakuSettings()
    {
        FreeleechOnly = false;
    }

    [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search Freeleech torrents only")]
    public bool FreeleechOnly { get; set; }
}
