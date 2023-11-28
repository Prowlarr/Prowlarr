using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetParser : IParseIndexerResponse
    {
        private static readonly Regex RegexProtocol = new ("^https?:", RegexOptions.Compiled);

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private readonly IndexerCapabilitiesCategories _categories;

        public BroadcastheNetParser(IndexerCapabilitiesCategories categories)
        {
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();
            var indexerHttpResponse = indexerResponse.HttpResponse;

            switch (indexerHttpResponse.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new IndexerAuthException("API Key invalid or not authorized");
                case HttpStatusCode.NotFound:
                    throw new IndexerException(indexerResponse, "Indexer API call returned NotFound, the Indexer API may have changed.");
                case HttpStatusCode.ServiceUnavailable:
                    throw new RequestLimitReachedException(indexerResponse, "Cannot do more than 150 API requests per hour.");
                default:
                    if (indexerHttpResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new IndexerException(indexerResponse, "Indexer API call returned an unexpected StatusCode [{0}]", indexerHttpResponse.StatusCode);
                    }

                    break;
            }

            if (indexerHttpResponse.Headers.ContentType != null && indexerHttpResponse.Headers.ContentType.Contains("text/html"))
            {
                throw new IndexerException(indexerResponse, "Indexer responded with html content. Site is likely blocked or unavailable.");
            }

            if (indexerResponse.Content.ContainsIgnoreCase("Call Limit Exceeded"))
            {
                throw new RequestLimitReachedException(indexerResponse, "Cannot do more than 150 API requests per hour.");
            }

            if (indexerResponse.Content == "Query execution was interrupted")
            {
                throw new IndexerException(indexerResponse, "Indexer API returned an internal server error");
            }

            var jsonResponse = new HttpResponse<JsonRpcResponse<BroadcastheNetTorrents>>(indexerHttpResponse).Resource;

            if (jsonResponse.Error != null || jsonResponse.Result == null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an error [{0}]", jsonResponse.Error);
            }

            if (jsonResponse.Result.Results == 0 || jsonResponse.Result?.Torrents?.Values == null)
            {
                return releaseInfos;
            }

            var protocol = indexerResponse.HttpRequest.Url.Scheme + ":";

            foreach (var torrent in jsonResponse.Result.Torrents.Values)
            {
                var flags = new HashSet<IndexerFlag>();

                if (torrent.Origin.ToUpperInvariant() == "INTERNAL")
                {
                    flags.Add(IndexerFlag.Internal);
                }

                var releaseInfo = new TorrentInfo
                {
                    Guid = $"BTN-{torrent.TorrentID}",
                    InfoUrl = $"{protocol}//broadcasthe.net/torrents.php?id={torrent.GroupID}&torrentid={torrent.TorrentID}",
                    DownloadUrl = RegexProtocol.Replace(torrent.DownloadURL, protocol),
                    Title = CleanReleaseName(torrent.ReleaseName),
                    Categories = _categories.MapTrackerCatToNewznab(torrent.Resolution),
                    InfoHash = torrent.InfoHash,
                    Size = torrent.Size,
                    Grabs = torrent.Snatched,
                    Seeders = torrent.Seeders,
                    Peers = torrent.Leechers + torrent.Seeders,
                    PublishDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime().AddSeconds(torrent.Time),
                    Origin = torrent.Origin,
                    Source = torrent.Source,
                    Container = torrent.Container,
                    Codec = torrent.Codec,
                    Resolution = torrent.Resolution,
                    Scene = torrent.Origin.ToUpperInvariant() == "SCENE",
                    IndexerFlags = flags,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = torrent.Category.ToUpperInvariant() == "SEASON" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                };

                if (torrent.TvdbID is > 0)
                {
                    releaseInfo.TvdbId = torrent.TvdbID.Value;
                }

                if (torrent.TvrageID is > 0)
                {
                    releaseInfo.TvRageId = torrent.TvrageID.Value;
                }

                if (torrent.ImdbID.IsNotNullOrWhiteSpace() && int.TryParse(torrent.ImdbID, out var imdbId))
                {
                    releaseInfo.ImdbId = imdbId;
                }

                // Default to TV if category could not be mapped
                if (releaseInfo.Categories == null || !releaseInfo.Categories.Any())
                {
                    releaseInfo.Categories = new List<IndexerCategory> { NewznabStandardCategory.TV };
                }

                releaseInfos.Add(releaseInfo);
            }

            return releaseInfos;
        }

        private string CleanReleaseName(string releaseName)
        {
            releaseName = releaseName.Replace("\\", "");

            return releaseName;
        }
    }
}
