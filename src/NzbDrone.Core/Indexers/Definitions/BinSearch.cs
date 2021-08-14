using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BinSearch : UsenetIndexerBase<BinSearchSettings>
    {
        public override string Name => "BinSearch";

        public override string[] IndexerUrls => new string[] { "https://binsearch.info/" };
        public override string Description => "BinSearch is a binary Usenet search engine.";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BinSearch(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BinSearchRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BinSearchParser(Settings);
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
        public BinSearchSettings Settings { get; set; }

        public BinSearchRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string searchTerm, SearchCriteriaBase searchCriteria)
        {
            var isEmptySearch = string.IsNullOrWhiteSpace(searchTerm);

            var standardParameters = new NameValueCollection
            {
                { "max", searchCriteria.Limit?.ToString() ?? "100" }, // max BinSearch allows is 250
                { "adv_col", "on" }
            };

            var searchUrl = string.Empty;
            if (isEmptySearch)
            {
                var browseParameters = new NameValueCollection
                {
                    { "bg", Settings.FallbackGroup },
                    { "server",  Settings.FallbackServer.ToString() },
                    { "all", "1" }
                };

                searchUrl = string.Format("{0}/browse.php?{1}&{2}", Settings.BaseUrl.TrimEnd('/'), standardParameters.GetQueryString(), browseParameters.GetQueryString());
            }
            else
            {
                var searchParameters = new NameValueCollection
                {
                    { "q", searchTerm },
                    { "adv_sort", "date" },
                    { "postdate", "date" },
                    { "min", searchCriteria.Offset.ToString() },
                    { "hideposter", "on" },
                    { "hidegroup", "on" }
                };

                searchUrl = string.Format("{0}/index.php?{1}&{2}", Settings.BaseUrl.TrimEnd('/'), standardParameters.GetQueryString(), searchParameters.GetQueryString());
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

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
        // c.f. https://github.com/theotherp/nzbhydra2/blob/6e879d635f2db24c711580f4fcc50365057ffa40/core/src/main/java/org/nzbhydra/indexers/Binsearch.java
        private static readonly Regex TitleRegex = new Regex(@"\""(?<title>.*)(?:\.(rar|nfo|mkv|par2|001|nzb|url|zip|r[0-9]{2}))\""");
        private static readonly Regex FileSizeRegex = new Regex(@"size: (?<size>[0-9]+(\.[0-9]+)?).(?<unit>(GB|MB|KB|B))");

        private readonly BinSearchSettings _settings;

        public BinSearchParser(BinSearchSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(indexerResponse.Content);

            var resultTable = document.QuerySelector("table.xMenuT");
            if (resultTable == null)
            {
                return releaseInfos;
            }

            var rows = resultTable.QuerySelectorAll("tr").Skip(1);
            foreach (var row in rows)
            {
                var rowDescription = row.QuerySelector("span.d");
                var rowTitle = row.QuerySelector("span.s") ?? rowDescription?.PreviousSibling;

                if (rowTitle == null)
                {
                    continue;
                }

                var releaseTitle = rowTitle.TextContent;

                var titleMatches = TitleRegex.Match(releaseTitle);
                if (titleMatches.Success)
                {
                    var matchedReleaseTitle = titleMatches.Groups["title"].Value?.Trim();
                    releaseTitle = matchedReleaseTitle.IsNotNullOrWhiteSpace() ? matchedReleaseTitle : releaseTitle;

                    if (releaseTitle.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                }

                var releaseGuid = row.QuerySelector("input[type=checkbox]").GetAttribute("name");
                var releaseUrl = string.Format("{0}/?action=nzb&{1}=1", _settings.BaseUrl.TrimEnd('/'), releaseGuid);
                var releaseInfoUrl = string.Format("{0}{1}", _settings.BaseUrl.TrimEnd('/'), rowDescription.QuerySelector("a").GetAttribute("href"));

                var trimmedDate = row.LastChild.TextContent.Trim();
                var releaseDate = DateTime.ParseExact(trimmedDate, new string[] { "dd-MMM-yyyy", "dd-MMM" }, CultureInfo.InvariantCulture);

                var release = new ReleaseInfo
                {
                    Guid = releaseGuid,
                    Title = releaseTitle,
                    DownloadUrl = releaseUrl,
                    InfoUrl = releaseInfoUrl,
                    PublishDate = releaseDate,
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other }
                };

                var matchReleaseSize = FileSizeRegex.Match(rowDescription.TextContent);
                if (matchReleaseSize.Success)
                {
                    release.Size = ParseUtil.GetBytes(string.Format("{0} {1}", matchReleaseSize.Groups["size"].Value, matchReleaseSize.Groups["unit"].Value));
                }

                releaseInfos.Add(release);
            }

            return releaseInfos;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BinSearchSettingsValidator : AbstractValidator<BinSearchSettings>
    {
        public BinSearchSettingsValidator()
        {
            RuleFor(c => c.FallbackGroup).NotEmpty();
            RuleFor(c => c.FallbackServer).NotEmpty();
        }
    }

    public class BinSearchSettings : IIndexerSettings
    {
        private static readonly BinSearchSettingsValidator Validator = new BinSearchSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Fallback Newsgroup", Advanced = true, Type = FieldType.Textbox, HelpText = "Use this newsgroup for empty searches.")]
        public string FallbackGroup { get; set; }

        [FieldDefinition(3, Label = "Fallback Server", Advanced = true, Type = FieldType.Select, SelectOptions = typeof(BinSearchFallbackServer), HelpText = "Use this Binsearch server for empty searches.")]
        public int FallbackServer { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public BinSearchSettings()
        {
            FallbackGroup = "alt.binaries.boneless";
            FallbackServer = (int)BinSearchFallbackServer.Primary;
        }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum BinSearchFallbackServer
    {
        Primary = 1,
        Secondary = 2
    }
}
