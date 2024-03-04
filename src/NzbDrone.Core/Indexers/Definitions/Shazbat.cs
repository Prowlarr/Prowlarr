using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions;

public class Shazbat : TorrentIndexerBase<ShazbatSettings>
{
    public override string Name => "Shazbat";
    public override string[] IndexerUrls => new[] { "https://www.shazbat.tv/" };
    public override string Description => "Shazbat is a PRIVATE Torrent Tracker with highly curated TV content";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(5.1);

    public Shazbat(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new ShazbatRequestGenerator(Settings);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new ShazbatParser(Definition, Settings, RateLimit, _httpClient, _logger);
    }

    protected override async Task DoLogin()
    {
        var loginUrl = Settings.BaseUrl + "login";

        var requestBuilder = new HttpRequestBuilder(loginUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true
        };

        var authLoginRequest = requestBuilder.Post()
            .AddFormParameter("referer", "")
            .AddFormParameter("query", "")
            .AddFormParameter("tv_timezone", "0")
            .AddFormParameter("tv_login", Settings.Username)
            .AddFormParameter("tv_password", Settings.Password)
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", loginUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            using var dom = await parser.ParseDocumentAsync(response.Content);
            var errorMessage = dom.QuerySelector("div#fail .modal-body")?.TextContent.Trim();

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        var cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded.");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse response)
    {
        return response.Content.ContainsIgnoreCase("sign in now");
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            },
            Flags = new List<IndexerFlag>
            {
                IndexerFlag.Scene
            }
        };

        caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.TV);
        caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.TVSD);
        caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.TVHD);
        caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.TVUHD);

        return caps;
    }
}

public class ShazbatRequestGenerator : IIndexerRequestGenerator
{
    private readonly ShazbatSettings _settings;

    public ShazbatRequestGenerator(ShazbatSettings settings)
    {
        _settings = settings;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term)
    {
        term = FixSearchTerm(term);

        if (term.IsNotNullOrWhiteSpace())
        {
            var request = new HttpRequestBuilder(_settings.BaseUrl + "search").Post()
                .AddFormParameter("search", term)
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("X-Requested-With", "XMLHttpRequest")
                .SetHeader("Referer", _settings.BaseUrl)
                .Accept(HttpAccept.Html)
                .Build();

            yield return new IndexerRequest(request);
        }
        else
        {
            var request = new HttpRequestBuilder(_settings.BaseUrl + "torrents")
                .SetHeader("Referer", _settings.BaseUrl)
                .Accept(HttpAccept.Html)
                .Build();

            yield return new IndexerRequest(request);
        }
    }

