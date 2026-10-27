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
using AngleSharp.Html.Parser;
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

public class WolfMax4K : TorrentIndexerBase<WolfMax4KSettings>
{
    private static readonly TimeSpan DomainOverrideLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan DomainLookupInterval = TimeSpan.FromMinutes(10);

    // wolfmax4k.com was rebranded as wolftorrent.com; accept both names on any TLD.
    private static readonly Regex SiteHostRegex = new(@"(?:^|\.)(?:wolfmax4k|wolftorrent)\.[a-z]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ICached<string> _domainCache;

    public override string Name => "WolfMax4K";

    // The site moves to a new domain when the current one is blocked; old domains redirect to the new one while they last.
    public override string[] IndexerUrls => new[] { "https://wolftorrent.com/" };
    public override string[] LegacyUrls => new[] { "https://wolfmax4k.com/" };
    public override string Description => "WolfTorrent (formerly WolfMax4K) is a SPANISH public site for movies, TV series and documentaries";
    public override string Language => "es-ES";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(3);

    public WolfMax4K(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, ICacheManager cacheManager)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
        _domainCache = cacheManager.GetCache<string>(GetType(), "domain");
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new WolfMax4KRequestGenerator(CurrentSettings());
    }

    public override IParseIndexerResponse GetParser()
    {
        return new WolfMax4KParser(Definition, CurrentSettings(), Capabilities.Categories, RateLimit, _httpClient, _logger);
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

    private WolfMax4KSettings CurrentSettings()
    {
        return new WolfMax4KSettings
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

    // Returns scheme://host/ when the response redirects to another domain of the site.
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
        _logger.Info("WolfMax4K moved from {0} to {1}", oldHost, baseUrl);
        _domainCache.Set(DomainKey, baseUrl, DomainOverrideLifetime);
    }

    // The site doesn't publish a domain list, so ask every known domain where it lives now:
    // old domains redirect to the current one, and a domain that serves the site is used as is.
    private async Task<string> LookUpCurrentBaseUrl(string failingHost)
    {
        var lookupKey = $"lookup:{Definition?.Id}";
        var baseUrl = _domainCache.Find(lookupKey);

        if (baseUrl == null)
        {
            var candidates = new[] { NormalizeBaseUrl(Settings.AlternativeDomain), GetBaseUrl(), Settings.BaseUrl }
                .Concat(IndexerUrls)
                .Concat(LegacyUrls)
                .Where(c => c != null && !new Uri(c).Host.Equals(failingHost, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                baseUrl = await ProbeDomain(candidate).ConfigureAwait(false);

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

    private async Task<string> ProbeDomain(string baseUrl)
    {
        var request = new HttpRequestBuilder(baseUrl)
            .Accept(HttpAccept.Html)
            .Build();

        request.SuppressHttpError = true;
        request.AllowAutoRedirect = false;

        try
        {
            var response = await _httpClient.ExecuteProxiedAsync(request, Definition).ConfigureAwait(false);

            return GetRedirectBaseUrl(response) ?? (LooksLikeSite(response) && response.StatusCode == HttpStatusCode.OK ? baseUrl : null);
        }
        catch (Exception ex) when (IsSiteUnreachable(ex))
        {
            _logger.Debug(ex, "WolfMax4K domain {0} is not answering", baseUrl);
            return null;
        }
    }

    private static bool LooksLikeSite(HttpResponse response)
    {
        // A dead domain may answer with an error or parking page instead of failing.
        return response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Gone) &&
               !response.HasHttpServerError &&
               (response.Content.Contains("wolf-card", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Contains("WolfTorrent", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Contains("WolfMax4K", StringComparison.OrdinalIgnoreCase));
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
        // Search results point to a local placeholder (api/descargas?tabla=...&code=...).
        // The real .torrent URL is only handed out after the site's proof-of-work exchange.
        if (!link.AbsolutePath.EndsWith("/api/descargas", StringComparison.OrdinalIgnoreCase))
        {
            return await base.Download(link).ConfigureAwait(false);
        }

        var query = HttpUtility.ParseQueryString(link.Query);
        var tabla = query["tabla"];

        var code = query["code"];

        if (tabla.IsNullOrWhiteSpace() || code.IsNullOrWhiteSpace())
        {
            // Links from before the site switched to content codes can't be resolved any more.
            throw new ReleaseUnavailableException("Outdated or invalid WolfMax4K download link, search again: {0}", link.AbsoluteUri);
        }

        // The release page travels as a relative path, so grab errors can point to it on the current domain.
        var info = query["info"];
        var releasePage = info.IsNotNullOrWhiteSpace() && info.StartsWith('/') && !info.StartsWith("//", StringComparison.Ordinal) ? new Uri(new Uri(GetBaseUrl()), info).AbsoluteUri : GetBaseUrl();

        var torrentUrl = await GetTorrentUrl(code, tabla, releasePage).ConfigureAwait(false);

        return await base.Download(new Uri(torrentUrl)).ConfigureAwait(false);
    }

    private async Task<string> GetTorrentUrl(string code, string tabla, string releasePage)
    {
        var generate = await PostDownloadApi(new { action = "generate", code, tabla }).ConfigureAwait(false);

        if (generate.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ReleaseUnavailableException("WolfMax4K content {0}/{1} no longer exists", tabla, code);
        }

        ThrowIfLimited(generate);
        ThrowIfHumanCheck(generate.Result, releasePage);

        if (!generate.Result.Success || generate.Result.Challenge.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("WolfMax4K did not return a download challenge: {0}", generate.Result.Error ?? generate.StatusCode.ToString());
        }

        var difficulty = generate.Result.Difficulty is > 0 and <= 8 ? generate.Result.Difficulty.Value : WolfMax4KProofOfWork.DefaultDifficulty;
        var nonce = WolfMax4KProofOfWork.Solve(generate.Result.Challenge, difficulty);

        var validate = await PostDownloadApi(new { action = "validate", challenge = generate.Result.Challenge, nonce }).ConfigureAwait(false);

        ThrowIfLimited(validate);
        ThrowIfHumanCheck(validate.Result, releasePage);

        if (!validate.Result.Success || validate.Result.DownloadUrl.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("WolfMax4K rejected the download request: {0}", validate.Result.Error ?? validate.Result.Status ?? validate.StatusCode.ToString());
        }

        var baseUrl = GetBaseUrl();
        var torrentUrl = new Uri(new Uri(baseUrl), validate.Result.DownloadUrl);

        // Some releases are only handed out through an external verification page (a link shortener)
        // that has to be completed by a person; it is not followed automatically.
        if (validate.Result.External == true || !torrentUrl.Host.Equals(new Uri(baseUrl).Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new ReleaseDownloadException("WolfMax4K only offers this release through an external verification page ({0}) that has to be completed in a browser, it can't be grabbed automatically. Download it from {1}", torrentUrl.Host, releasePage);
        }

        return torrentUrl.AbsoluteUri;
    }

    private static void ThrowIfHumanCheck(WolfMax4KApiResult result, string releasePage)
    {
        // Captchas and Cloudflare Turnstile checks are deliberately left to a human in a browser.
        if (result.Status == "captcha_required")
        {
            throw new ReleaseDownloadException("WolfMax4K is asking for a captcha. Open {0} in a browser, download the release once to solve it, then retry.", releasePage);
        }

        if (result.Status == "turnstile_required" || result.Verification != null)
        {
            throw new ReleaseDownloadException("WolfMax4K asks for a human verification (Cloudflare Turnstile) on every download, it can't be grabbed automatically. Download the release in a browser from {0}", releasePage);
        }
    }

    private void ThrowIfLimited(WolfMax4KApiResponse response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests || response.Result.Status == "limit_exceeded")
        {
            var minutes = response.Result.WaitMinutes.GetValueOrDefault(60);

            _logger.Warn("WolfMax4K download limit reached, the site asks to wait {0} minutes", minutes);

            throw new ReleaseDownloadException("WolfMax4K download limit reached, retry in {0} minutes", minutes);
        }
    }

    private async Task<WolfMax4KApiResponse> PostDownloadApi(object body)
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

        WolfMax4KApiResult result;

        try
        {
            result = JsonConvert.DeserializeObject<WolfMax4KApiResult>(response.Content) ?? new WolfMax4KApiResult();
        }
        catch (JsonException ex)
        {
            throw new ReleaseDownloadException("Unexpected response from WolfMax4K download API (HTTP {0}): {1}", response.StatusCode, ex.Message);
        }

        return new WolfMax4KApiResponse(response.StatusCode, result);
    }

    private Task<HttpResponse> ExecuteDownloadApi(string baseUrl, object body)
    {
        var request = new HttpRequestBuilder(baseUrl)
            .Resource("api/descargas")
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
        caps.Categories.AddCategoryMapping("animaciones", NewznabStandardCategory.TVAnime, "Animaciones");
        caps.Categories.AddCategoryMapping("telenovelas", NewznabStandardCategory.TVForeign, "Telenovelas");

        return caps;
    }
}

public class WolfMax4KRequestGenerator : IIndexerRequestGenerator
{
    private static readonly Regex TrailingYearRegex = new(@"\s+(?:19|20)\d{2}$", RegexOptions.Compiled);

    private readonly WolfMax4KSettings _settings;

    public WolfMax4KRequestGenerator(WolfMax4KSettings settings)
    {
        _settings = settings;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        return GetRequestChain(searchCriteria.SanitizedSearchTerm);
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        // The site only knows show names; season/episode are resolved from the results.
        return GetRequestChain(searchCriteria.SanitizedSearchTerm);
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        return GetRequestChain(searchCriteria.SanitizedSearchTerm);
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    private IndexerPageableRequestChain GetRequestChain(string term)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(term));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term)
    {
        term = TrailingYearRegex.Replace(term?.Trim() ?? string.Empty, string.Empty).Trim();

        var url = term.IsNullOrWhiteSpace()
            ? $"{_settings.BaseUrl.TrimEnd('/')}/ultimos"
            : $"{_settings.BaseUrl.TrimEnd('/')}/buscar?q={Uri.EscapeDataString(term)}";

        var request = new HttpRequestBuilder(url)
            .Accept(HttpAccept.Html)
            .SetHeader("Referer", _settings.BaseUrl)
            .Build();

        yield return new IndexerRequest(request);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class WolfMax4KParser : IParseIndexerResponse
{
    private const int MaxExtraEpisodePages = 10;

    private readonly ProviderDefinition _definition;
    private readonly IndexerCapabilitiesCategories _categories;
    private readonly TimeSpan _rateLimit;
    private readonly IIndexerHttpClient _httpClient;
    private readonly Logger _logger;
    private string _baseUrl;

    public WolfMax4KParser(ProviderDefinition definition, WolfMax4KSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient, Logger logger)
    {
        _definition = definition;
        _categories = categories;
        _rateLimit = rateLimit;
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = settings.BaseUrl;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

        var releases = new List<ReleaseInfo>();

        // Resolve links against the domain that actually answered (it may have just changed).
        _baseUrl = $"{indexerResponse.HttpRequest.Url.Scheme}://{indexerResponse.HttpRequest.Url.Host}/";

        // Older episodes of a season are loaded on demand; only worth it for real searches.
        var expandEpisodes = indexerResponse.HttpRequest.Url.Path.Contains("/buscar", StringComparison.OrdinalIgnoreCase);

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        foreach (var card in dom.QuerySelectorAll("article.wolf-card"))
        {
            var cardLink = card.QuerySelector("a.wolf-card-main");

            if (cardLink == null)
            {
                continue;
            }

            var info = new WolfMax4KCard
            {
                Title = cardLink.TextContent,
                InfoUrl = GetAbsoluteUrl(cardLink.GetAttribute("href")),
                Year = ParseYear(card.QuerySelector("p.wolf-card-meta span")?.TextContent),
                Genres = card.QuerySelectorAll("p.wolf-card-meta span").Skip(1).Select(s => s.TextContent.Trim()).Where(s => s.IsNotNullOrWhiteSpace()).ToList(),
                Description = card.QuerySelector("p.wolf-card-summary")?.TextContent.Trim(),
                PublishDate = ParseDate(card.QuerySelector("p.wolf-card-date time")?.GetAttribute("datetime"))
            };

            var seen = new HashSet<string>();
            var rows = card.QuerySelectorAll("li.wolf-card-file").ToList();

            if (expandEpisodes && card.QuerySelector("details.wolf-card-more[data-wolf-episodes]") is { } more)
            {
                rows.AddRange(FetchExtraEpisodes(more.GetAttribute("data-wolf-episodes")));
            }

            foreach (var row in rows)
            {
                var release = ParseRow(info, row);

                if (release != null && seen.Add(release.Guid))
                {
                    releases.Add(release);
                }
            }
        }

        return releases;
    }

    private TorrentInfo ParseRow(WolfMax4KCard card, IElement row)
    {
        var button = row.QuerySelector("button.protected-download");
        var contentId = button?.GetAttribute("data-content-code");
        var tabla = button?.GetAttribute("data-tabla");

        if (contentId.IsNullOrWhiteSpace() || tabla.IsNullOrWhiteSpace())
        {
            return null;
        }

        var format = row.QuerySelector("a.wolf-card-format");
        var label = format?.QuerySelector("strong")?.TextContent;
        var qualityLabel = format?.QuerySelector("span")?.TextContent;

        // Movies only have the quality in <strong>; episodes have "Episodio 1x02" + <span>quality</span>.
        if (qualityLabel.IsNullOrWhiteSpace())
        {
            qualityLabel = label;
            label = null;
        }

        var parsed = WolfMax4KTitleBuilder.Build(tabla, card.Title, card.Year, label, qualityLabel);
        var sizeText = row.QuerySelector("span.wolf-card-size")?.TextContent;
        var infoUrl = GetAbsoluteUrl(format?.GetAttribute("href")) ?? card.InfoUrl;
        var downloadUrl = $"{_baseUrl.TrimEnd('/')}/api/descargas?tabla={Uri.EscapeDataString(tabla)}&code={Uri.EscapeDataString(contentId)}";

        if (infoUrl != null)
        {
            downloadUrl += $"&info={Uri.EscapeDataString(new Uri(infoUrl).PathAndQuery)}";
        }

        return new TorrentInfo
        {
            // Domain independent, so releases stay the same when the site moves.
            Guid = $"WolfMax4K-{tabla}-{contentId}",
            Title = parsed.Title,
            InfoUrl = infoUrl,
            DownloadUrl = downloadUrl,
            Categories = GetCategories(tabla, parsed),
            Size = sizeText.IsNotNullOrWhiteSpace() ? ParseUtil.GetBytes(sizeText) : null,
            PublishDate = card.PublishDate,
            Year = card.Year,
            Genres = card.Genres,
            Description = card.Description,
            Resolution = parsed.Resolution,

            // The site does not publish swarm information.
            Seeders = 1,
            Peers = 2,
            DownloadVolumeFactor = 0,
            UploadVolumeFactor = 1
        };
    }

    private IEnumerable<IElement> FetchExtraEpisodes(string path)
    {
        var rows = new List<IElement>();

        if (path.IsNullOrWhiteSpace())
        {
            return rows;
        }

        var nextUrl = GetAbsoluteUrl(path);
        var parser = new HtmlParser();

        for (var page = 0; page < MaxExtraEpisodePages && nextUrl != null; page++)
        {
            var request = new HttpRequestBuilder(nextUrl)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Requested-With", "XMLHttpRequest")
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
                _logger.Debug(ex, "Could not load extra WolfMax4K episodes from {0}", nextUrl);
                break;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Debug("Could not load extra WolfMax4K episodes from {0}: HTTP {1}", nextUrl, response.StatusCode);
                break;
            }

            WolfMax4KEpisodesResult result;

            try
            {
                result = JsonConvert.DeserializeObject<WolfMax4KEpisodesResult>(response.Content);
            }
            catch (JsonException ex)
            {
                _logger.Debug(ex, "Unexpected WolfMax4K episodes response from {0}", nextUrl);
                break;
            }

            if (result is not { Success: true } || result.Html.IsNullOrWhiteSpace())
            {
                break;
            }

            var fragment = parser.ParseDocument($"<ul>{result.Html}</ul>");
            rows.AddRange(fragment.QuerySelectorAll("li.wolf-card-file"));

            nextUrl = result.NextAfter is > 0 ? SetQueryParameter(nextUrl, "after", result.NextAfter.Value.ToString(CultureInfo.InvariantCulture)) : null;
        }

        return rows;
    }

    private ICollection<IndexerCategory> GetCategories(string tabla, WolfMax4KParsedTitle parsed)
    {
        var categories = _categories.MapTrackerCatToNewznab(tabla).ToList();

        var isMovie = tabla == "peliculas" || (!parsed.IsEpisodic && tabla == "animaciones");

        if (isMovie)
        {
            categories.Add(parsed.Resolution switch
            {
                "2160p" => NewznabStandardCategory.MoviesUHD,
                "1080p" or "720p" => NewznabStandardCategory.MoviesHD,
                _ => NewznabStandardCategory.MoviesSD
            });

            if (tabla != "peliculas")
            {
                categories.Add(NewznabStandardCategory.Movies);
            }
        }
        else if (tabla is "series" or "telenovelas")
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
        if (url.IsNullOrWhiteSpace())
        {
            return null;
        }

        return new Uri(new Uri(_baseUrl), url).AbsoluteUri;
    }

    private static string SetQueryParameter(string url, string key, string value)
    {
        var uri = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        query[key] = value;
        uri.Query = query.ToString();

        return uri.Uri.AbsoluteUri;
    }

    private static int ParseYear(string value)
    {
        return int.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is > 1870 and < 2200 ? year : 0;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : DateTime.UtcNow;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    private class WolfMax4KCard
    {
        public string Title { get; init; }
        public string InfoUrl { get; init; }
        public int Year { get; init; }
        public ICollection<string> Genres { get; init; }
        public string Description { get; init; }
        public DateTime PublishDate { get; init; }
    }
}

public class WolfMax4KParsedTitle
{
    public string Title { get; init; }
    public string Resolution { get; init; }
    public bool IsEpisodic { get; init; }
}

public static class WolfMax4KTitleBuilder
{
    private static readonly Regex SeasonInTitleRegex = new(@"\s*-\s*(?<season>\d{1,2})\s*[ªº°]?\s*Temporada\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BracketRegex = new(@"\[(?<tag>[^\]]*)\]", RegexOptions.Compiled);
    private static readonly Regex EpisodeRegex = new(@"(?<season>\d{1,2})x(?<first>\d{1,3})(?:\s*(?<sep>al|a|-|&|y)\s*(?:(?<season2>\d{1,2})x)?(?<last>\d{1,3}))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ResolutionRegex = new(@"(?<res>\d{3,4})p", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FourKRegex = new(@"\b(?:4K|UHD)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SubtitledRegex = new(@"\b(?:Subs?\.?|Subtitulad[oa]|V\.?O\.?S\.?E?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex QualityOnlyTagRegex = new(@"^\s*(?:\d{3,4}p|4K|UHD|HD|SD)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static WolfMax4KParsedTitle Build(string tabla, string cardTitle, int year, string episodeLabel, string qualityLabel)
    {
        var name = ParseUtil.NormalizeMultiSpaces(cardTitle ?? string.Empty);
        var tags = new List<string>();

        foreach (var match in BracketRegex.Matches(name).Cast<Match>())
        {
            var tag = match.Groups["tag"].Value.Trim().TrimEnd('.');

            if (tag.IsNotNullOrWhiteSpace() && !QualityOnlyTagRegex.IsMatch(tag))
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

        var (quality, resolution) = NormalizeQuality(qualityLabel);
        var language = tags.Any(t => SubtitledRegex.IsMatch(t)) ? "VOSE" : "SPANISH";

        var episode = FormatEpisode(episodeLabel, titleSeason);
        var isEpisodic = episode != null || titleSeason.HasValue || tabla is "series" or "telenovelas";

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

            if (episode == null && episodeLabel.IsNotNullOrWhiteSpace() && episodeLabel.Contains("Completo", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add("Completo");
            }
        }
        else if (year > 0)
        {
            parts.Add($"({year})");
        }

        parts.AddRange(tags.Where(t => !SubtitledRegex.IsMatch(t)).Select(t => $"[{t}]"));
        parts.Add(quality);
        parts.Add(language);

        return new WolfMax4KParsedTitle
        {
            Title = ParseUtil.NormalizeMultiSpaces(string.Join(" ", parts.Where(p => p.IsNotNullOrWhiteSpace()))),
            Resolution = resolution,
            IsEpisodic = isEpisodic
        };
    }

    private static string FormatEpisode(string label, int? titleSeason)
    {
        if (label.IsNullOrWhiteSpace())
        {
            return null;
        }

        var match = EpisodeRegex.Match(label);

        if (!match.Success)
        {
            return null;
        }

        var season = int.Parse(match.Groups["season"].Value, CultureInfo.InvariantCulture);
        var first = int.Parse(match.Groups["first"].Value, CultureInfo.InvariantCulture);

        if (season == 0 && titleSeason.HasValue)
        {
            season = titleSeason.Value;
        }

        var result = $"S{season:00}E{first:00}";

        if (match.Groups["last"].Success)
        {
            var last = int.Parse(match.Groups["last"].Value, CultureInfo.InvariantCulture);
            var sameSeason = !match.Groups["season2"].Success || int.Parse(match.Groups["season2"].Value, CultureInfo.InvariantCulture) == season;

            if (sameSeason && last > first)
            {
                var sep = match.Groups["sep"].Value.ToLowerInvariant();

                result += sep is "&" or "y" ? $"E{last:00}" : $"-E{last:00}";
            }
        }

        return result;
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

public static class WolfMax4KProofOfWork
{
    public const int DefaultDifficulty = 3;
    private const long MaxIterations = 100_000_000;

    // Mirrors the site's download.js: find the smallest nonce so that
    // hex(sha256(challenge + nonce)) starts with `difficulty` zeros.
    public static long Solve(string challenge, int difficulty = DefaultDifficulty)
    {
        var prefix = Encoding.UTF8.GetBytes(challenge);
        var buffer = new byte[prefix.Length + 20];
        Buffer.BlockCopy(prefix, 0, buffer, 0, prefix.Length);

        var hash = new byte[32];

        for (long nonce = 0; nonce < MaxIterations; nonce++)
        {
            var length = prefix.Length + WriteDecimal(buffer, prefix.Length, nonce);

            SHA256.HashData(buffer.AsSpan(0, length), hash);

            if (HasLeadingZeroNibbles(hash, difficulty))
            {
                return nonce;
            }
        }

        throw new ReleaseDownloadException("Could not solve WolfMax4K download challenge");
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

    private static int WriteDecimal(byte[] buffer, int offset, long value)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < digits.Length; i++)
        {
            buffer[offset + i] = (byte)digits[i];
        }

        return digits.Length;
    }
}

public class WolfMax4KApiResult
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

    public bool? External { get; set; }

    public object Verification { get; set; }
}

public class WolfMax4KApiResponse
{
    public WolfMax4KApiResponse(HttpStatusCode statusCode, WolfMax4KApiResult result)
    {
        StatusCode = statusCode;
        Result = result;
    }

    public HttpStatusCode StatusCode { get; }
    public WolfMax4KApiResult Result { get; }
}

public class WolfMax4KEpisodesResult
{
    public bool Success { get; set; }
    public string Html { get; set; }

    [JsonProperty("next_after")]
    public long? NextAfter { get; set; }
}

public class WolfMax4KSettings : NoAuthTorrentBaseSettings
{
    [FieldDefinition(2, Label = "Alternative domain", Type = FieldType.Textbox, Placeholder = "https://wolftorrent.example/", HelpText = "Optional. The indexer follows the site's redirects when it changes domain. If it still can't find the site, paste the current domain here.")]
    public string AlternativeDomain { get; set; }
}
