using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.TorrentPotato
{
    public class TorrentPotatoParser : IParseIndexerResponse
    {
        private static readonly Regex RegexGuid = new (@"^magnet:\?xt=urn:btih:([a-f0-9]+)", RegexOptions.Compiled);

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an unexpected StatusCode [{0}]", indexerResponse.HttpResponse.StatusCode);
            }

            var jsonResponse = new HttpResponse<TorrentPotatoResponse>(indexerResponse.HttpResponse);

            if (jsonResponse.Resource?.error != null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an error [{0}]", jsonResponse.Resource.error);
            }

            if (jsonResponse.Resource?.results == null)
            {
                return releaseInfos;
            }

            foreach (var torrent in jsonResponse.Resource.results)
            {
                var torrentInfo = new TorrentInfo
                {
                    Guid = GetGuid(torrent),
                    Title = WebUtility.HtmlDecode(torrent.release_name),
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Movies },
                    Size = torrent.size * 1000 * 1000,
                    DownloadUrl = torrent.download_url,
                    InfoUrl = torrent.details_url,
                    ImdbId = ParseUtil.GetImdbId(torrent.imdb_id).GetValueOrDefault(),
                    PublishDate = torrent.publish_date.ToUniversalTime(),
                    Seeders = torrent.seeders,
                    Peers = torrent.leechers + torrent.seeders
                };

                releaseInfos.Add(torrentInfo);
            }

            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private string GetGuid(Result torrent)
        {
            var match = RegexGuid.Match(torrent.download_url);

            if (match.Success)
            {
                return $"potato-{match.Groups[1].Value}";
            }

            return $"potato-{torrent.download_url}";
        }
    }
}
