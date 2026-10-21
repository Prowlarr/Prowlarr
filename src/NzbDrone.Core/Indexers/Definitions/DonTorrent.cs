using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MonoTorrent;
using MonoTorrent.BEncoding;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions;

public class DonTorrent : TorrentIndexerBase<DonTorrentSettings>
{
    private static readonly TimeSpan DomainOverrideLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan DomainLookupInterval = TimeSpan.FromMinutes(10);
    private static readonly Regex SiteHostRegex = new(@"(?:^|\.)dontorrent\.[a-z]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CurrentDomainRegex = new(@"^https?://(?:[a-z0-9-]+\.)*dontorrent\.[a-z]{2,}/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ICacheManager _cacheManager;
    private readonly ICached<string> _domainCache;

    public override string Name => "DonTorrent";

    // The site moves to a new domain whenever the current one is blocked; old domains redirect to the
    // current one while they last, and /dominios lists the current domain ("Dominio Actual").
    public override string[] IndexerUrls => new[] { "https://dontorrent.meme/" };
    public override string[] LegacyUrls => new[] { "https://dontorrent.moi/" };
    public override string Description => "DonTorrent is a SPANISH public site for movies, TV series and documentaries";
    public override string Language => "es-ES";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(1);

    public DonTorrent(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, ICacheManager cacheManager)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
        _cacheManager = cacheManager;
        _domainCache = cacheManager.GetCache<string>(GetType(), "domain");
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new DonTorrentRequestGenerator(CurrentSettings());
    }

    public override IParseIndexerResponse GetParser()
    {
        return new DonTorrentParser(Definition, CurrentSettings(), Capabilities.Categories, RateLimit, _httpClient, _cacheManager.GetCache<DonTorrentDetails>(GetType(), "details"), _logger);
    }

    public string GetBaseUrl()
    {
        return _domainCache.Find(DomainKey) ?? NormalizeBaseUrl(Settings.AlternativeDomain) ?? Settings.BaseUrl;
    }

    private string DomainKey => $"override:{Definition?.Id}:{Settings.BaseUrl}:{Settings.AlternativeDomain}";

