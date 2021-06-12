using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListParser : IParseIndexerResponse
    {
        private readonly string _baseUrl;
        private readonly FileListSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public FileListParser(FileListSettings settings, string baseUrl, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _baseUrl = baseUrl;
            _categories = categories;
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

            var queryResults = JsonConvert.DeserializeObject<List<FileListTorrent>>(indexerResponse.Content);

            foreach (var result in queryResults)
            {
                var id = result.Id;

                var flags = new List<IndexerFlag>();

                if (result.FreeLeech)
                {
                    flags.Add(IndexerFlag.FreeLeech);
                }

                var imdbId = 0;
                if (result.ImdbId != null && result.ImdbId.Length > 2)
                {
                    imdbId = int.Parse(result.ImdbId.Substring(2));
                }

                var downloadVolumeFactor = result.FreeLeech == true ? 0 : 1;
                var uploadVolumeFactor = result.DoubleUp == true ? 2 : 1;

                torrentInfos.Add(new TorrentInfo()
                {
                    Guid = string.Format("FileList-{0}", id),
                    Title = result.Name,
                    Size = result.Size,
                    Categories = _categories.MapTrackerCatDescToNewznab(result.Category),
                    DownloadUrl = GetDownloadUrl(id),
                    InfoUrl = GetInfoUrl(id),
                    Seeders = result.Seeders,
                    Peers = result.Leechers + result.Seeders,
                    PublishDate = result.UploadDate,
                    ImdbId = imdbId,
                    IndexerFlags = flags,
                    Files = (int)result.Files,
                    Grabs = (int)result.TimesCompleted,
                    DownloadVolumeFactor = downloadVolumeFactor,
                    UploadVolumeFactor = uploadVolumeFactor,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, //48 hours
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
                .AddQueryParam("passkey", _settings.Passkey);

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
