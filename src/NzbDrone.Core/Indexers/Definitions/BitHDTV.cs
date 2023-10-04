using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BitHDTV : TorrentIndexerBase<CookieTorrentBaseSettings>
    {
        public override string Name => "BitHDTV";
        public override string[] IndexerUrls => new string[] { "https://www.bit-hdtv.com/" };
        public override string Description => "BIT-HDTV - Home of High Definition";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BitHDTV(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BitHDTVRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BitHDTVParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return false;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                                   {
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                                   },
                MovieSearchParams = new List<MovieSearchParam>
                                   {
                                       MovieSearchParam.Q, MovieSearchParam.ImdbId
                                   }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesBluRay, "Movies/Blu-ray");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVDocumentary, "Documentaries");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.AudioLossless, "HQ Audio");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.Other, "Other");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TV, "TV/Seasonpack");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.XXX, "XXX");

            return caps;
        }
    }

    public class BitHDTVRequestGenerator : IIndexerRequestGenerator
    {
        public CookieTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            var qc = new NameValueCollection
            {
                { "cat", Capabilities.Categories.MapTorznabCapsToTrackers(categories, true).FirstIfSingleOrDefault("0") }
            };

            var search = new UriBuilder(searchUrl);

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", imdbId);
                qc.Add("options", "4"); //Search URL field for IMDB link
                search.Query = qc.GetQueryString();
                yield return new IndexerRequest(search.ToString(), HttpAccept.Html);

                qc["Options"] = "1"; //Search Title and Description
                search.Query = qc.GetQueryString();
                yield return new IndexerRequest(search.ToString(), HttpAccept.Html);
            }
            else
            {
                //Site handles empty string on search param. No need to check for IsNullOrEmpty()
                qc.Add("search", term);
                qc.Add("options", "0"); //Search Title Only
                search.Query = qc.GetQueryString();
                yield return new IndexerRequest(search.ToString(), HttpAccept.Html);
            }
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public class BitHDTVParser : IParseIndexerResponse
    {
        private readonly CookieTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public BitHDTVParser(CookieTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);
            foreach (var child in dom.QuerySelectorAll("#needseed"))
            {
                child.Remove();
            }

            var table = dom.QuerySelector("table[align=center] + br + table > tbody");

            // No results, so skip this search
            if (table == null)
            {
                return torrentInfos;
            }

            foreach (var row in table.Children.Skip(1))
            {
                var release = new TorrentInfo();
                var qLink = row.Children[2].QuerySelector("a");
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 172800; // 48 hours
                release.Title = qLink.GetAttribute("title");
                var detailsLink = new Uri(qLink.GetAttribute("href"));

                //Skip irrelevant and duplicate entries
                if (torrentInfos.Any(r => r.Guid == detailsLink.AbsoluteUri))
                {
                    continue;
                }

                release.Files = ParseUtil.CoerceInt(row.Children[3].TextContent);
                release.Grabs = ParseUtil.CoerceInt(row.Children[7].TextContent);
                release.Guid = detailsLink.AbsoluteUri;
                release.InfoUrl = release.Guid;
                release.DownloadUrl = new Uri(_settings.BaseUrl + row.QuerySelector("a[href^=\"download.php\"]").GetAttribute("href")).AbsoluteUri;
                var catUrl = new Uri(_settings.BaseUrl + row.Children[1].FirstElementChild.GetAttribute("href"));
                var catQuery = HttpUtility.ParseQueryString(catUrl.Query);
                var catNum = catQuery["cat"];
                release.Categories = _categories.MapTrackerCatToNewznab(catNum);

                var dateString = row.Children[5].TextContent.Trim();
                var pubDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                release.PublishDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Local);
                var sizeStr = row.Children[6].TextContent;
                release.Size = ParseUtil.GetBytes(sizeStr);
                release.Seeders = ParseUtil.CoerceInt(row.Children[8].TextContent.Trim());
                release.Peers = ParseUtil.CoerceInt(row.Children[9].TextContent.Trim()) + release.Seeders;
                switch (row.GetAttribute("bgcolor"))
                {
                    case "#DDDDDD":
                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 2;
                        break;
                    case "#FFFF99":
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;
                        break;
                    case "#CCFF99":
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 2;
                        break;
                    default:
                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;
                        break;
                }

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
