using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazParser : IParseIndexerResponse
    {
        private readonly AvistazSettings _settings;
        private readonly IndexerCapabilities _capabilities;
        private readonly string _baseUrl;
        private readonly HashSet<string> _hdResolutions = new HashSet<string> { "1080p", "1080i", "720p" };

        public AvistazParser(AvistazSettings settings, IndexerCapabilities capabilities, string baseUrl)
        {
            _settings = settings;
            _capabilities = capabilities;
            _baseUrl = baseUrl;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<AvistazResponse>(indexerResponse.HttpResponse);

            foreach (var row in jsonResponse.Resource.Data)
            {
                var details = row.Url;
                var link = row.Download;

                var cats = ParseCategories(row);

                var release = new TorrentInfo
                {
                    Title = row.FileName,
                    DownloadUrl = link,
                    InfoHash = row.InfoHash,
                    InfoUrl = details,
                    Guid = details,
                    Category = cats,
                    PublishDate = row.CreatedAt,
                    Size = row.FileSize,
                    Files = row.FileCount,
                    Grabs = row.Completed,
                    Seeders = row.Seed,
                    Peers = row.Leech + row.Seed,
                    ImdbId = ParseUtil.GetImdbID(row.MovieTvinfo.Imdb).Value,
                    TvdbId = row.MovieTvinfo.Tvdb,
                    TmdbId = row.MovieTvinfo.Tmdb,
                    DownloadVolumeFactor = row.DownloadMultiply,
                    UploadVolumeFactor = row.UploadMultiply,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            // order by date
            return torrentInfos.OrderByDescending(o => o.PublishDate).ToArray();
        }

        // hook to adjust category parsing
        protected virtual List<IndexerCategory> ParseCategories(AvistazRelease row)
        {
            var cats = new List<IndexerCategory>();
            var resolution = row.VideoQuality;

            switch (row.Type)
            {
                case "Movie":
                    cats.Add(resolution switch
                    {
                        var res when _hdResolutions.Contains(res) => NewznabStandardCategory.MoviesHD,
                        "2160p" => NewznabStandardCategory.MoviesUHD,
                        _ => NewznabStandardCategory.MoviesSD
                    });
                    break;
                case "TV-Show":
                    cats.Add(resolution switch
                    {
                        var res when _hdResolutions.Contains(res) => NewznabStandardCategory.TVHD,
                        "2160p" => NewznabStandardCategory.TVUHD,
                        _ => NewznabStandardCategory.TVSD
                    });
                    break;
                case "Music":
                    cats.Add(NewznabStandardCategory.Audio);
                    break;
                default:
                    throw new Exception("Error parsing category!");
            }

            return cats;
        }
    }
}
