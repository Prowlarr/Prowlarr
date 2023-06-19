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
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class MyAnonamouse : TorrentIndexerBase<MyAnonamouseSettings>
    {
        public override string Name => "MyAnonamouse";
        public override string[] IndexerUrls => new[] { "https://www.myanonamouse.net/" };
        public override string Description => "MyAnonaMouse (MAM) is a large ebook and audiobook tracker.";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        private readonly ICacheManager _cacheManager;
        private static readonly Regex TorrentIdRegex = new Regex(@"tor/download.php\?tid=(?<id>\d+)$");

        public MyAnonamouse(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, ICacheManager cacheManager)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _cacheManager = cacheManager;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MyAnonamouseRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MyAnonamouseParser(Settings, Capabilities.Categories, _httpClient, _cacheManager, _logger);
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
                    var resource = Json.Deserialize<MyAnonamouseBuyPersonalFreeleechResponse>(response.Content);

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

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("13", NewznabStandardCategory.AudioAudiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping("15", NewznabStandardCategory.AudioAudiobook, "Musicology");
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.AudioAudiobook, "Radio");
            caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("49", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Art");
            caps.Categories.AddCategoryMapping("50", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Biographical");
            caps.Categories.AddCategoryMapping("83", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Business");
            caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("97", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crafts");
            caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Fantasy");
            caps.Categories.AddCategoryMapping("106", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Food");
            caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Fiction");
            caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Non-Fic");
            caps.Categories.AddCategoryMapping("98", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("54", NewznabStandardCategory.AudioAudiobook, "Audiobooks - History");
            caps.Categories.AddCategoryMapping("55", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Home/Garden");
            caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Horror");
            caps.Categories.AddCategoryMapping("99", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Humor");
            caps.Categories.AddCategoryMapping("84", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Instructional");
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Juvenile");
            caps.Categories.AddCategoryMapping("56", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Language");
            caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Literary Classics");
            caps.Categories.AddCategoryMapping("57", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("85", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Medical");
            caps.Categories.AddCategoryMapping("87", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Mystery");
            caps.Categories.AddCategoryMapping("119", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Nature");
            caps.Categories.AddCategoryMapping("88", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Philosophy");
            caps.Categories.AddCategoryMapping("58", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("59", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Recreation");
            caps.Categories.AddCategoryMapping("46", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Romance");
            caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Science Fiction");
            caps.Categories.AddCategoryMapping("53", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Self-Help");
            caps.Categories.AddCategoryMapping("89", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("100", NewznabStandardCategory.AudioAudiobook, "Audiobooks - True Crime");
            caps.Categories.AddCategoryMapping("108", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Western");
            caps.Categories.AddCategoryMapping("111", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Young Adult");
            caps.Categories.AddCategoryMapping("60", NewznabStandardCategory.BooksEBook, "Ebooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("71", NewznabStandardCategory.BooksEBook, "Ebooks - Art");
            caps.Categories.AddCategoryMapping("72", NewznabStandardCategory.BooksEBook, "Ebooks - Biographical");
            caps.Categories.AddCategoryMapping("90", NewznabStandardCategory.BooksEBook, "Ebooks - Business");
            caps.Categories.AddCategoryMapping("61", NewznabStandardCategory.BooksComics, "Ebooks - Comics/Graphic novels");
            caps.Categories.AddCategoryMapping("73", NewznabStandardCategory.BooksEBook, "Ebooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("101", NewznabStandardCategory.BooksEBook, "Ebooks - Crafts");
            caps.Categories.AddCategoryMapping("62", NewznabStandardCategory.BooksEBook, "Ebooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("63", NewznabStandardCategory.BooksEBook, "Ebooks - Fantasy");
            caps.Categories.AddCategoryMapping("107", NewznabStandardCategory.BooksEBook, "Ebooks - Food");
            caps.Categories.AddCategoryMapping("64", NewznabStandardCategory.BooksEBook, "Ebooks - General Fiction");
            caps.Categories.AddCategoryMapping("74", NewznabStandardCategory.BooksEBook, "Ebooks - General Non-Fiction");
            caps.Categories.AddCategoryMapping("102", NewznabStandardCategory.BooksEBook, "Ebooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("76", NewznabStandardCategory.BooksEBook, "Ebooks - History");
            caps.Categories.AddCategoryMapping("77", NewznabStandardCategory.BooksEBook, "Ebooks - Home/Garden");
            caps.Categories.AddCategoryMapping("65", NewznabStandardCategory.BooksEBook, "Ebooks - Horror");
            caps.Categories.AddCategoryMapping("103", NewznabStandardCategory.BooksEBook, "Ebooks - Humor");
            caps.Categories.AddCategoryMapping("115", NewznabStandardCategory.BooksEBook, "Ebooks - Illusion/Magic");
            caps.Categories.AddCategoryMapping("91", NewznabStandardCategory.BooksEBook, "Ebooks - Instructional");
            caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.BooksEBook, "Ebooks - Juvenile");
            caps.Categories.AddCategoryMapping("78", NewznabStandardCategory.BooksEBook, "Ebooks - Language");
            caps.Categories.AddCategoryMapping("67", NewznabStandardCategory.BooksEBook, "Ebooks - Literary Classics");
            caps.Categories.AddCategoryMapping("79", NewznabStandardCategory.BooksMags, "Ebooks - Magazines/Newspapers");
            caps.Categories.AddCategoryMapping("80", NewznabStandardCategory.BooksTechnical, "Ebooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("92", NewznabStandardCategory.BooksEBook, "Ebooks - Medical");
            caps.Categories.AddCategoryMapping("118", NewznabStandardCategory.BooksEBook, "Ebooks - Mixed Collections");
            caps.Categories.AddCategoryMapping("94", NewznabStandardCategory.BooksEBook, "Ebooks - Mystery");
            caps.Categories.AddCategoryMapping("120", NewznabStandardCategory.BooksEBook, "Ebooks - Nature");
            caps.Categories.AddCategoryMapping("95", NewznabStandardCategory.BooksEBook, "Ebooks - Philosophy");
            caps.Categories.AddCategoryMapping("81", NewznabStandardCategory.BooksEBook, "Ebooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("82", NewznabStandardCategory.BooksEBook, "Ebooks - Recreation");
            caps.Categories.AddCategoryMapping("68", NewznabStandardCategory.BooksEBook, "Ebooks - Romance");
            caps.Categories.AddCategoryMapping("69", NewznabStandardCategory.BooksEBook, "Ebooks - Science Fiction");
            caps.Categories.AddCategoryMapping("75", NewznabStandardCategory.BooksEBook, "Ebooks - Self-Help");
            caps.Categories.AddCategoryMapping("96", NewznabStandardCategory.BooksEBook, "Ebooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("104", NewznabStandardCategory.BooksEBook, "Ebooks - True Crime");
            caps.Categories.AddCategoryMapping("109", NewznabStandardCategory.BooksEBook, "Ebooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.BooksEBook, "Ebooks - Western");
            caps.Categories.AddCategoryMapping("112", NewznabStandardCategory.BooksEBook, "Ebooks - Young Adult");
            caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.AudioAudiobook, "Guitar/Bass Tabs");
            caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.AudioAudiobook, "Individual Sheet");
            caps.Categories.AddCategoryMapping("24", NewznabStandardCategory.AudioAudiobook, "Individual Sheet MP3");
            caps.Categories.AddCategoryMapping("126", NewznabStandardCategory.AudioAudiobook, "Instructional Book with Video");
            caps.Categories.AddCategoryMapping("22", NewznabStandardCategory.AudioAudiobook, "Instructional Media - Music");
            caps.Categories.AddCategoryMapping("113", NewznabStandardCategory.AudioAudiobook, "Lick Library - LTP/Jam With");
            caps.Categories.AddCategoryMapping("114", NewznabStandardCategory.AudioAudiobook, "Lick Library - Techniques/QL");
            caps.Categories.AddCategoryMapping("17", NewznabStandardCategory.AudioAudiobook, "Music - Complete Editions");
            caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.AudioAudiobook, "Music Book");
            caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.AudioAudiobook, "Music Book MP3");
            caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.AudioAudiobook, "Sheet Collection");
            caps.Categories.AddCategoryMapping("31", NewznabStandardCategory.AudioAudiobook, "Sheet Collection MP3");
            caps.Categories.AddCategoryMapping("127", NewznabStandardCategory.AudioAudiobook, "Radio -  Comedy");
            caps.Categories.AddCategoryMapping("130", NewznabStandardCategory.AudioAudiobook, "Radio - Drama");
            caps.Categories.AddCategoryMapping("128", NewznabStandardCategory.AudioAudiobook, "Radio - Factual/Documentary");
            caps.Categories.AddCategoryMapping("132", NewznabStandardCategory.AudioAudiobook, "Radio - Reading");

            return caps;
        }
    }

    public class MyAnonamouseRequestGenerator : IIndexerRequestGenerator
    {
        public MyAnonamouseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria)
        {
            var term = searchCriteria.SanitizedSearchTerm.Trim();

            var searchType = Settings.SearchType switch
            {
                (int)MyAnonamouseSearchType.Active => "active",
                (int)MyAnonamouseSearchType.Freeleech => "fl",
                (int)MyAnonamouseSearchType.FreeleechOrVip => "fl-VIP",
                (int)MyAnonamouseSearchType.Vip => "VIP",
                (int)MyAnonamouseSearchType.NotVip => "nVIP",
                _ => "all"
            };

            var parameters = new NameValueCollection
            {
                { "tor[text]", term },
                { "tor[searchType]", searchType },
                { "tor[srchIn][title]", "true" },
                { "tor[srchIn][author]", "true" },
                { "tor[srchIn][narrator]", "true" },
                { "tor[searchIn]", "torrents" },
                { "tor[sortType]", "default" },
                { "tor[perpage]", searchCriteria.Limit?.ToString() ?? "100" },
                { "tor[startNumber]", searchCriteria.Offset?.ToString() ?? "0" },
                { "thumbnails", "1" }, // gives links for thumbnail sized versions of their posters
                { "description", "1" } // include the description
            };

            if (Settings.SearchInDescription)
            {
                parameters.Add("tor[srchIn][description]", "true");
            }

            if (Settings.SearchInSeries)
            {
                parameters.Add("tor[srchIn][series]", "true");
            }

            if (Settings.SearchInFilenames)
            {
                parameters.Add("tor[srchIn][filenames]", "true");
            }

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);
            if (catList.Any())
            {
                var index = 0;
                foreach (var cat in catList)
                {
                    parameters.Add("tor[cat][" + index + "]", cat);
                    index++;
                }
            }
            else
            {
                parameters.Add("tor[cat][]", "0");
            }

            var searchUrl = Settings.BaseUrl + "tor/js/loadSearchJSONbasic.php";

            if (parameters.Count > 0)
            {
                searchUrl += $"?{parameters.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class MyAnonamouseParser : IParseIndexerResponse
    {
        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly IIndexerHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly ICached<string> _userClassCache;
        private readonly HashSet<string> _vipFreeleechUserClasses = new (StringComparer.OrdinalIgnoreCase)
        {
            "VIP",
            "Elite VIP",
        };

        public MyAnonamouseParser(MyAnonamouseSettings settings,
            IndexerCapabilitiesCategories categories,
            IIndexerHttpClient httpClient,
            ICacheManager cacheManager,
            Logger logger)
        {
            _settings = settings;
            _categories = categories;
            _httpClient = httpClient;
            _logger = logger;
            _userClassCache = cacheManager.GetCache<string>(GetType());
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            // Throw auth errors here before we try to parse
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new IndexerAuthException("[403 Forbidden] - mam_id expired or invalid");
            }

            // Throw common http errors here before we try to parse
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var torrentInfos = new List<TorrentInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseResponse>(indexerResponse.Content);

            var error = jsonResponse.Error;
            if (error is "Nothing returned, out of 0" or "Nothing returned, out of 1")
            {
                return torrentInfos.ToArray();
            }

            var hasUserVip = HasUserVip();

            foreach (var item in jsonResponse.Data)
            {
                //TODO shift to ReleaseInfo object initializer for consistency
                var release = new TorrentInfo();

                var id = item.Id;

                release.Title = item.Title;
                release.Description = item.Description;

                if (item.AuthorInfo != null)
                {
                    var authorInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.AuthorInfo);
                    var author = authorInfo?.Take(5).Select(v => v.Value).Join(", ");

                    if (author.IsNotNullOrWhiteSpace())
                    {
                        release.Title += " by " + author;
                    }
                }

                var flags = new List<string>();

                var languageCode = item.LanguageCode;
                if (!string.IsNullOrEmpty(languageCode))
                {
                    flags.Add(languageCode);
                }

                var filetype = item.Filetype;
                if (!string.IsNullOrEmpty(filetype))
                {
                    flags.Add(filetype.ToUpper());
                }

                if (flags.Count > 0)
                {
                    release.Title += " [" + flags.Join(" / ") + "]";
                }

                if (item.Vip)
                {
                    release.Title += " [VIP]";
                }

                release.DownloadUrl = _settings.BaseUrl + "tor/download.php?tid=" + id;
                release.InfoUrl = _settings.BaseUrl + "t/" + id;
                release.Guid = release.InfoUrl;
                release.Categories = _categories.MapTrackerCatToNewznab(item.Category);
                release.PublishDate = DateTime.ParseExact(item.Added, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                release.Grabs = item.Grabs;
                release.Files = item.NumFiles;
                release.Seeders = item.Seeders;
                release.Peers = item.Leechers + release.Seeders;
                release.Size = ParseUtil.GetBytes(item.Size);
                release.DownloadVolumeFactor = item.Free ? 0 : hasUserVip && item.FreeVip ? 0 : 1;
                release.UploadVolumeFactor = 1;
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 259200; // 72 hours

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        private bool HasUserVip()
        {
            var cacheKey = "myanonamouse_user_class_" + _settings.ToJson().SHA256Hash();

            var userClass = _userClassCache.Get(
                cacheKey,
                () =>
                {
                    var request = new HttpRequestBuilder(_settings.BaseUrl.Trim('/'))
                        .Resource("/jsonLoad.php")
                        .Accept(HttpAccept.Json)
                        .Build();

                    _logger.Debug("Fetching user data: " + request.Url.FullUri);

                    request.Cookies.Add("mam_id", _settings.MamId);

                    var response = _httpClient.Get(request);
                    var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseUserDataResponse>(response.Content);

                    return jsonResponse.UserClass?.Trim();
                },
                TimeSpan.FromHours(1));

            return _vipFreeleechUserClasses.Contains(userClass);
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class MyAnonamouseSettingsValidator : NoAuthSettingsValidator<MyAnonamouseSettings>
    {
        public MyAnonamouseSettingsValidator()
        {
            RuleFor(c => c.MamId).NotEmpty();
        }
    }

    public class MyAnonamouseSettings : NoAuthTorrentBaseSettings
    {
        private static readonly MyAnonamouseSettingsValidator Validator = new ();

        public MyAnonamouseSettings()
        {
            MamId = "";
            SearchType = (int)MyAnonamouseSearchType.All;
            SearchInDescription = false;
            SearchInSeries = false;
            SearchInFilenames = false;
        }

        [FieldDefinition(2, Type = FieldType.Textbox, Label = "Mam Id", HelpText = "Mam Session Id (Created Under Preferences -> Security)")]
        public string MamId { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, Label = "Search Type", SelectOptions = typeof(MyAnonamouseSearchType), HelpText = "Specify the desired search type")]
        public int SearchType { get; set; }

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Buy Freeleech Token", HelpText = "Buy personal freeleech token for download")]
        public bool Freeleech { get; set; }

        [FieldDefinition(5, Type = FieldType.Checkbox, Label = "Search in description", HelpText = "Search text in the description")]
        public bool SearchInDescription { get; set; }

        [FieldDefinition(6, Type = FieldType.Checkbox, Label = "Search in series", HelpText = "Search text in the series")]
        public bool SearchInSeries { get; set; }

        [FieldDefinition(7, Type = FieldType.Checkbox, Label = "Search in filenames", HelpText = "Search text in the filenames")]
        public bool SearchInFilenames { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum MyAnonamouseSearchType
    {
        [FieldOption(Label="All torrents", Hint = "Search everything")]
        All = 0,
        [FieldOption(Label="Only active", Hint = "Last update had 1+ seeders")]
        Active = 1,
        [FieldOption(Label="Freeleech", Hint = "Freeleech torrents")]
        Freeleech = 2,
        [FieldOption(Label="Freeleech or VIP", Hint = "Freeleech or VIP torrents")]
        FreeleechOrVip = 3,
        [FieldOption(Label="VIP", Hint = "VIP torrents")]
        Vip = 4,
        [FieldOption(Label="Not VIP", Hint = "Torrents not VIP")]
        NotVip = 5,
    }

    public class MyAnonamouseTorrent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [JsonProperty(PropertyName = "author_info")]
        public string AuthorInfo { get; set; }
        public string Description { get; set; }
        [JsonProperty(PropertyName = "lang_code")]
        public string LanguageCode { get; set; }
        public string Filetype { get; set; }
        public bool Vip { get; set; }
        public bool Free { get; set; }
        [JsonProperty(PropertyName = "fl_vip")]
        public bool FreeVip { get; set; }
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

    public class MyAnonamouseBuyPersonalFreeleechResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class MyAnonamouseUserDataResponse
    {
        [JsonProperty(PropertyName = "class")]
        public string UserClass { get; set; }
    }
}
