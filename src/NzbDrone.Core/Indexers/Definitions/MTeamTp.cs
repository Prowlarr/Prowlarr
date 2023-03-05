using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
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
    public override string[] IndexerUrls => new[]
    {
        "https://kp.m-team.cc/",
        "https://tp.m-team.cc/",
        "https://pt.m-team.cc/"
    };
    public override string Description => "M-Team TP (MTTP) is a CHINESE Private Torrent Tracker for HD MOVIES / TV / 3X";
    public override string Language => "zh-CN";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override int PageSize => 100;
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(5);
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.35";

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

    public override async Task<byte[]> Download(Uri link)
    {
        var request = new HttpRequestBuilder(link.ToString())
            .SetHeader("x-api-key", Settings.ApiKey)
            .Accept(HttpAccept.Json)
            .Post()
            .Build();

        request.Headers.UserAgent = UserAgent;

        var response = await _httpClient.ExecuteProxiedAsync(request, Definition);

        if (!STJson.TryDeserialize<MTeamTpApiDownloadTokenResponse>(response.Content, out var jsonResponse))
        {
            throw new ReleaseDownloadException("Invalid response received from M-Team, not a valid JSON");
        }

        if (jsonResponse.Data.IsNullOrWhiteSpace())
        {
            throw new ReleaseDownloadException($"Unable to find download link for: {link}");
        }

        return await base.Download(new Uri(jsonResponse.Data));
    }

    protected override Task<HttpRequest> GetDownloadRequest(Uri link)
    {
        var request = new HttpRequest(link.AbsoluteUri)
        {
            AllowAutoRedirect = true,
            Headers =
            {
                UserAgent = UserAgent
            }
        };

        return Task.FromResult(request);
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
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
        caps.Categories.AddCategoryMapping(439, NewznabStandardCategory.MoviesHD, "Movie(電影)/Remux");
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
        caps.Categories.AddCategoryMapping(424, NewznabStandardCategory.XXXSD, "AV(有碼)/SD Censored");
        caps.Categories.AddCategoryMapping(430, NewznabStandardCategory.XXXSD, "AV(無碼)/SD Uncensored");
        caps.Categories.AddCategoryMapping(426, NewznabStandardCategory.XXXDVD, "AV(無碼)/DVDiSo Uncensored");
        caps.Categories.AddCategoryMapping(437, NewznabStandardCategory.XXXDVD, "AV(有碼)/DVDiSo Censored");
        caps.Categories.AddCategoryMapping(431, NewznabStandardCategory.XXX, "AV(有碼)/Blu-Ray Censored");
        caps.Categories.AddCategoryMapping(432, NewznabStandardCategory.XXX, "AV(無碼)/Blu-Ray Uncensored");
        caps.Categories.AddCategoryMapping(436, NewznabStandardCategory.XXX, "AV(網站)/0Day");
        caps.Categories.AddCategoryMapping(425, NewznabStandardCategory.XXX, "IV(寫真影集)/Video Collection");
        caps.Categories.AddCategoryMapping(433, NewznabStandardCategory.XXXImageSet, "IV(寫真圖集)/Picture Collection");
        caps.Categories.AddCategoryMapping(411, NewznabStandardCategory.XXX, "H-Game(遊戲)");
        caps.Categories.AddCategoryMapping(412, NewznabStandardCategory.XXX, "H-Anime(動畫)");
        caps.Categories.AddCategoryMapping(413, NewznabStandardCategory.XXX, "H-Comic(漫畫)");
        caps.Categories.AddCategoryMapping(440, NewznabStandardCategory.XXX, "AV(Gay)/HD");

        return caps;
    }
}

public class MTeamTpRequestGenerator : IIndexerRequestGenerator
{
    private readonly MTeamTpSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    private readonly int[] _trackerAdultCategories = { 410, 429, 424, 430, 426, 437, 431, 432, 436, 425, 433, 411, 412, 413, 440 };

