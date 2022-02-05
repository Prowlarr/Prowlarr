using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentDay : TorrentIndexerBase<CookieTorrentBaseSettings>
    {
        public override string Name => "TorrentDay";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public TorrentDay(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentDayRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentDayParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }
    }

    public class TorrentDayRequestGenerator : IIndexerRequestGenerator
    {
        public CookieTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public TorrentDayRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "t.json";

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (cats.Count == 0)
            {
                cats = Capabilities.Categories.GetTrackerCategories();
            }

            var catStr = string.Join(";", cats);
            searchUrl = searchUrl + "?" + catStr;
            searchUrl += ";q=";

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                searchUrl += imdbId + " ".UrlEncode(Encoding.UTF8);
            }

            searchUrl += term.UrlEncode(Encoding.UTF8);

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentDayParser : IParseIndexerResponse
    {
        private readonly CookieTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentDayParser(CookieTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var rows = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);

            foreach (var row in rows)
            {
                var title = (string)row.name;

                var torrentId = (long)row.t;
                var details = new Uri(_settings.BaseUrl + "details.php?id=" + torrentId);
                var seeders = (int)row.seeders;
                var imdbId = (string)row["imdb-id"];
                var downloadMultiplier = (double?)row["download-multiplier"] ?? 1;
                var link = new Uri(_settings.BaseUrl + "download.php/" + torrentId + "/" + torrentId + ".torrent");
                var publishDate = DateTimeUtil.UnixTimestampToDateTime((long)row.ctime).ToLocalTime();
                var imdb = ParseUtil.GetImdbID(imdbId) ?? 0;

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = _categories.MapTrackerCatToNewznab(row.c.ToString()),
                    Size = (long)row.size,
                    Files = (int)row.files,
                    Grabs = (int)row.completed,
                    Seeders = seeders,
                    Peers = seeders + (int)row.leechers,
                    ImdbId = imdb,
                    DownloadVolumeFactor = downloadMultiplier,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
