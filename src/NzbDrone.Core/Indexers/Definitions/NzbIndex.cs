using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class NzbIndex : UsenetIndexerBase<NzbIndexSettings>
    {
        public override string Name => "NZBIndex";
        public override string[] IndexerUrls => new[] { "https://nzbindex.com/" };
        public override string Description => "A Usenet Indexer";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public NzbIndex(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NzbIndexRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NzbIndexParser(Settings);
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

    public class NzbIndexRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NzbIndexSettings _settings;

        public NzbIndexRequestGenerator(NzbIndexSettings settings)
        {
            _settings = settings;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedTvSearchString, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));
            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int limit, int offset)
        {
            var queryCollection = new List<KeyValuePair<string, string>>
            {
                { "max", limit.ToString(CultureInfo.InvariantCulture) }
            };

            if (_settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("key", _settings.ApiKey.Trim());
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("q", term);
            }

            var page = offset / limit;
            if (page > 0)
            {
                queryCollection.Add("p", page.ToString(CultureInfo.InvariantCulture));
            }

            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/search?{queryCollection.GetQueryString()}";

            yield return new IndexerRequest(searchUrl, HttpAccept.Json);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexParser : IParseIndexerResponse
    {
        private readonly NzbIndexSettings _settings;

        private static readonly Regex ParseTitleRegex = new(@"\""(?<title>[^:\/]*?)(?:\.(rar|nfo|mkv|par2|001|nzb|url|zip|r[0-9]{2}))?\""", RegexOptions.Compiled);

        public NzbIndexParser(NzbIndexSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Unexpected response status {0} code from indexer request", indexerResponse.HttpResponse.StatusCode);
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonContent = JObject.Parse(indexerResponse.Content);

            if (jsonContent.Value<bool>("error"))
            {
                var errorMessage = jsonContent.Value<string>("errorMessage");
                throw new IndexerException(indexerResponse, $"NZBIndex API error: {errorMessage}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            if (jsonContent["data"]?["content"] is not JArray content)
            {
                return releaseInfos;
            }

            var baseUrl = _settings.BaseUrl.TrimEnd('/');

            foreach (var row in content)
            {
                var id = row.Value<string>("id");
                var parsedTitle = ParseTitleRegex.Match(row.Value<string>("name"));

                if (id.IsNullOrWhiteSpace() || !parsedTitle.Success || parsedTitle.Groups["title"].Value.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var details = $"{baseUrl}/collection/{id}";

                var release = new ReleaseInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = $"{baseUrl}/api/download/{id}.nzb",
                    Title = parsedTitle.Groups["title"].Value,
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other },
                    PublishDate = DateTimeOffset.FromUnixTimeSeconds(row.Value<long>("posted")).UtcDateTime,
                    Size = row.Value<long>("size"),
                    Files = row.Value<int>("fileCount"),
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexSettingsValidator : AbstractValidator<NzbIndexSettings>;

    public class NzbIndexSettings : IIndexerSettings
    {
        private static readonly NzbIndexSettingsValidator Validator = new();

        public NzbIndexSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(1, Label = "IndexerSettingsBaseUrl", HelpText = "IndexerSettingsBaseUrlHelpText", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerNzbIndexSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
