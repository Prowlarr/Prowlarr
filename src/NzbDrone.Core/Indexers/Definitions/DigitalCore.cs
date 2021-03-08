using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class DigitalCore : HttpIndexerBase<DigitalCoreSettings>
    {
        public override string Name => "DigitalCore";
        public override string BaseUrl => "https://digitalcore.club/";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public DigitalCore(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DigitalCoreRequestGenerator() { Settings = Settings, PageSize = PageSize, Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DigitalCoreParser(Settings, Capabilities.Categories, BaseUrl);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            var cookies = new Dictionary<string, string>();

            cookies.Add("uid", Settings.UId);
            cookies.Add("pass", Settings.Passphrase);

            return cookies;
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
                                       MovieSearchParam.Q, MovieSearchParam.ImdbId
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesDVD, "Movies/DVDR");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesSD, "Movies/SD");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.MoviesBluRay, "Movies/BluRay");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.MoviesUHD, "Movies/4K");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesHD, "Movies/720p");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesHD, "Movies/1080p");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesHD, "Movies/PACKS");

            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVHD, "TV/720p");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVHD, "TV/1080p");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TVSD, "TV/SD");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.TVSD, "TV/DVDR");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TVHD, "TV/PACKS");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TVUHD, "TV/4K");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVHD, "TV/BluRay");

            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.Other, "Unknown");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.PC0day, "Apps/0day");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.PCISO, "Apps/PC");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.PCMac, "Apps/Mac");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.PC, "Apps/Tutorials");

            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.Audio, "Music/MTV");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.Audio, "Music/PACKS");

            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCGames, "Games/PC");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.Console, "Games/NSW");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.PCMac, "Games/Mac");

            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.Books, "Ebooks");

            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.XXXSD, "XXX/SD");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.XXX, "XXX/HD");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.XXXUHD, "XXX/4K");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.XXXSD, "XXX/Movies/SD");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.XXX, "XXX/Movies/HD");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.XXXUHD, "XXX/Movies/4K");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.XXXImageSet, "XXX/Imagesets");

            return caps;
        }
    }

    public class DigitalCoreRequestGenerator : IIndexerRequestGenerator
    {
        public string BaseUrl { get; set; }
        public DigitalCoreSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public int MaxPages { get; set; }
        public int PageSize { get; set; }

        public DigitalCoreRequestGenerator()
        {
            MaxPages = 30;
            PageSize = 100;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/api/v1/torrents", BaseUrl.TrimEnd('/'));

            var parameters = new NameValueCollection();

            parameters.Add("extendedSearch", "false");
            parameters.Add("freeleech", "false");
            parameters.Add("index", "0");
            parameters.Add("limit", "100");
            parameters.Add("order", "desc");
            parameters.Add("page", "search");

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Add("searchText", imdbId);
            }
            else
            {
                parameters.Add("searchText", term);
            }

            parameters.Add("sort", "d");
            parameters.Add("section", "all");
            parameters.Add("stereoscopic", "false");
            parameters.Add("watchview", "false");

            searchUrl += "?" + parameters.GetQueryString();

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                searchUrl += "&categories[]=" + cat;
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DigitalCoreParser : IParseIndexerResponse
    {
        private readonly string _baseUrl;
        private readonly DigitalCoreSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public DigitalCoreParser(DigitalCoreSettings settings, IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _settings = settings;
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse,
                    "Unexpected response status {0} code from API request",
                    indexerResponse.HttpResponse.StatusCode);
            }

            try
            {
                //var json = JArray.Parse(results.Content);
                var json = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);

                foreach (var row in json ?? Enumerable.Empty<dynamic>())
                {
                    var release = new TorrentInfo();
                    var descriptions = new List<string>();
                    var tags = new List<string>();

                    release.MinimumRatio = 1.1;
                    release.MinimumSeedTime = 432000; // 120 hours
                    release.Title = row.name;
                    release.Category = _categories.MapTrackerCatToNewznab(row.category.ToString());
                    release.Size = row.size;
                    release.Seeders = row.seeders;
                    release.Peers = row.leechers + release.Seeders;
                    release.PublishDate = DateTime.ParseExact(row.added.ToString() + " +01:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                    release.Files = row.numfiles;
                    release.Grabs = row.times_completed;

                    release.Guid = new Uri(_baseUrl + "torrent/" + row.id.ToString() + "/").ToString();
                    release.DownloadUrl = _baseUrl + "api/v1/torrents/download/" + row.id.ToString();

                    if (row.frileech == 1)
                    {
                        release.DownloadVolumeFactor = 0;
                    }
                    else
                    {
                        release.DownloadVolumeFactor = 1;
                    }

                    release.UploadVolumeFactor = 1;

                    if (row.imdbid2 != null && row.imdbid2.ToString().StartsWith("tt"))
                    {
                        if (int.TryParse((string)row.imdbid2, out int imdbNumber))
                        {
                            release.ImdbId = imdbNumber;
                        }
                    }

                    torrentInfos.Add(release);
                }
            }
            catch (Exception ex)
            {
                throw new IndexerException(indexerResponse,
                    "Unable to parse response from DigitalCore: {0}",
                    ex.Message);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DigitalCoreSettingsValidator : AbstractValidator<DigitalCoreSettings>
    {
        public DigitalCoreSettingsValidator()
        {
            RuleFor(c => c.UId).NotEmpty();
            RuleFor(c => c.Passphrase).NotEmpty();
        }
    }

    public class DigitalCoreSettings : IProviderConfig
    {
        private static readonly DigitalCoreSettingsValidator Validator = new DigitalCoreSettingsValidator();

        public DigitalCoreSettings()
        {
            UId = "";
            Passphrase = "";
        }

        [FieldDefinition(1, Label = "UID", HelpText = "Uid from login cookie")]
        public string UId { get; set; }

        [FieldDefinition(2, Label = "Passphrase", HelpText = "Pass from login cookie")]
        public string Passphrase { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
