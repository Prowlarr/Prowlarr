using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.HDBits
{
    public class HDBitsParser : IParseIndexerResponse
    {
        private readonly string _baseUrl;
        private readonly HDBitsSettings _settings;

        public HDBitsParser(HDBitsSettings settings, string baseUrl)
        {
            _settings = settings;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse,
                    "Unexpected response status {0} code from API request",
                    indexerResponse.HttpResponse.StatusCode);
            }

            var jsonResponse = JsonConvert.DeserializeObject<HDBitsResponse>(indexerResponse.Content);

            if (jsonResponse.Status != StatusCode.Success)
            {
                throw new IndexerException(indexerResponse,
                    "HDBits API request returned status code {0}: {1}",
                    jsonResponse.Status,
                    jsonResponse.Message ?? string.Empty);
            }

            var responseData = jsonResponse.Data as JArray;
            if (responseData == null)
            {
                throw new IndexerException(indexerResponse,
                    "Indexer API call response missing result data");
            }

            var queryResults = responseData.ToObject<TorrentQueryResponse[]>();

            foreach (var result in queryResults)
            {
                var id = result.Id;
                var internalRelease = result.TypeOrigin == 1 ? true : false;

                var flags = new List<IndexerFlag>();

                if (internalRelease)
                {
                    flags.Add(IndexerFlag.Internal);
                }

                torrentInfos.Add(new HDBitsInfo()
                {
                    Guid = string.Format("HDBits-{0}", id),
                    Title = result.Name,
                    Size = result.Size,
                    InfoHash = result.Hash,
                    DownloadUrl = GetDownloadUrl(id),
                    InfoUrl = GetInfoUrl(id),
                    Seeders = result.Seeders,
                    Grabs = (int)result.TimesCompleted,
                    Files = (int)result.NumFiles,
                    Peers = result.Leechers + result.Seeders,
                    PublishDate = result.Added.ToUniversalTime(),
                    Internal = internalRelease,
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
            var url = new HttpUri(_baseUrl)
                .CombinePath("/download.php")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("passkey", _settings.ApiKey);

            return url.FullUri;
        }

        private string GetInfoUrl(string torrentId)
        {
            var url = new HttpUri(_baseUrl)
                .CombinePath("/details.php")
                .AddQueryParam("id", torrentId);

            return url.FullUri;
        }
    }
}
