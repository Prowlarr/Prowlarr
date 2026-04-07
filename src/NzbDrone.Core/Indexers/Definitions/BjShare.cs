using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
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

public class BjShare : TorrentIndexerBase<BjShareSettings>
{
    public override string Name => "Bj-Share";
    public override string[] IndexerUrls => new[] { "https://bj-share.info/" };
    public override string Description => "Private PT-BR torrent tracker";
    public override string Language => "pt-BR";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public BjShare(
        IIndexerHttpClient httpClient,
        IEventAggregator eventAggregator,
        IIndexerStatusService indexerStatusService,
        IConfigService configService,
        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new BjShareRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new BjShareParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        if (string.IsNullOrWhiteSpace(Settings.Cookie))
        {
            throw new IndexerAuthException("BJ-Share cookie is empty");
        }

        var cookies = ParseCookieHeader(Settings.Cookie);

        if (cookies.Count == 0)
        {
            throw new IndexerAuthException("BJ-Share cookie is invalid");
        }

        UpdateCookies(cookies, DateTime.Now.AddDays(30));
        await Task.CompletedTask;
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return httpResponse.RedirectUrl.Contains("login.php") ||
            !httpResponse.Content.Contains("/logout.php?auth=");
    }

    private static IDictionary<string, string> ParseCookieHeader(string rawCookie)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in rawCookie.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var name = part[..idx].Trim();
            var value = part[(idx + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(name))
            {
                dict[name] = value;
            }
        }

        return dict;
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep },
            MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q },
            SupportsRawSearch = true
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Filmes");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.PC, "Aplicativos");
        caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Jogos");
        caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.BooksComics, "Mangás");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "Outros");
        caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVSport, "Esportes");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.BooksMags, "Revistas");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.Books, "E-Books");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.AudioAudiobook, "Audiobook");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.BooksComics, "HQ");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TV, "Stand Up Comedy");
        caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Cursos");
        caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.XXX, "Filmes Adultos");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.XXXOther, "Jogos Adultos");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.XXXOther, "Mangás Adultos");
        caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.XXXOther, "Animes Adultos");
        caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.XXXOther, "HQ Adultas");

        return caps;
    }
}

public class BjShareRequestGenerator : IIndexerRequestGenerator
{
    private readonly BjShareSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    public BjShareRequestGenerator(BjShareSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        return BuildSearch(searchCriteria, new[] { 2, 14 });
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        return BuildSearch(searchCriteria, new[] { 1 });
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        return BuildSearch(searchCriteria, null);
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return BuildSearch(searchCriteria, null);
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return BuildSearch(searchCriteria, null);
    }

    private IndexerPageableRequestChain BuildSearch(SearchCriteriaBase searchCriteria, IEnumerable<int> forcedCategories)
    {
        var chain = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        var query = searchCriteria.SearchTerm?.Trim() ?? string.Empty;

        if (searchCriteria is TvSearchCriteria)
        {
            query = Regex.Replace(query, @"(S\d+E\d+|S\d+)", "", RegexOptions.IgnoreCase).Trim();
        }

        parameters.Set("searchstr", query);
        parameters.Set("action", "basic");
        parameters.Set("searchsubmit", "1");

        if (_settings.FreeleechOnly)
        {
            parameters.Set("freetorrent", "1");
        }

        foreach (var cat in forcedCategories ?? Array.Empty<int>())
        {
            parameters.Set($"filter_cat[{cat}]", "1");
        }

        var request = new HttpRequestBuilder($"{_settings.BaseUrl.TrimEnd('/')}/torrents.php?{parameters.GetQueryString()}")
            .Accept(HttpAccept.Html)
            .SetCookies(GetCookies() ?? new Dictionary<string, string>())
            .Build();

        chain.Add(new[] { new IndexerRequest(request) });
        return chain;
    }
}

public class BjShareParser : IParseIndexerResponse
{
    private readonly BjShareSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    public BjShareParser(BjShareSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releases = new List<ReleaseInfo>();
        var parser = new HtmlParser();

        using var document = parser.ParseDocument(indexerResponse.Content);

        var rows = document.QuerySelectorAll("table#torrent_table tr, table.torrent_table tr");
        GroupContext currentGroup = null;

        foreach (var row in rows)
        {
            if (row.ClassList.Contains("colhead"))
            {
                continue;
            }

            if (row.ClassList.Contains("group"))
            {
                currentGroup = ParseGroup(row, indexerResponse.Request.Url.FullUri);
                continue;
            }

            if (row.ClassList.Contains("group_torrent"))
            {
                if (currentGroup == null)
                {
                    continue;
                }

                var release = ParseGroupedTorrent(row, currentGroup, indexerResponse.Request.Url.FullUri);
                if (release != null)
                {
                    releases.Add(release);
                }

                continue;
            }

            if (row.ClassList.Contains("torrent"))
            {
                var release = ParseStandaloneTorrent(row, indexerResponse.Request.Url.FullUri);
                if (release != null)
                {
                    releases.Add(release);
                }
            }
        }

        return releases;
    }

    private GroupContext ParseGroup(IElement row, string baseUrl)
    {
        var titleCell = row.QuerySelector("div.group_info");
        var categoryHref = row.QuerySelector("td.cats_col a")?.GetAttribute("href") ?? string.Empty;

        var rawTitle = titleCell?.TextContent?.Trim() ?? string.Empty;
        rawTitle = Regex.Replace(rawTitle, @"\s+", " ");

        var groupDetailsHref = row.QuerySelector("div.group_info a[href^=\"torrents.php?id=\"]")?.GetAttribute("href");
        var seriesHref = row.QuerySelector("div.group_info a[href^=\"series.php?id=\"]")?.GetAttribute("href");

        var title = ExtractEnglishOrFallbackTitle(rawTitle);
        var year = ExtractYear(rawTitle);
        var categoryId = ExtractCategoryId(categoryHref);

        return new GroupContext
        {
            Title = title,
            Year = year,
            CategoryId = categoryId,
            GroupDetailsUrl = ToAbsolute(baseUrl, groupDetailsHref),
            SeriesUrl = ToAbsolute(baseUrl, seriesHref)
        };
    }

