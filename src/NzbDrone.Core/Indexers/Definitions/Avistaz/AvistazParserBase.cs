using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazParserBase : IParseIndexerResponse
    {
        private readonly HashSet<string> _hdResolutions = new() { "1080p", "1080i", "720p" };

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return releaseInfos;
            }

            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RequestLimitReachedException(indexerResponse, "API Request Limit Reached");
            }

            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                STJson.TryDeserialize<AvistazErrorResponse>(indexerResponse.HttpResponse.Content, out var errorResponse);
                throw new IndexerAuthException(errorResponse?.Message ?? "Unauthorized request to indexer");
            }

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = STJson.Deserialize<AvistazResponse>(indexerResponse.HttpResponse.Content);

            foreach (var row in jsonResponse.Data)
            {
                var detailsUrl = row.Url;

                var release = new TorrentInfo
                {
                    Guid = detailsUrl,
                    InfoUrl = detailsUrl,
                    Title = ParseTitle(row),
                    DownloadUrl = row.Download,
                    Categories = ParseCategories(row).ToList(),
                    InfoHash = row.InfoHash,
                    Size = row.FileSize,
                    Files = row.FileCount,
                    Grabs = row.Completed,
                    Seeders = row.Seed,
                    Peers = row.Leech + row.Seed,
                    PublishDate = DateTime.Parse(row.CreatedAtIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    DownloadVolumeFactor = row.DownloadMultiply,
                    UploadVolumeFactor = row.UploadMultiply,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200, // 72 hours
                    Languages = row.Audio?.Select(x => x.Language).ToList() ?? [],
                    Subs = row.Subtitle?.Select(x => x.Language).ToList() ?? []
                };

                if (row.FileSize is > 0)
                {
                    var sizeGigabytes = row.FileSize.Value / Math.Pow(1024, 3);

                    release.MinimumSeedTime = sizeGigabytes switch
                    {
                        > 50.0 => (long)((100 * Math.Log(sizeGigabytes)) - 219.2023) * 3600,
                        _ => 259200 + (long)(sizeGigabytes * 7200)
                    };
                }

                if (row.MovieTvinfo is not null)
                {
                    release.ImdbId = ParseUtil.GetImdbId(row.MovieTvinfo.Imdb).GetValueOrDefault();
                    release.TmdbId = row.MovieTvinfo.Tmdb.IsNotNullOrWhiteSpace() && ParseUtil.TryCoerceInt(row.MovieTvinfo.Tmdb, out var tmdbResult) ? tmdbResult : 0;
                    release.TvdbId = row.MovieTvinfo.Tvdb.IsNotNullOrWhiteSpace() && ParseUtil.TryCoerceInt(row.MovieTvinfo.Tvdb, out var tvdbResult) ? tvdbResult : 0;
                }

                releaseInfos.Add(release);
            }

            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        protected virtual IReadOnlyList<IndexerCategory> ParseCategories(AvistazRelease row)
        {
            var categories = new List<IndexerCategory>();
            var videoQuality = row.VideoQuality;

            switch (row.Type.ToUpperInvariant())
            {
                case "MOVIE":
                    categories.Add(videoQuality switch
                    {
                        var res when _hdResolutions.Contains(res) => NewznabStandardCategory.MoviesHD,
                        "2160p" => NewznabStandardCategory.MoviesUHD,
                        _ => NewznabStandardCategory.MoviesSD
                    });
                    break;
                case "TV-SHOW":
                    categories.Add(videoQuality switch
                    {
                        var res when _hdResolutions.Contains(res) => NewznabStandardCategory.TVHD,
                        "2160p" => NewznabStandardCategory.TVUHD,
                        _ => NewznabStandardCategory.TVSD
                    });
                    break;
                case "MUSIC":
                    categories.Add(NewznabStandardCategory.Audio);
                    break;
                default:
                    throw new Exception($"Error parsing Avistaz category type \"{row.Type}\"");
            }

            return categories;
        }

        protected virtual string ParseTitle(AvistazRelease row)
        {
            return row.FileName;
        }
    }
}
