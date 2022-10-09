using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DryIoc;
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
    public class NzbIndex : TorrentIndexerBase<NzbIndexSettings>
    {
        public override string Name => "NZBIndex";
        public override string[] IndexerUrls => new[] { "https://nzbindex.com/" };
        public override string Description => "A Usenet Indexer";
        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public NzbIndex(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NzbIndexRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NzbIndexParser(Settings, Capabilities.Categories);
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

            // TODO build this out more
            caps.Categories.AddCategoryMapping(83, NewznabStandardCategory.BooksComics, "a.b.comics");
            caps.Categories.AddCategoryMapping(234, NewznabStandardCategory.Console, "a.b.games");
            caps.Categories.AddCategoryMapping(244, NewznabStandardCategory.ConsoleWii, "a.b.games.wii");
            caps.Categories.AddCategoryMapping(246, NewznabStandardCategory.ConsoleXBox, "a.b.games.xbox");
            caps.Categories.AddCategoryMapping(247, NewznabStandardCategory.ConsoleXBox, "a.b.games.xbox360");
            caps.Categories.AddCategoryMapping(327, NewznabStandardCategory.Movies, "a.b.movies");
            caps.Categories.AddCategoryMapping(358, NewznabStandardCategory.Movies, "a.b.movies.x264");
            caps.Categories.AddCategoryMapping(409, NewznabStandardCategory.XXX, "a.b.multimedia.erotica");
            caps.Categories.AddCategoryMapping(587, NewznabStandardCategory.AudioMP3, "a.b.sounds.lossless");
            caps.Categories.AddCategoryMapping(604, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3");
            caps.Categories.AddCategoryMapping(731, NewznabStandardCategory.TV, "a.b.tv");

            return caps;
        }
    }

    public class NzbIndexRequestGenerator : IIndexerRequestGenerator
    {
        public NzbIndexSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public NzbIndexRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, int limit, int offset)
        {
            var searchString = term;

            var queryCollection = new NameValueCollection
            {
                { "key", Settings.ApiKey },
                { "max", limit.ToString() },
                { "q", searchString },
                { "p", ((offset / limit) + 1).ToString() }
            };

            var searchUrl = string.Format("{0}/api/v3/search/?{1}", Settings.BaseUrl.TrimEnd('/'), queryCollection.GetQueryString());

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    searchUrl += string.Format("&g[]={0}", cat);
                }
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexParser : IParseIndexerResponse
    {
        private readonly NzbIndexSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public NzbIndexParser(NzbIndexSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            // TODO Deserialize to TorrentSyndikatResponse Type
            var jsonContent = JObject.Parse(indexerResponse.Content);

            foreach (var row in jsonContent.Value<JArray>("results"))
            {
                var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                var id = row.Value<string>("id");
                var details = _settings.BaseUrl + "collection/" + id;

                var parsedTitle = ParseTitleRegex.Match(row.Value<string>("name"));

                if (!parsedTitle.Success || parsedTitle.Groups["title"].Value.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var release = new ReleaseInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = _settings.BaseUrl + "download/" + id,
                    Title = parsedTitle.Groups["title"].Value,
                    Categories = row.Value<JArray>("group_ids").SelectMany(g => _categories.MapTrackerCatToNewznab(g.Value<string>())).Distinct().ToList(),
                    PublishDate = dateTime.AddMilliseconds(row.Value<long>("posted")).ToLocalTime(),
                    Size = row.Value<long>("size"),
                    Files = row.Value<int>("file_count")
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        private static readonly Regex ParseTitleRegex = new Regex(@"\""(?<title>[^:\/]*?)(?:\.(rar|nfo|mkv|par2|001|nzb|url|zip|r[0-9]{2}))?\""");

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexSettingsValidator : AbstractValidator<NzbIndexSettings>
    {
        public NzbIndexSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class NzbIndexSettings : IIndexerSettings
    {
        private static readonly NzbIndexSettingsValidator Validator = new NzbIndexSettingsValidator();

        public NzbIndexSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "Site API Key")]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
