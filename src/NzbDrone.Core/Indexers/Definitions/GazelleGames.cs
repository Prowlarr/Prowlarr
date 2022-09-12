using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class GazelleGames : TorrentIndexerBase<GazelleGamesSettings>
    {
        public override string Name => "GazelleGames";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public GazelleGames(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GazelleGamesRequestGenerator() { Settings = Settings, Capabilities = Capabilities, HttpClient = _httpClient };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GazelleGamesParser(Settings, Capabilities.Categories);
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            ((GazelleGamesRequestGenerator)GetRequestGenerator()).FetchPasskey();
            await base.Test(failures);
        }
    }

    public class GazelleGamesRequestGenerator : IIndexerRequestGenerator
    {
        public GazelleGamesSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public IIndexerHttpClient HttpClient { get; set; }

        public GazelleGamesRequestGenerator()
        {
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories)));

            return pageableRequests;
        }

        public void FetchPasskey()
        {
            // GET on index for the passkey
            var request = RequestBuilder().Resource("api.php?request=quick_user").Build();
            var indexResponse = HttpClient.Execute(request);
            var index = Json.Deserialize<GazelleGamesUserResponse>(indexResponse.Content);
            if (index == null ||
                string.IsNullOrWhiteSpace(index.Status) ||
                index.Status != "success" ||
                string.IsNullOrWhiteSpace(index.Response.PassKey))
            {
                throw new Exception("Failed to authenticate with GazelleGames.");
            }

            // Set passkey on settings so it can be used to generate the download URL
            Settings.Passkey = index.Response.PassKey;
        }

        private IEnumerable<IndexerRequest> GetRequest(string parameters)
        {
            var req = RequestBuilder()
                .Resource($"api.php?{parameters}")
                .Build();

            yield return new IndexerRequest(req);
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{Settings.BaseUrl.Trim().TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("X-API-Key", Settings.Apikey);
        }

        private string GetBasicSearchParameters(string searchTerm, int[] categories)
        {
            var parameters = "request=search&search_type=torrents&empty_groups=filled&order_by=time&order_way=desc";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchType = Settings.SearchGroupNames ? "groupname" : "searchstr";

                parameters += string.Format("&{1}={0}", searchTerm.Replace(".", " "), searchType);
            }

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    parameters += string.Format("&artistcheck[]={0}", cat);
                }
            }

            return parameters;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class GazelleGamesParser : IParseIndexerResponse
    {
        private readonly GazelleGamesSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public GazelleGamesParser(GazelleGamesSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<GazelleGamesResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                string.IsNullOrWhiteSpace(jsonResponse.Resource.Status) ||
                jsonResponse.Resource.Response == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Resource.Response)
            {
                Dictionary<string, GazelleGamesTorrent> torrents;

                try
                {
                    torrents = ((JObject)result.Value.Torrents).ToObject<Dictionary<string, GazelleGamesTorrent>>();
                }
                catch
                {
                    continue;
                }

                if (result.Value.Torrents != null)
                {
                    var categories = result.Value.Artists.Select(a => a.Name);

                    foreach (var torrent in torrents)
                    {
                        var id = int.Parse(torrent.Key);

                        var infoUrl = GetInfoUrl(result.Key, id);

                        var release = new TorrentInfo()
                        {
                            Guid = infoUrl,
                            Title = torrent.Value.ReleaseTitle,
                            Files = torrent.Value.FileCount,
                            Grabs = torrent.Value.Snatched,
                            Size = long.Parse(torrent.Value.Size),
                            DownloadUrl = GetDownloadUrl(id),
                            InfoUrl = infoUrl,
                            Seeders = torrent.Value.Seeders,
                            Categories = _categories.MapTrackerCatDescToNewznab(categories.FirstOrDefault()),
                            Peers = torrent.Value.Leechers + torrent.Value.Seeders,
                            PublishDate = torrent.Value.Time.ToUniversalTime(),
                            DownloadVolumeFactor = torrent.Value.FreeTorrent == GazelleGamesFreeTorrent.FreeLeech || torrent.Value.FreeTorrent == GazelleGamesFreeTorrent.Neutral || torrent.Value.LowSeedFL ? 0 : 1,
                            UploadVolumeFactor = torrent.Value.FreeTorrent == GazelleGamesFreeTorrent.Neutral ? 0 : 1
                        };

                        torrentInfos.Add(release);
                    }
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        private string GetDownloadUrl(int torrentId)
        {
            // AuthKey is required but not checked, just pass in a dummy variable
            // to avoid having to track authkey, which is randomly cycled
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("authkey", "prowlarr")
                .AddQueryParam("torrent_pass", _settings.Passkey);

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

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class GazelleGamesSettingsValidator : AbstractValidator<GazelleGamesSettings>
    {
        public GazelleGamesSettingsValidator()
        {
            RuleFor(c => c.Apikey).NotEmpty();
        }
    }

    public class GazelleGamesSettings : NoAuthTorrentBaseSettings
    {
        private static readonly GazelleGamesSettingsValidator Validator = new GazelleGamesSettingsValidator();

        public GazelleGamesSettings()
        {
            Apikey = "";
            Passkey = "";
        }

        [FieldDefinition(2, Label = "API Key", HelpText = "API Key from the Site (Found in Settings => Access Settings), Must have User Permissions", Privacy = PrivacyLevel.ApiKey)]
        public string Apikey { get; set; }

        [FieldDefinition(3, Label = "Search Group Names", Type = FieldType.Checkbox, HelpText = "Search Group Names Only")]
        public bool SearchGroupNames { get; set; }

        public string Passkey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class GazelleGamesResponse
    {
        public string Status { get; set; }
        public Dictionary<string, GazelleGamesGroup> Response { get; set; }
    }

    public class GazelleGamesGroup
    {
        public List<GazelleGamesArtist> Artists { get; set; }
        public object Torrents { get; set; }
    }

    public class GazelleGamesArtist
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class GazelleGamesTorrent
    {
        public string Size { get; set; }
        public int? Snatched { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string ReleaseTitle { get; set; }
        public DateTime Time { get; set; }
        public int FileCount { get; set; }
        public GazelleGamesFreeTorrent FreeTorrent { get; set; }
        public bool PersonalFL { get; set; }
        public bool LowSeedFL { get; set; }
    }

    public class GazelleGamesUserResponse
    {
        public string Status { get; set; }
        public GazelleGamesUser Response { get; set; }
    }

    public class GazelleGamesUser
    {
        public string PassKey { get; set; }
    }

    public enum GazelleGamesFreeTorrent
    {
        Normal,
        FreeLeech,
        Neutral,
        Either
    }
}
