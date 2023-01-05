using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleParser : IParseIndexerResponse
    {
        protected readonly GazelleSettings _settings;
        protected readonly IndexerCapabilities _capabilities;

        public GazelleParser(GazelleSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
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

            var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
                jsonResponse.Resource.Response == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Resource.Response.Results)
            {
                var posterUrl = GetPosterUrl(result.Cover);

                if (result.Torrents != null)
                {
                    foreach (var torrent in result.Torrents)
                    {
                        var id = torrent.TorrentId;
                        var artist = WebUtility.HtmlDecode(result.Artist);
                        var album = WebUtility.HtmlDecode(result.GroupName);

                        var title = $"{result.Artist} - {result.GroupName} ({result.GroupYear}) [{torrent.Format} {torrent.Encoding}] [{torrent.Media}]";
                        if (torrent.HasCue)
                        {
                            title += " [Cue]";
                        }

                        var infoUrl = GetInfoUrl(result.GroupId, id);

                        var release = new GazelleInfo()
                        {
                            Guid = infoUrl,
                            Title = WebUtility.HtmlDecode(title),
                            Container = torrent.Encoding,
                            Files = torrent.FileCount,
                            Grabs = torrent.Snatches,
                            Codec = torrent.Format,
                            Size = long.Parse(torrent.Size),
                            DownloadUrl = GetDownloadUrl(id, long.Parse(torrent.Size)),
                            InfoUrl = infoUrl,
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.Time.ToUniversalTime(),
                            Scene = torrent.Scene,
                            PosterUrl = posterUrl,
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

                        torrentInfos.Add(release);
                    }
                }
                else
                {
                    var id = result.TorrentId;
                    var groupName = WebUtility.HtmlDecode(result.GroupName);
                    var infoUrl = GetInfoUrl(result.GroupId, id);

                    var release = new GazelleInfo()
                    {
                        Guid = infoUrl,
                        Title = groupName,
                        Size = long.Parse(result.Size),
                        DownloadUrl = GetDownloadUrl(id, long.Parse(result.Size)),
                        InfoUrl = infoUrl,
                        Seeders = int.Parse(result.Seeders),
                        Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                        Files = result.FileCount,
                        Grabs = result.Snatches,
                        PublishDate = long.TryParse(result.GroupTime, out var num) ? DateTimeOffset.FromUnixTimeSeconds(num).UtcDateTime : DateTimeUtil.FromFuzzyTime((string)result.GroupTime),
                        PosterUrl = posterUrl,
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

                    torrentInfos.Add(release);
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        protected virtual string GetDownloadUrl(int torrentId, long torrentSize)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("usetoken", _settings.UseFreeleechToken && torrentSize >= _settings.FreeleechSize ? "1" : "0")
                .AddQueryParam("id", torrentId);

            return url.FullUri;
        }

        protected virtual string GetPosterUrl(string cover)
        {
            if (!string.IsNullOrEmpty(cover))
            {
                return cover.StartsWith("http") ?
                    new HttpUri(cover).FullUri :
                    new HttpUri(_settings.BaseUrl).CombinePath(cover).FullUri;
            }

            return null;
        }

        protected virtual string GetInfoUrl(string groupId, int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("id", groupId)
                .AddQueryParam("torrentid", torrentId);

            return url.FullUri;
        }
    }
}
