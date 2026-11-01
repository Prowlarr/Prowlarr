using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.FileList;

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
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, "Unexpected response status {0} code from indexer request", indexerResponse.HttpResponse.StatusCode);
        }

        if (indexerResponse.Content.StartsWith("{\"error\"") && STJson.TryDeserialize<FileListErrorResponse>(indexerResponse.Content, out var errorResponse))
        {
            throw new IndexerException(indexerResponse, "Unexpected response from indexer request: {0}", errorResponse.Error);
        }

        if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
        {
            throw new IndexerException(indexerResponse, "Unexpected response header {0} from indexer request, expected {1}", indexerResponse.HttpResponse.Headers.ContentType, HttpAccept.Json.Value);
        }

        var releaseInfos = new List<ReleaseInfo>();

        var results = STJson.Deserialize<List<FileListTorrent>>(indexerResponse.Content);

        foreach (var row in results)
        {
            // skip non-freeleech results when freeleech only is set
            if (_settings.FreeleechOnly && !row.FreeLeech)
            {
                continue;
            }

            var id = row.Id;

            var flags = new HashSet<IndexerFlag>();
            if (row.Internal)
            {
                flags.Add(IndexerFlag.Internal);
            }

            var imdbId = 0;
            if (row.ImdbId is { Length: > 2 })
            {
                int.TryParse(row.ImdbId.TrimStart('t'), out imdbId);
            }

            var downloadVolumeFactor = row.FreeLeech ? 0 : 1;
            var uploadVolumeFactor = row.DoubleUp ? 2 : 1;

            releaseInfos.Add(new TorrentInfo
            {
                Guid = $"FileList-{id}",
                Title = row.Name,
                Size = row.Size,
                Categories = _categories.MapTrackerCatDescToNewznab(row.Category),
                DownloadUrl = GetDownloadUrl(id),
                InfoUrl = GetInfoUrl(id),
                Seeders = row.Seeders,
                Peers = row.Leechers + row.Seeders,
                PublishDate = DateTime.Parse(row.UploadDate + " +0200", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Description = row.SmallDescription,
                Genres = row.SmallDescription.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                ImdbId = imdbId,
                IndexerFlags = flags,
                Files = (int)row.Files,
                Grabs = (int)row.TimesCompleted,
                DownloadVolumeFactor = downloadVolumeFactor,
                UploadVolumeFactor = uploadVolumeFactor,
                MinimumRatio = 1,
                MinimumSeedTime = 172800, // 48 hours
            });
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    private string GetDownloadUrl(uint torrentId)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/download.php")
            .AddQueryParam("id", torrentId.ToString())
            .AddQueryParam("passkey", _settings.Passkey.Trim());

        return url.FullUri;
    }

    private string GetInfoUrl(uint torrentId)
    {
        var url = new HttpUri(_settings.BaseUrl)
            .CombinePath("/details.php")
            .AddQueryParam("id", torrentId.ToString());

        return url.FullUri;
    }
}
