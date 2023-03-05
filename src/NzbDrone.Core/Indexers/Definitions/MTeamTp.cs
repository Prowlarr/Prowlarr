using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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

namespace NzbDrone.Core.Indexers.Definitions;

public class MTeamTp : TorrentIndexerBase<MTeamTpSettings>
{
    public override string Name => "M-Team - TP";
    public override string[] IndexerUrls => new[] { "https://kp.m-team.cc/" };
    public override string Description => "M-Team TP (MTTP) is a CHINESE Private Torrent Tracker for HD MOVIES / TV / 3X";
    public override string Language => "zh-CN";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override int PageSize => 100;
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(10);
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public MTeamTp(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new MTeamTpRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new MTeamTpParser(Settings, Capabilities.Categories);
    }

    protected override IDictionary<string, string> GetCookies()
    {
        return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        if (!httpResponse.GetCookies().ContainsKey("tp"))
        {
            throw new IndexerAuthException("Invalid cookies. Check your settings.");
        }

        if (!httpResponse.Content.Contains("请求太频繁") && !httpResponse.Content.Contains("logout.php"))
        {
            throw new IndexerAuthException("Authentication with cookies failed.");
        }

        return false;
    }

    private static IndexerCapabilities SetCapabilities()
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

        caps.Categories.AddCategoryMapping(401, NewznabStandardCategory.MoviesSD, "Movie(電影)/SD");
        caps.Categories.AddCategoryMapping(419, NewznabStandardCategory.MoviesHD, "Movie(電影)/HD");
        caps.Categories.AddCategoryMapping(420, NewznabStandardCategory.MoviesDVD, "Movie(電影)/DVDiSo");
        caps.Categories.AddCategoryMapping(421, NewznabStandardCategory.MoviesBluRay, "Movie(電影)/Blu-Ray");
        caps.Categories.AddCategoryMapping(439, NewznabStandardCategory.MoviesOther, "Movie(電影)/Remux");
        caps.Categories.AddCategoryMapping(403, NewznabStandardCategory.TVSD, "TV Series(影劇/綜藝)/SD");
        caps.Categories.AddCategoryMapping(402, NewznabStandardCategory.TVHD, "TV Series(影劇/綜藝)/HD");
        caps.Categories.AddCategoryMapping(435, NewznabStandardCategory.TVSD, "TV Series(影劇/綜藝)/DVDiSo");
        caps.Categories.AddCategoryMapping(438, NewznabStandardCategory.TVHD, "TV Series(影劇/綜藝)/BD");
        caps.Categories.AddCategoryMapping(404, NewznabStandardCategory.TVDocumentary, "紀錄教育");
        caps.Categories.AddCategoryMapping(405, NewznabStandardCategory.TVAnime, "Anime(動畫)");
        caps.Categories.AddCategoryMapping(407, NewznabStandardCategory.TVSport, "Sports(運動)");
        caps.Categories.AddCategoryMapping(422, NewznabStandardCategory.PC0day, "Software(軟體)");
        caps.Categories.AddCategoryMapping(423, NewznabStandardCategory.PCGames, "PCGame(PC遊戲)");
        caps.Categories.AddCategoryMapping(427, NewznabStandardCategory.Books, "eBook(電子書)");
        caps.Categories.AddCategoryMapping(409, NewznabStandardCategory.Other, "Misc(其他)");

        // music
        caps.Categories.AddCategoryMapping(406, NewznabStandardCategory.AudioVideo, "MV(演唱)");
        caps.Categories.AddCategoryMapping(408, NewznabStandardCategory.AudioOther, "Music(AAC/ALAC)");
        caps.Categories.AddCategoryMapping(434, NewznabStandardCategory.Audio, "Music(無損)");

