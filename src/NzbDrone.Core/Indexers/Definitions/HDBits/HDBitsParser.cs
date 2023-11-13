using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.HDBits
{
    public class HDBitsParser : IParseIndexerResponse
    {
        private readonly HDBitsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public HDBitsParser(HDBitsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();
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

            var responseData = jsonResponse.Data as JArray;
            if (responseData == null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call response missing result data");
            }

            var queryResults = responseData.ToObject<TorrentQueryResponse[]>();

            foreach (var result in queryResults)
            {
                var id = result.Id;
                var internalRelease = result.TypeOrigin == 1;

                var flags = new HashSet<IndexerFlag>();

                if (internalRelease)
                {
                    flags.Add(IndexerFlag.Internal);
                }

                torrentInfos.Add(new HDBitsInfo
                {
                    Guid = $"HDBits-{id}",
                    Title = result.Name,
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
                    Internal = internalRelease,
                    Year = result.ImdbInfo?.Year ?? 0,
                    ImdbId = result.ImdbInfo?.Id ?? 0,
                    TvdbId = result.TvdbInfo?.Id ?? 0,
                    DownloadVolumeFactor = result.FreeLeech == "yes" ? 0 : 1,
                    UploadVolumeFactor = 1,
                    IndexerFlags = flags
                });
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

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
    }
}