    private ReleaseInfo ParseGroupedTorrent(IElement row, GroupContext group, string baseUrl)
    {
        var downloadHref = row.QuerySelector("a[title=\"Baixar\"]")?.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(downloadHref))
        {
            return null;
        }

        var detailsHref = row.QuerySelector("a[href*=\"torrentid=\"]")?.GetAttribute("href");
        var infoText = row.QuerySelector("td[colspan=\"3\"] a[href*=\"torrentid=\"]")?.TextContent?.Trim() ?? string.Empty;

        var release = new TorrentInfo
        {
            Guid = ToAbsolute(baseUrl, downloadHref),
            DownloadUrl = ToAbsolute(baseUrl, downloadHref),
            InfoUrl = ToAbsolute(baseUrl, detailsHref),
            Title = BuildReleaseTitle(group.Title, group.Year, infoText),
            Size = ParseSize(row.QuerySelector("td:nth-last-child(4)")?.TextContent),
            Seeders = ParseInt(row.QuerySelector("td:nth-last-child(2)")?.TextContent),
            Peers = ParseInt(row.QuerySelector("td:nth-last-child(1)")?.TextContent),
            Grabs = ParseInt(row.QuerySelector("td:nth-last-child(3)")?.TextContent),
            PublishDate = ParseBjDate(row.QuerySelector("td.nobr .time")?.GetAttribute("title")),
            DownloadVolumeFactor = row.QuerySelector("strong[title*=\"Free\"]") != null ? 0 : 1,
            UploadVolumeFactor = 1,
            MinimumRatio = 1.0,
            MinimumSeedTime = 604800
        };

        var cats = _categories.MapTrackerCatToNewznab(group.CategoryId.ToString());
        if (cats.Any())
        {
            release.Categories = cats;
        }

        return release;
    }

    private ReleaseInfo ParseStandaloneTorrent(IElement row, string baseUrl)
    {
        // Implementar se o BJ-Share realmente retornar esse formato para outras buscas.
        return null;
    }

    private static string ExtractEnglishOrFallbackTitle(string raw)
    {
        var match = Regex.Match(raw, @"^(.*?)\[(.*?)\]");
        if (match.Success)
        {
            var inner = match.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(inner) && !Regex.IsMatch(inner, @"^\d{4}$"))
            {
                return inner;
            }
        }

        return Regex.Replace(raw, @"\[\d{4}\].*$", "").Trim();
    }

    private static int? ExtractYear(string raw)
    {
        var match = Regex.Match(raw, @"\[(\d{4})\]");
        if (!match.Success)
        {
            return null;
        }

        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static int ExtractCategoryId(string href)
    {
        var match = Regex.Match(href ?? string.Empty, @"filter_cat%5b(\d+)%5d|filter_cat\[(\d+)\]");
        if (!match.Success)
        {
            return 0;
        }

        var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return int.TryParse(value, out var id) ? id : 0;
    }

    private static string BuildReleaseTitle(string title, int? year, string infoText)
    {
        var cleanInfo = Regex.Replace(infoText ?? string.Empty, @"[\[\]]", "");
        cleanInfo = cleanInfo.Replace("Full HD", "1080p");
        cleanInfo = cleanInfo.Replace("4K", "2160p");
        cleanInfo = cleanInfo.Replace("SD", "480p");
        cleanInfo = cleanInfo.Replace(" / Free", "");
        cleanInfo = Regex.Replace(cleanInfo, @"\s+", " ").Trim();

        return $"{title} {(year.HasValue ? year.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)} {cleanInfo}".Trim();
    }

    private static DateTime PublishDateFallback() => DateTime.UtcNow;

    private static DateTime ParseBjDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PublishDateFallback();
        }

        var formats = new[]
        {
            "MMM dd yyyy, HH:mm",
            "MMM dd yyyy, HH:mm",
            "MMM dd yyyy, HH:mm"
        };

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return PublishDateFallback();
    }

    private static long ParseSize(string value)
    {
        return ParseUtil.GetBytes(value);
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return ParseUtil.CoerceInt(value.Replace(",", "").Trim());
    }

    private static string ToAbsolute(string baseUrl, string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        return new HttpUri(baseUrl).CombinePath(href).FullUri;
    }

    private sealed class GroupContext
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public int CategoryId { get; set; }
        public string GroupDetailsUrl { get; set; }
        public string SeriesUrl { get; set; }
    }
}

public class BjShareSettingsValidator : NoAuthSettingsValidator<BjShareSettings>
{
    public BjShareSettingsValidator()
    {
        RuleFor(x => x.Cookie).NotEmpty();
        RuleFor(x => x.BaseUrl).NotEmpty();
    }
}

public class BjShareSettings : NoAuthTorrentBaseSettings
{
    private static readonly BjShareSettingsValidator Validator = new();

    [FieldDefinition(1, Label = "Cookie", Type = FieldType.Textbox, HelpText = "Cookie completo da sessão autenticada")]
    public string Cookie { get; set; }

    [FieldDefinition(2, Label = "Freeleech only", Type = FieldType.Checkbox)]
    public bool FreeleechOnly { get; set; }

    public BjShareSettings()
    {
        BaseUrl = "https://bj-share.info/";
    }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
