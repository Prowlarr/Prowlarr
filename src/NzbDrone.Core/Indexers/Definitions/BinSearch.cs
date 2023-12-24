using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    [Obsolete("Site has shutdown")]
    public class BinSearch : UsenetIndexerBase<BinSearchSettings>
    {
        public override string Name => "BinSearch";
        public override string[] IndexerUrls => new string[] { "https://binsearch.info/" };
        public override string Description => "The binary Usenet search engine";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override bool SupportsRss => false;
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BinSearch(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BinSearchRequestGenerator() { Capabilities = Capabilities, Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BinSearchParser(Capabilities.Categories, Settings);
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
        public BinSearchSettings Settings { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, SearchCriteriaBase searchCriteria)
        {
            var qc = new NameValueCollection
            {
                { "adv_col", "on" },
                { "postdate", "date" },
                { "adv_sort", "date" },
                { "q", term },
                { "m", searchCriteria.Offset.ToString() },
                { "max", searchCriteria.Limit?.ToString() ?? "100" }
            };

            var searchUrl = string.Format("{0}/?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

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
        private readonly BinSearchSettings _settings;

        public BinSearchParser(IndexerCapabilitiesCategories categories, BinSearchSettings settings)
        {
            _categories = categories;
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table.xMenuT > tbody > tr").Skip(1);
            foreach (var row in rows)
            {
                var titleElement = row.QuerySelector("td > span.s");

                if (titleElement == null)
                {
                    continue;
                }

                var parsedTitle = ParseTitleRegex.Match(titleElement.TextContent);

                if (!parsedTitle.Success || parsedTitle.Groups["title"].Value.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var guid = row.QuerySelector("input[type=checkbox]").GetAttribute("name");
                var publishDate = DateTimeUtil.FromUnknown(row.QuerySelector("td:nth-child(6)").TextContent);
                var sizeElement = row.QuerySelector("td > span.d");
                var size = ParseSizeRegex.Match(sizeElement.TextContent);
                var infoUrl = string.Format("{0}{1}", _settings.BaseUrl.TrimEnd('/'), row.QuerySelector("a").GetAttribute("href"));
                var downloadUrl = string.Format("{0}/?action=nzb&{1}=1", _settings.BaseUrl.TrimEnd('/'), guid);

                var release = new ReleaseInfo
                {
                    Guid = guid,
                    Title = parsedTitle.Groups["title"].Value,
                    Size = ParseUtil.GetBytes(string.Format("{0} {1}", size.Groups["size"].Value, size.Groups["unit"].Value)),
                    PublishDate = publishDate,
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other },
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadUrl
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        private static readonly Regex ParseTitleRegex = new Regex(@"\""(?<title>.*)(?:\.(rar|nfo|mkv|par2|001|nzb|url|zip|r[0-9]{2}))\""");
        private static readonly Regex ParseSizeRegex = new Regex(@"size: (?<size>[0-9]+(\.[0-9]+)?).(?<unit>(GB|MB|KB|B))");

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BinSearchSettings : IIndexerSettings
    {
        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult();
        }
    }
}
