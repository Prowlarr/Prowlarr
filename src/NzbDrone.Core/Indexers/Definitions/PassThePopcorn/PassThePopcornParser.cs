using System;
using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcornParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilities _capabilities;
        private readonly PassThePopcornSettings _settings;
        private readonly Logger _logger;
        public PassThePopcornParser(PassThePopcornSettings settings, IndexerCapabilities capabilities, Logger logger)
        {
            _settings = settings;
            _capabilities = capabilities;
            _logger = logger;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();
            var indexerHttpResponse = indexerResponse.HttpResponse;

            if (indexerHttpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                if (indexerHttpResponse.HasHttpRedirect && indexerHttpResponse.RedirectUrl
                        .ContainsIgnoreCase("login.php"))
                {
                    CookiesUpdater(null, null);
                    throw new IndexerAuthException("We are being redirected to the PTP login page. Most likely your session expired or was killed. Recheck your cookie or credentials and try testing the indexer.");
                }

                if (indexerHttpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new RequestLimitReachedException(indexerResponse, "PTP Query Limit Reached. Please try again later.");
                }

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (indexerHttpResponse.Headers.ContentType != HttpAccept.Json.Value)
            {
                if (indexerHttpResponse.Request.Url.Path.ContainsIgnoreCase("login.php"))
                {
                    CookiesUpdater(null, null);
                    throw new IndexerAuthException("We are currently on the login page. Most likely your session expired or was killed. Try testing the indexer in the settings.");
                }

                // Remove cookie cache
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = STJson.Deserialize<PassThePopcornResponse>(indexerResponse.Content);
            if (jsonResponse.TotalResults == "0" ||
                jsonResponse.TotalResults.IsNullOrWhiteSpace() ||
                jsonResponse.Movies == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Movies)
            {
                foreach (var torrent in result.Torrents)
                {
                    var id = torrent.Id;
                    var title = torrent.ReleaseName;

                    var flags = new HashSet<IndexerFlag>();

                    if (torrent.GoldenPopcorn)
                    {
                        flags.Add(PassThePopcornFlag.Golden);
                    }

                    if (torrent.Checked)
                    {
                        flags.Add(PassThePopcornFlag.Approved); //title = $"{title} ✔";
                    }

                    if (torrent.Scene)
                    {
                        flags.Add(IndexerFlag.Scene);
                    }

                    var free = !(torrent.FreeleechType is null);

                    // Only add approved torrents
                    try
                    {
                        torrentInfos.Add(new TorrentInfo
                        {
                            Guid = string.Format("PassThePopcorn-{0}", id),
                            Title = title,
                            Size = long.Parse(torrent.Size),
                            DownloadUrl = GetDownloadUrl(id, jsonResponse.AuthKey, jsonResponse.PassKey),
                            InfoUrl = GetInfoUrl(result.GroupId, id),
                            Grabs = int.Parse(torrent.Snatched),
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.UploadTime.ToUniversalTime(),
                            ImdbId = result.ImdbId.IsNotNullOrWhiteSpace() ? int.Parse(result.ImdbId) : 0,
                            IndexerFlags = flags,
                            MinimumRatio = 1,
                            MinimumSeedTime = 345600,
                            DownloadVolumeFactor = free ? 0 : 1,
                            UploadVolumeFactor = 1,
                            Categories = new List<IndexerCategory> { NewznabStandardCategory.Movies }
                        });
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Encountered exception parsing PTP torrent: {" +
                                         $"Size: {torrent.Size}" +
                                         $"UploadTime: {torrent.UploadTime}" +
                                         $"Seeders: {torrent.Seeders}" +
                                         $"Leechers: {torrent.Leechers}" +
                                         $"ReleaseName: {torrent.ReleaseName}" +
                                         $"ID: {torrent.Id}" +
                                         "}. Please immediately report this info on https://github.com/Prowlarr/Prowlarr/issues/1584.");
                        throw;
                    }
                }
            }

            return torrentInfos;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private string GetDownloadUrl(int torrentId, string authKey, string passKey)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("authkey", authKey)
                .AddQueryParam("torrent_pass", passKey);

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
    }
}
