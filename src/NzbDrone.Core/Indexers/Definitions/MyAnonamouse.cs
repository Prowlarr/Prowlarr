using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
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
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class MyAnonamouse : TorrentIndexerBase<MyAnonamouseSettings>
    {
        private static readonly Regex TorrentIdRegex = new Regex(@"tor/download.php\?tid=(?<id>\d+)$");

        public override string Name => "MyAnonamouse";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public MyAnonamouse(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MyAnonamouseRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MyAnonamouseParser(Settings, Capabilities.Categories);
        }

        public override async Task<byte[]> Download(Uri link)
        {
            if (Settings.Freeleech)
            {
                _logger.Debug($"Attempting to use freeleech token for {link.AbsoluteUri}");

                var idMatch = TorrentIdRegex.Match(link.AbsoluteUri);
                if (idMatch.Success)
                {
                    var id = int.Parse(idMatch.Groups["id"].Value);
                    var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                    var freeleechUrl = Settings.BaseUrl + $"json/bonusBuy.php/{timestamp}";

                    var freeleechRequest = new HttpRequestBuilder(freeleechUrl)
                        .AddQueryParam("spendtype", "personalFL")
                        .AddQueryParam("torrentid", id)
                        .AddQueryParam("timestamp", timestamp.ToString())
                        .Build();

                    var indexerReq = new IndexerRequest(freeleechRequest);
                    var response = await FetchIndexerResponse(indexerReq).ConfigureAwait(false);
                    var resource = Json.Deserialize<MyAnonamouseFreeleechResponse>(response.Content);

                    if (resource.Success)
                    {
                        _logger.Debug($"Successfully to used freeleech token for torrentid ${id}");
                    }
                    else
                    {
                        _logger.Debug($"Failed to use freeleech token: ${resource.Error}");
                    }
                }
                else
                {
                    _logger.Debug($"Could not get torrent id from link ${link.AbsoluteUri}, skipping freeleech");
                }
            }

            return await base.Download(link).ConfigureAwait(false);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary("mam_id=" + Settings.MamId);
        }
    }

    public class MyAnonamouseRequestGenerator : IIndexerRequestGenerator
    {
        public MyAnonamouseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public MyAnonamouseRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var qParams = new NameValueCollection
            {
                { "tor[text]", term },
                { "tor[srchIn][title]", "true" },
                { "tor[srchIn][author]", "true" },
                { "tor[searchType]", Settings.ExcludeVip ? "nVIP" : "all" }, // exclude VIP torrents
                { "tor[searchIn]", "torrents" },
                { "tor[hash]", "" },
                { "tor[sortType]", "default" },
                { "tor[startNumber]", "0" },
                { "thumbnails", "1" }, // gives links for thumbnail sized versions of their posters

                //{ "posterLink", "1"}, // gives links for a full sized poster
                //{ "dlLink", "1"}, // include the url to download the torrent
                { "description", "1" } // include the description

                //{"bookmarks", "0"} // include if the item is bookmarked or not
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (catList.Any())
            {
                var index = 0;
                foreach (var cat in catList)
                {
                    qParams.Add("tor[cat][" + index + "]", cat);
                    index++;
                }
            }
            else
            {
                qParams.Add("tor[cat][]", "0");
            }

            var urlSearch = Settings.BaseUrl + "tor/js/loadSearchJSONbasic.php";

            if (qParams.Count > 0)
            {
                urlSearch += $"?{qParams.GetQueryString()}";
            }

            var request = new IndexerRequest(urlSearch, HttpAccept.Json);

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

    public class MyAnonamouseParser : IParseIndexerResponse
    {
        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public MyAnonamouseParser(MyAnonamouseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            // Throw auth errors here before we try to parse
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new IndexerAuthException("[403 Forbidden] - mam_session_id expired or invalid");
            }

            // Throw common http errors here before we try to parse
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

            var torrentInfos = new List<TorrentInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseResponse>(indexerResponse.Content);

            var error = jsonResponse.Error;
            if (error != null && error == "Nothing returned, out of 0")
            {
                return torrentInfos.ToArray();
            }

            foreach (var item in jsonResponse.Data)
            {
                //TODO shift to ReleaseInfo object initializer for consistency
                var release = new TorrentInfo();

                var id = item.Id;
                release.Title = item.Title;

                // release.Description = item.Value<string>("description");
                var author = string.Empty;

                if (item.AuthorInfo != null)
                {
                    var authorInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.AuthorInfo);
                    author = authorInfo?.First().Value;
                }

                if (author != null)
                {
                    release.Title += " by " + author;
                }

                var flags = new List<string>();

                var langCode = item.LangCode;
                if (!string.IsNullOrEmpty(langCode))
                {
                    flags.Add(langCode);
                }

                var filetype = item.Filetype;
                if (!string.IsNullOrEmpty(filetype))
                {
                    flags.Add(filetype);
                }

                if (flags.Count > 0)
                {
                    release.Title += " [" + string.Join(" / ", flags) + "]";
                }

                if (item.Vip)
                {
                    release.Title += " [VIP]";
                }

                var category = item.Category;
                release.Categories = _categories.MapTrackerCatToNewznab(category);

                release.DownloadUrl = _settings.BaseUrl + "/tor/download.php?tid=" + id;
                release.InfoUrl = _settings.BaseUrl + "/t/" + id;
                release.Guid = release.InfoUrl;

                var dateStr = item.Added;
                var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                release.Grabs = item.Grabs;
                release.Files = item.NumFiles;
                release.Seeders = item.Seeders;
                release.Peers = item.Leechers + release.Seeders;
                var size = item.Size;
                release.Size = ParseUtil.GetBytes(size);

                release.DownloadVolumeFactor = item.Free ? 0 : 1;
                release.UploadVolumeFactor = 1;

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class MyAnonamouseSettingsValidator : AbstractValidator<MyAnonamouseSettings>
    {
        public MyAnonamouseSettingsValidator()
        {
            RuleFor(c => c.MamId).NotEmpty();
        }
    }

    public class MyAnonamouseSettings : NoAuthTorrentBaseSettings
    {
        private static readonly MyAnonamouseSettingsValidator Validator = new MyAnonamouseSettingsValidator();

        public MyAnonamouseSettings()
        {
            MamId = "";
        }

        [FieldDefinition(2, Label = "Mam Id", HelpText = "Mam Session Id (Created Under Preferences -> Security)")]
        public string MamId { get; set; }

        [FieldDefinition(3, Type = FieldType.Checkbox, Label = "Exclude VIP", HelpText = "Exclude VIP Torrents from search results")]
        public bool ExcludeVip { get; set; }

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Freeleech", HelpText = "Use freeleech token for download")]
        public bool Freeleech { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class MyAnonamouseTorrent
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [JsonProperty(PropertyName = "author_info")]
        public string AuthorInfo { get; set; }

        [JsonProperty(PropertyName = "lang_code")]
        public string LangCode { get; set; }
        public string Filetype { get; set; }
        public bool Vip { get; set; }
        public bool Free { get; set; }
        public string Category { get; set; }
        public string Added { get; set; }

        [JsonProperty(PropertyName = "times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public int NumFiles { get; set; }
        public string Size { get; set; }
    }

    public class MyAnonamouseResponse
    {
        public string Error { get; set; }
        public List<MyAnonamouseTorrent> Data { get; set; }
    }

    public class MyAnonamouseFreeleechResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