    public MTeamTpRequestGenerator(MTeamTpSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Normal, searchCriteria, searchCriteria.SanitizedSearchTerm, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Normal, searchCriteria, searchCriteria.SanitizedTvSearchString, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Normal, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Normal, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Normal, searchCriteria, searchCriteria.SanitizedSearchTerm));
        pageableRequests.Add(GetPagedRequests(MTeamTpRequestType.Adult, searchCriteria, searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(MTeamTpRequestType mTeamTpRequestType, SearchCriteriaBase searchCriteria, string searchTerm, string imdbId = null)
    {
        var categoryMapping = _capabilities.Categories
            .MapTorznabCapsToTrackers(searchCriteria.Categories)
            .Select(int.Parse)
            .Distinct()
            .ToList();

        var adultCategories = categoryMapping.Where(c => _trackerAdultCategories.Contains(c)).ToList();
        var normalCategories = categoryMapping.Except(adultCategories).ToList();

        switch (mTeamTpRequestType)
        {
            case MTeamTpRequestType.Adult when adultCategories.Any():
                yield return BuildSearchRequest(mTeamTpRequestType, adultCategories, searchTerm, imdbId);
                break;
            case MTeamTpRequestType.Normal when !categoryMapping.Any() || normalCategories.Any():
                yield return BuildSearchRequest(mTeamTpRequestType, normalCategories, searchTerm, imdbId);
                break;
        }
    }

    private IndexerRequest BuildSearchRequest(MTeamTpRequestType requestType, IEnumerable<int> categoryMapping, string searchTerm, string imdbId)
    {
        var request = new HttpRequestBuilder(_settings.BaseUrl)
            .Resource("/api/torrent/search")
            .SetHeader("x-api-key", _settings.ApiKey)
            .Accept(HttpAccept.Json)
            .Post()
            .Build();

        var query = new MTeamTpApiSearchQuery
        {
            Mode = requestType,
            Categories = categoryMapping?.Select(x => x.ToString()).ToArray() ?? Array.Empty<string>(),
            PageNumber = 1,
            PageSize = 100
        };

        if (imdbId.IsNotNullOrWhiteSpace())
        {
            query.Imdb = imdbId.Trim();
        }

        if (searchTerm.IsNotNullOrWhiteSpace())
        {
            query.Keyword = searchTerm.Trim();
        }

        if (_settings.FreeleechOnly)
        {
            query.Discount = "FREE";
        }

        request.Headers.ContentType = "application/json";
        request.SetContent(query.ToJson());
        request.ContentSummary = query.ToJson(Formatting.None);
        request.Headers.UserAgent = MTeamTp.UserAgent;

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
        var httpResponse = indexerResponse.HttpResponse;

        if (httpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

        if (!httpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
        {
            throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
        }

        if (!STJson.TryDeserialize<MTeamTpApiResponse>(indexerResponse.Content, out var jsonResponse))
        {
            throw new IndexerException(indexerResponse, "Invalid response received from M-Team, not a valid JSON");
        }

        var releaseInfos = new List<ReleaseInfo>();

        if (jsonResponse?.Data?.Torrents == null)
        {
            return releaseInfos;
        }

        foreach (var torrent in jsonResponse.Data.Torrents)
        {
            var torrentId = int.Parse(torrent.Id);
            var infoUrl = $"{_settings.BaseUrl.TrimEnd('/')}/detail/{torrentId}";

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                Title = CleanTitle(torrent.Name),
                InfoUrl = infoUrl,
                DownloadUrl = GetDownloadUrl(torrentId),
                Categories = _categories.MapTrackerCatToNewznab(torrent.Category),
                Description = torrent.Description,
                Files = int.Parse(torrent.NumFiles),
                Size = long.Parse(torrent.Size),
                Grabs = int.Parse(torrent.Status.TimesCompleted),
                Seeders = int.Parse(torrent.Status.Seeders),
                Peers = int.Parse(torrent.Status.Seeders) + int.Parse(torrent.Status.Leechers),
                DownloadVolumeFactor = torrent.Status.Discount.ToUpperInvariant() switch
                {
                    "FREE" => 0,
                    "_2X_FREE" => 0,
                    "PERCENT_50" => 0.5,
                    "_2X_PERCENT_50" => 0.5,
                    "PERCENT_70" => 0.3,
                    _ => 1
                },
                UploadVolumeFactor = torrent.Status.Discount.ToUpperInvariant() switch
                {
                    "_2X_FREE" => 2,
                    "_2X_PERCENT_50" => 2,
                    _ => 1
                },
                MinimumRatio = 1,
                MinimumSeedTime = 172800 // 2 days
            };

            if (torrent.Imdb.IsNotNullOrWhiteSpace())
            {
                release.ImdbId = ParseUtil.GetImdbId(torrent.Imdb.Split('/').LastOrDefault()).GetValueOrDefault();
            }

            if (torrent.Status?.CreatedDate != null &&
                DateTime.TryParseExact($"{torrent.Status.CreatedDate} +08:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publishDate))
            {
                release.PublishDate = publishDate;
            }

            releaseInfos.Add(release);
        }

        return releaseInfos
            .OrderByDescending(o => o.PublishDate)
            .ToArray();
    }

    private string GetDownloadUrl(int torrentId)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/api/torrent/genDlToken")
            .AddQueryParam("id", torrentId);

        return url.FullUri;
    }

    private static string CleanTitle(string title)
    {
        title = Regex.Replace(title, @"\s+", " ", RegexOptions.Compiled);

        return title.Trim();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class MTeamTpValidator : NoAuthSettingsValidator<MTeamTpSettings>
{
    public MTeamTpValidator()
    {
        RuleFor(c => c.ApiKey).NotEmpty();
    }
}

public class MTeamTpSettings : NoAuthTorrentBaseSettings
{
    private static readonly MTeamTpValidator Validator = new ();

    [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerMTeamTpSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
    public string ApiKey { get; set; }

    [FieldDefinition(3, Label = "IndexerSettingsFreeleechOnly", Type = FieldType.Checkbox, HelpText = "IndexerMTeamTpSettingsFreeleechOnlyHelpText")]
    public bool FreeleechOnly { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}

internal enum MTeamTpRequestType
{
    Normal,
    Adult
}

internal class MTeamTpApiSearchQuery
{
    [JsonProperty(Required = Required.Always)]
    public MTeamTpRequestType Mode { get; set; }

    [JsonProperty(Required = Required.Always)]
    public IEnumerable<string> Categories { get; set; }

    public string Discount { get; set; }
    public string Imdb { get; set; }
    public string Keyword { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

internal class MTeamTpApiResponse
{
    public MTeamTpApiData Data { get; set; }
}

internal class MTeamTpApiData
{
    [JsonPropertyName("data")]
    public IReadOnlyCollection<MTeamTpApiTorrent> Torrents { get; set; }
}

internal class MTeamTpApiTorrent
{
    public string Id { get; set; }
    public string Name { get; set; }

    [JsonPropertyName("smallDescr")]
    public string Description { get; set; }

    public string Category { get; set; }

    [JsonPropertyName("numfiles")]
    public string NumFiles { get; set; }

    public string Imdb { get; set; }
    public string Size { get; set; }
    public MTeamTpApiReleaseStatus Status { get; set; }
}

internal class MTeamTpApiReleaseStatus
{
    public string CreatedDate { get; set; }
    public string Discount { get; set; }
    public string TimesCompleted { get; set; }
    public string Seeders { get; set; }
    public string Leechers { get; set; }
}

internal class MTeamTpApiDownloadTokenResponse
{
    public string Data { get; set; }
}
