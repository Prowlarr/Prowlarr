using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions;

public class GreatPosterWall : Gazelle.Gazelle
{
    public override string Name => "GreatPosterWall";
    public override string[] IndexerUrls => new string[] { "https://greatposterwall.com/" };
    public override string Description => "GreatPosterWall (GPW) is a CHINESE Private site for MOVIES";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public GreatPosterWall(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new GreatPosterWallRequestGenerator()
        {
            Settings = Settings,
            HttpClient = _httpClient,
            Logger = _logger,
            Capabilities = Capabilities
        };
    }

    public override IParseIndexerResponse GetParser()
    {
        return new GreatPosterWallParser(Settings, Capabilities);
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

    public new IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters += string.Format("&searchstr={0}", searchCriteria.FullImdbId);
        }

        var pageableRequests = new IndexerPageableRequestChain();
        pageableRequests.Add(GetRequest(parameters));
        return pageableRequests;
    }
}

public class GreatPosterWallParser : GazelleParser
{
    public GreatPosterWallParser(GazelleSettings settings, IndexerCapabilities capabilities)
        : base(settings, capabilities)
    {
    }

    public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var torrentInfos = new List<ReleaseInfo>();

        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            // Remove cookie cache
            CookiesUpdater(null, null);

            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
        }

        if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
        {
            // Remove cookie cache
            CookiesUpdater(null, null);

            throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
        }

        var jsonResponse = new HttpResponse<GreatPosterWallResponse>(indexerResponse.HttpResponse);
        if (jsonResponse.Resource.Status != "success" ||
            jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
            jsonResponse.Resource.Response == null)
        {
            return torrentInfos;
        }

        foreach (var result in jsonResponse.Resource.Response.Results)
        {
            foreach (var torrent in result.Torrents)
            {
                var infoUrl = GetInfoUrl(result.GroupId.ToString(), torrent.TorrentId);

                var time = DateTime.SpecifyKind(torrent.Time, DateTimeKind.Unspecified);

                var release = new GazelleInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800,
                    Title = torrent.FileName,
                    InfoUrl = infoUrl,
                    Guid = infoUrl,
                    PosterUrl = GetPosterUrl(result.Cover),
                    DownloadUrl = GetDownloadUrl(torrent.TorrentId, torrent.CanUseToken),
                    PublishDate = new DateTimeOffset(time, TimeSpan.FromHours(8)).LocalDateTime, // Time is Chinese Time, add 8 hours difference from UTC and then convert back to local time
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Movies },
                    Size = torrent.Size,
                    Seeders = torrent.Seeders,
                    Peers = torrent.Seeders + torrent.Leechers,
                    Grabs = torrent.Snatches,
                    Files = torrent.FileCount,
                    Scene = torrent.Scene,
                    DownloadVolumeFactor = torrent.IsFreeleech || torrent.IsNeutralLeech || torrent.IsPersonalFreeleech ? 0 : 1,
                    UploadVolumeFactor = torrent.IsNeutralLeech ? 0 : 1
                };

                var imdbId = ParseUtil.GetImdbID(result.ImdbId);

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

                torrentInfos.Add(release);
            }
        }

        return torrentInfos;
    }

    protected string GetDownloadUrl(int torrentId, bool canUseToken)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/torrents.php")
            .AddQueryParam("action", "download")
            .AddQueryParam("usetoken", _settings.UseFreeleechToken && canUseToken ? "1" : "0")
            .AddQueryParam("id", torrentId);

        return url.FullUri;
    }
}

public class GreatPosterWallResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("response")]
    public Response Response { get; set; }
}

public class Response
{
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pages")]
        public int Pages { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }
}

public class Result
{
        [JsonProperty("groupId")]
        public int GroupId { get; set; }

        [JsonProperty("groupName")]
        public string GroupName { get; set; }

        [JsonProperty("groupSubName")]
        public string GroupSubName { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("bookmarked")]
        public bool Bookmarked { get; set; }

        [JsonProperty("groupYear")]
        public int GroupYear { get; set; }

        [JsonProperty("releaseType")]
        public string ReleaseType { get; set; }

        [JsonProperty("groupTime")]
        public string GroupTime { get; set; }

        [JsonProperty("maxSize")]
        public object MaxSize { get; set; }

        [JsonProperty("totalSnatched")]
        public int TotalSnatched { get; set; }

        [JsonProperty("totalSeeders")]
        public int TotalSeeders { get; set; }

        [JsonProperty("totalLeechers")]
        public int TotalLeechers { get; set; }

        [JsonProperty("imdbId")]
        public string ImdbId { get; set; }

        [JsonProperty("imdbRating")]
        public string ImdbRating { get; set; }

        [JsonProperty("imdbVote")]
        public string ImdbVote { get; set; }

        [JsonProperty("doubanId")]
        public string DoubanId { get; set; }

        [JsonProperty("doubanRating")]
        public string DoubanRating { get; set; }

        [JsonProperty("doubanVote")]
        public string DoubanVote { get; set; }

        [JsonProperty("rtRating")]
        public string RtRating { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("torrents")]
        public List<GreatPosterWallTorrent> Torrents { get; set; }
}

public class GreatPosterWallTorrent
{
        [JsonProperty("torrentId")]
        public int TorrentId { get; set; }

        [JsonProperty("editionId")]
        public int EditionId { get; set; }

        [JsonProperty("remasterYear")]
        public int RemasterYear { get; set; }

        [JsonProperty("remasterTitle")]
        public string RemasterTitle { get; set; }

        [JsonProperty("remasterCustomTitle")]
        public string RemasterCustomTitle { get; set; }

        [JsonProperty("scene")]
        public bool Scene { get; set; }

        [JsonProperty("jinzhuan")]
        public bool Jinzhuan { get; set; }

        [JsonProperty("fileCount")]
        public int FileCount { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("snatches")]
        public int Snatches { get; set; }

        [JsonProperty("seeders")]
        public int Seeders { get; set; }

        [JsonProperty("leechers")]
        public int Leechers { get; set; }

        [JsonProperty("isFreeleech")]
        public bool IsFreeleech { get; set; }

        [JsonProperty("isNeutralLeech")]
        public bool IsNeutralLeech { get; set; }

        [JsonProperty("freeType")]
        public string FreeType { get; set; }

        [JsonProperty("isPersonalFreeleech")]
        public bool IsPersonalFreeleech { get; set; }

        [JsonProperty("canUseToken")]
        public bool CanUseToken { get; set; }

        [JsonProperty("hasSnatched")]
        public bool HasSnatched { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("codec")]
        public string Codec { get; set; }

        [JsonProperty("container")]
        public string Container { get; set; }

        [JsonProperty("processing")]
        public string Processing { get; set; }

        [JsonProperty("chineseDubbed")]
        public string ChineseDubbed { get; set; }

        [JsonProperty("specialSub")]
        public string SpecialSub { get; set; }

        [JsonProperty("subtitles")]
        public string Subtitles { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("releaseGroup")]
        public string ReleaseGroup { get; set; }
}
