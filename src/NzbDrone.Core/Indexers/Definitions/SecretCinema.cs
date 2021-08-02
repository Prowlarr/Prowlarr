using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SecretCinema : Gazelle.Gazelle
    {
        public override string Name => "Secret Cinema";
        public override string[] IndexerUrls => new string[] { "https://secret-cinema.pw/" };
        public bool ImdbInTags => false;
        public string APIUrl => Settings.BaseUrl + "ajax.php";
        public string GetSearchTerm(string term) => term;
        public override string Description => "A tracker for rare movies.";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SecretCinema(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SecretCinemaRequestGenerator() { Settings = Settings, Capabilities = Capabilities, HttpClient = _httpClient };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SecretCinemaParser(Settings, Capabilities);
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var filter = "";
            if (searchParameters == null)
            {
            }

            var request =
                new IndexerRequest(
                    $"{APIUrl}?{searchParameters}{filter}",
                    HttpAccept.Json);

            yield return request;
        }

        private string GetBasicSearchParameters(string searchTerm, int[] categories)
        {
            var searchString = GetSearchTerm(searchTerm);

            var parameters = "action=browse&order_by=time&order_way=desc";

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                parameters += string.Format("&searchstr={0}", searchString);
            }

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    parameters += string.Format("&filter_cat[{0}]=1", cat);
                }
            }

            return parameters;
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Audio, "Music");

            return caps;
        }
    }

    public class SecretCinemaRequestGenerator : IIndexerRequestGenerator
    {
        public GazelleSettings Settings { get; set; }

        public IDictionary<string, string> AuthCookieCache { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Logger Logger { get; set; }

        protected virtual string APIUrl => Settings.BaseUrl + "ajax.php";
        protected virtual bool ImdbInTags => false;

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var filter = "";
            if (searchParameters == null)
            {
            }

            var request =
                new IndexerRequest(
                    $"{APIUrl}?{searchParameters}{filter}",
                    HttpAccept.Json);

            yield return request;
        }

        private string GetBasicSearchParameters(string searchTerm, int[] categories)
        {
            var searchString = GetSearchTerm(searchTerm);

            var parameters = "action=browse&order_by=time&order_way=desc";

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                parameters += string.Format("&searchstr={0}", searchString);
            }

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    parameters += string.Format("&filter_cat[{0}]=1", cat);
                }
            }

            return parameters;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                if (ImdbInTags)
                {
                    parameters += string.Format("&taglist={0}", searchCriteria.ImdbId);
                }
                else
                {
                    // secret cinema uses the tt<id> format for imdb ids, so add that back in
                    parameters += string.Format("&cataloguenumber=tt{0}", searchCriteria.ImdbId);
                }
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            if (searchCriteria.Artist != null)
            {
                parameters += string.Format("&artistname={0}", searchCriteria.Artist);
            }

            if (searchCriteria.Label != null)
            {
                parameters += string.Format("&recordlabel={0}", searchCriteria.Label);
            }

            if (searchCriteria.Album != null)
            {
                parameters += string.Format("&groupname={0}", searchCriteria.Album);
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                if (ImdbInTags)
                {
                    parameters += string.Format("&taglist={0}", searchCriteria.ImdbId);
                }
                else
                {
                    parameters += string.Format("&cataloguenumber={0}", searchCriteria.ImdbId);
                }
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        // hook to adjust the search term
        protected virtual string GetSearchTerm(string term) => term;

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }
    }

    public class SecretCinemaParser : IParseIndexerResponse
        {
            protected readonly GazelleSettings _settings;
            protected readonly IndexerCapabilities _capabilities;

            public SecretCinemaParser(GazelleSettings settings, IndexerCapabilities capabilities)
            {
                _settings = settings;
                _capabilities = capabilities;
            }

            public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

            public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
            {
                var torrentInfos = new List<ReleaseInfo>();

                if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                {
                    // Remove cookie cache
                    CookiesUpdater(null, null);

                    throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
                }

                if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
                {
                    // Remove cookie cache
                    CookiesUpdater(null, null);

                    throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
                }

                var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
                if (jsonResponse.Resource.Status != "success" ||
                    jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
                    jsonResponse.Resource.Response == null)
                {
                    return torrentInfos;
                }

                foreach (var result in jsonResponse.Resource.Response.Results)
                {
                    if (result.Torrents != null)
                    {
                        foreach (var torrent in result.Torrents)
                        {
                            var id = torrent.TorrentId;

                            // in SC movies, artist=director and GroupName=title
                            var artist = WebUtility.HtmlDecode(result.Artist);
                            var album = WebUtility.HtmlDecode(result.GroupName);
                            var title = WebUtility.HtmlDecode(result.GroupName);

                            var release = new GazelleInfo()
                            {
                                Guid = string.Format("SecretCinema-{0}", id),
                                Title = WebUtility.HtmlDecode(title),
                                Container = torrent.Encoding,
                                Files = torrent.FileCount,
                                Grabs = torrent.Snatches,
                                Codec = torrent.Format,
                                Size = long.Parse(torrent.Size),
                                DownloadUrl = GetDownloadUrl(id),
                                InfoUrl = GetInfoUrl(result.GroupId, id),
                                Seeders = int.Parse(torrent.Seeders),
                                Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                                PublishDate = torrent.Time.ToUniversalTime(),
                                Scene = torrent.Scene,
                            };

                            var category = torrent.Category;
                            if (category == null || category.Contains("Select Category"))
                            {
                                release.Categories = _capabilities.Categories.MapTrackerCatToNewznab("1");
                            }
                            else
                            {
                                release.Categories = _capabilities.Categories.MapTrackerCatDescToNewznab(category);
                            }

                            if (IsAnyMovieCategory(release.Categories))
                            {
                                // Remove director from title
                                // SC API returns no more useful information than this
                                release.Title = $"{result.GroupName} ({result.GroupYear}) {torrent.Media}";

                                // Replace media formats with standards
                                release.Title = Regex.Replace(release.Title, "BDMV", "COMPLETE BLURAY", RegexOptions.IgnoreCase);
                                release.Title = Regex.Replace(release.Title, "SD", "DVDRip", RegexOptions.IgnoreCase);
                            }
                            else
                            {
                                // SC API currently doesn't return anything but title.
                                release.Title = $"{result.Artist} - {result.GroupName} ({result.GroupYear}) [{torrent.Format} {torrent.Encoding}] [{torrent.Media}]";
                            }

                            if (torrent.HasCue)
                            {
                                release.Title += " [Cue]";
                            }

                            torrentInfos.Add(release);
                        }
                    }
                    else
                    {
                        var id = result.TorrentId;
                        var groupName = WebUtility.HtmlDecode(result.GroupName);

                        var release = new GazelleInfo()
                        {
                            Guid = string.Format("SecretCinema-{0}", id),
                            Title = groupName,
                            Size = long.Parse(result.Size),
                            DownloadUrl = GetDownloadUrl(id),
                            InfoUrl = GetInfoUrl(result.GroupId, id),
                            Seeders = int.Parse(result.Seeders),
                            Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                            Files = result.FileCount,
                            Grabs = result.Snatches,
                            PublishDate = DateTimeOffset.FromUnixTimeSeconds(result.GroupTime).UtcDateTime,
                        };

                        var category = result.Category;
                        if (category == null || category.Contains("Select Category"))
                        {
                            release.Categories = _capabilities.Categories.MapTrackerCatToNewznab("1");
                        }
                        else
                        {
                            release.Categories = _capabilities.Categories.MapTrackerCatDescToNewznab(category);
                        }

                        torrentInfos.Add(release);
                    }
                }

                // order by date
                return
                    torrentInfos
                        .OrderByDescending(o => o.PublishDate)
                        .ToArray();
            }

            private bool IsAnyMovieCategory(ICollection<IndexerCategory> category)
            {
                return category.Contains(NewznabStandardCategory.Movies)
                    || NewznabStandardCategory.Movies.SubCategories.Any(subCat => category.Contains(subCat));
            }

            protected virtual string GetDownloadUrl(int torrentId)
            {
                var url = new HttpUri(_settings.BaseUrl)
                    .CombinePath("/torrents.php")
                    .AddQueryParam("action", "download")
                    .AddQueryParam("useToken", _settings.UseFreeleechToken ? "1" : "0")
                    .AddQueryParam("id", torrentId);

                return url.FullUri;
            }

            private string GetInfoUrl(string groupId, int torrentId)
            {
                var url = new HttpUri(_settings.BaseUrl)
                    .CombinePath("/torrents.php")
                    .AddQueryParam("id", groupId)
                    .AddQueryParam("torrentid", torrentId);

                return url.FullUri;
            }
        }
}
