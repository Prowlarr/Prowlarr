using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Orpheus : TorrentIndexerBase<OrpheusSettings>
    {
        public override string Name => "Orpheus";
        public override string[] IndexerUrls => new[] { "https://orpheus.network/" };
        public override string Description => "Orpheus (APOLLO) is a Private Torrent Tracker for MUSIC";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override bool SupportsRedirect => true;

        public Orpheus(IIndexerHttpClient httpClient,
                       IEventAggregator eventAggregator,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new OrpheusRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OrpheusParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album, MusicSearchParam.Year
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC, "Applications");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Other, "Comedy");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.BooksComics, "Comics");

            return caps;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var request = new HttpRequestBuilder(link.AbsoluteUri)
                .SetHeader("Authorization", $"token {Settings.Apikey}")
                .Build();

            var downloadBytes = Array.Empty<byte>();

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                downloadBytes = response.ResponseData;

                if (downloadBytes.Length >= 1
                    && downloadBytes[0] != 'd' // simple test for torrent vs HTML content
                    && link.Query.Contains("usetoken=1"))
                {
                    var html = Encoding.GetString(downloadBytes);
                    if (html.Contains("You do not have any freeleech tokens left.")
                        || html.Contains("You do not have enough freeleech tokens")
                        || html.Contains("This torrent is too large.")
                        || html.Contains("You cannot use tokens here"))
                    {
                        // download again without usetoken
                        request.Url = new HttpUri(link.ToString().Replace("&usetoken=1", ""));

                        response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                        downloadBytes = response.ResponseData;
                    }
                }
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
            }

            return downloadBytes;
        }
    }

    public class OrpheusRequestGenerator : IIndexerRequestGenerator
    {
        private readonly OrpheusSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public OrpheusRequestGenerator(OrpheusSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Artist.IsNotNullOrWhiteSpace())
            {
                parameters.Add("artistname", searchCriteria.Artist);
            }

            if (searchCriteria.Album.IsNotNullOrWhiteSpace())
            {
                parameters.Add("groupname", searchCriteria.Album);
            }

            if (searchCriteria.Year.HasValue)
            {
                parameters.Add("year", searchCriteria.Year.ToString());
            }

            pageableRequests.Add(GetRequest(searchCriteria, parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            pageableRequests.Add(GetRequest(searchCriteria, parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            pageableRequests.Add(GetRequest(searchCriteria, parameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
        {
            var term = searchCriteria.SanitizedSearchTerm.Trim();

            parameters.Add("action", "browse");
            parameters.Add("order_by", "time");
            parameters.Add("order_way", "desc");
            parameters.Add("searchstr", term);

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (queryCats.Count > 0)
            {
                foreach (var cat in queryCats)
                {
                    parameters.Add($"filter_cat[{cat}]", "1");
                }
            }

            var request = RequestBuilder()
                .Resource($"/ajax.php?{parameters.GetQueryString()}")
                .Build();

            yield return new IndexerRequest(request);
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{_settings.BaseUrl.TrimEnd('/')}")
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", $"token {_settings.Apikey}");
        }
    }

    public class OrpheusParser : IParseIndexerResponse
    {
        private readonly OrpheusSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public OrpheusParser(OrpheusSettings settings, IndexerCapabilitiesCategories categories)
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

            var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                string.IsNullOrWhiteSpace(jsonResponse.Resource.Status) ||
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

                        var title = GetTitle(result, torrent);
                        var infoUrl = GetInfoUrl(result.GroupId, id);

                        var release = new GazelleInfo
                        {
                            Guid = infoUrl,
                            Title = WebUtility.HtmlDecode(title),
                            Container = torrent.Encoding,
                            Codec = torrent.Format,
                            Size = long.Parse(torrent.Size),
                            DownloadUrl = GetDownloadUrl(id, torrent.CanUseToken),
                            InfoUrl = infoUrl,
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.Time.ToUniversalTime(),
                            Scene = torrent.Scene,
                            Freeleech = torrent.IsFreeLeech || torrent.IsPersonalFreeLeech,
                            Files = torrent.FileCount,
                            Grabs = torrent.Snatches,
                            DownloadVolumeFactor = torrent.IsFreeLeech || torrent.IsNeutralLeech || torrent.IsPersonalFreeLeech ? 0 : 1,
                            UploadVolumeFactor = torrent.IsNeutralLeech ? 0 : 1
                        };

                        var category = torrent.Category;
                        if (category == null || category.Contains("Select Category"))
                        {
                            release.Categories = _categories.MapTrackerCatToNewznab("1");
                        }
                        else
                        {
                            release.Categories = _categories.MapTrackerCatDescToNewznab(category);
                        }

                        torrentInfos.Add(release);
                    }
                }

                // Non-Audio files are formatted a little differently (1:1 for group and torrents)
                else
                {
                    var id = result.TorrentId;
                    var infoUrl = GetInfoUrl(result.GroupId, id);

                    var release = new GazelleInfo
                    {
                        Guid = infoUrl,
                        Title = WebUtility.HtmlDecode(result.GroupName),
                        Size = long.Parse(result.Size),
                        DownloadUrl = GetDownloadUrl(id, result.CanUseToken),
                        InfoUrl = infoUrl,
                        Seeders = int.Parse(result.Seeders),
                        Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                        PublishDate = long.TryParse(result.GroupTime, out var num) ? DateTimeOffset.FromUnixTimeSeconds(num).UtcDateTime : DateTimeUtil.FromFuzzyTime(result.GroupTime),
                        Freeleech = result.IsFreeLeech || result.IsPersonalFreeLeech,
                        Files = result.FileCount,
                        Grabs = result.Snatches,
                        DownloadVolumeFactor = result.IsFreeLeech || result.IsNeutralLeech || result.IsPersonalFreeLeech ? 0 : 1,
                        UploadVolumeFactor = result.IsNeutralLeech ? 0 : 1
                    };

                    var category = result.Category;
                    if (category == null || category.Contains("Select Category"))
                    {
                        release.Categories = _categories.MapTrackerCatToNewznab("1");
                    }
                    else
                    {
                        release.Categories = _categories.MapTrackerCatDescToNewznab(category);
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

        private string GetTitle(GazelleRelease result, GazelleTorrent torrent)
        {
            var title = $"{result.Artist} - {result.GroupName} [{result.GroupYear}]";

            if (result.ReleaseType.IsNotNullOrWhiteSpace() && result.ReleaseType != "Unknown")
            {
                title += " [" + result.ReleaseType + "]";
            }

            if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
            {
                title += $" [{$"{torrent.RemasterTitle} {torrent.RemasterYear}".Trim()}]";
            }

            title += $" [{torrent.Format} {torrent.Encoding}] [{torrent.Media}]";

            if (torrent.HasCue)
            {
                title += " [Cue]";
            }

            return title;
        }

        private string GetDownloadUrl(int torrentId, bool canUseToken)
        {
            // AuthKey is required but not checked, just pass in a dummy variable
            // to avoid having to track authkey, which is randomly cycled
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/ajax.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId);

            // Orpheus fails to download if usetoken=0 so we need to only add if we will use one
            if (_settings.UseFreeleechToken)
            {
                url = url.AddQueryParam("usetoken", "1");
            }

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

    public class OrpheusSettingsValidator : NoAuthSettingsValidator<OrpheusSettings>
    {
        public OrpheusSettingsValidator()
        {
            RuleFor(c => c.Apikey).NotEmpty();
        }
    }

    public class OrpheusSettings : NoAuthTorrentBaseSettings
    {
        private static readonly OrpheusSettingsValidator Validator = new ();

        public OrpheusSettings()
        {
            Apikey = "";
            UseFreeleechToken = false;
        }

        [FieldDefinition(2, Label = "API Key", HelpText = "API Key from the Site (Found in Settings => Access Settings)", Privacy = PrivacyLevel.ApiKey)]
        public string Apikey { get; set; }

        [FieldDefinition(3, Label = "Use Freeleech Tokens", HelpText = "Use freeleech tokens when available", Type = FieldType.Checkbox)]
        public bool UseFreeleechToken { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