        // adult
        caps.Categories.AddCategoryMapping(410, NewznabStandardCategory.XXX, "AV(有碼)/HD Censored");
        caps.Categories.AddCategoryMapping(429, NewznabStandardCategory.XXX, "AV(無碼)/HD Uncensored");
        caps.Categories.AddCategoryMapping(424, NewznabStandardCategory.XXX, "AV(有碼)/SD Censored");
        caps.Categories.AddCategoryMapping(430, NewznabStandardCategory.XXX, "AV(無碼)/SD Uncensored");
        caps.Categories.AddCategoryMapping(426, NewznabStandardCategory.XXX, "AV(無碼)/DVDiSo Uncensored");
        caps.Categories.AddCategoryMapping(437, NewznabStandardCategory.XXX, "AV(有碼)/DVDiSo Censored");
        caps.Categories.AddCategoryMapping(431, NewznabStandardCategory.XXX, "AV(有碼)/Blu-Ray Censored");
        caps.Categories.AddCategoryMapping(432, NewznabStandardCategory.XXX, "AV(無碼)/Blu-Ray Uncensored");
        caps.Categories.AddCategoryMapping(436, NewznabStandardCategory.XXX, "AV(網站)/0Day");
        caps.Categories.AddCategoryMapping(425, NewznabStandardCategory.XXX, "IV(寫真影集)/Video Collection");
        caps.Categories.AddCategoryMapping(433, NewznabStandardCategory.XXX, "IV(寫真圖集)/Picture Collection");
        caps.Categories.AddCategoryMapping(411, NewznabStandardCategory.XXX, "H-Game(遊戲)");
        caps.Categories.AddCategoryMapping(412, NewznabStandardCategory.XXX, "H-Anime(動畫)");
        caps.Categories.AddCategoryMapping(413, NewznabStandardCategory.XXX, "H-Comic(漫畫)");

        return caps;
    }
}

public class MTeamTpRequestGenerator : IIndexerRequestGenerator
{
    private readonly MTeamTpSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    private enum RequestTypes
    {
        General,
        Music,
        Adult
    }

    private string SearchTorrentsUrl => _settings.BaseUrl + "torrents.php";
    private string SearchMusicUrl => _settings.BaseUrl + "music.php";
    private string SearchAdultUrl => _settings.BaseUrl + "adult.php";

