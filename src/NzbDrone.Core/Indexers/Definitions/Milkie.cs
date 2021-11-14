using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    [Obsolete]
    public class Milkie : TorrentIndexerBase<MilkieSettings>
    {
        public override string Name => "Milkie";

        public override string[] IndexerUrls => new string[] { "https://milkie.cc/" };
        public override string Description => "Milkie is a general tracker providing unpacked and 0day/0sec scene content.";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Milkie(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MilkieRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MilkieParser(Settings, Capabilities.Categories);
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

            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping("5", NewznabStandardCategory.Books, "Ebook");
            caps.Categories.AddCategoryMapping("6", NewznabStandardCategory.PC, "Apps");
            caps.Categories.AddCategoryMapping("7", NewznabStandardCategory.XXX, "Adult");
            return caps;
        }
    }

    public class MilkieRequestGenerator : IIndexerRequestGenerator
    {
        public MilkieSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public MilkieRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "api/v1/torrents";

            var qc = new NameValueCollection
            {
                { "ps", "100" }
            };

            if (!string.IsNullOrWhiteSpace(term))
            {
                qc.Add("query", term);
            }

            if (categories != null && categories.Length > 0)
            {
                qc.Add("categories", string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(categories)));
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            request.HttpRequest.Headers.Add("x-milkie-auth", Settings.ApiKey);

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public class MilkieParser : IParseIndexerResponse
    {
        private readonly MilkieSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public MilkieParser(MilkieSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var response = JsonConvert.DeserializeObject<MilkieResponse>(indexerResponse.Content);

            var dlQueryParams = new NameValueCollection
            {
                { "key", _settings.ApiKey }
            };

            foreach (var torrent in response.Torrents)
            {
                var torrentUrl = _settings.BaseUrl + "api/v1/torrents";
                var link = $"{torrentUrl}/{torrent.Id}/torrent?{dlQueryParams.GetQueryString()}";
                var details = $"{_settings.BaseUrl}browse/{torrent.Id}";
                var publishDate = DateTimeUtil.FromUnknown(torrent.CreatedAt);

                var release = new TorrentInfo
                {
                    Title = torrent.ReleaseName,
                    DownloadUrl = link,
                    InfoUrl = details,
                    Guid = details,
                    PublishDate = publishDate,
                    Categories = _categories.MapTrackerCatToNewznab(torrent.Category.ToString()),
                    Size = torrent.Size,
                    Seeders = torrent.Seeders,
                    Peers = torrent.Seeders + torrent.PartialSeeders + torrent.Leechers,
                    Grabs = torrent.Downloaded,
                    UploadVolumeFactor = 1,
                    DownloadVolumeFactor = 0,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class MilkieSettingsValidator : AbstractValidator<MilkieSettings>
    {
        public MilkieSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class MilkieSettings : IIndexerSettings
    {
        private static readonly MilkieSettingsValidator Validator = new MilkieSettingsValidator();

        public MilkieSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", HelpText = "Site API Key", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class MilkieResponse
    {
        public int Hits { get; set; }
        public int Took { get; set; }
        public MilkieTorrent[] Torrents { get; set; }
    }

    public class MilkieTorrent
    {
        public string Id { get; set; }
        public string ReleaseName { get; set; }
        public int Category { get; set; }
        public int Downloaded { get; set; }
        public int Seeders { get; set; }
        public int PartialSeeders { get; set; }
        public int Leechers { get; set; }
        public long Size { get; set; }
        public string CreatedAt { get; set; }
    }
}
