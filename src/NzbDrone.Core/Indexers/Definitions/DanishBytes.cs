using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class DanishBytes : TorrentIndexerBase<DanishBytesSettings>
    {
        public override string Name => "DanishBytes";
        public override string[] IndexerUrls => new string[] { "https://danishbytes.org/" };
        public override string Description => "DanishBytes is a Private Danish Tracker";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public DanishBytes(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DanishBytesRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DanishBytesParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
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
            caps.Categories.AddCategoryMapping("5", NewznabStandardCategory.PC0day, "Appz");
            caps.Categories.AddCategoryMapping("8", NewznabStandardCategory.Books, "Bookz");

            return caps;
        }
    }

    public class DanishBytesRequestGenerator : IIndexerRequestGenerator
    {
        public DanishBytesSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public DanishBytesRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null, int tmdbId = 0, int tvdbId = 0)
        {
            var qc = new NameValueCollection
            {
                { "search", term },
                { "api_token", Settings.ApiKey },
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("imdb", imdbId);
            }

            if (tmdbId > 0)
            {
                qc.Add("tmdb", tmdbId.ToString());
            }

            if (tvdbId > 0)
            {
                qc.Add("tvdb", tvdbId.ToString());
            }

            var searchUrl = string.Format("{0}/api/torrents/v2/filter?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                searchUrl += $"&categories[]={cat}";
            }

            var request = new HttpRequest(searchUrl, HttpAccept.Json);

            var indexerRequest = new IndexerRequest(request);

            yield return indexerRequest;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId, searchCriteria.TmdbId.GetValueOrDefault(0)));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId, 0, searchCriteria.TvdbId.GetValueOrDefault(0)));

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

    public class DanishBytesParser : IParseIndexerResponse
    {
        private readonly DanishBytesSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public DanishBytesParser(DanishBytesSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<DanishBytesResponse>(indexerResponse.HttpResponse);

            foreach (var row in jsonResponse.Resource.Torrents)
            {
                var infoUrl = $"{_settings.BaseUrl}torrents/{row.Id}";

                var release = new TorrentInfo
                {
                    Title = row.Name,
                    InfoUrl = infoUrl,
                    DownloadUrl = $"{_settings.BaseUrl}torrent/download/{row.Id}.{jsonResponse.Resource.Rsskey}",
                    Guid = infoUrl,
                    PosterUrl = row.PosterImage,
                    PublishDate = row.CreatedAt,
                    Categories = _categories.MapTrackerCatToNewznab(row.CategoryId),
                    Size = row.Size,
                    Seeders = row.Seeders,
                    Peers = row.Leechers + row.Seeders,
                    Grabs = row.TimesCompleted,
                    DownloadVolumeFactor = row.Free ? 0 : 1,
                    UploadVolumeFactor = row.Doubleup ? 2 : 1
                };

                torrentInfos.Add(release);
            }

            // order by date
            return torrentInfos.OrderByDescending(o => o.PublishDate).ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DanishBytesSettingsValidator : AbstractValidator<DanishBytesSettings>
    {
        public DanishBytesSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class DanishBytesSettings : IIndexerSettings
    {
        private static readonly DanishBytesSettingsValidator Validator = new DanishBytesSettingsValidator();

        public DanishBytesSettings()
        {
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "API Key", HelpText = "API Key from Site", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class DanishBytesTorrent
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonProperty(PropertyName = "info_hash")]
        public string InfoHash { get; set; }
        public long Size { get; set; }
        public int Leechers { get; set; }
        public int Seeders { get; set; }

        [JsonProperty(PropertyName = "times_completed")]
        public int TimesCompleted { get; set; }

        [JsonProperty(PropertyName = "category_id")]
        public string CategoryId { get; set; }
        public string Tmdb { get; set; }
        public string Igdb { get; set; }
        public string Mal { get; set; }
        public string Tvdb { get; set; }
        public string Imdb { get; set; }
        public int Stream { get; set; }
        public bool Free { get; set; }

        [JsonProperty(PropertyName = "on_fire")]
        public bool OnFire { get; set; }
        public bool Doubleup { get; set; }
        public bool Highspeed { get; set; }
        public bool Featured { get; set; }
        public bool Webstream { get; set; }
        public bool Anon { get; set; }
        public bool Sticky { get; set; }
        public bool Sd { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "resolution_id")]
        public int ResolutionId { get; set; }

        [JsonProperty(PropertyName = "poster_image")]
        public string PosterImage { get; set; }
        public string Video { get; set; }

        [JsonProperty(PropertyName = "thanks_count")]
        public int ThanksCount { get; set; }

        [JsonProperty(PropertyName = "comments_count")]
        public int CommentsCount { get; set; }
        public string GetSize { get; set; }

        [JsonProperty(PropertyName = "created_at_human")]
        public string CreatedAtHuman { get; set; }
        public bool Bookmarked { get; set; }
        public bool Liked { get; set; }

        [JsonProperty(PropertyName = "show_last_torrents")]
        public bool ShowLastTorrents { get; set; }
    }

    public class DanishBytesPageLinks
    {
        public int To { get; set; }
        public string Qty { get; set; }

        [JsonProperty(PropertyName = "current_page")]
        public int CurrentPage { get; set; }
    }

    public class DanishBytesResponse
    {
        public DanishBytesTorrent[] Torrents { get; set; }
        public int ResultsCount { get; set; }
        public DanishBytesPageLinks Links { get; set; }
        public string CurrentCount { get; set; }
        public int TorrentCountTotal { get; set; }
        public string Rsskey { get; set; }
    }
}
