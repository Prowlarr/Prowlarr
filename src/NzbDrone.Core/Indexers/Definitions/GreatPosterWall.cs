using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions;

public class GreatPosterWall : GazelleBase<GreatPosterWallSettings>
{
    public override string Name => "GreatPosterWall";
    public override string[] IndexerUrls => new[] { "https://greatposterwall.com/" };
    public override string Description => "GreatPosterWall (GPW) is a CHINESE Private site for MOVIES";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public GreatPosterWall(IIndexerHttpClient httpClient,
                           IEventAggregator eventAggregator,
                           IIndexerStatusService indexerStatusService,
                           IConfigService configService,
                           Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new GreatPosterWallRequestGenerator(Settings, Capabilities, _httpClient, _logger);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new GreatPosterWallParser(Settings, Capabilities, _logger);
    }

    protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
    {
        var cleanReleases = base.CleanupReleases(releases, searchCriteria);

        if (searchCriteria.IsRssSearch)
        {
            cleanReleases = cleanReleases.Take(50).ToList();
        }

        return cleanReleases;
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies 电影");

        return caps;
    }
}

public class GreatPosterWallRequestGenerator : GazelleRequestGenerator
{
    protected override bool ImdbInTags => false;
    private readonly GreatPosterWallSettings _settings;

    public GreatPosterWallRequestGenerator(GreatPosterWallSettings settings,
                                           IndexerCapabilities capabilities,
                                           IIndexerHttpClient httpClient,
                                           Logger logger)
        : base(settings, capabilities, httpClient, logger)
    {
        _settings = settings;
    }

    public override IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters.Set("searchstr", searchCriteria.FullImdbId);
        }

        var pageableRequests = new IndexerPageableRequestChain();
        pageableRequests.Add(GetRequest(parameters));
        return pageableRequests;
    }

    protected override NameValueCollection GetBasicSearchParameters(string term, int[] categories)
    {
        var parameters = base.GetBasicSearchParameters(term, categories);

        if (_settings.FreeleechOnly)
        {
            parameters.Set("freetorrent", "1");
        }

        return parameters;
    }
}

public class GreatPosterWallParser : GazelleParser
{
    private readonly GreatPosterWallSettings _settings;
    private readonly Logger _logger;

    private readonly HashSet<string> _hdResolutions = new () { "1080p", "1080i", "720p" };

    public GreatPosterWallParser(GreatPosterWallSettings settings, IndexerCapabilities capabilities, Logger logger)
        : base(settings, capabilities)
    {
        _settings = settings;
        _logger = logger;
    }

    public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            if (indexerResponse.HttpResponse.HasHttpRedirect)
            {
                _logger.Warn("Redirected to {0} from indexer request", indexerResponse.HttpResponse.RedirectUrl);

                if (indexerResponse.HttpResponse.RedirectUrl.ContainsIgnoreCase("/login.php"))
                {
                    // Remove cookie cache
                    CookiesUpdater(null, null);
                    throw new IndexerException(indexerResponse, "We are being redirected to the login page. Most likely your session expired or was killed. Recheck your cookie or credentials and try testing the indexer.");
                }

                throw new IndexerException(indexerResponse, $"Redirected to {indexerResponse.HttpResponse.RedirectUrl} from indexer request");
            }

            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
        }

        if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
        {
            throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
        }

        var jsonResponse = new HttpResponse<GreatPosterWallResponse>(indexerResponse.HttpResponse);
        if (jsonResponse.Resource.Status != "success" ||
            jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
            jsonResponse.Resource.Response == null)
        {
            return releaseInfos;
        }

        foreach (var result in jsonResponse.Resource.Response.Results)
        {
            foreach (var torrent in result.Torrents)
            {
                var infoUrl = GetInfoUrl(result.GroupId.ToString(), torrent.TorrentId);
                var time = DateTime.SpecifyKind(torrent.Time, DateTimeKind.Unspecified);

                var release = new TorrentInfo
                {
                    Title = WebUtility.HtmlDecode(torrent.FileName).Trim(),
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    PosterUrl = GetPosterUrl(result.Cover),
                    DownloadUrl = GetDownloadUrl(torrent.TorrentId, torrent.CanUseToken),
                    PublishDate = new DateTimeOffset(time, TimeSpan.FromHours(8)).UtcDateTime, // Time is Chinese Time, add 8 hours difference from UTC
                    Categories = ParseCategories(torrent),
                    Size = torrent.Size,
                    Seeders = torrent.Seeders,
                    Peers = torrent.Seeders + torrent.Leechers,
                    Grabs = torrent.Snatches,
                    Files = torrent.FileCount,
                    Scene = torrent.Scene,
                    DownloadVolumeFactor = torrent.IsFreeleech || torrent.IsNeutralLeech || torrent.IsPersonalFreeleech ? 0 : 1,
                    UploadVolumeFactor = torrent.IsNeutralLeech ? 0 : 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                var imdbId = ParseUtil.GetImdbId(result.ImdbId);
                if (imdbId != null)
                {
                    release.ImdbId = (int)imdbId;
                }

                switch (torrent.FreeType)
                {
                    case "11":
                        release.DownloadVolumeFactor = 0.75;
                        break;
                    case "12":
                        release.DownloadVolumeFactor = 0.5;
                        break;
                    case "13":
                        release.DownloadVolumeFactor = 0.25;
                        break;
                    case "1":
                        release.DownloadVolumeFactor = 0;
                        break;
                    case "2":
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 0;
                        break;
                }

                releaseInfos.Add(release);
            }
        }

        return releaseInfos
            .OrderByDescending(o => o.PublishDate)
            .ToArray();
    }

    protected override string GetDownloadUrl(int torrentId, bool canUseToken)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/torrents.php")
            .AddQueryParam("action", "download")
            .AddQueryParam("id", torrentId);

        if (_settings.UseFreeleechToken && canUseToken)
        {
            url = url.AddQueryParam("usetoken", "1");
        }

        return url.FullUri;
    }

    private List<IndexerCategory> ParseCategories(GreatPosterWallTorrent torrent)
    {
        var cats = new List<IndexerCategory>
        {
            NewznabStandardCategory.Movies,
            torrent.Resolution switch
            {
                var res when _hdResolutions.Contains(res) => NewznabStandardCategory.MoviesHD,
                "2160p" => NewznabStandardCategory.MoviesUHD,
                _ => NewznabStandardCategory.MoviesSD
            }
        };

        return cats;
    }
}

