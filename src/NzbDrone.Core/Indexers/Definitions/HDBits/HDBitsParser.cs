using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.HDBits
{
    public class HDBitsParser : IParseIndexerResponse
    {
        private readonly HDBitsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        private readonly List<int> _halfLeechMediums = new () { (int)HdBitsMedium.Bluray, (int)HdBitsMedium.Remux, (int)HdBitsMedium.Capture };

        public HDBitsParser(HDBitsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var indexerHttpResponse = indexerResponse.HttpResponse;

            if (indexerHttpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new RequestLimitReachedException(indexerResponse, "HDBits Query Limit Reached. Please try again later.");
            }

            if (indexerHttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Unexpected response status {0} code from indexer request", indexerHttpResponse.StatusCode);
            }

            var jsonResponse = JsonConvert.DeserializeObject<HDBitsResponse>(indexerResponse.Content);

            if (jsonResponse.Status != StatusCode.Success)
            {
                throw new IndexerException(indexerResponse, "HDBits API request returned status code {0}: {1}", jsonResponse.Status, jsonResponse.Message ?? string.Empty);
            }

            if (jsonResponse.Data is not JArray responseData)
            {
                throw new IndexerException(indexerResponse, "Indexer API call response missing result data");
            }

            var releaseInfos = new List<ReleaseInfo>();

            var queryResults = responseData.ToObject<TorrentQueryResponse[]>();

            foreach (var result in queryResults)
            {
                // Skip non-freeleech results when freeleech only is set
                if (_settings.FreeleechOnly && result.FreeLeech != "yes")
                {
                    continue;
                }

                var id = result.Id;

                releaseInfos.Add(new TorrentInfo
                {
                    Guid = $"HDBits-{id}",
                    Title = GetTitle(result),
                    Size = result.Size,
                    Categories = _categories.MapTrackerCatToNewznab(result.TypeCategory.ToString()),
                    InfoHash = result.Hash,
                    DownloadUrl = GetDownloadUrl(id),
                    InfoUrl = GetInfoUrl(id),
                    Seeders = result.Seeders,
                    Grabs = (int)result.TimesCompleted,
                    Files = (int)result.NumFiles,
                    Peers = result.Leechers + result.Seeders,
                    PublishDate = result.Added.ToUniversalTime(),
                    Year = result.ImdbInfo?.Year ?? 0,
                    ImdbId = result.ImdbInfo?.Id ?? 0,
                    TvdbId = result.TvdbInfo?.Id ?? 0,
                    DownloadVolumeFactor = GetDownloadVolumeFactor(result),
                    UploadVolumeFactor = GetUploadVolumeFactor(result),
                    IndexerFlags = GetIndexerFlags(result)
                });
            }

            return releaseInfos.ToArray();
        }

        private string GetTitle(TorrentQueryResponse item)
        {
            // Use release name for XXX content and full discs
            return item.TypeCategory != 7 && item.TypeMedium != 1 && _settings.UseFilenames && item.FileName.IsNotNullOrWhiteSpace()
                ? item.FileName.Replace(".torrent", "", StringComparison.InvariantCultureIgnoreCase)
                : item.Name;
        }

        private static HashSet<IndexerFlag> GetIndexerFlags(TorrentQueryResponse item)
        {
            var flags = new HashSet<IndexerFlag>();

            if (item.TypeOrigin == 1)
            {
                flags.Add(IndexerFlag.Internal);
            }

            if (item.TypeExclusive == 1)
            {
                flags.Add(IndexerFlag.Exclusive);
            }

            return flags;
        }

        private double GetDownloadVolumeFactor(TorrentQueryResponse item)
        {
            if (item.FreeLeech == "yes")
            {
                return 0;
            }

            // 100% Neutral Leech: all XXX content.
            if (item.TypeCategory == 7)
            {
                return 0;
            }

            // 50% Free Leech: all full discs, remuxes, captures and all internal encodes, also all TV and Documentary content.
            if (_halfLeechMediums.Contains(item.TypeMedium) || item.TypeOrigin == 1 || item.TypeCategory is 2 or 3)
            {
                return 0.5;
            }

            return 1;
        }

        private static double GetUploadVolumeFactor(TorrentQueryResponse item)
        {
            // 100% Neutral Leech: all XXX content.
            return item.TypeCategory == 7 ? 0 : 1;
        }

        private string GetDownloadUrl(string torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/download.php")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("passkey", _settings.ApiKey);

            return url.FullUri;
        }

        private string GetInfoUrl(string torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/details.php")
                .AddQueryParam("id", torrentId);

            return url.FullUri;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
