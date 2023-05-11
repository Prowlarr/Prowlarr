using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentsCSV : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "TorrentsCSV";
        public override string[] IndexerUrls => new[] { "https://torrents-csv.ml/" };
        public override string Language => "en-US";
        public override string Description => "Torrents.csv is a self-hostable open source torrent search engine and database";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override bool SupportsRss => false;

        public TorrentsCSV(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentsCSVRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
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
        public NoAuthTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
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

            var searchUrl = string.Format("{0}/service/search?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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
            var torrentInfos = new List<ReleaseInfo>();

            var jsonStart = indexerResponse.Content;
            var jsonContent = JArray.Parse(jsonStart);

            foreach (var torrent in jsonContent)
            {
                if (torrent == null)
                {
                    continue;
                }

                var title = torrent.Value<string>("name");
                var size = torrent.Value<long>("size_bytes");
                var seeders = torrent.Value<int>("seeders");
                var leechers = torrent.Value<int>("leechers");
                var grabs = ParseUtil.CoerceInt(torrent.Value<string>("completed") ?? "0");
                var infoHash = torrent.Value<JToken>("infohash").ToString();

                // convert unix timestamp to human readable date
                var publishDate = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                publishDate = publishDate.AddSeconds(torrent.Value<long>("created_unix"));

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = _settings.BaseUrl, // there is no details link
                    Guid = $"magnet:?xt=urn:btih:{infoHash}",
                    InfoHash = infoHash, // magnet link is auto generated from infohash
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other },
                    PublishDate = publishDate,
                    Size = size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
