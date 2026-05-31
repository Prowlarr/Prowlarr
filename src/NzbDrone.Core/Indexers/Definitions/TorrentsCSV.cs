using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentsCSV : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "TorrentsCSV";
        public override string[] IndexerUrls => new[] { "https://torrents-csv.com/" };
        public override string[] LegacyUrls => new[] { "https://torrents-csv.ml/" };
        public override string Language => "en-US";
        public override string Description => "Torrents.csv is a self-hostable open source torrent search engine and database";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override bool SupportsRss => false;

        public TorrentsCSV(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentsCSVRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentsCSVParser(Settings);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Other);

            return caps;
        }
    }

    public class TorrentsCSVRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NoAuthTorrentBaseSettings _settings;

        public TorrentsCSVRequestGenerator(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            // search cannot be blank and needs at least 3 characters
            if (term.IsNullOrWhiteSpace() || term.Length < 3)
            {
                yield break;
            }

            var qc = new NameValueCollection
            {
                { "size", "100" },
                { "q", term }
            };

            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/service/search?{qc.GetQueryString()}";

            yield return new IndexerRequest(searchUrl, HttpAccept.Json);
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedTvSearchString));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentsCSVParser : IParseIndexerResponse
    {
        private readonly NoAuthTorrentBaseSettings _settings;

        public TorrentsCSVParser(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var jsonResponse = STJson.Deserialize<TorrentsCSVResponse>(indexerResponse.Content);

            foreach (var torrent in jsonResponse.Torrents)
            {
                if (torrent == null)
                {
                    continue;
                }

                var infoHash = torrent.InfoHash;
                var title = torrent.Name;
                var seeders = torrent.Seeders ?? 0;
                var leechers = torrent.Leechers ?? 0;
                var grabs = torrent.Completed ?? 0;
                var publishDate = DateTime.UnixEpoch.AddSeconds(torrent.Created);

                var release = new TorrentInfo
                {
                    Guid = $"magnet:?xt=urn:btih:{infoHash}",
                    InfoUrl = $"{_settings.BaseUrl.TrimEnd('/')}/search?q={title}", // there is no details link
                    Title = title,
                    InfoHash = infoHash, // magnet link is auto generated from infohash
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other },
                    PublishDate = publishDate,
                    Size = torrent.Size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                releaseInfos.Add(release);
            }

            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentsCSVResponse
    {
        public IReadOnlyCollection<TorrentsCSVTorrent> Torrents { get; set; }
    }

    public class TorrentsCSVTorrent
    {
        [JsonPropertyName("infohash")]
        public string InfoHash { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("size_bytes")]
        public long Size { get; set; }

        [JsonPropertyName("created_unix")]
        public long Created { get; set; }

        public int? Leechers { get; set; }

        public int? Seeders { get; set; }

        public int? Completed { get; set; }
    }
}
