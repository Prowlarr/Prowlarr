using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

public class SpeedCD : TorrentIndexerBase<SpeedCDSettings>
{
    public override string Name => "SpeedCD";
    public override string[] IndexerUrls => new[]
    {
        "https://speed.cd/",
        "https://speed.click/",
        "https://speeders.me/"
    };
    public override string Description => "Your home now!";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override bool SupportsPagination => true;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public SpeedCD(IIndexerHttpClient httpClient,
        IEventAggregator eventAggregator,
        IIndexerStatusService indexerStatusService,
        IConfigService configService,
        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new SpeedCDRequestGenerator(Settings, Capabilities, Encoding);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new SpeedCDParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        var requestBuilder = new HttpRequestBuilder($"{Settings.BaseUrl.TrimEnd('/')}/checkpoint/API")
        {
            Method = HttpMethod.Post,
            LogResponseContent = true,
            AllowAutoRedirect = true
        };

        var loginPage = await ExecuteAuth(requestBuilder.AddFormParameter("username", Settings.Username).Build());

        var tokenRegex = new Regex(@"name=\\""a\\"" value=\\""([^""]+)\\""");
        var matches = tokenRegex.Match(loginPage.Content);
        if (!matches.Success)
        {
            throw new IndexerAuthException("Error parsing the login form");
        }

        var token = matches.Groups[1].Value;

        Cookies = null;

        var requestBuilder2 = new HttpRequestBuilder($"{Settings.BaseUrl.TrimEnd('/')}/checkpoint/")
        {
            Method = HttpMethod.Post,
            LogResponseContent = true,
            AllowAutoRedirect = true
        };

        var authLoginRequest = requestBuilder2
            .AddFormParameter("pwd", Settings.Password)
            .AddFormParameter("a", token)
            .SetCookies(loginPage.GetCookies())
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector("h5")?.TextContent.Trim();

            if (response.Content.Contains("Wrong Captcha!"))
            {
                errorMessage = "Captcha required due to a failed login attempt. Login via a browser to whitelist your IP and then reconfigure Prowlarr.";
            }

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        var cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded.");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return !httpResponse.Content.Contains("/browse.php");
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

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesOther, "Movies/XviD");
        caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.Movies, "Movies/Packs");
        caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.Movies, "Movies/Kids");
        caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.MoviesHD, "Movies/HD");
        caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies, "Movies/DiVERSiTY");
        caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.MoviesBluRay, "Movies/B-Ray");
        caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Movies3D, "Movies/3D");
        caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.MoviesDVD, "Movies/DVD-R");
        caps.Categories.AddCategoryMapping(56, NewznabStandardCategory.Movies, "Movies/Anime");
        caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.TVSport, "TV/Sports");
        caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.TVHD, "TV/B-Ray");
        caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.TVSD, "TV/DVD-R");
        caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TV, "TV/Packs");
        caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.TV, "TV/Kids");
        caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.TV, "TV/DiVERSiTY");
        caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.TVHD, "TV/HD");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD, "TV/Episodes");
        caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVAnime, "TV/Anime");
        caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCISO, "Games/PC ISO");
        caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.ConsoleWii, "Games/Wii");
        caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.ConsolePS3, "Games/PS3");
        caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.Console, "Games/Nintendo");
        caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.ConsoleXBox360, "Games/XboX360");
        caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.PCMobileOther, "Mobile");
        caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PC0day, "Apps/0DAY");
        caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.PCMac, "Mac");
        caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.Books, "Educational");
        caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "Books-Mags");
        caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.Audio, "Music/Audio");
        caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music/Flac");
        caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.Audio, "Music/Pack");
        caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.AudioVideo, "Music/Video");

        return caps;
    }
}

public class SpeedCDRequestGenerator : IIndexerRequestGenerator
{
    private readonly SpeedCDSettings _settings;
    private readonly IndexerCapabilities _capabilities;
    private readonly Encoding _encoding;

