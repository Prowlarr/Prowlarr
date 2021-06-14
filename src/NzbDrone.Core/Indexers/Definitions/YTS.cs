using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
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
    public class YTS : TorrentIndexerBase<YTSSettings>
    {
        public override string Name => "YTS";
        public override string[] IndexerUrls => new string[] { "https://yts.mx/" };
        public override string Language => "en-us";
        public override string Description => "YTS is a Public torrent site specialising in HD movies of small size";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1252");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.5);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public YTS(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new YTSRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new YTSParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                                   {
                                   },
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId },
                MusicSearchParams = new List<MusicSearchParam>
                                   {
                                   },
                BookSearchParams = new List<BookSearchParam>
                                   {
                                   }
            };

            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.MoviesHD, "Movies/x264/720p");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.MoviesHD, "Movies/x264/1080p");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.MoviesUHD, "Movies/x264/2160p");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies3D, "Movies/x264/3D");

            return caps;
        }
    }

    public class YTSRequestGenerator : IIndexerRequestGenerator
    {
        public YTSSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public YTSRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/api/v2/list_movies.json", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            var queryCollection = new NameValueCollection
            {
                // without this the API sometimes returns nothing
                { "sort", "date_added" },
                { "limit", "50" }
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("query_term", imdbId);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                queryCollection.Add("query_term", searchString);
            }

            searchUrl = searchUrl + "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class YTSParser : IParseIndexerResponse
    {
        private readonly YTSSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public YTSParser(YTSSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var contentString = indexerResponse.Content;

            // returned content might start with an html error message, remove it first
            var jsonStart = contentString.IndexOf('{');
            var jsonContentStr = contentString.Remove(0, jsonStart);

            var jsonContent = JObject.Parse(jsonContentStr);

            var result = jsonContent.Value<string>("status");

            // query was not successful
            if (result != "ok")
            {
                return new List<ReleaseInfo>();
            }

            var dataItems = jsonContent.Value<JToken>("data");
            var movieCount = dataItems.Value<int>("movie_count");

            // no results found in query
            if (movieCount < 1)
            {
                return new List<ReleaseInfo>();
            }

            var movies = dataItems.Value<JToken>("movies");
            if (movies == null)
            {
                return new List<ReleaseInfo>();
            }

            foreach (var movie in movies)
            {
                var torrents = movie.Value<JArray>("torrents");
                if (torrents == null)
                {
                    continue;
                }

                foreach (var torrent in torrents)
                {
                    var release = new TorrentInfo();

                    // append type: BRRip or WEBRip, resolves #3558 via #4577
                    var type = torrent.Value<string>("type");
                    switch (type)
                    {
                        case "web":
                            type = "WEBRip";
                            break;
                        default:
                            type = "BRRip";
                            break;
                    }

                    var quality = torrent.Value<string>("quality");
                    var title = movie.Value<string>("title").Replace(":", "").Replace(' ', '.');
                    var year = movie.Value<int>("year");
                    release.Title = $"{title}.{year}.{quality}.{type}-YTS";

                    var imdb = movie.Value<string>("imdb_code");
                    release.ImdbId = ParseUtil.GetImdbID(imdb).Value;

                    release.InfoHash = torrent.Value<string>("hash"); // magnet link is auto generated from infohash

                    // ex: 2015-08-16 21:25:08 +0000
                    var dateStr = torrent.Value<string>("date_uploaded");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    release.DownloadUrl = torrent.Value<string>("url");
                    release.Seeders = torrent.Value<int>("seeds");
                    release.Peers = torrent.Value<int>("peers") + release.Seeders;
                    release.Size = torrent.Value<long>("size_bytes");
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    release.InfoUrl = movie.Value<string>("url");

                    //release.Poster = new Uri(movie.Value<string>("large_cover_image"));
                    release.Guid = release.DownloadUrl;

                    // map the quality to a newznab category for torznab compatibility (for Radarr, etc)
                    switch (quality)
                    {
                        case "720p":
                            release.Categories = _categories.MapTrackerCatToNewznab("45");
                            break;
                        case "1080p":
                            release.Categories = _categories.MapTrackerCatToNewznab("44");
                            break;
                        case "2160p":
                            release.Categories = _categories.MapTrackerCatToNewznab("46");
                            break;
                        case "3D":
                            release.Categories = _categories.MapTrackerCatToNewznab("47");
                            break;
                        default:
                            release.Categories = _categories.MapTrackerCatToNewznab("45");
                            break;
                    }

                    if (release == null)
                    {
                        continue;
                    }

                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class YTSSettingsValidator : AbstractValidator<YTSSettings>
    {
    }

    public class YTSSettings : IIndexerSettings
    {
        private static readonly YTSSettingsValidator Validator = new YTSSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
