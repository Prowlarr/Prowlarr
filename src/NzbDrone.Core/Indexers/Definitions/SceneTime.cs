using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using FluentValidation;
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
    public class SceneTime : TorrentIndexerBase<SceneTimeSettings>
    {
        public override string Name => "SceneTime";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public SceneTime(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SceneTimeRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SceneTimeParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }
    }

    public class SceneTimeRequestGenerator : IIndexerRequestGenerator
    {
        public SceneTimeSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public SceneTimeRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var qc = new NameValueCollection
            {
                { "cata", "yes" },
                { "sec", "jax" }
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in catList)
            {
                qc.Add("c" + cat, "1");
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", term);
            }

            if (Settings.FreeLeechOnly)
            {
                qc.Add("freeleech", "on");
            }

            var searchUrl = string.Format("{0}/browse.php?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

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
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
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

    public class SceneTimeParser : IParseIndexerResponse
    {
        private readonly SceneTimeSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SceneTimeParser(SceneTimeSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var table = dom.QuerySelector("table.movehere");
            if (table == null)
            {
                return torrentInfos; // no results
            }

            var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head")
                                     .Select(x => x.TextContent).ToList();
            var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
            var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
            var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
            var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
            var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

            var rows = dom.QuerySelectorAll("tr.browse");

            foreach (var row in rows)
            {
                // TODO convert to initializer
                var qDescCol = row.Children[nameIndex];
                var qLink = qDescCol.QuerySelector("a");
                var details = _settings.BaseUrl + "/" + qLink.GetAttribute("href");
                var torrentId = qLink.GetAttribute("href").Split('=')[1];
                var sizeStr = row.Children[sizeIndex].TextContent;
                var seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());

                var catId = "82"; // default
                var qCatLink = row.Children[categoryIndex].QuerySelector("a");
                if (qCatLink != null)
                {
                    catId = new Regex(@"\?cat=(\d*)").Match(qCatLink.GetAttribute("href")).Groups[1].ToString().Trim();
                }

                var release = new TorrentInfo
                {
                    Title = qLink.TextContent,
                    InfoUrl = details,
                    Categories = _categories.MapTrackerCatToNewznab(catId),
                    DownloadUrl = string.Format("{0}/download.php/{1}/download.torrent", _settings.BaseUrl, torrentId),
                    Guid = details,
                    PublishDate = DateTimeUtil.FromTimeAgo(qDescCol.ChildNodes.Last().TextContent),
                    Size = ParseUtil.GetBytes(sizeStr),
                    Seeders = seeders,
                    Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + seeders,
                    DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200 // 72 hours
                };

                release.Categories = _categories.MapTrackerCatToNewznab(catId);

                //TODO Do something with this filtering
                //if (!query.MatchQueryStringAND(release.Title))
                //    continue;
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SceneTimeSettings : CookieTorrentBaseSettings
    {
        public SceneTimeSettings()
        {
        }

        [FieldDefinition(3, Label = "FreeLeech Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "Search Freeleech torrents only")]
        public bool FreeLeechOnly { get; set; }
    }
}
