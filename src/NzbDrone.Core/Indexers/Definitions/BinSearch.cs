using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BinSearch : UsenetIndexerBase<BinSearchSettings>
    {
        public override string Name => "BinSearch";
        public override string BaseUrl => "https://binsearch.info/";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BinSearch(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BinSearchRequestGenerator() { Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BinSearchParser(Capabilities.Categories, BaseUrl);
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
                                   },
                MusicSearchParams = new List<MusicSearchParam>
                                   {
                                       MusicSearchParam.Q
                                   },
                BookSearchParams = new List<BookSearchParam>
                                   {
                                       BookSearchParam.Q
                                   }
            };

            return caps;
        }
    }

    public class BinSearchRequestGenerator : IIndexerRequestGenerator
    {
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public BinSearchRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, SearchCriteriaBase searchCriteria)
        {
            var qc = new NameValueCollection
            {
                { "adv_col", "on" },
                { "postdate", "date" },
                { "adv_sort", "date" },
                { "q", term },
                { "min", searchCriteria.Offset.ToString() },
                { "max", searchCriteria.Limit.ToString() }
            };

            var searchUrl = string.Format("{0}/?{1}", BaseUrl.TrimEnd('/'), qc.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BinSearchParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public BinSearchParser(IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table.torrent_table > tbody > tr.torrent");
            foreach (var row in rows)
            {
                var qDetailsLink = row.QuerySelector("a.torrent_name");
                var year = qDetailsLink.NextSibling.TextContent.Replace("[", "").Replace("]", "").Trim();
                var tags = row.QuerySelector("div.torrent_info").FirstChild.TextContent.Replace(" / ", " ").Trim();
                var title = $"{qDetailsLink.TextContent} {year} {tags}";
                var description = row.QuerySelector("div.tags").TextContent.Trim();
                var details = _baseUrl + qDetailsLink.GetAttribute("href");
                var torrentId = qDetailsLink.GetAttribute("href").Split('=').Last();
                var link = _baseUrl + "torrents.php?action=download&id=" + torrentId;
                var posterStr = qDetailsLink.GetAttribute("data-cover");
                var poster = !string.IsNullOrWhiteSpace(posterStr) ? new Uri(qDetailsLink.GetAttribute("data-cover")) : null;

                var files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(3)").TextContent);
                var publishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(4)").TextContent);
                var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(5)").FirstChild.TextContent);
                var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent);

                var cat = row.QuerySelector("td.cats_col > div").GetAttribute("class").Replace("tooltip cats_", "");
                var category = new List<IndexerCategory>
                {
                    cat switch
                    {
                        "featurefilm" => NewznabStandardCategory.Movies,
                        "shortfilm" => NewznabStandardCategory.Movies,
                        "miniseries" => NewznabStandardCategory.TV,
                        "other" => NewznabStandardCategory.Other,
                        _ => throw new Exception($"Unknown category: {cat}")
                    }
                };

                // TODO: TMDb is also available
                var qImdb = row.QuerySelector("a[href^=\"https://www.imdb.com\"]");
                var imdb = qImdb != null ? ParseUtil.GetImdbID(qImdb.GetAttribute("href").Split('/').Last()) : null;

                var release = new ReleaseInfo
                {
                    Description = description,
                    Title = title,
                    PublishDate = publishDate,
                    Categories = category,
                    DownloadUrl = link,
                    InfoUrl = details,
                    Guid = link,
                    ImdbId = imdb.GetValueOrDefault(),
                    Size = size,
                    Grabs = grabs,
                    Files = files
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BinSearchSettings : IProviderConfig
    {
        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult();
        }
    }
}
