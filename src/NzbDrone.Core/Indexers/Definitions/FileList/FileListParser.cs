using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListParser : IParseIndexerResponse
    {
        private readonly FileListSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public FileListParser(FileListSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Unexpected response status {0} code from API request", indexerResponse.HttpResponse.StatusCode);
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var queryResults = JsonConvert.DeserializeObject<List<FileListTorrent>>(indexerResponse.Content);

            foreach (var result in queryResults)
            {
                var id = result.Id;

                var flags = new HashSet<IndexerFlag>();

                if (result.Internal)
                {
                    flags.Add(IndexerFlag.Internal);
                }

                var imdbId = 0;
                if (result.ImdbId != null && result.ImdbId.Length > 2)
                {
                    imdbId = int.Parse(result.ImdbId.Substring(2));
                }

                var downloadVolumeFactor = result.FreeLeech ? 0 : 1;
                var uploadVolumeFactor = result.DoubleUp ? 2 : 1;

                torrentInfos.Add(new TorrentInfo
                {
                    Guid = string.Format("FileList-{0}", id),
                    Title = result.Name,
                    Size = result.Size,
                    Categories = _categories.MapTrackerCatDescToNewznab(result.Category),
                    DownloadUrl = GetDownloadUrl(id),
                    InfoUrl = GetInfoUrl(id),
                    Seeders = result.Seeders,
                    Peers = result.Leechers + result.Seeders,
                    PublishDate = DateTime.Parse(result.UploadDate + " +0200", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    Description = result.SmallDescription,
                    Genres = result.SmallDescription.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ImdbId = imdbId,
                    IndexerFlags = flags,
                    Files = (int)result.Files,
                    Grabs = (int)result.TimesCompleted,
                    DownloadVolumeFactor = downloadVolumeFactor,
                    UploadVolumeFactor = uploadVolumeFactor,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
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
                .AddQueryParam("passkey", _settings.Passkey);

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