    private static string FixSearchTerm(string term)
    {
        term = Regex.Replace(term, @"\b[S|E]\d+\b", string.Empty, RegexOptions.IgnoreCase);
        term = Regex.Replace(term, @"(.+)\b\d{4}(\.\d{2}\.\d{2})?\b", "$1");
        term = Regex.Replace(term, @"[\.\s\(\)\[\]]+", " ");

        return term.ToLower().Trim();
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class ShazbatParser : IParseIndexerResponse
{
    private readonly ProviderDefinition _definition;
    private readonly ShazbatSettings _settings;
    private readonly TimeSpan _rateLimit;
    private readonly IIndexerHttpClient _httpClient;
    private readonly Logger _logger;

    private readonly Regex _torrentInfoRegex = new (@"\((?<size>\d+)\):(?<seeders>\d+) \/ :(?<leechers>\d+)$", RegexOptions.Compiled);
    private readonly HashSet<string> _hdResolutions = new () { "1080p", "1080i", "720p" };

    public ShazbatParser(ProviderDefinition definition, ShazbatSettings settings, TimeSpan rateLimit, IIndexerHttpClient httpClient, Logger logger)
    {
        _definition = definition;
        _settings = settings;
        _rateLimit = rateLimit;
        _httpClient = httpClient;
        _logger = logger;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var hasGlobalFreeleech = dom.QuerySelector("span:contains(\"Freeleech until:\"):has(span.datetime)") != null;

        releaseInfos.AddRange(ParseResults(indexerResponse, hasGlobalFreeleech));

        var shows = dom.QuerySelectorAll("div.show[data-id]");
        if (shows.Any())
        {
            var showPagesFetchLimit = _settings.ShowPagesFetchLimit ?? 2;

            if (showPagesFetchLimit is < 1 or > 5)
            {
                throw new IndexerException(indexerResponse, "Value for Show Pages Fetch Limit should be between 1 and 5. Current value: {0}.", showPagesFetchLimit);
            }

            if (shows.Length > showPagesFetchLimit)
            {
                _logger.Debug($"Your search returned {shows.Length} shows. Use a more specific search term for more relevant results.");
            }

            if (indexerResponse.HttpResponse.GetCookies() == null || !indexerResponse.HttpResponse.GetCookies().Any())
            {
                throw new IndexerException(indexerResponse, "Invalid cookies. Most likely your session expired or was killed.");
            }

            foreach (var show in shows.Take(showPagesFetchLimit))
            {
                var showPageUrl = new HttpRequestBuilder(_settings.BaseUrl + "show")
                    .AddQueryParam("id", show.GetAttribute("data-id"))
                    .Build()
                    .Url.FullUri;

                var showRequest =  new HttpRequestBuilder(_settings.BaseUrl + "show").Post()
                    .SetCookies(indexerResponse.HttpResponse.GetCookies() ?? new Dictionary<string, string>())
                    .AddQueryParam("id", show.GetAttribute("data-id"))
                    .AddQueryParam("show_mode", "torrents")
                    .AddFormParameter("portlet", "true")
                    .AddFormParameter("tab", "true")
                    .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                    .SetHeader("X-Requested-With", "XMLHttpRequest")
                    .SetHeader("Referer", showPageUrl)
                    .Accept(HttpAccept.Html)
                    .WithRateLimit(_rateLimit.TotalSeconds)
                    .Build();

                _logger.Debug("Downloading Feed " + showRequest.ToString());

                var releaseRequest = new IndexerRequest(showRequest);
                var releaseResponse = new IndexerResponse(releaseRequest, _httpClient.ExecuteProxied(releaseRequest.HttpRequest, _definition));

                if (releaseResponse.HttpResponse.Content.ContainsIgnoreCase("sign in now"))
                {
                    // Remove cookie cache
                    CookiesUpdater(null, null);
                    throw new IndexerAuthException("We are being redirected to the Shazbat login page. Most likely your session expired or was killed.");
                }

                if (releaseResponse.HttpResponse.HasHttpError)
                {
                    if (releaseResponse.HttpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        throw new TooManyRequestsException(releaseRequest.HttpRequest, releaseResponse.HttpResponse);
                    }

                    throw new IndexerException(releaseResponse, $"HTTP Error - {releaseResponse.HttpResponse.StatusCode}. {showRequest.Url.FullUri}");
                }

                releaseInfos.AddRange(ParseResults(releaseResponse, hasGlobalFreeleech));
            }
        }

        return releaseInfos.ToArray();
    }

    private IList<ReleaseInfo> ParseResults(IndexerResponse indexerResponse, bool hasGlobalFreeleech = false)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        if (!hasGlobalFreeleech)
        {
            hasGlobalFreeleech = dom.QuerySelector("span:contains(\"Freeleech until:\"):has(span.datetime)") != null;
        }

        var publishDate = DateTime.Now;

        var rows = dom.QuerySelectorAll("#torrent-table tr.eprow, table tr.eprow");
        foreach (var row in rows)
        {
            var downloadUrl = _settings.BaseUrl + row.QuerySelector("td:nth-of-type(5) a[href^=\"load_torrent?\"]")?.GetAttribute("href");
            var infoUrl = _settings.BaseUrl + row.QuerySelector("td:nth-of-type(5) [href^=\"torrent_info?\"]")?.GetAttribute("href");
            var title = ParseTitle(row.QuerySelector("td:nth-of-type(3)"));

            var infoString = row.QuerySelector("td:nth-of-type(4)")?.TextContent.Trim() ?? string.Empty;
            var matchInfo = _torrentInfoRegex.Match(infoString);
            var size = matchInfo.Groups["size"].Success && long.TryParse(matchInfo.Groups["size"].Value, out var outSize) ? outSize : 0;
            var seeders = matchInfo.Groups["seeders"].Success && int.TryParse(matchInfo.Groups["seeders"].Value, out var outSeeders) ? outSeeders : 0;
            var leechers = matchInfo.Groups["leechers"].Success && int.TryParse(matchInfo.Groups["leechers"].Value, out var outLeechers) ? outLeechers : 0;

            var dateTimestamp = row.QuerySelector(".datetime[data-timestamp]")?.GetAttribute("data-timestamp");
            publishDate = dateTimestamp != null && ParseUtil.TryCoerceDouble(dateTimestamp, out var timestamp) ? DateTimeUtil.UnixTimestampToDateTime(timestamp) : publishDate.AddMinutes(-1);

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = ParseCategories(title),
                Size = size,
                Seeders = seeders,
                Peers = seeders + leechers,
                PublishDate = publishDate,
                Scene = true,
                Genres = row.QuerySelectorAll("label.label-tag").Select(t => t.TextContent.Trim()).ToList(),
                DownloadVolumeFactor = hasGlobalFreeleech ? 0 : 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 1,
                MinimumSeedTime = 172800, // 48 hours
            };

            releaseInfos.Add(release);
        }

        return releaseInfos;
    }

    private static string ParseTitle(IElement titleRow)
    {
        var title = titleRow?.ChildNodes.First(n => n.NodeType == NodeType.Text && n.TextContent.Trim().IsNotNullOrWhiteSpace());

        return title?.TextContent.Trim();
    }

    protected virtual List<IndexerCategory> ParseCategories(string title)
    {
        var categories = new List<IndexerCategory>
        {
            NewznabStandardCategory.TV,
            title switch
            {
                _ when _hdResolutions.Any(title.Contains) => NewznabStandardCategory.TVHD,
                _ when title.Contains("2160p") => NewznabStandardCategory.TVUHD,
                _ => NewznabStandardCategory.TVSD
            }
        };

        return categories;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class ShazbatSettingsValidator : UserPassBaseSettingsValidator<ShazbatSettings>
{
    public ShazbatSettingsValidator()
    {
        RuleFor(c => c.ShowPagesFetchLimit).GreaterThan(0).When(c => c.ShowPagesFetchLimit.HasValue).WithMessage("Should be greater than zero");
        RuleFor(c => c.ShowPagesFetchLimit).LessThanOrEqualTo(5).When(c => c.ShowPagesFetchLimit.HasValue).WithMessage("Should be less than or equal to 5");
    }
}

public class ShazbatSettings : UserPassTorrentBaseSettings
{
    private static readonly ShazbatSettingsValidator Validator = new ();

    [FieldDefinition(4, Type = FieldType.Number, Label = "Show Pages Fetch Limit", HelpText = "The number of show pages should Prowlarr fetch when searching. Default: 2.")]
    public int? ShowPagesFetchLimit { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