public class GreatPosterWallSettings : GazelleUserPassOrCookieSettings
{
    private static readonly GazelleUserPassOrCookieValidator<GreatPosterWallSettings> Validator = new ();

    [FieldDefinition(6, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech torrents only")]
    public bool FreeleechOnly { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}

public class GreatPosterWallResponse
{
    public string Status { get; set; }

    public GreatPosterWallResponseWithResults Response { get; set; }
}

public class GreatPosterWallResponseWithResults
{
    public int CurrentPage { get; set; }
    public int Pages { get; set; }

    [JsonProperty("results")]
    public List<GreatPosterWallResult> Results { get; set; }
}

public class GreatPosterWallResult
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string GroupSubName { get; set; }
    public string Cover { get; set; }
    public List<string> Tags { get; set; }
    public bool Bookmarked { get; set; }
    public int GroupYear { get; set; }
    public string ReleaseType { get; set; }
    public string GroupTime { get; set; }
    public object MaxSize { get; set; }
    public int TotalSnatched { get; set; }
    public int TotalSeeders { get; set; }
    public int TotalLeechers { get; set; }
    public string ImdbId { get; set; }
    public string ImdbRating { get; set; }
    public string ImdbVote { get; set; }
    public string DoubanId { get; set; }
    public string DoubanRating { get; set; }
    public string DoubanVote { get; set; }
    public string RtRating { get; set; }
    public string Region { get; set; }
    [JsonProperty("torrents")]
    public List<GreatPosterWallTorrent> Torrents { get; set; }
}

public class GreatPosterWallTorrent
{
    public int TorrentId { get; set; }
    public int EditionId { get; set; }
    public int RemasterYear { get; set; }
    public string RemasterTitle { get; set; }
    public string RemasterCustomTitle { get; set; }
    public bool Scene { get; set; }
    public bool Jinzhuan { get; set; }
    public int FileCount { get; set; }
    public DateTime Time { get; set; }
    public long Size { get; set; }
    public int Snatches { get; set; }
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public bool IsFreeleech { get; set; }
    public bool IsNeutralLeech { get; set; }
    public string FreeType { get; set; }
    public bool IsPersonalFreeleech { get; set; }
    public bool CanUseToken { get; set; }
    public bool HasSnatched { get; set; }
    public string Resolution { get; set; }
    public string Source { get; set; }
    public string Codec { get; set; }
    public string Container { get; set; }
    public string Processing { get; set; }
    public string ChineseDubbed { get; set; }
    public string SpecialSub { get; set; }
    public string Subtitles { get; set; }
    public string FileName { get; set; }
    public string ReleaseGroup { get; set; }
}
