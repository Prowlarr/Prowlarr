using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
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

public class FunFile : TorrentIndexerBase<UserPassTorrentBaseSettings>
{
    public override string Name => "FunFile";
    public override string[] IndexerUrls => new[] { "https://www.funfile.org/" };
    public override string Description => "FunFile is a general tracker";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public FunFile(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new FunFileRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new FunFileParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl + "takelogin.php")
        {
            LogResponseContent = true,
            AllowAutoRedirect = true,
            Method = HttpMethod.Post
        };

        var authLoginRequest = requestBuilder
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("returnto", "")
            .AddFormParameter("login", "Login")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            using var dom = await parser.ParseDocumentAsync(response.Content);
            var errorMessage = dom.QuerySelector("td.mf_content")?.TextContent.Trim();

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

        caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PC, "Applications");
        caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.AudioAudiobook, "Audio Books");
        caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "Ebook");
        caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Games");
        caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.OtherMisc, "Miscellaneous");
        caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.PCMobileOther, "Portable");
        caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.Other, "Tutorials");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TV, "TV");

        return caps;
    }
}

public class FunFileRequestGenerator : IIndexerRequestGenerator
{
    private readonly UserPassTorrentBaseSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public FunFileRequestGenerator(UserPassTorrentBaseSettings settings, IndexerCapabilities capabilities)
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

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
    {
        var parameters = new NameValueCollection
        {
            { "cat", "0" },
            { "incldead", "1" },
            { "showspam", "1" },
            { "s_title", "1" }
        };

        if (imdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Set("search", imdbId);
            parameters.Set("s_desc", "1");
        }
        else if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("search", term);
        }

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
        if (queryCats.Any())
        {
            queryCats.ForEach(cat => parameters.Set($"c{cat}", "1"));
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

public class FunFileParser : IParseIndexerResponse
{
    private readonly UserPassTorrentBaseSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    private readonly List<string> _validTagList = new ()
    {
        "action",
        "adventure",
        "animation",
        "biography",
        "comedy",
        "crime",
        "documentary",
        "drama",
        "family",
        "fantasy",
        "game-show",
        "history",
        "home_&_garden",
        "home_and_garden",
        "horror",
        "music",
        "musical",
        "mystery",
        "news",
        "reality",
        "reality-tv",
        "romance",
        "sci-fi",
        "science-fiction",
        "short",
        "sport",
        "talk-show",
        "thriller",
        "travel",
        "war",
        "western"
    };

    private readonly char[] _delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };

    public FunFileParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<TorrentInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("table.mainframe table[cellpadding=\"2\"] > tbody > tr:has(td.row3)");
        foreach (var row in rows)
        {
            var qDownloadLink = row.QuerySelector("a[href^=\"download.php\"]");
            if (qDownloadLink == null)
            {
                throw new Exception("Download links not found. Make sure you can download from the website.");
            }

            var downloadUrl = _settings.BaseUrl + qDownloadLink.GetAttribute("href");

            var qDetailsLink = row.QuerySelector("a[href^=\"details.php?id=\"]");
            var title = qDetailsLink?.GetAttribute("title")?.Trim();
            var infoUrl = _settings.BaseUrl + qDetailsLink?.GetAttribute("href")?.Replace("&hit=1", "");

            var categoryLink = row.QuerySelector("a[href^=\"browse.php?cat=\"]")?.GetAttribute("href");
            var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");

            var seeders = ParseUtil.CoerceInt(row.Children[9].TextContent);
            var leechers = ParseUtil.CoerceInt(row.Children[10].TextContent);

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(cat),
                Size = ParseUtil.GetBytes(row.Children[7].TextContent),
                Files = ParseUtil.CoerceInt(row.Children[3].TextContent),
                Grabs = ParseUtil.CoerceInt(row.Children[8].TextContent),
                Seeders = seeders,
                Peers = leechers + seeders,
                PublishDate = DateTimeUtil.FromTimeAgo(row.Children[5].TextContent),
                DownloadVolumeFactor = 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 172800 // 48 hours
            };

            var nextRow = row.NextElementSibling;
            if (nextRow != null)
            {
                var qStats = nextRow.QuerySelector("table > tbody > tr:nth-child(3)");
                release.UploadVolumeFactor = ParseUtil.CoerceDouble(qStats?.Children[0].TextContent.Replace("X", ""));
                release.DownloadVolumeFactor = ParseUtil.CoerceDouble(qStats?.Children[1].TextContent.Replace("X", ""));

                release.Description = nextRow.QuerySelector("span[style=\"float:left\"]")?.TextContent.Trim();
                var genres = release.Description.ToLower().Replace(" & ", "_&_").Replace(" and ", "_and_");

                var releaseGenres = _validTagList.Intersect(genres.Split(_delimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                release.Genres = releaseGenres.Select(x => x.Replace("_", " ")).ToList();
            }

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
