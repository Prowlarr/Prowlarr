using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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
        return new BjShareRequestGenerator(Settings);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new BjShareParser(Capabilities.Categories);
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        if (!httpResponse.Content.Contains("logout.php?auth="))
        {
            throw new IndexerAuthException("BjShare authentication with cookies failed.");
        }

        return false;
    }

    protected override IDictionary<string, string> GetCookies()
    {
        return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
    }

    private static IndexerCapabilities SetCapabilities()
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
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TV, "Vídeos de TV");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "Outros");
        caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVSport, "Esportes");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.BooksMags, "Revistas");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.Books, "E-Books");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.AudioAudiobook, "Audiobook");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.BooksComics, "Histórias em Quadrinhos");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TV, "Stand Up Comedy");
        caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXImageSet, "Fotos Adultas");
        caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVOther, "Desenho Animado");
        caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVDocumentary, "Documentários");
        caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Cursos");
        caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.XXX, "Filmes Adultos");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.XXXOther, "Jogos Adultos");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.XXXOther, "Mangás Adultos");
        caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.XXXOther, "Animes Adultos");
        caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.XXXOther, "HQ Adultos");

        return caps;
    }
}

public class BjShareRequestGenerator : IIndexerRequestGenerator
{
    private readonly BjShareSettings _settings;

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    public BjShareRequestGenerator(BjShareSettings settings)
    {
        _settings = settings;
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
        int? seasonFromQuery = null;

        var seasonMatch = Regex.Match(query, @"\{season:\s*(\d+)\}", RegexOptions.IgnoreCase);
        if (seasonMatch.Success)
        {
            seasonFromQuery = int.Parse(seasonMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            query = Regex.Replace(query, @"\{season:\s*\d+\}", "", RegexOptions.IgnoreCase).Trim();
        }

        var episodeMatch = Regex.Match(query, @"\{episode:\s*(\d+)\}", RegexOptions.IgnoreCase);
        if (episodeMatch.Success)
        {
            query = Regex.Replace(query, @"\{episode:\s*\d+\}", "", RegexOptions.IgnoreCase).Trim();
        }

        if (searchCriteria is TvSearchCriteria tv)
        {
            query = Regex.Replace(query, @"(S\d+E\d+|S\d+)", "", RegexOptions.IgnoreCase).Trim();

            var season = tv.Season ?? seasonFromQuery;
            if (season.HasValue)
            {
                query = $"{query} S{season.Value:00}";
            }
        }
        else if (seasonFromQuery.HasValue)
        {
            query = $"{query} S{seasonFromQuery.Value:00}";
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
    private readonly IndexerCapabilitiesCategories _categories;

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    public BjShareParser(IndexerCapabilitiesCategories categories)
    {
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

        var groupDetailsText = row.QuerySelector("div.group_info a[href^=\"torrents.php?id=\"][title=\"View torrent group\"]")?.TextContent?.Trim();
        var titleSource = row.QuerySelector("div.group_info a[href^=\"series.php?id=\"]")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(titleSource))
        {
            titleSource = RemoveGroupDetailsText(rawTitle, groupDetailsText);
        }

        var seasonInformation = ExtractSeasonEpisode(groupDetailsText);
        var title = ExtractEnglishOrFallbackTitle(string.IsNullOrWhiteSpace(titleSource) ? rawTitle : titleSource);
        var year = ExtractYear(rawTitle);
        var categoryId = ExtractCategoryId(categoryHref);

        return new GroupContext
        {
            Title = title,
            Year = year,
            SeasonEpisode = seasonInformation,
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
        var peerStats = ParsePeerStats(row);

        var release = new TorrentInfo
        {
            Guid = ToAbsolute(baseUrl, downloadHref),
            DownloadUrl = ToAbsolute(baseUrl, downloadHref),
            InfoUrl = ToAbsolute(baseUrl, detailsHref),
            Title = BuildReleaseTitle(group.Title, group.Year, group.SeasonEpisode, infoText),
            Categories = _categories.MapTrackerCatToNewznab(group.CategoryId.ToString()),
            Size = ParseSize(row.QuerySelector("td:nth-last-child(4)")?.TextContent),
            Seeders = peerStats.Seeders,
            Peers = peerStats.Peers,
            Grabs = ParseInt(row.QuerySelector("td:nth-last-child(3)")?.TextContent),
            PublishDate = ParseBjDate(row.QuerySelector("td.nobr .time")?.GetAttribute("title")),
            DownloadVolumeFactor = row.QuerySelector("strong[title*=\"Free\"]") != null ? 0 : 1,
            UploadVolumeFactor = 1,
            MinimumRatio = 1,
            MinimumSeedTime = 604800
        };

        return release;
    }

    private ReleaseInfo ParseStandaloneTorrent(IElement row, string baseUrl)
    {
        var downloadHref = row.QuerySelector("span.download_torrent a")?.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(downloadHref))
        {
            return null;
        }

        var detailsHref = row.QuerySelector("a[href*=\"torrentid=\"]")?.GetAttribute("href");
        var categoryHref = row.QuerySelector("td.cats_col a")?.GetAttribute("href") ?? string.Empty;

        var rawTitle = row.QuerySelector("div.group_info a[href^=\"series.php\"]")?.TextContent?.Trim() ?? string.Empty;
        var groupInfoText = Regex.Replace(row.QuerySelector("div.group_info")?.TextContent?.Trim() ?? string.Empty, @"\s+", " ");
        var infoText = row.QuerySelector("div.torrent_info")?.TextContent?.Trim() ?? string.Empty;

        var title = ExtractEnglishOrFallbackTitle(rawTitle);
        var year = ExtractYear(groupInfoText);
        var seasonLink = row.QuerySelector("div.group_info a.tooltip[href^=\"torrents.php\"]");
        var seasonEpisode = seasonLink?.TextContent?.Trim() ?? string.Empty;
        var categoryId = ExtractCategoryId(categoryHref);
        var peerStats = ParsePeerStats(row);

        return new TorrentInfo
        {
            Guid = ToAbsolute(baseUrl, downloadHref),
            DownloadUrl = ToAbsolute(baseUrl, downloadHref),
            InfoUrl = ToAbsolute(baseUrl, detailsHref),
            Title = BuildReleaseTitle(title, year, seasonEpisode, infoText),
            Categories = _categories.MapTrackerCatToNewznab(categoryId.ToString()),
            Size = ParseSize(row.QuerySelector("td:nth-last-child(4)")?.TextContent),
            Seeders = peerStats.Seeders,
            Peers = peerStats.Peers,
            Grabs = ParseInt(row.QuerySelector("td:nth-last-child(3)")?.TextContent),
            PublishDate = ParseBjDate(row.QuerySelector("td.nobr .time")?.GetAttribute("title")),
            DownloadVolumeFactor = row.QuerySelector("strong[title*=\"Free\"]") != null ? 0 : 1,
            UploadVolumeFactor = 1,
            MinimumRatio = 1,
            MinimumSeedTime = 604800
        };
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

    private static string RemoveGroupDetailsText(string rawTitle, string groupDetailsText)
    {
        var title = rawTitle ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(groupDetailsText))
        {
            title = Regex.Replace(title, $@"\s*-\s*{Regex.Escape(groupDetailsText)}(?=\s*\[|$)", string.Empty);
        }

        title = Regex.Replace(title, @"\s*-\s*(?=\[(?:19|20)\d{2}\])", " ");

        return title.Trim();
    }

    private static string ExtractSeasonEpisode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var match = Regex.Match(value, @"\b[ST](\d{1,2})(?:\s*E(\d{1,3}))?\b", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return string.Empty;
        }

        var season = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        if (!match.Groups[2].Success)
        {
            return $"S{season:00}";
        }

        var episode = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return $"S{season:00}E{episode:00}";
    }

    private static string BuildReleaseTitle(string title, int? year, string seasonEpisode, string infoText)
    {
        var cleanInfo = Regex.Replace(infoText ?? string.Empty, @"[\[\]]", "");
        cleanInfo = cleanInfo.Replace("Full HD", "1080p");
        cleanInfo = cleanInfo.Replace("4K", "2160p");
        cleanInfo = cleanInfo.Replace("SD", "480p");
        cleanInfo = Regex.Replace(cleanInfo, @"(^|/\s*)HD(?=\s*/|$)", "${1}720p", RegexOptions.IgnoreCase);
        cleanInfo = cleanInfo.Replace(" / Free", "");
        cleanInfo = Regex.Replace(cleanInfo, @"\s+", " ").Trim();

        if (!string.IsNullOrWhiteSpace(seasonEpisode) && Regex.IsMatch(cleanInfo, $@"\b{Regex.Escape(seasonEpisode)}\b", RegexOptions.IgnoreCase))
        {
            seasonEpisode = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(seasonEpisode) && Regex.IsMatch(title ?? string.Empty, $@"\b{Regex.Escape(seasonEpisode)}\b", RegexOptions.IgnoreCase))
        {
            seasonEpisode = string.Empty;
        }

        return string.Join(" ", new[]
        {
            title,
            year?.ToString(CultureInfo.InvariantCulture),
            seasonEpisode,
            cleanInfo
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static (int Seeders, int Peers) ParsePeerStats(IElement row)
    {
        var seeders = ParseInt(row.QuerySelector("td:nth-last-child(2)")?.TextContent);
        var leechers = ParseInt(row.QuerySelector("td:nth-last-child(1)")?.TextContent);

        return (seeders, seeders + leechers);
    }

    private static DateTime PublishDateFallback() => DateTime.UtcNow;

    private static DateTime ParseBjDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PublishDateFallback();
        }

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

        return (new HttpUri(baseUrl) + new HttpUri(href)).FullUri;
    }

    private sealed class GroupContext
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public string SeasonEpisode { get; set; }
        public int CategoryId { get; set; }
        public string GroupDetailsUrl { get; set; }
        public string SeriesUrl { get; set; }
    }
}

public class BjShareSettingsValidator : CookieBaseSettingsValidator<BjShareSettings>;

public class BjShareSettings : CookieTorrentBaseSettings
{
    private static readonly BjShareSettingsValidator Validator = new();

    [FieldDefinition(3, Label = "IndexerSettingsFreeleechOnly", Type = FieldType.Checkbox)]
    public bool FreeleechOnly { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
