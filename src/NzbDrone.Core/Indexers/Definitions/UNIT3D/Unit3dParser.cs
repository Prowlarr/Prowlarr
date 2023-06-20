using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public class Unit3dParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly Unit3dSettings _settings;

        protected virtual string TorrentUrl => _settings.BaseUrl + "torrents";

        public Unit3dParser(Unit3dSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<Unit3dResponse>(indexerResponse.HttpResponse);

            foreach (var row in jsonResponse.Resource.Data)
            {
                var details = row.Attributes.DetailsLink;
                var link = row.Attributes.DownloadLink;

                var release = new TorrentInfo
                {
                    Title = row.Attributes.Name,
                    DownloadUrl = link,
                    InfoHash = row.Id,
                    InfoUrl = details,
                    Guid = details,
                    Categories = _categories.MapTrackerCatDescToNewznab(row.Attributes.Category),
                    PublishDate = DateTime.Parse(row.Attributes.CreatedAt, CultureInfo.InvariantCulture),
                    Size = row.Attributes.Size,
                    Files = row.Attributes.Files,
                    Grabs = row.Attributes.Grabs,
                    Seeders = row.Attributes.Seeders,
                    ImdbId = ParseUtil.GetImdbId(row.Attributes.ImdbId).GetValueOrDefault(),
                    TmdbId = row.Attributes.TmdbId.IsNullOrWhiteSpace() ? 0 : ParseUtil.CoerceInt(row.Attributes.TmdbId),
                    TvdbId = row.Attributes.TvdbId.IsNullOrWhiteSpace() ? 0 : ParseUtil.CoerceInt(row.Attributes.TvdbId),
                    Peers = row.Attributes.Leechers + row.Attributes.Seeders,
                    DownloadVolumeFactor = row.Attributes.Freeleech ? 0 : 1,
                    UploadVolumeFactor = row.Attributes.DoubleUpload ? 2 : 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
                };

                torrentInfos.Add(release);
            }

            // order by date
            return torrentInfos.OrderByDescending(o => o.PublishDate).ToArray();
        }
    }
}
