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
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using NLog;
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

public class WolfMax4K : TorrentIndexerBase<NoAuthTorrentBaseSettings>
{
    public override string Name => "WolfMax4K";
    public override string[] IndexerUrls => new[] { "https://wolfmax4k.com/" };
    public override string Description => "WolfMax4K is a SPANISH public site for movies, TV series and documentaries";
    public override string Language => "es-ES";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(3);

    public WolfMax4K(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new WolfMax4KRequestGenerator(Settings);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new WolfMax4KParser(Definition, Settings, Capabilities.Categories, RateLimit, _httpClient, _logger);
    }

    public override async Task<IndexerDownloadResponse> Download(Uri link)
    {
        // Search results point to a local placeholder (api/descargas?tabla=...&content_id=...).
        // The real .torrent URL is only handed out after the site's proof-of-work exchange.
        if (!link.AbsolutePath.EndsWith("/api/descargas", StringComparison.OrdinalIgnoreCase))
        {
            return await base.Download(link).ConfigureAwait(false);
        }

        var query = HttpUtility.ParseQueryString(link.Query);
        var tabla = query["tabla"];

        if (tabla.IsNullOrWhiteSpace() || !int.TryParse(query["content_id"], NumberStyles.None, CultureInfo.InvariantCulture, out var contentId))
        {
            throw new ReleaseDownloadException("Invalid WolfMax4K download link: {0}", link.AbsoluteUri);
        }

        var torrentUrl = await GetTorrentUrl(contentId, tabla).ConfigureAwait(false);

        return await base.Download(new Uri(torrentUrl)).ConfigureAwait(false);
    }

    private async Task<string> GetTorrentUrl(int contentId, string tabla)
    {
        var generate = await PostDownloadApi(new { action = "generate", content_id = contentId, tabla }).ConfigureAwait(false);

        if (generate.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ReleaseUnavailableException("WolfMax4K content {0}/{1} no longer exists", tabla, contentId);
        }

        ThrowIfLimited(generate);

        if (!generate.Result.Success || generate.Result.Challenge.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("WolfMax4K did not return a download challenge: {0}", generate.Result.Error ?? generate.StatusCode.ToString());
        }

        var difficulty = generate.Result.Difficulty is > 0 and <= 8 ? generate.Result.Difficulty.Value : WolfMax4KProofOfWork.DefaultDifficulty;
        var nonce = WolfMax4KProofOfWork.Solve(generate.Result.Challenge, difficulty);

        var validate = await PostDownloadApi(new { action = "validate", challenge = generate.Result.Challenge, nonce }).ConfigureAwait(false);

        ThrowIfLimited(validate);

        if (validate.Result.Status == "captcha_required")
        {
            // Solving the captcha is deliberately left to a human in a browser.
            throw new ReleaseDownloadException("WolfMax4K is asking for a captcha. Open {0} in a browser, download any release once to solve it, then retry.", Settings.BaseUrl);
        }

        if (!validate.Result.Success || validate.Result.DownloadUrl.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException("WolfMax4K rejected the download request: {0}", validate.Result.Error ?? validate.StatusCode.ToString());
        }

        return new Uri(new Uri(Settings.BaseUrl), validate.Result.DownloadUrl).AbsoluteUri;
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
        var request = new HttpRequestBuilder(Settings.BaseUrl)
            .Resource("api/descargas")
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

    private readonly NoAuthTorrentBaseSettings _settings;

    public WolfMax4KRequestGenerator(NoAuthTorrentBaseSettings settings)
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
    private readonly NoAuthTorrentBaseSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;
    private readonly TimeSpan _rateLimit;
    private readonly IIndexerHttpClient _httpClient;
    private readonly Logger _logger;

    public WolfMax4KParser(ProviderDefinition definition, NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient, Logger logger)
    {
        _definition = definition;
        _settings = settings;
        _categories = categories;
        _rateLimit = rateLimit;
        _httpClient = httpClient;
        _logger = logger;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

        var releases = new List<ReleaseInfo>();

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
        var contentId = button?.GetAttribute("data-content-id");
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

        return new TorrentInfo
        {
            Guid = $"{_settings.BaseUrl.TrimEnd('/')}/{tabla}/{contentId}",
            Title = parsed.Title,
            InfoUrl = GetAbsoluteUrl(format?.GetAttribute("href")) ?? card.InfoUrl,
            DownloadUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/descargas?tabla={Uri.EscapeDataString(tabla)}&content_id={Uri.EscapeDataString(contentId)}",
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

        return new Uri(new Uri(_settings.BaseUrl), url).AbsoluteUri;
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