    public SpeedCDRequestGenerator(SpeedCDSettings settings, IndexerCapabilities capabilities, Encoding encoding)
    {
        _settings = settings;
        _capabilities = capabilities;
        _encoding = encoding;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var term = $"{searchCriteria.SanitizedSearchTerm}";

        if (searchCriteria.FullImdbId.IsNotNullOrWhiteSpace())
        {
            term = $"{searchCriteria.FullImdbId}";
        }

        pageableRequests.Add(GetPagedRequests(term.Trim(), searchCriteria, searchCriteria.FullImdbId.IsNotNullOrWhiteSpace()));

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

        var term = $"{searchCriteria.SanitizedTvSearchString}";

        if (searchCriteria.FullImdbId.IsNotNullOrWhiteSpace())
        {
            term = $"{searchCriteria.FullImdbId}";

            if (searchCriteria.EpisodeSearchString.IsNotNullOrWhiteSpace())
            {
                term += $" {searchCriteria.EpisodeSearchString}";

                if (searchCriteria.Season.HasValue && searchCriteria.Episode.IsNullOrWhiteSpace())
                {
                    term += "*";
                }
            }
        }

        pageableRequests.Add(GetPagedRequests(term.Trim(), searchCriteria, searchCriteria.FullImdbId.IsNotNullOrWhiteSpace()));

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

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, SearchCriteriaBase searchCriteria, bool deep = false)
    {
        var parameters = new List<string>();

        var catList = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);
        foreach (var cat in catList)
        {
            parameters.Add(cat);
        }

        if (_settings.FreeleechOnly)
        {
            parameters.Add("freeleech");
        }

        if (_settings.ExcludeArchives)
        {
            parameters.Add("norar");
        }

        if (deep)
        {
            parameters.Add("deep");
        }

        if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Add("q");
            parameters.Add(term.UrlEncode(_encoding));
        }

        if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
        {
            var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;

            parameters.Add("p");
            parameters.Add(page.ToString());
        }

        var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/browse/{string.Join("/", parameters)}";

        yield return new IndexerRequest(searchUrl, HttpAccept.Html);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class SpeedCDParser : IParseIndexerResponse
{
    private readonly SpeedCDSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public SpeedCDParser(SpeedCDSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("div.boxContent > table > tbody > tr");
        foreach (var row in rows)
        {
            var title = CleanTitle(row.QuerySelector("td:nth-child(2) > div > a[href^=\"/t/\"]")?.TextContent);
            var downloadUrl = _settings.BaseUrl + row.QuerySelector("td:nth-child(4) a[href^=\"/download/\"]")?.GetAttribute("href")?.TrimStart('/');
            var infoUrl = _settings.BaseUrl + row.QuerySelector("td:nth-child(2) > div > a[href^=\"/t/\"]")?.GetAttribute("href")?.TrimStart('/');
            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent);
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent);
            var dateAdded = row.QuerySelector("td:nth-child(2) span[class^=\"elapsedDate\"]")?.GetAttribute("title")?.Replace(" at", "");
            var cat = row.QuerySelector("td:nth-child(1) a")?.GetAttribute("href")?.Split('/').Last();
            var downloadVolumeFactor = row.QuerySelector("td:nth-child(2) span:contains(\"[Freeleech]\")") != null ? 0 : 1;

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(cat),
                PublishDate = DateTime.ParseExact(dateAdded, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(6)")?.TextContent),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)")?.TextContent),
                Seeders = seeders,
                Peers = seeders + leechers,
                DownloadVolumeFactor = downloadVolumeFactor,
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 259200 // 72 hours
            };

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    private static string CleanTitle(string title)
    {
        title = Regex.Replace(title, @"\[REQ(UEST)?\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return title.Trim(' ', '.');
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class SpeedCDSettings : UserPassTorrentBaseSettings
{
    [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech torrents only")]
    public bool FreeleechOnly { get; set; }

    [FieldDefinition(5, Label = "Exclude Archives", Type = FieldType.Checkbox, HelpText = "Exclude torrents containing RAR files from results")]
    public bool ExcludeArchives { get; set; }
}
