using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
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
    public class TorrentSyndikat : TorrentIndexerBase<TorrentSyndikatSettings>
    {
        public override string Name => "TorrentSyndikat";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public TorrentSyndikat(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentSyndikatRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentSyndikatParser(Settings, Capabilities.Categories);
        }
    }

    public class TorrentSyndikatRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentSyndikatSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public TorrentSyndikatRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchString = term;
            var queryCollection = new NameValueCollection { { "apikey", Settings.ApiKey } };

            queryCollection.Add("limit", "50"); // Default 30
            queryCollection.Add("ponly", Settings.ProductsOnly ? "true" : "false");
            foreach (var releaseType in Settings.ReleaseTypes)
            {
                queryCollection.Add("release_type", ((TorrentSyndikatReleaseTypes)releaseType).ToString().Replace("_", "-"));
            }

            if (imdbId != null)
            {
                queryCollection.Add("imdbId", imdbId);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                // Suffix the first occurence of `s01` surrounded by whitespace with *
                // That way we also search for single episodes in a whole season search
                var regex = new Regex(@"(^|\s)(s\d{2})(\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                queryCollection.Add("searchstring", regex.Replace(searchString.Trim(), @"$1$2*$3"));
            }

            var cats = string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(categories));
            if (!string.IsNullOrEmpty(cats))
            {
                queryCollection.Add("cats", cats);
            }

            var searchUrl = string.Format("{0}/api_9djWe8Tb2NE3p6opyqnh/v1/browse.php?{1}", Settings.BaseUrl.TrimEnd('/'), queryCollection.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.ImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.ImdbId));

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

    public class TorrentSyndikatParser : IParseIndexerResponse
    {
        private readonly TorrentSyndikatSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentSyndikatParser(TorrentSyndikatSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            // TODO Deserialize to TorrentSyndikatResponse Type
            var jsonContent = JObject.Parse(indexerResponse.Content);

            foreach (var row in jsonContent.Value<JArray>("rows"))
            {
                var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                var id = row.Value<string>("id");
                var details = _settings.BaseUrl + "details.php?id=" + id;
                var seeders = row.Value<int>("seeders");

                var release = new TorrentInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 96 * 60 * 60,
                    DownloadVolumeFactor = 1,
                    UploadVolumeFactor = 1,
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = _settings.BaseUrl + "download.php?id=" + id + "&apikey=" + _settings.ApiKey,
                    Title = row.Value<string>("name"),
                    Categories = _categories.MapTrackerCatToNewznab(row.Value<int>("category").ToString()),
                    PublishDate = dateTime.AddSeconds(row.Value<long>("added")).ToLocalTime(),
                    Size = row.Value<long>("size"),
                    Files = row.Value<int>("numfiles"),
                    Seeders = seeders,
                    Peers = seeders + row.Value<int>("leechers"),
                    Grabs = row.Value<int>("snatched"),
                    ImdbId = row.Value<int?>("imdbId").GetValueOrDefault(),
                    TvdbId = row.Value<int?>("tvdbId").GetValueOrDefault(),
                    TmdbId = row.Value<int?>("tmdbId").GetValueOrDefault()
                };

                var poster = row.Value<string>("poster");
                if (!string.IsNullOrWhiteSpace(poster))
                {
                    release.PosterUrl = _settings.BaseUrl + poster.Substring(1);
                }

                var descriptions = new List<string>();
                var title = row.Value<string>("title");
                var titleOrigin = row.Value<string>("title_origin");
                var year = row.Value<int?>("year");
                var pid = row.Value<int?>("pid");
                var releaseType = row.Value<string>("release_type");
                var tags = row.Value<JArray>("tags");
                var genres = row.Value<JArray>("genres");

                if (!string.IsNullOrWhiteSpace(title))
                {
                    descriptions.Add("Title: " + title);
                }

                if (!string.IsNullOrWhiteSpace(titleOrigin))
                {
                    descriptions.Add("Original Title: " + titleOrigin);
                }

                if (year > 0)
                {
                    descriptions.Add("Year: " + year);
                }

                if (pid > 0)
                {
                    descriptions.Add("Product-Link: " + _settings.BaseUrl + "product.php?pid=" + pid);
                }

                if (genres != null && genres.Any())
                {
                    descriptions.Add("Genres: " + string.Join(", ", genres));
                }

                if (tags != null && tags.Any())
                {
                    descriptions.Add("Tags: " + string.Join(", ", tags));
                }

                if (!string.IsNullOrWhiteSpace(releaseType))
                {
                    descriptions.Add("Release Type: " + releaseType);
                }

                if (descriptions.Any())
                {
                    release.Description = string.Join(Environment.NewLine, descriptions);
                }

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentSyndikatSettingsValidator : AbstractValidator<TorrentSyndikatSettings>
    {
        public TorrentSyndikatSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class TorrentSyndikatSettings : NoAuthTorrentBaseSettings
    {
        private static readonly TorrentSyndikatSettingsValidator Validator = new TorrentSyndikatSettingsValidator();

        public TorrentSyndikatSettings()
        {
            ApiKey = "";
            ReleaseTypes = new List<int>();
        }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "Site API Key")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "Products Only", Type = FieldType.Checkbox, HelpText = "Limit search to torrents linked to a product")]
        public bool ProductsOnly { get; set; }

        [FieldDefinition(4, Label = "Release Types", Type = FieldType.Select, SelectOptions = typeof(TorrentSyndikatReleaseTypes))]
        public IEnumerable<int> ReleaseTypes { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum TorrentSyndikatReleaseTypes
    {
        [FieldOption]
        P2P = 1,
        [FieldOption]
        Scene = 2,
        [FieldOption]
        O_Scene = 3
    }
}
