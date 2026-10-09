using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions;

public class DonTorrent : TorrentIndexerBase<NoAuthTorrentBaseSettings>
{
    private readonly ICacheManager _cacheManager;

    public override string Name => "DonTorrent";
    public override string[] IndexerUrls => new[] { "https://dontorrent.moi/" };
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
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new DonTorrentRequestGenerator(Settings);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new DonTorrentParser(Definition, Settings, Capabilities.Categories, RateLimit, _httpClient, _cacheManager.GetCache<DonTorrentDetails>(GetType(), "details"), _logger);
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

        return await base.Download(new Uri(torrentUrl)).ConfigureAwait(false);
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
            throw new ReleaseDownloadException("DonTorrent is asking for a captcha. Open {0} in a browser, download any release once to solve it, then retry.", Settings.BaseUrl);
        }

        if (!validate.Result.Success || validate.Result.DownloadUrl.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("DonTorrent rejected the download request: {0}", validate.Result.Error ?? validate.StatusCode.ToString());
        }

        return new Uri(new Uri(Settings.BaseUrl), validate.Result.DownloadUrl).AbsoluteUri;
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
        var request = new HttpRequestBuilder(Settings.BaseUrl)
            .Resource("api_validate_pow.php")
            .Accept(HttpAccept.Json)
            .SetHeader("Referer", Settings.BaseUrl)
            .WithRateLimit(RateLimit.TotalSeconds)
            .Build();

        request.Method = HttpMethod.Post;
        request.Headers.ContentType = "application/json";
        request.SetContent(body.ToJson(Formatting.None));

        // 403/404/429 carry a JSON body with the reason, read it instead of throwing.
        request.SuppressHttpError = true;

        var response = await _httpClient.ExecuteProxiedAsync(request, Definition).ConfigureAwait(false);

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
    private static readonly Regex LatestEpisodeRegex = new(@"^(?<title>.+?)\s*:\s*(?<episode>\d{1,2}x\d{1,3}.*)$", RegexOptions.Compiled);

    private readonly ProviderDefinition _definition;
    private readonly NoAuthTorrentBaseSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;
    private readonly TimeSpan _rateLimit;
    private readonly IIndexerHttpClient _httpClient;
    private readonly ICached<DonTorrentDetails> _detailsCache;
    private readonly Logger _logger;

    private int _detailRequests;

    public DonTorrentParser(ProviderDefinition definition, NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient, ICached<DonTorrentDetails> detailsCache, Logger logger)
    {
        _definition = definition;
        _settings = settings;
        _categories = categories;
        _rateLimit = rateLimit;
        _httpClient = httpClient;
        _detailsCache = detailsCache;
        _logger = logger;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

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

            yield return CreateRelease(item, "peliculas", contentId, parsed, details?.Year ?? 0, item.PublishDate ?? DateTime.UtcNow, 1);

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

            yield return CreateRelease(item, row.Tabla ?? tabla, row.ContentId, parsed, 0, item.PublishDate ?? row.PublishDate ?? DateTime.UtcNow, parsed.EpisodeCount);
        }
    }

    private TorrentInfo CreateRelease(DonTorrentItem item, string tabla, string contentId, DonTorrentParsedTitle parsed, int year, DateTime publishDate, int episodeCount)
    {
        return new TorrentInfo
        {
            Guid = $"{_settings.BaseUrl.TrimEnd('/')}/{tabla}/{contentId}",
            Title = parsed.Title,
            InfoUrl = item.InfoUrl,
            DownloadUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api_validate_pow.php?tabla={Uri.EscapeDataString(tabla)}&content_id={Uri.EscapeDataString(contentId)}",
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
            .SetHeader("Referer", _settings.BaseUrl)
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
        return new Uri(new Uri(_settings.BaseUrl), url).AbsoluteUri;
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
    public List<DonTorrentDetailsRow> Rows { get; } = new();
}

public class DonTorrentDetailsRow
{
    public string ContentId { get; set; }
    public string Tabla { get; set; }
    public string Label { get; set; }
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
