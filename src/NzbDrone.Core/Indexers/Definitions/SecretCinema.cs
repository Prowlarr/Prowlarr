using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions;

public class SecretCinema : GazelleBase<GazelleSettings>
{
    public override string Name => "Secret Cinema";
    public override string[] IndexerUrls => new[] { "https://secret-cinema.pw/" };
    public override string Description => "A tracker for rare movies.";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public SecretCinema(IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IParseIndexerResponse GetParser()
    {
        return new SecretCinemaParser(Settings, Capabilities);
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            },
            MusicSearchParams = new List<MusicSearchParam>
            {
                MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album, MusicSearchParam.Label, MusicSearchParam.Year
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Audio, "Music");

        return caps;
    }
}

public class SecretCinemaParser : IParseIndexerResponse
{
    private readonly GazelleSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public SecretCinemaParser(GazelleSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

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

        var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
        if (jsonResponse.Resource.Status != "success" ||
            jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
            jsonResponse.Resource.Response == null)
        {
            return releaseInfos.ToArray();
        }

        foreach (var result in jsonResponse.Resource.Response.Results)
        {
            if (result.Torrents != null)
            {
                foreach (var torrent in result.Torrents)
                {
                    var id = torrent.TorrentId;

                    // in SC movies, artist=director and GroupName=title
                    var artist = WebUtility.HtmlDecode(result.Artist);
                    var title = WebUtility.HtmlDecode(result.GroupName);
                    var time = DateTime.SpecifyKind(torrent.Time, DateTimeKind.Unspecified);

                    var release = new GazelleInfo
                    {
                        Guid = $"SecretCinema-{id}",
                        Title = title,
                        Container = torrent.Encoding,
                        Files = torrent.FileCount,
                        Grabs = torrent.Snatches,
                        Codec = torrent.Format,
                        Size = long.Parse(torrent.Size),
                        DownloadUrl = GetDownloadUrl(id),
                        InfoUrl = GetInfoUrl(result.GroupId, id),
                        Seeders = int.Parse(torrent.Seeders),
                        Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                        PublishDate = new DateTimeOffset(time, TimeSpan.FromHours(2)).UtcDateTime,
                        Scene = torrent.Scene,
                        DownloadVolumeFactor = torrent.IsFreeLeech || torrent.IsNeutralLeech || torrent.IsPersonalFreeLeech ? 0 : 1,
                        UploadVolumeFactor = torrent.IsNeutralLeech ? 0 : 1
                    };

                    var category = torrent.Category;
                    if (category == null || category.Contains("Select Category"))
                    {
                        release.Categories = _capabilities.Categories.MapTrackerCatToNewznab("1");
                    }
                    else
                    {
                        release.Categories = _capabilities.Categories.MapTrackerCatDescToNewznab(category);
                    }

                    if (IsAnyMovieCategory(release.Categories))
                    {
                        // Remove director from title
                        // SC API returns no more useful information than this
                        release.Title = $"{title} ({result.GroupYear}) {torrent.Media}";

                        // Replace media formats with standards
                        release.Title = Regex.Replace(release.Title, @"\bBDMV\b", "COMPLETE BLURAY", RegexOptions.IgnoreCase);
                        release.Title = Regex.Replace(release.Title, @"\bSD\b", "DVDRip", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // SC API currently doesn't return anything but title.
                        release.Title = $"{artist} - {title} ({result.GroupYear}) [{torrent.Format} {torrent.Encoding}] [{torrent.Media}]";
                        release.Artist = artist;
                        release.Album = title;
                    }

                    if (torrent.HasCue)
                    {
                        release.Title += " [Cue]";
                    }

                    releaseInfos.Add(release);
                }
            }
            else
            {
                var id = result.TorrentId;
                var groupName = WebUtility.HtmlDecode(result.GroupName);

                var release = new GazelleInfo
                {
                    Guid = $"SecretCinema-{id}",
                    Title = groupName,
                    Size = long.Parse(result.Size),
                    DownloadUrl = GetDownloadUrl(id),
                    InfoUrl = GetInfoUrl(result.GroupId, id),
                    Seeders = int.Parse(result.Seeders),
                    Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                    Files = result.FileCount,
                    Grabs = result.Snatches,
                    PublishDate = DateTimeOffset.FromUnixTimeSeconds(ParseUtil.CoerceLong(result.GroupTime)).UtcDateTime,
                    DownloadVolumeFactor = result.IsFreeLeech || result.IsNeutralLeech || result.IsPersonalFreeLeech ? 0 : 1,
                    UploadVolumeFactor = result.IsNeutralLeech ? 0 : 1
                };

                var category = result.Category;
                if (category == null || category.Contains("Select Category"))
                {
                    release.Categories = _capabilities.Categories.MapTrackerCatToNewznab("1");
                }
                else
                {
                    release.Categories = _capabilities.Categories.MapTrackerCatDescToNewznab(category);
                }

                releaseInfos.Add(release);
            }
        }

        // order by date
        return
            releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
    }

    private bool IsAnyMovieCategory(ICollection<IndexerCategory> category)
    {
        return category.Contains(NewznabStandardCategory.Movies) || NewznabStandardCategory.Movies.SubCategories.Any(subCat => category.Contains(subCat));
    }

    private string GetDownloadUrl(int torrentId)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/torrents.php")
            .AddQueryParam("action", "download")
            .AddQueryParam("id", torrentId);

        if (_settings.UseFreeleechToken)
        {
            url = url.AddQueryParam("useToken", "1");
        }

        return url.FullUri;
    }

    private string GetInfoUrl(string groupId, int torrentId)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/torrents.php")
            .AddQueryParam("id", groupId)
            .AddQueryParam("torrentid", torrentId);

        return url.FullUri;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
