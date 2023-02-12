using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class RarbgParser : IParseIndexerResponse
    {
        private static readonly Regex RegexGuid = new (@"^magnet:\?xt=urn:btih:([a-f0-9]+)", RegexOptions.Compiled);

        private readonly IndexerCapabilities _capabilities;
        private readonly Logger _logger;

        public RarbgParser(IndexerCapabilities capabilities, Logger logger)
        {
            _capabilities = capabilities;
            _logger = logger;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var results = new List<ReleaseInfo>();
            var responseCode = (int)indexerResponse.HttpResponse.StatusCode;

            switch (responseCode)
            {
                case (int)HttpStatusCode.TooManyRequests:
                    throw new TooManyRequestsException(indexerResponse.HttpRequest, indexerResponse.HttpResponse, TimeSpan.FromMinutes(2));
                case 520:
                    throw new TooManyRequestsException(indexerResponse.HttpRequest, indexerResponse.HttpResponse, TimeSpan.FromMinutes(3));
                case (int)HttpStatusCode.OK:
                    break;
                default:
                    throw new IndexerException(indexerResponse, "Indexer API call returned an unexpected StatusCode [{0}]", responseCode);
            }

            var jsonResponse = new HttpResponse<RarbgResponse>(indexerResponse.HttpResponse);

            if (jsonResponse.Resource.error_code.HasValue)
            {
                if (jsonResponse.Resource.error_code is 20 or 8 or 9 or 10 or 5 or 13 or 14)
                {
                    var reason = $"{jsonResponse.Resource.error} ({jsonResponse.Resource.error_code})";

                    if (jsonResponse.Resource.error_code is 5 || (jsonResponse.Resource.rate_limit is 1 && jsonResponse.Resource.torrent_results == null))
                    {
                        throw new TooManyRequestsException(indexerResponse.HttpRequest, indexerResponse.HttpResponse, TimeSpan.FromMinutes(5));
                    }
                    else
                    {
                        _logger.Debug("No results or imdbid/tvdb not found. Reason: {0}", reason);
                    }

                    return results;
                }

                throw new IndexerException(indexerResponse, "Indexer API call returned error {0}: {1}", jsonResponse.Resource.error_code, jsonResponse.Resource.error);
            }

            if (jsonResponse.Resource.torrent_results == null)
            {
                if (jsonResponse.Resource.rate_limit == 1)
                {
                    throw new TooManyRequestsException(indexerResponse.HttpRequest, indexerResponse.HttpResponse, TimeSpan.FromMinutes(5));
                }

                return results;
            }

            foreach (var torrent in jsonResponse.Resource.torrent_results)
            {
                var torrentInfo = new TorrentInfo
                {
                    Guid = GetGuid(torrent),
                    Categories = _capabilities.Categories.MapTrackerCatDescToNewznab(torrent.category),
                    Title = torrent.title,
                    Size = torrent.size,
                    DownloadUrl = torrent.download,
                    InfoUrl = $"{torrent.info_page}&app_id={BuildInfo.AppName}",
                    PublishDate = torrent.pubdate.ToUniversalTime(),
                    Seeders = torrent.seeders,
                    Peers = torrent.leechers + torrent.seeders,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                if (torrent.movie_info != null)
                {
                    if (torrent.movie_info.tvdb != null)
                    {
                        torrentInfo.TvdbId = torrent.movie_info.tvdb.Value;
                    }

                    if (torrent.movie_info.tvrage != null)
                    {
                        torrentInfo.TvRageId = torrent.movie_info.tvrage.Value;
                    }
                }

                results.Add(torrentInfo);
            }

            return results;
        }

        private string GetGuid(RarbgTorrent torrent)
        {
            var match = RegexGuid.Match(torrent.download);

            if (match.Success)
            {
                return string.Format("rarbg-{0}", match.Groups[1].Value);
            }
            else
            {
                return string.Format("rarbg-{0}", torrent.download);
            }
        }
    }
}