    private static string NormalizeBaseUrl(string url)
    {
        if (url.IsNullOrWhiteSpace())
        {
            return null;
        }

        url = url.Trim();

        if (!url.Contains("://", StringComparison.Ordinal))
        {
            url = $"https://{url}";
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? $"{uri.Scheme}://{uri.Host}/"
            : null;
    }

    private DonTorrentSettings CurrentSettings()
    {
        return new DonTorrentSettings
        {
            BaseUrl = GetBaseUrl(),
            AlternativeDomain = Settings.AlternativeDomain,
            BaseSettings = Settings.BaseSettings,
            TorrentBaseSettings = Settings.TorrentBaseSettings
        };
    }

    protected override async Task<IndexerResponse> FetchIndexerResponse(IndexerRequest request)
    {
        IndexerResponse response = null;
        Exception failure = null;

        try
        {
            response = await base.FetchIndexerResponse(request).ConfigureAwait(false);

            var movedTo = GetRedirectBaseUrl(response.HttpResponse);

            if (movedTo != null)
            {
                SwitchBaseUrl(movedTo, request.Url.Host);
                request.HttpRequest.Url = new HttpUri(ReplaceBaseUrl(request.Url.FullUri, movedTo));

                return await base.FetchIndexerResponse(request).ConfigureAwait(false);
            }

            if (LooksLikeSite(response.HttpResponse))
            {
                return response;
            }
        }
        catch (Exception ex) when (IsSiteUnreachable(ex))
        {
            failure = ex;
        }

        var currentBaseUrl = await LookUpCurrentBaseUrl(request.Url.Host).ConfigureAwait(false);

        if (currentBaseUrl == null)
        {
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            return response;
        }

        request.HttpRequest.Url = new HttpUri(ReplaceBaseUrl(request.Url.FullUri, currentBaseUrl));

        return await base.FetchIndexerResponse(request).ConfigureAwait(false);
    }

    // Returns scheme://host/ when the response redirects to another DonTorrent domain.
    private static string GetRedirectBaseUrl(HttpResponse response)
    {
        if ((int)response.StatusCode is < 300 or > 399 || response.RedirectUrl.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (!Uri.TryCreate(new Uri(response.Request.Url.FullUri), response.RedirectUrl, out var target) ||
            !SiteHostRegex.IsMatch(target.Host) ||
            target.Host.Equals(response.Request.Url.Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"{target.Scheme}://{target.Host}/";
    }

    private void SwitchBaseUrl(string baseUrl, string oldHost)
    {
        _logger.Info("DonTorrent moved from {0} to {1}", oldHost, baseUrl);
        _domainCache.Set(DomainKey, baseUrl, DomainOverrideLifetime);
    }

    // Reads "Dominio Actual" from /dominios on any domain that still answers; null if nothing better is found.
    private async Task<string> LookUpCurrentBaseUrl(string failingHost)
    {
        var lookupKey = $"lookup:{Definition?.Id}";
        var baseUrl = _domainCache.Find(lookupKey);

        if (baseUrl == null)
        {
            var candidates = new[] { GetBaseUrl(), NormalizeBaseUrl(Settings.AlternativeDomain), Settings.BaseUrl }
                .Concat(IndexerUrls)
                .Concat(LegacyUrls)
                .Where(c => c != null)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                baseUrl = await ReadDomainDirectory(candidate).ConfigureAwait(false);

                if (baseUrl != null)
                {
                    break;
                }
            }

            // Remember the answer (even an empty one) for a while so failures don't hammer the site.
            _domainCache.Set(lookupKey, baseUrl ?? string.Empty, DomainLookupInterval);
        }

        if (baseUrl.IsNullOrWhiteSpace() || new Uri(baseUrl).Host.Equals(failingHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        SwitchBaseUrl(baseUrl, failingHost);

        return baseUrl;
    }

    private async Task<string> ReadDomainDirectory(string baseUrl)
    {
        var request = new HttpRequestBuilder(baseUrl)
            .Resource("dominios")
            .Accept(HttpAccept.Html)
            .Build();

        request.SuppressHttpError = true;
        request.AllowAutoRedirect = false;

        try
        {
            var response = await _httpClient.ExecuteProxiedAsync(request, Definition).ConfigureAwait(false);

            return GetRedirectBaseUrl(response) ??
                   (response.StatusCode == HttpStatusCode.OK ? ParseDomainDirectory(response.Content) : null);
        }
        catch (Exception ex) when (IsSiteUnreachable(ex))
        {
            _logger.Debug(ex, "Could not read the DonTorrent domain list from {0}", baseUrl);
            return null;
        }
    }

    public static string ParseDomainDirectory(string html)
    {
        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(html);

        var current = dom.QuerySelectorAll("div.alert p, p")
            .FirstOrDefault(p => p.TextContent.Contains("Dominio Actual", StringComparison.OrdinalIgnoreCase));

        // The URL and the "Dominio Actual" label are separate links; read the URL from its own element.
        var url = current?.QuerySelectorAll("a")
            .Select(a => a.TextContent.Trim())
            .FirstOrDefault(t => CurrentDomainRegex.IsMatch(t));

        return url == null ? null : $"{new Uri(url).Scheme}://{new Uri(url).Host}/";
    }

    private static bool LooksLikeSite(HttpResponse response)
    {
        // A dead domain may answer with an error or parking page instead of failing.
        return response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Gone) &&
               !response.HasHttpServerError &&
               response.Content.Contains("dontorrent", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSiteUnreachable(Exception ex)
    {
        return ex switch
        {
            null => false,
            CloudFlareProtectionException => false,
            HttpException httpException => httpException.Response.HasHttpServerError || httpException.Response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone,
            WebException or HttpRequestException or TaskCanceledException or IOException => true,
            _ => IsSiteUnreachable(ex.InnerException)
        };
    }

    private static string ReplaceBaseUrl(string url, string baseUrl)
    {
        return new Uri(new Uri(baseUrl), new Uri(url).PathAndQuery).AbsoluteUri;
    }

    public override async Task<IndexerDownloadResponse> Download(Uri link)
    {
        // Results point to a local placeholder (api_validate_pow.php?tabla=...&content_id=...).
        // The real .torrent URL is only handed out after the site's proof-of-work exchange.
        if (!link.AbsolutePath.EndsWith("/api_validate_pow.php", StringComparison.OrdinalIgnoreCase))
        {
            return await base.Download(link).ConfigureAwait(false);
        }

        var query = HttpUtility.ParseQueryString(link.Query);
        var tabla = query["tabla"];

        if (tabla.IsNullOrWhiteSpace() || !int.TryParse(query["content_id"], NumberStyles.None, CultureInfo.InvariantCulture, out var contentId))
        {
            throw new ReleaseDownloadException("Invalid DonTorrent download link: {0}", link.AbsoluteUri);
        }

        var torrentUrl = await GetTorrentUrl(contentId, tabla).ConfigureAwait(false);
        var response = await base.Download(new Uri(torrentUrl)).ConfigureAwait(false);

        // Some releases are password protected archives; keep the password with the torrent.
        var password = query["clave"];

        return password.IsNullOrWhiteSpace()
            ? response
            : new IndexerDownloadResponse(DonTorrentTorrentComment.AddPassword(response.Data, password), response.ElapsedTime);
    }

    private async Task<string> GetTorrentUrl(int contentId, string tabla)
    {
        var generate = await PostDownloadApi(new { action = "generate", content_id = contentId, tabla }).ConfigureAwait(false);

        if (generate.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ReleaseUnavailableException("DonTorrent content {0}/{1} no longer exists", tabla, contentId);
        }

        ThrowIfLimited(generate);

        if (!generate.Result.Success || generate.Result.Challenge.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("DonTorrent did not return a download challenge: {0}", generate.Result.Error ?? generate.StatusCode.ToString());
        }

        var difficulty = generate.Result.Difficulty is > 0 and <= 8 ? generate.Result.Difficulty.Value : DonTorrentProofOfWork.DefaultDifficulty;
        var nonce = DonTorrentProofOfWork.Solve(generate.Result.Challenge, difficulty);

        var validate = await PostDownloadApi(new { action = "validate", challenge = generate.Result.Challenge, nonce }).ConfigureAwait(false);

        ThrowIfLimited(validate);

        if (validate.Result.Status == "captcha_required")
        {
            // Solving the captcha is deliberately left to a human in a browser.
            throw new ReleaseDownloadException("DonTorrent is asking for a captcha. Open {0} in a browser, download any release once to solve it, then retry.", GetBaseUrl());
        }

        if (!validate.Result.Success || validate.Result.DownloadUrl.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("DonTorrent rejected the download request: {0}", validate.Result.Error ?? validate.StatusCode.ToString());
        }

        return new Uri(new Uri(GetBaseUrl()), validate.Result.DownloadUrl).AbsoluteUri;
    }

    private void ThrowIfLimited(DonTorrentApiResponse response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests || response.Result.Status == "limit_exceeded")
        {
            var minutes = response.Result.WaitMinutes.GetValueOrDefault(60);

            _logger.Warn("DonTorrent download limit reached, the site asks to wait {0} minutes", minutes);

            throw new ReleaseDownloadException("DonTorrent download limit reached, retry in {0} minutes", minutes);
        }
    }

    private async Task<DonTorrentApiResponse> PostDownloadApi(object body)
    {
        HttpResponse response;

        try
        {
            response = await ExecuteDownloadApi(GetBaseUrl(), body).ConfigureAwait(false);

            var movedTo = GetRedirectBaseUrl(response);

            if (movedTo != null)
            {
                SwitchBaseUrl(movedTo, response.Request.Url.Host);
                response = await ExecuteDownloadApi(movedTo, body).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (IsSiteUnreachable(ex))
        {
            var currentBaseUrl = await LookUpCurrentBaseUrl(new Uri(GetBaseUrl()).Host).ConfigureAwait(false);

            if (currentBaseUrl == null)
            {
                throw;
            }

            response = await ExecuteDownloadApi(currentBaseUrl, body).ConfigureAwait(false);
        }

        DonTorrentApiResult result;

        try
        {
            result = JsonConvert.DeserializeObject<DonTorrentApiResult>(response.Content) ?? new DonTorrentApiResult();
        }
        catch (JsonException ex)
        {
            throw new ReleaseDownloadException("Unexpected response from DonTorrent download API (HTTP {0}): {1}", response.StatusCode, ex.Message);
        }

        return new DonTorrentApiResponse(response.StatusCode, result);
    }

    private Task<HttpResponse> ExecuteDownloadApi(string baseUrl, object body)
    {
        var request = new HttpRequestBuilder(baseUrl)
            .Resource("api_validate_pow.php")
            .Accept(HttpAccept.Json)
            .SetHeader("Referer", baseUrl)
            .WithRateLimit(RateLimit.TotalSeconds)
            .Build();

        request.Method = HttpMethod.Post;
        request.Headers.ContentType = "application/json";
        request.SetContent(body.ToJson(Formatting.None));
        request.AllowAutoRedirect = false;

        // 403/404/429 carry a JSON body with the reason, read it instead of throwing.
        request.SuppressHttpError = true;

        return _httpClient.ExecuteProxiedAsync(request, Definition);
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
            }
        };

        caps.Categories.AddCategoryMapping("peliculas", NewznabStandardCategory.Movies, "Películas");
        caps.Categories.AddCategoryMapping("series", NewznabStandardCategory.TV, "Series");
        caps.Categories.AddCategoryMapping("documentales", NewznabStandardCategory.TVDocumentary, "Documentales");

        return caps;
    }
}

public enum DonTorrentSearchType
{
    All,
    Movies,
    Tv
}

public class DonTorrentRequest : IndexerRequest
{
    public DonTorrentRequest(HttpRequest httpRequest, DonTorrentSearchType searchType)
        : base(httpRequest)
    {
        SearchType = searchType;
    }

    public DonTorrentSearchType SearchType { get; }
}

public class DonTorrentRequestGenerator : IIndexerRequestGenerator
{
    private static readonly Regex TrailingYearRegex = new(@"\s+(?:19|20)\d{2}$", RegexOptions.Compiled);

    private readonly NoAuthTorrentBaseSettings _settings;

    public DonTorrentRequestGenerator(NoAuthTorrentBaseSettings settings)
    {
        _settings = settings;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        return GetRequestChain(searchCriteria.SanitizedSearchTerm, DonTorrentSearchType.Movies);
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        // The site only knows show names; season/episode are resolved from the results.
        return GetRequestChain(searchCriteria.SanitizedSearchTerm, DonTorrentSearchType.Tv);
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        return GetRequestChain(searchCriteria.SanitizedSearchTerm, DonTorrentSearchType.All);
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    private IndexerPageableRequestChain GetRequestChain(string term, DonTorrentSearchType searchType)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(term, searchType));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, DonTorrentSearchType searchType)
    {
        term = TrailingYearRegex.Replace(term?.Trim() ?? string.Empty, string.Empty).Trim();

        HttpRequest request;

        if (term.IsNullOrWhiteSpace())
        {
            request = new HttpRequestBuilder($"{_settings.BaseUrl.TrimEnd('/')}/ultimos")
                .Accept(HttpAccept.Html)
                .SetHeader("Referer", _settings.BaseUrl)
                .Build();
        }
        else
        {
            request = new HttpRequestBuilder($"{_settings.BaseUrl.TrimEnd('/')}/buscar")
                .Accept(HttpAccept.Html)
                .SetHeader("Referer", _settings.BaseUrl)
                .Build();

            request.Method = HttpMethod.Post;
            request.Headers.ContentType = "application/x-www-form-urlencoded";
            request.SetContent($"valor={Uri.EscapeDataString(term)}&Buscar=Buscar");
            request.ContentSummary = $"valor={term}";
        }

        yield return new DonTorrentRequest(request, searchType);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class DonTorrentParser : IParseIndexerResponse
{
    private const int MaxDetailPages = 30;

    private static readonly Regex ItemPathRegex = new(@"^/?(?<kind>pelicula|serie|documental)/(?<id>\d+)(?:/(?<first>\d+))?/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FormatInParenthesesRegex = new(@"^\s*\((?<format>[^)]+)\)\s*$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"(?:data-clave|value)\s*=\s*['""](?<password>[^'""]+)['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LatestEpisodeRegex = new(@"^(?<title>.+?)\s*:\s*(?<episode>\d{1,2}x\d{1,3}.*)$", RegexOptions.Compiled);

    private readonly ProviderDefinition _definition;
    private readonly IndexerCapabilitiesCategories _categories;
    private readonly TimeSpan _rateLimit;
    private readonly IIndexerHttpClient _httpClient;
    private readonly ICached<DonTorrentDetails> _detailsCache;
    private readonly Logger _logger;

    private string _baseUrl;
    private int _detailRequests;

    public DonTorrentParser(ProviderDefinition definition, NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient, ICached<DonTorrentDetails> detailsCache, Logger logger)
    {
        _definition = definition;
        _categories = categories;
        _rateLimit = rateLimit;
        _httpClient = httpClient;
        _detailsCache = detailsCache;
        _logger = logger;
        _baseUrl = settings.BaseUrl;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

        // Links are resolved against the domain that actually answered (the site moves often).
        _baseUrl = $"{indexerResponse.HttpRequest.Url.Scheme}://{indexerResponse.HttpRequest.Url.Host}/";

        var searchType = (indexerResponse.Request as DonTorrentRequest)?.SearchType ?? DonTorrentSearchType.All;
        var isLatest = indexerResponse.HttpRequest.Url.Path.Contains("/ultimos", StringComparison.OrdinalIgnoreCase);

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var items = isLatest ? ParseLatest(dom) : ParseSearch(dom);
        var releases = new List<ReleaseInfo>();
        var seen = new HashSet<string>();

        _detailRequests = 0;

        foreach (var item in items)
        {
            if (!IsWanted(item.Kind, searchType))
            {
                continue;
            }

            foreach (var release in GetReleases(item))
            {
                if (seen.Add(release.Guid))
                {
                    releases.Add(release);
                }
            }
        }

        return releases;
    }

    private static bool IsWanted(string kind, DonTorrentSearchType searchType)
    {
        return searchType switch
        {
            DonTorrentSearchType.Movies => kind == "pelicula",
            DonTorrentSearchType.Tv => kind is "serie" or "documental",
            _ => true
        };
    }

    private IEnumerable<DonTorrentItem> ParseSearch(IHtmlDocument dom)
    {
        foreach (var row in dom.QuerySelectorAll("div.card-body > p"))
        {
            var link = row.QuerySelector("a[href]");
            var item = CreateItem(link?.GetAttribute("href"), link?.TextContent);

            if (item == null)
            {
                continue;
            }

            var format = link.ParentElement?.QuerySelectorAll("span").Select(s => FormatInParenthesesRegex.Match(s.TextContent)).FirstOrDefault(m => m.Success);
            item.Format = format?.Groups["format"].Value.Trim();

            yield return item;
        }
    }

    private IEnumerable<DonTorrentItem> ParseLatest(IHtmlDocument dom)
    {
        foreach (var link in dom.QuerySelectorAll("a.text-primary[href]"))
        {
            var text = link.TextContent?.Trim();
            string episode = null;

            var episodeMatch = LatestEpisodeRegex.Match(text ?? string.Empty);

            if (episodeMatch.Success)
            {
                text = episodeMatch.Groups["title"].Value;
                episode = episodeMatch.Groups["episode"].Value.Trim();
            }

            var item = CreateItem(link.GetAttribute("href"), text);

            if (item == null)
            {
                continue;
            }

            item.Episode = episode;

            if (link.PreviousElementSibling is { } previous && DateTime.TryParseExact(previous.TextContent.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                item.PublishDate = date;
            }

            if (link.NextElementSibling is { } next && FormatInParenthesesRegex.Match(next.TextContent) is { Success: true } formatMatch)
            {
                item.Format = formatMatch.Groups["format"].Value.Trim();
            }

            yield return item;
        }
    }

    private DonTorrentItem CreateItem(string href, string title)
    {
        if (href.IsNullOrWhiteSpace() || title.IsNullOrWhiteSpace())
        {
            return null;
        }

        var infoUrl = GetAbsoluteUrl(href);
        var match = ItemPathRegex.Match(new Uri(infoUrl).AbsolutePath);

        if (!match.Success)
        {
            return null;
        }

        return new DonTorrentItem
        {
            Kind = match.Groups["kind"].Value.ToLowerInvariant(),
            Id = match.Groups["id"].Value,
            InfoUrl = infoUrl,
            Title = title.Trim()
        };
    }

    private IEnumerable<TorrentInfo> GetReleases(DonTorrentItem item)
    {
        var details = GetDetails(item.InfoUrl);

        if (item.Kind == "pelicula")
        {
            var contentId = details?.Rows.FirstOrDefault()?.ContentId ?? item.Id;
            var format = details?.Format ?? item.Format;
            var parsed = DonTorrentTitleBuilder.Build("peliculas", details?.Title ?? item.Title, details?.Year ?? 0, null, format);

            var password = details?.Rows.FirstOrDefault()?.Password ?? details?.Password;

            yield return CreateRelease(item, "peliculas", contentId, parsed, details?.Year ?? 0, item.PublishDate ?? DateTime.UtcNow, 1, password);

            yield break;
        }

        if (details == null)
        {
            yield break;
        }

        var tabla = item.Kind == "documental" ? "documentales" : "series";

        foreach (var row in details.Rows)
        {
            if (item.Episode != null && !row.Label.StartsWith(item.Episode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parsed = DonTorrentTitleBuilder.Build(row.Tabla ?? tabla, details.Title ?? item.Title, 0, row.Label, details.Format ?? item.Format);

            yield return CreateRelease(item, row.Tabla ?? tabla, row.ContentId, parsed, 0, item.PublishDate ?? row.PublishDate ?? DateTime.UtcNow, parsed.EpisodeCount, row.Password);
        }
    }

    private TorrentInfo CreateRelease(DonTorrentItem item, string tabla, string contentId, DonTorrentParsedTitle parsed, int year, DateTime publishDate, int episodeCount, string password)
    {
        var downloadUrl = $"{_baseUrl.TrimEnd('/')}/api_validate_pow.php?tabla={Uri.EscapeDataString(tabla)}&content_id={Uri.EscapeDataString(contentId)}";

        if (password.IsNotNullOrWhiteSpace())
        {
            downloadUrl += $"&clave={Uri.EscapeDataString(password)}";
        }

        return new TorrentInfo
        {
            // The domain changes often, keep the guid independent of it.
            Guid = $"DonTorrent-{tabla}-{contentId}",

            // Password protected archives are flagged in the title so they are noticed before grabbing.
            Title = password.IsNotNullOrWhiteSpace() ? $"{parsed.Title} [Clave {password}]" : parsed.Title,
            Description = password.IsNotNullOrWhiteSpace() ? $"Contraseña / password: {password}" : null,
            InfoUrl = item.InfoUrl,
            DownloadUrl = downloadUrl,
            Categories = GetCategories(tabla, parsed),

            // The site does not publish sizes; use a typical size for the quality so the release is usable.
            Size = DonTorrentTitleBuilder.EstimateSize(parsed, episodeCount),
            PublishDate = publishDate,
            Year = year,
            Resolution = parsed.Resolution,

            // The site does not publish swarm information.
            Seeders = 1,
            Peers = 2,
            DownloadVolumeFactor = 0,
            UploadVolumeFactor = 1
        };
    }

    private DonTorrentDetails GetDetails(string url)
    {
        var cached = _detailsCache.Find(url);

        if (cached != null)
        {
            return cached;
        }

        if (_detailRequests >= MaxDetailPages)
        {
            _logger.Debug("Skipping DonTorrent details for {0}, too many detail requests for one search", url);
            return null;
        }

        _detailRequests++;

        var request = new HttpRequestBuilder(url)
            .Accept(HttpAccept.Html)
            .SetHeader("Referer", _baseUrl)
            .WithRateLimit(_rateLimit.TotalSeconds)
            .Build();

        request.SuppressHttpError = true;

        HttpResponse response;

        try
        {
            response = _httpClient.ExecuteProxied(request, _definition);
        }
        catch (Exception ex) when (ex is HttpException or WebException)
        {
            _logger.Debug(ex, "Could not load DonTorrent details from {0}", url);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new TooManyRequestsException(request, response);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.Debug("Could not load DonTorrent details from {0}: HTTP {1}", url, response.StatusCode);
            return null;
        }

        var details = ParseDetails(response.Content);

        _detailsCache.Set(url, details, url.Contains("/pelicula/", StringComparison.OrdinalIgnoreCase) ? TimeSpan.FromDays(1) : TimeSpan.FromMinutes(30));

        return details;
    }

    public static DonTorrentDetails ParseDetails(string html)
    {
        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(html);

        var details = new DonTorrentDetails
        {
            Title = dom.QuerySelector("h2.descargarTitulo")?.TextContent.Trim()
        };

        foreach (var info in dom.QuerySelectorAll("p"))
        {
            var label = info.QuerySelector("b")?.TextContent.Trim();

            if (label == null)
            {
                continue;
            }

            var value = info.TextContent.Replace(label, string.Empty).Trim();

            if (label.StartsWith("Año", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var year))
            {
                details.Year = year;
            }
            else if (label.StartsWith("Formato", StringComparison.OrdinalIgnoreCase) && value.IsNotNullOrWhiteSpace())
            {
                details.Format = value;
            }
            else if (label.StartsWith("Clave", StringComparison.OrdinalIgnoreCase))
            {
                details.Password = ParsePassword(info);
            }
        }

        foreach (var button in dom.QuerySelectorAll("a.protected-download[data-content-id], button.protected-download[data-content-id]"))
        {
            var row = new DonTorrentDetailsRow
            {
                ContentId = button.GetAttribute("data-content-id"),
                Tabla = button.GetAttribute("data-tabla")
            };

            if (button.Closest("tr") is { } tr)
            {
                var cells = tr.QuerySelectorAll("td").ToList();

                row.Label = cells.FirstOrDefault()?.TextContent.Trim();
                row.Password = ParsePassword(tr);
                row.PublishDate = cells
                    .Select(c => DateTime.TryParseExact(c.TextContent.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date) ? date : (DateTime?)null)
                    .FirstOrDefault(d => d.HasValue);
            }

            row.Label ??= string.Empty;

            if (row.ContentId.IsNotNullOrWhiteSpace())
            {
                details.Rows.Add(row);
            }
        }

        return details;
    }

    // Passwords are shown in a click popover: <a data-toggle="popover" title="... data-clave='X' ..." data-content="<input value='X'>">.
    private static string ParsePassword(IElement container)
    {
        var popover = container.QuerySelector("a[data-toggle='popover'][data-trigger='click']");

        if (popover == null)
        {
            return null;
        }

        foreach (var attribute in new[] { popover.GetAttribute("data-content"), popover.GetAttribute("title") })
        {
            var match = PasswordRegex.Match(attribute ?? string.Empty);

            if (match.Success && match.Groups["password"].Value.IsNotNullOrWhiteSpace())
            {
                return match.Groups["password"].Value.Trim();
            }
        }

        return null;
    }

    private ICollection<IndexerCategory> GetCategories(string tabla, DonTorrentParsedTitle parsed)
    {
        var categories = _categories.MapTrackerCatToNewznab(tabla).ToList();

        if (tabla == "peliculas")
        {
            categories.Add(parsed.Resolution switch
            {
                "2160p" => NewznabStandardCategory.MoviesUHD,
                "1080p" or "720p" => NewznabStandardCategory.MoviesHD,
                _ => NewznabStandardCategory.MoviesSD
            });
        }
        else if (tabla == "series")
        {
            categories.Add(parsed.Resolution switch
            {
                "2160p" => NewznabStandardCategory.TVUHD,
                "1080p" or "720p" => NewznabStandardCategory.TVHD,
                _ => NewznabStandardCategory.TVSD
            });
        }

        return categories.Distinct().ToList();
    }

    private string GetAbsoluteUrl(string url)
    {
        // Pages may still link to a previous domain; always follow the one that answered.
        var uri = new Uri(new Uri(_baseUrl), url);

        return new Uri(new Uri(_baseUrl), uri.PathAndQuery).AbsoluteUri;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    private class DonTorrentItem
    {
        public string Kind { get; init; }
        public string Id { get; init; }
        public string InfoUrl { get; init; }
        public string Title { get; init; }
        public string Format { get; set; }
        public string Episode { get; set; }
        public DateTime? PublishDate { get; set; }
    }
}

public class DonTorrentDetails
{
    public string Title { get; set; }
    public int Year { get; set; }
    public string Format { get; set; }
    public string Password { get; set; }
    public List<DonTorrentDetailsRow> Rows { get; } = new();
}

public class DonTorrentDetailsRow
{
    public string ContentId { get; set; }
    public string Tabla { get; set; }
    public string Label { get; set; }
    public string Password { get; set; }
    public DateTime? PublishDate { get; set; }
}

public class DonTorrentParsedTitle
{
    public string Title { get; init; }
    public string Quality { get; init; }
    public string Resolution { get; init; }
    public bool IsEpisodic { get; init; }
    public int EpisodeCount { get; init; }
}

public static class DonTorrentTitleBuilder
{
    private static readonly Regex SeasonInTitleRegex = new(@"\s*-\s*\(?\s*(?<season>\d{1,2})\s*[ªº°]?\s*Temporada\s*\)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BracketRegex = new(@"\[(?<tag>[^\]]*)\]", RegexOptions.Compiled);
    private static readonly Regex EpisodeRegex = new(@"(?<season>\d{1,2})x(?<first>\d{1,3})(?:\s*(?<sep>al|a|-|&|y)\s*(?:(?<season2>\d{1,2})x)?(?<last>\d{1,3}))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ResolutionRegex = new(@"(?<res>\d{3,4})p", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FourKRegex = new(@"\b(?:4K|UHD)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SubtitledRegex = new(@"\b(?:Subs?\.?|Subtitulad[oa]|V\.?O\.?S\.?E?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex QualityOnlyTagRegex = new(@"^\s*(?:\d{3,4}p|4K|UHD|HD|SD)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static DonTorrentParsedTitle Build(string tabla, string rawTitle, int year, string episodeLabel, string qualityLabel)
    {
        var name = ParseUtil.NormalizeMultiSpaces(rawTitle ?? string.Empty);
        var tags = new List<string>();
        var resolutionTag = (string)null;

        foreach (var match in BracketRegex.Matches(name).Cast<Match>())
        {
            var tag = match.Groups["tag"].Value.Trim().TrimEnd('.');

            if (QualityOnlyTagRegex.IsMatch(tag))
            {
                resolutionTag ??= tag;
            }
            else if (tag.IsNotNullOrWhiteSpace())
            {
                tags.Add(tag);
            }
        }

        name = BracketRegex.Replace(name, string.Empty);

        int? titleSeason = null;
        var seasonMatch = SeasonInTitleRegex.Match(name);

        if (seasonMatch.Success)
        {
            titleSeason = int.Parse(seasonMatch.Groups["season"].Value, CultureInfo.InvariantCulture);
            name = name.Substring(0, seasonMatch.Index);
        }

        name = ParseUtil.NormalizeMultiSpaces(name).Trim(' ', '.', '-');

        var (quality, resolution) = NormalizeQuality(qualityLabel.IsNotNullOrWhiteSpace() ? qualityLabel : resolutionTag);
        var language = tags.Any(t => SubtitledRegex.IsMatch(t)) ? "VOSE" : "SPANISH";

        var (episode, episodeCount) = FormatEpisode(episodeLabel, titleSeason);
        var isEpisodic = episode != null || titleSeason.HasValue || tabla is "series";

        var parts = new List<string> { name };

        if (isEpisodic)
        {
            if (episode != null)
            {
                parts.Add(episode);
            }
            else if (titleSeason.HasValue)
            {
                parts.Add($"S{titleSeason.Value:00}");
            }
        }
        else if (year > 0)
        {
            parts.Add($"({year})");
        }

        parts.AddRange(tags.Where(t => !SubtitledRegex.IsMatch(t)).Select(t => $"[{t}]"));
        parts.Add(quality);
        parts.Add(language);

        return new DonTorrentParsedTitle
        {
            Title = ParseUtil.NormalizeMultiSpaces(string.Join(" ", parts.Where(p => p.IsNotNullOrWhiteSpace()))),
            Quality = quality,
            Resolution = resolution,
            IsEpisodic = isEpisodic,
            EpisodeCount = Math.Max(1, episodeCount)
        };
    }

    public static long EstimateSize(DonTorrentParsedTitle parsed, int episodeCount)
    {
        const long mb = 1024L * 1024L;
        var quality = parsed.Quality ?? string.Empty;
        var isRemux = quality.Contains("Remux", StringComparison.OrdinalIgnoreCase);

        if (!parsed.IsEpisodic)
        {
            return parsed.Resolution switch
            {
                "2160p" => 20480 * mb,
                "1080p" when isRemux => 25600 * mb,
                "1080p" => 10240 * mb,
                "720p" => 5120 * mb,
                _ => quality.Contains("DVD", StringComparison.OrdinalIgnoreCase) ? 700 * mb : 1536 * mb
            };
        }

        var perEpisode = parsed.Resolution switch
        {
            "2160p" => 5120 * mb,
            "1080p" => 2048 * mb,
            "720p" => 1024 * mb,
            _ => 512 * mb
        };

        return perEpisode * Math.Max(1, episodeCount);
    }

    private static (string Episode, int Count) FormatEpisode(string label, int? titleSeason)
    {
        if (label.IsNullOrWhiteSpace())
        {
            return (null, 1);
        }

        var match = EpisodeRegex.Match(label);

        if (!match.Success)
        {
            return (null, 1);
        }

        var season = int.Parse(match.Groups["season"].Value, CultureInfo.InvariantCulture);
        var first = int.Parse(match.Groups["first"].Value, CultureInfo.InvariantCulture);

        if (season == 0 && titleSeason.HasValue)
        {
            season = titleSeason.Value;
        }

        var result = $"S{season:00}E{first:00}";
        var count = 1;

        if (match.Groups["last"].Success)
        {
            var last = int.Parse(match.Groups["last"].Value, CultureInfo.InvariantCulture);
            var sameSeason = !match.Groups["season2"].Success || int.Parse(match.Groups["season2"].Value, CultureInfo.InvariantCulture) == season;

            if (sameSeason && last > first)
            {
                var sep = match.Groups["sep"].Value.ToLowerInvariant();

                if (sep is "&" or "y")
                {
                    result += $"E{last:00}";
                    count = 2;
                }
                else
                {
                    result += $"-E{last:00}";
                    count = last - first + 1;
                }
            }
        }

        return (result, count);
    }

    private static (string Quality, string Resolution) NormalizeQuality(string label)
    {
        if (label.IsNullOrWhiteSpace())
        {
            return (null, null);
        }

        var quality = ParseUtil.NormalizeMultiSpaces(label.Replace('-', ' ').Replace('_', ' ')).Trim(' ', '.');
        string resolution = null;

        if (FourKRegex.IsMatch(quality))
        {
            quality = FourKRegex.Replace(quality, "2160p");
        }

        // "HDTV720p" -> "HDTV 720p"
        quality = Regex.Replace(quality, @"(?<=[A-Za-z])(?=\d{3,4}p\b)", " ");
        quality = Regex.Replace(quality, @"\bBDremux\b", "BDRemux", RegexOptions.IgnoreCase);

        var resolutionMatch = ResolutionRegex.Match(quality);

        if (resolutionMatch.Success)
        {
            resolution = $"{resolutionMatch.Groups["res"].Value}p";
        }

        return (ParseUtil.NormalizeMultiSpaces(quality), resolution);
    }
}

public static class DonTorrentProofOfWork
{
    public const int DefaultDifficulty = 3;
    private const long MaxIterations = 100_000_000;

    // Mirrors the site's don_download.js: find the smallest nonce so that
    // hex(sha256(challenge + nonce)) starts with `difficulty` zeros.
    public static long Solve(string challenge, int difficulty = DefaultDifficulty)
    {
        var prefix = Encoding.UTF8.GetBytes(challenge);
        var buffer = new byte[prefix.Length + 20];
        Buffer.BlockCopy(prefix, 0, buffer, 0, prefix.Length);

        var hash = new byte[32];

        for (long nonce = 0; nonce < MaxIterations; nonce++)
        {
            var digits = nonce.ToString(CultureInfo.InvariantCulture);

            for (var i = 0; i < digits.Length; i++)
            {
                buffer[prefix.Length + i] = (byte)digits[i];
            }

            SHA256.HashData(buffer.AsSpan(0, prefix.Length + digits.Length), hash);

            if (HasLeadingZeroNibbles(hash, difficulty))
            {
                return nonce;
            }
        }

        throw new ReleaseDownloadException("Could not solve DonTorrent download challenge");
    }

    public static bool HasLeadingZeroNibbles(ReadOnlySpan<byte> hash, int difficulty)
    {
        for (var i = 0; i < difficulty; i++)
        {
            var value = hash[i / 2];
            var nibble = i % 2 == 0 ? value >> 4 : value & 0x0F;

            if (nibble != 0)
            {
                return false;
            }
        }

        return true;
    }
}

public class DonTorrentApiResult
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
    public string Challenge { get; set; }
    public int? Difficulty { get; set; }

    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; }

    [JsonProperty("wait_minutes")]
    public int? WaitMinutes { get; set; }
}

public class DonTorrentApiResponse
{
    public DonTorrentApiResponse(HttpStatusCode statusCode, DonTorrentApiResult result)
    {
        StatusCode = statusCode;
        Result = result;
    }

    public HttpStatusCode StatusCode { get; }
    public DonTorrentApiResult Result { get; }
}

public static class DonTorrentTorrentComment
{
    // Adds the archive password as the torrent comment (shown by most clients). The comment lives
    // outside the info dictionary, so the info hash does not change; if it would, the original is kept.
    public static byte[] AddPassword(byte[] torrentData, string password)
    {
        try
        {
            var originalHash = Torrent.Load(torrentData).InfoHashes.V1OrV2.ToHex();
            var torrent = BEncodedValue.Decode<BEncodedDictionary>(torrentData);
            var comment = $"Contraseña / password: {password}";

            if (torrent.TryGetValue("comment", out var existing) && existing.ToString().IsNotNullOrWhiteSpace())
            {
                comment = $"{comment} | {existing}";
            }

            torrent["comment"] = new BEncodedString(comment);
            torrent["comment.utf-8"] = new BEncodedString(comment);

            var modified = torrent.Encode();

            return Torrent.Load(modified).InfoHashes.V1OrV2.ToHex() == originalHash ? modified : torrentData;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return torrentData;
        }
    }
}

public class DonTorrentSettings : NoAuthTorrentBaseSettings
{
    [FieldDefinition(2, Label = "Alternative domain", Type = FieldType.Textbox, Placeholder = "https://dontorrent.example/", HelpText = "Optional. DonTorrent changes domain often and the indexer follows it on its own (redirects and the site's /dominios page). If it still can't find the site, paste the current domain here.")]
    public string AlternativeDomain { get; set; }
}
