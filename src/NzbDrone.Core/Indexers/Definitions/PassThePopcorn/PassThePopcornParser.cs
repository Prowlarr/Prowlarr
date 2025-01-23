using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.PassThePopcorn
{
    public class PassThePopcornParser : IParseIndexerResponse
    {
        private readonly PassThePopcornSettings _settings;

        private static Regex SeasonRegex => new (@"\bS\d{2,3}(E\d{2,3})?\b", RegexOptions.Compiled);

        public PassThePopcornParser(PassThePopcornSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var httpResponse = indexerResponse.HttpResponse;

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new RequestLimitReachedException(indexerResponse, "PTP Query Limit Reached. Please try again later.");
                }

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (httpResponse.Headers.ContentType != HttpAccept.Json.Value)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
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
                    // skip non-freeleech results when freeleech only is set
                    var downloadVolumeFactor = torrent.FreeleechType?.ToUpperInvariant() switch
                    {
                        "FREELEECH" => 0,
                        "HALF LEECH" => 0.5,
                        _ => 1
                    };

                    if (_settings.FreeleechOnly && downloadVolumeFactor != 0.0)
                    {
                        continue;
                    }

                    var id = torrent.Id;
                    var title = torrent.ReleaseName;

                    var flags = new HashSet<IndexerFlag>();

                    if (torrent.GoldenPopcorn)
                    {
                        flags.Add(PassThePopcornFlag.Golden);
                    }

                    if (torrent.Checked)
                    {
                        flags.Add(PassThePopcornFlag.Approved);
                    }

                    var categories = new List<IndexerCategory> { NewznabStandardCategory.Movies };

                    if (title != null && SeasonRegex.Match(title).Success)
                    {
                        categories.Add(NewznabStandardCategory.TV);
                    }

                    torrentInfos.Add(new TorrentInfo
                    {
                        Guid = $"PassThePopcorn-{id}",
                        Title = title,
                        Year = int.Parse(result.Year),
                        InfoUrl = GetInfoUrl(result.GroupId, id),
                        DownloadUrl = GetDownloadUrl(id, jsonResponse.AuthKey, jsonResponse.PassKey),
                        Categories = categories,
                        Size = long.Parse(torrent.Size),
                        Grabs = int.Parse(torrent.Snatched),
                        Seeders = int.Parse(torrent.Seeders),
                        Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                        PublishDate = DateTime.Parse(torrent.UploadTime + " +0000", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                        ImdbId = result.ImdbId.IsNotNullOrWhiteSpace() ? int.Parse(result.ImdbId) : 0,
                        Scene = torrent.Scene,
                        IndexerFlags = flags,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 345600,
                        Genres = result.Tags ?? new List<string>(),
                        PosterUrl = GetPosterUrl(result.Cover)
                    });
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

        private static string GetPosterUrl(string cover)
        {
            if (cover.IsNotNullOrWhiteSpace() &&
                Uri.TryCreate(cover, UriKind.Absolute, out var posterUri) &&
                (posterUri.Scheme == Uri.UriSchemeHttp || posterUri.Scheme == Uri.UriSchemeHttps))
            {
                return posterUri.AbsoluteUri;
            }

            return null;
        }
    }
}
