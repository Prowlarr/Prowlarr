using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using NLog;
using NzbDrone.Common.Http;
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
    public class SubsPlease : TorrentIndexerBase<SubsPleaseSettings>
    {
        public override string Name => "SubsPlease";
        public override string BaseUrl => "https://subsplease.org/";
        public override string Language => "en-us";
        public override string Description => "SubsPlease - A better HorribleSubs/Erai replacement";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SubsPlease(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SubsPleaseRequestGenerator() { Settings = Settings, Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SubsPleaseParser(Settings, Capabilities.Categories, BaseUrl);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                                   {
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                                   },
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "Anime");

            return caps;
        }
    }

    public class SubsPleaseRequestGenerator : IIndexerRequestGenerator
    {
        public SubsPleaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public SubsPleaseRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetSearchRequests(string term)
        {
            var searchUrl = string.Format("{0}/api/?", BaseUrl.TrimEnd('/'));

            string searchTerm = Regex.Replace(term, "\\[?SubsPlease\\]?\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

            // If the search terms contain a resolution, remove it from the query sent to the API
            Match resMatch = Regex.Match(searchTerm, "\\d{3,4}[p|P]");
            if (resMatch.Success)
            {
                searchTerm = searchTerm.Replace(resMatch.Value, string.Empty);
            }

            var queryParameters = new NameValueCollection
            {
                { "f", "search" },
                { "tz", "America/New_York" },
                { "s", searchTerm }
            };

            var request = new IndexerRequest(searchUrl + queryParameters.GetQueryString(), HttpAccept.Json);

            yield return request;
        }

        private IEnumerable<IndexerRequest> GetRssRequest()
        {
            var searchUrl = string.Format("{0}/api/?", BaseUrl.TrimEnd('/'));

            var queryParameters = new NameValueCollection
            {
                { "f", "latest" },
                { "tz", "America/New_York" }
            };

            var request = new IndexerRequest(searchUrl + queryParameters.GetQueryString(), HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.RssSearch)
            {
                pageableRequests.Add(GetRssRequest());
            }
            else
            {
                pageableRequests.Add(GetSearchRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString)));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.RssSearch)
            {
                pageableRequests.Add(GetRssRequest());
            }
            else
            {
                pageableRequests.Add(GetSearchRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));
            }

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SubsPleaseParser : IParseIndexerResponse
    {
        private readonly SubsPleaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public SubsPleaseParser(SubsPleaseSettings settings, IndexerCapabilitiesCategories categories, string baseurl)
        {
            _settings = settings;
            _categories = categories;
            _baseUrl = baseurl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            // When there are no results, the API returns an empty array or empty response instead of an object
            if (string.IsNullOrWhiteSpace(indexerResponse.Content) || indexerResponse.Content == "[]")
            {
                return torrentInfos;
            }

            var jsonResponse = new HttpResponse<Dictionary<string, SubPleaseRelease>>(indexerResponse.HttpResponse);

            foreach (var keyValue in jsonResponse.Resource)
            {
                SubPleaseRelease r = keyValue.Value;

                foreach (var d in r.Downloads)
                {
                    var release = new TorrentInfo
                    {
                        InfoUrl = _baseUrl + $"shows/{r.Page}/",
                        PublishDate = r.Release_Date.DateTime,
                        Files = 1,
                        Category = new List<IndexerCategory> { NewznabStandardCategory.TVAnime },
                        Seeders = 1,
                        Peers = 2,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    // Ex: [SubsPlease] Shingeki no Kyojin (The Final Season) - 64 (1080p)
                    release.Title += $"[SubsPlease] {r.Show} - {r.Episode} ({d.Res}p)";
                    release.MagnetUrl = d.Magnet;
                    release.DownloadUrl = null;
                    release.Guid = d.Magnet;

                    // The API doesn't tell us file size, so give an estimate based on resolution
                    if (string.Equals(d.Res, "1080"))
                    {
                        release.Size = 1395864371; // 1.3GB
                    }
                    else if (string.Equals(d.Res, "720"))
                    {
                        release.Size = 734003200; // 700MB
                    }
                    else if (string.Equals(d.Res, "480"))
                    {
                        release.Size = 367001600; // 350MB
                    }
                    else
                    {
                        release.Size = 1073741824; // 1GB
                    }

                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SubsPleaseSettingsValidator : AbstractValidator<SubsPleaseSettings>
    {
    }

    public class SubsPleaseSettings : IProviderConfig
    {
        private static readonly SubsPleaseSettingsValidator Validator = new SubsPleaseSettingsValidator();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class SubPleaseRelease
    {
        public string Time { get; set; }
        public DateTimeOffset Release_Date { get; set; }
        public string Show { get; set; }
        public string Episode { get; set; }
        public SubPleaseDownloadInfo[] Downloads { get; set; }
        public string Xdcc { get; set; }
        public string ImageUrl { get; set; }
        public string Page { get; set; }
    }

    public class SubPleaseDownloadInfo
    {
        public string Res { get; set; }
        public string Magnet { get; set; }
    }
}