    public MTeamTpRequestGenerator(MTeamTpSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(RequestTypes.General, searchCriteria, searchCriteria.SanitizedSearchTerm, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(RequestTypes.General, searchCriteria, searchCriteria.SanitizedTvSearchString, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(RequestTypes.Music, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(RequestTypes.General, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(RequestTypes.General, searchCriteria, searchCriteria.SanitizedSearchTerm));
        pageableRequests.Add(GetPagedRequests(RequestTypes.Music, searchCriteria, searchCriteria.SanitizedSearchTerm));
        pageableRequests.Add(GetPagedRequests(RequestTypes.Adult, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    private static int[] FilterIndexerCategories(IndexerCategory category, int[] categories)
    {
        if (category == null)
        {
            return Array.Empty<int>();
        }

        var allCategories = category.SubCategories.Concat(new[] { category });

        return allCategories.Where(c => categories.Contains(c.Id)).Select(c => c.Id).Distinct().ToArray();
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(RequestTypes requestType, SearchCriteriaBase searchCriteria, string searchTerm, string imdbId = null)
    {
        searchTerm ??= string.Empty;

        var categories = searchCriteria.Categories ?? Array.Empty<int>();
        var page = (int)(searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0 ? searchCriteria.Offset / searchCriteria.Limit : 0);

        var categoryMapping = _capabilities.Categories.MapTorznabCapsToTrackers(categories);

        var adultCategories = _capabilities.Categories.MapTorznabCapsToTrackers(FilterIndexerCategories(NewznabStandardCategory.XXX, categories));
        var musicCategories = _capabilities.Categories.MapTorznabCapsToTrackers(FilterIndexerCategories(NewznabStandardCategory.Audio, categories));
        var generalCategories = categoryMapping.Except(adultCategories).Except(musicCategories).ToList();

        switch (requestType)
        {
            case RequestTypes.Adult when _settings.AdultContent && adultCategories.Any():
                yield return BuildSearchRequest(SearchAdultUrl, adultCategories, searchTerm, page: page);
                break;
            case RequestTypes.Music when musicCategories.Any():
                yield return BuildSearchRequest(SearchMusicUrl, musicCategories, searchTerm, page: page);
                break;
            case RequestTypes.General when !categoryMapping.Any() || generalCategories.Any():
                yield return BuildSearchRequest(SearchTorrentsUrl, generalCategories, searchTerm, imdbId, page: page);
                break;
        }
    }

    private IndexerRequest BuildSearchRequest(string searchUrl, List<string> categories = null, string searchTerm = null, string imdbId = null, int page = 0)
    {
        if (searchUrl == null)
        {
            throw new ArgumentNullException(nameof(searchUrl));
        }

        var requestBuilder = new HttpRequestBuilder(searchUrl)
        {
            AllowAutoRedirect = true
        };

        if (categories != null && categories.Any())
        {
            categories.ForEach(cat => requestBuilder.AddQueryParam($"cat{cat}", "1"));
        }

        searchTerm = $"{(imdbId.IsNotNullOrWhiteSpace() ? imdbId : searchTerm)}".Trim();

        requestBuilder
            .AddQueryParam("incldead", "0")
            .AddQueryParam("spstate", _settings.FreeLeechOnly ? "2" : "0")
            .AddQueryParam("inclbookmarked", "0")
            .AddQueryParam("search", searchTerm)
            .AddQueryParam("search_area", imdbId.IsNotNullOrWhiteSpace() ? "4" : "0")
            .AddQueryParam("search_mode", "0")
            .SetHeader("Referer", searchUrl)
            .Accept(HttpAccept.Html);

        if (page > 0)
        {
            requestBuilder.AddQueryParam("page", page.ToString());
        }

        var request = requestBuilder.Build();

        if (_settings.UserAgent.IsNotNullOrWhiteSpace())
        {
            request.Headers.UserAgent = _settings.UserAgent;
        }

        return new IndexerRequest(request);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class MTeamTpParser : IParseIndexerResponse
{
    private readonly MTeamTpSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public MTeamTpParser(MTeamTpSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        CheckResponseStatus(indexerResponse);

        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        var doc = parser.ParseDocument(indexerResponse.Content);

        var rows = doc.QuerySelectorAll("table.torrents > tbody > tr:has(table.torrentname)");
        foreach (var row in rows)
        {
            // not available if IMDB tooltips are turned on
            var title = row.QuerySelector("a[href^=\"details.php?id=\"][title]")?.GetAttribute("title")?.Trim();
            if (title.IsNullOrWhiteSpace())
            {
                // shortened for long release names
                title = row.QuerySelector("a[href^=\"details.php?id=\"] > b")?.TextContent.Trim();
            }

            var infoUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"details.php?id=\"]")?.GetAttribute("href")?.Trim();
            var downloadUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"download.php?id=\"]")?.GetAttribute("href")?.Trim();

            var categoryLink = row.QuerySelector("a[href^=\"?cat=\"]")?.GetAttribute("href")?.Trim();
            var category = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");

            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td.rowfollow:nth-last-child(5)")?.TextContent.Trim());
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td.rowfollow:nth-last-child(4)")?.TextContent.Trim());

            var dateAdded = row.QuerySelector("td.rowfollow:nth-last-child(7) > span[title]")?.GetAttribute("title")?.Trim();

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(category),
                PublishDate = DateTime.TryParseExact($"{dateAdded} +08:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date) ? date : DateTime.Now,
                Size = ParseUtil.GetBytes(row.QuerySelector("td.rowfollow:nth-last-child(6)")?.TextContent.Trim()),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td.rowfollow:nth-last-child(3)")?.TextContent),
                Seeders = seeders,
                Peers = seeders + leechers,
                ImdbId = (int)ParseUtil.GetLongFromString(row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href")?.Trim()).GetValueOrDefault(),
                DownloadVolumeFactor = 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 172800 // 48 hours
            };

            switch (true)
            {
                case true when row.QuerySelector("img.pro_free") != null:
                    release.DownloadVolumeFactor = 0;
                    break;
                case true when row.QuerySelector("img.pro_free2up") != null:
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 2;
                    break;
                case true when row.QuerySelector("img.pro_50pctdown") != null:
                    release.DownloadVolumeFactor = 0.5;
                    break;
                case true when row.QuerySelector("img.pro_50pctdown2up") != null:
                    release.DownloadVolumeFactor = 0.5;
                    release.UploadVolumeFactor = 2;
                    break;
                case true when row.QuerySelector("img.pro_30pctdown") != null:
                    release.DownloadVolumeFactor = 0.3;
                    break;
                case true when row.QuerySelector("img.pro_2up") != null:
                    release.UploadVolumeFactor = 2;
                    break;
            }

            var thumbnail = row.QuerySelector("img[alt=\"torrent thumbnail\"][src]:not([src$=\"pic/nopic.jpg\"])")?.GetAttribute("src")?.Trim();
            if (thumbnail.IsNotNullOrWhiteSpace())
            {
                release.PosterUrl = thumbnail.StartsWith("http") ? thumbnail : _settings.BaseUrl + thumbnail.TrimStart('/');
            }

            releaseInfos.Add(release);
        }

        return releaseInfos;
    }

    private static void CheckResponseStatus(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new IndexerAuthException("Forbidden from accessing the site. Check your account.");
        }

        if (indexerResponse.HttpResponse.HasHttpRedirect)
        {
            if (indexerResponse.HttpResponse.RedirectUrl.ContainsIgnoreCase("login.php"))
            {
                throw new IndexerAuthException("Redirected to the login page. Most likely your session expired or was killed. Recheck your cookie and try testing the indexer. [{0}]", indexerResponse.HttpResponse.RedirectUrl);
            }

            if (indexerResponse.HttpResponse.RedirectUrl.ContainsIgnoreCase("verify.php"))
            {
                throw new IndexerAuthException("Redirected to the verify page. Recheck your cookie and try testing the indexer. [{0}]", indexerResponse.HttpResponse.RedirectUrl);
            }

            throw new IndexerAuthException("Redirected to {0} from API request", indexerResponse.HttpResponse.RedirectUrl);
        }

        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerAuthException($"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
        }

        if (indexerResponse.HttpResponse.Content.Contains("请求太频繁"))
        {
            throw new TooManyRequestsException(indexerResponse.HttpRequest, indexerResponse.HttpResponse, TimeSpan.FromMinutes(2));
        }
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class MTeamTpValidator : CookieBaseSettingsValidator<MTeamTpSettings>
{
    public MTeamTpValidator()
    {
        RuleFor(c => c.UserAgent).NotEmpty();
    }
}

public class MTeamTpSettings : CookieTorrentBaseSettings
{
    private static readonly MTeamTpValidator Validator = new ();

    [FieldDefinition(3, Label = "Cookie User-Agent", Type = FieldType.Textbox, HelpText = "User-Agent associated with cookie used from Browser")]
    public string UserAgent { get; set; }

    [FieldDefinition(4, Label = "FreeLeech Only", Type = FieldType.Checkbox, HelpText = "Search FreeLeech torrents only")]
    public bool FreeLeechOnly { get; set; }

    [FieldDefinition(5, Label = "Adult Content", Type = FieldType.Checkbox, HelpText = "Allow Adult Content (Must be enabled in your User CP settings)")]
    public bool AdultContent { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
