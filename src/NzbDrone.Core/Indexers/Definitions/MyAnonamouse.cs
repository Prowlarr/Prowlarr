using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
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

        public MyAnonamouse(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, ICacheManager cacheManager)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _cacheManager = cacheManager;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MyAnonamouseRequestGenerator(Settings, Capabilities, _logger);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MyAnonamouseParser(Definition, Settings, Capabilities.Categories, _httpClient, _cacheManager, _logger);
        }

        public override async Task<IndexerDownloadResponse> Download(Uri link)
        {
            var downloadLink = link.RemoveQueryParam("canUseToken");

            if (Settings.UseFreeleechWedge is (int)MyAnonamouseFreeleechWedgeAction.Preferred or (int)MyAnonamouseFreeleechWedgeAction.Required &&
                bool.TryParse(link.GetQueryParam("canUseToken"), out var canUseToken) && canUseToken)
            {
                _logger.Debug("Attempting to use freeleech wedge for {0}", downloadLink.AbsoluteUri);

                if (int.TryParse(link.GetQueryParam("tid"), out var torrentId) && torrentId > 0)
                {
                    var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                    var freeleechUrl = Settings.BaseUrl + $"json/bonusBuy.php/{timestamp}";

                    var freeleechRequestBuilder = new HttpRequestBuilder(freeleechUrl)
                        .Accept(HttpAccept.Json)
                        .AddQueryParam("spendtype", "personalFL")
                        .AddQueryParam("torrentid", torrentId)
                        .AddQueryParam("timestamp", timestamp.ToString());

                    freeleechRequestBuilder.LogResponseContent = true;

                    var cookies = GetCookies();

                    if (cookies != null && cookies.Any())
                    {
                        freeleechRequestBuilder.SetCookies(Cookies);
                    }

                    var freeleechRequest = freeleechRequestBuilder.Build();

                    var freeleechResponse = await _httpClient.ExecuteProxiedAsync(freeleechRequest, Definition).ConfigureAwait(false);

                    var resource = Json.Deserialize<MyAnonamouseBuyPersonalFreeleechResponse>(freeleechResponse.Content);

                    if (resource.Success)
                    {
                        _logger.Debug("Successfully used freeleech wedge for torrentid {0}.", torrentId);
                    }
                    else if (resource.Error.IsNotNullOrWhiteSpace() && resource.Error.ContainsIgnoreCase("This is already a personal freeleech"))
                    {
                        _logger.Debug("{0} is already a personal freeleech, continuing downloading: {1}", torrentId, resource.Error);
                    }
                    else
                    {
                        _logger.Warn("Failed to purchase freeleech wedge for {0}: {1}", torrentId, resource.Error);

                        if (Settings.UseFreeleechWedge == (int)MyAnonamouseFreeleechWedgeAction.Preferred)
                        {
                            _logger.Debug("'Use Freeleech Wedge' option set to preferred, continuing downloading: '{0}'", downloadLink.AbsoluteUri);
                        }
                        else
                        {
                            throw new ReleaseUnavailableException($"Failed to buy freeleech wedge and 'Use Freeleech Wedge' is set to required, aborting download: '{downloadLink.AbsoluteUri}'");
                        }
                    }
                }
                else
                {
                    _logger.Warn("Could not get torrent id from link {0}, skipping use of freeleech wedge.", downloadLink.AbsoluteUri);
                }
            }

            return await base.Download(downloadLink).ConfigureAwait(false);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            var cookies = base.GetCookies();

            if (cookies is { Count: > 0 } && cookies.TryGetValue("mam_id", out var mamId) && mamId.IsNotNullOrWhiteSpace())
            {
                return cookies;
            }

            return CookieUtil.CookieHeaderToDictionary($"mam_id={Settings.MamId}");
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            UpdateCookies(null, null);

            _logger.Debug("Cookies cleared.");

            await base.Test(failures).ConfigureAwait(false);
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
        private static readonly Regex SanitizeSearchQueryRegex = new ("[^\\w]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilities _capabilities;
        private readonly Logger _logger;

        public MyAnonamouseRequestGenerator(MyAnonamouseSettings settings, IndexerCapabilities capabilities, Logger logger)
        {
            _settings = settings;
            _capabilities = capabilities;
            _logger = logger;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria)
        {
            var term = SanitizeSearchQueryRegex.Replace(searchCriteria.SanitizedSearchTerm, " ").Trim();

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && term.IsNullOrWhiteSpace())
            {
                _logger.Debug("Search term is empty after being sanitized, stopping search. Initial search term: '{0}'", searchCriteria.SearchTerm);

                yield break;
            }

            var searchType = _settings.SearchType switch
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

            if (_settings.SearchInDescription)
            {
                parameters.Set("tor[srchIn][description]", "true");
            }

            if (_settings.SearchInSeries)
            {
                parameters.Set("tor[srchIn][series]", "true");
            }

            if (_settings.SearchInFilenames)
            {
                parameters.Set("tor[srchIn][filenames]", "true");
            }

            if (_settings.SearchLanguages.Any())
            {
                foreach (var (language, index) in _settings.SearchLanguages.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[browse_lang][{index}]", language.ToString());
                }
            }

            var catList = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Distinct().ToList();

            if (catList.Any())
            {
                foreach (var (category, index) in catList.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[cat][{index}]", category);
                }
            }
            else
            {
                parameters.Set("tor[cat][]", "0");
            }

            if (searchCriteria.MinSize is > 0)
            {
                parameters.Set("tor[minSize]", searchCriteria.MinSize.Value.ToString());
            }

            if (searchCriteria.MaxSize is > 0)
            {
                parameters.Set("tor[maxSize]", searchCriteria.MaxSize.Value.ToString());
            }

            if (searchCriteria.MinSize is > 0 || searchCriteria.MaxSize is > 0)
            {
                parameters.Set("tor[unit]", "1");
            }

            var searchUrl = _settings.BaseUrl + "tor/js/loadSearchJSONbasic.php";

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
        private readonly ProviderDefinition _definition;
        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly IIndexerHttpClient _httpClient;
        private readonly Logger _logger;

        private readonly ICached<string> _userClassCache;
        private readonly HashSet<string> _vipFreeleechUserClasses = new (StringComparer.OrdinalIgnoreCase)
        {
            "VIP",
            "Elite VIP"
        };

        public MyAnonamouseParser(ProviderDefinition definition,
            MyAnonamouseSettings settings,
            IndexerCapabilitiesCategories categories,
            IIndexerHttpClient httpClient,
            ICacheManager cacheManager,
            Logger logger)
        {
            _definition = definition;
            _settings = settings;
            _categories = categories;
            _httpClient = httpClient;
            _logger = logger;

            _userClassCache = cacheManager.GetCache<string>(GetType());
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var httpResponse = indexerResponse.HttpResponse;

            // Throw auth errors here before we try to parse
            if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new IndexerAuthException("[403 Forbidden] - mam_id expired or invalid");
            }

            // Throw common http errors here before we try to parse
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response status {httpResponse.StatusCode} code from indexer request");
            }

            if (!httpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response header {httpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseResponse>(indexerResponse.Content);

            var error = jsonResponse.Error;
            if (error.IsNotNullOrWhiteSpace() && error.StartsWithIgnoreCase("Nothing returned, out of"))
            {
                return releaseInfos.ToArray();
            }

            if (jsonResponse.Data == null)
            {
                throw new IndexerException(indexerResponse, "Unexpected response content from indexer request: {0}", jsonResponse.Message ?? "Check the logs for more information.");
            }

            var hasUserVip = HasUserVip(httpResponse.GetCookies());

            foreach (var item in jsonResponse.Data)
            {
                //TODO shift to ReleaseInfo object initializer for consistency
                var release = new TorrentInfo();

                var id = item.Id;

                release.Title = item.Title;
                release.Description = item.Description;

                release.BookTitle = item.Title;

                if (item.AuthorInfo != null)
                {
                    var authorInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.AuthorInfo);
                    var author = authorInfo?.Take(5).Select(v => v.Value).Join(", ");

                    if (author.IsNotNullOrWhiteSpace())
                    {
                        release.Title += " by " + author;
                        release.Author = author;
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

                var isFreeLeech = item.Free || item.PersonalFreeLeech || (hasUserVip && item.FreeVip);

                release.DownloadUrl = GetDownloadUrl(id, !isFreeLeech);
                release.InfoUrl = $"{_settings.BaseUrl}t/{id}";
                release.Guid = release.InfoUrl;
                release.Categories = _categories.MapTrackerCatToNewznab(item.Category);
                release.PublishDate = DateTime.ParseExact(item.Added, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                release.Grabs = item.Grabs;
                release.Files = item.NumFiles;
                release.Seeders = item.Seeders;
                release.Peers = item.Leechers + release.Seeders;
                release.Size = ParseUtil.GetBytes(item.Size);
                release.DownloadVolumeFactor = isFreeLeech ? 0 : 1;
                release.UploadVolumeFactor = 1;
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 259200; // 72 hours

                releaseInfos.Add(release);
            }

            // Update cookies with the updated mam_id value received in the response
            CookiesUpdater(httpResponse.GetCookies(), DateTime.Now.AddDays(30));

            return releaseInfos.ToArray();
        }

        private string GetDownloadUrl(int torrentId, bool canUseToken)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/tor/download.php")
                .AddQueryParam("tid", torrentId);

            if (_settings.UseFreeleechWedge is (int)MyAnonamouseFreeleechWedgeAction.Preferred or (int)MyAnonamouseFreeleechWedgeAction.Required && canUseToken)
            {
                url = url.AddQueryParam("canUseToken", "true");
            }

            return url.FullUri;
        }

        private bool HasUserVip(Dictionary<string, string> cookies)
        {
            var cacheKey = "myanonamouse_user_class_" + _settings.ToJson().SHA256Hash();

            var userClass = _userClassCache.Get(
                cacheKey,
                () =>
                {
                    var request = new HttpRequestBuilder(_settings.BaseUrl.Trim('/'))
                        .Resource("/jsonLoad.php")
                        .Accept(HttpAccept.Json)
                        .SetCookies(cookies)
                        .Build();

                    _logger.Debug("Fetching user data: {0}", request.Url.FullUri);

                    var response = _httpClient.ExecuteProxied(request, _definition);
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
            SearchLanguages = Array.Empty<int>();
            UseFreeleechWedge = (int)MyAnonamouseFreeleechWedgeAction.Never;
        }

        [FieldDefinition(2, Type = FieldType.Textbox, Label = "Mam Id", HelpText = "Mam Session Id (Created Under Preferences -> Security)")]
        public string MamId { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, Label = "Search Type", SelectOptions = typeof(MyAnonamouseSearchType), HelpText = "Specify the desired search type")]
        public int SearchType { get; set; }

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Search in description", HelpText = "Search text in the description")]
        public bool SearchInDescription { get; set; }

        [FieldDefinition(5, Type = FieldType.Checkbox, Label = "Search in series", HelpText = "Search text in the series")]
        public bool SearchInSeries { get; set; }

        [FieldDefinition(6, Type = FieldType.Checkbox, Label = "Search in filenames", HelpText = "Search text in the filenames")]
        public bool SearchInFilenames { get; set; }

        [FieldDefinition(7, Type = FieldType.Select, Label = "Search Languages", SelectOptions = typeof(MyAnonamouseSearchLanguages), HelpText = "Specify the desired languages. If unspecified, all options are used.")]
        public IEnumerable<int> SearchLanguages { get; set; }

        [FieldDefinition(8, Type = FieldType.Select, Label = "Use Freeleech Wedges", SelectOptions = typeof(MyAnonamouseFreeleechWedgeAction), HelpText = "Use freeleech wedges to make grabbed torrents personal freeleech")]
        public int UseFreeleechWedge { get; set; }

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

    public enum MyAnonamouseSearchLanguages
    {
        [FieldOption(Label="English")]
        English = 1,

        [FieldOption(Label="Afrikaans")]
        Afrikaans = 17,

        [FieldOption(Label="Arabic")]
        Arabic = 32,

        [FieldOption(Label="Bengali")]
        Bengali = 35,

        [FieldOption(Label="Bosnian")]
        Bosnian = 51,

        [FieldOption(Label="Bulgarian")]
        Bulgarian = 18,

        [FieldOption(Label="Burmese")]
        Burmese = 6,

        [FieldOption(Label="Cantonese")]
        Cantonese = 44,

        [FieldOption(Label="Catalan")]
        Catalan = 19,

        [FieldOption(Label="Chinese")]
        Chinese = 2,

        [FieldOption(Label="Croatian")]
        Croatian = 49,

        [FieldOption(Label="Czech")]
        Czech = 20,

        [FieldOption(Label="Danish")]
        Danish = 21,

        [FieldOption(Label="Dutch")]
        Dutch = 22,

        [FieldOption(Label="Estonian")]
        Estonian = 61,

        [FieldOption(Label="Farsi")]
        Farsi = 39,

        [FieldOption(Label="Finnish")]
        Finnish = 23,

        [FieldOption(Label="French")]
        French = 36,

        [FieldOption(Label="German")]
        German = 37,

        [FieldOption(Label="Greek")]
        Greek = 26,

        [FieldOption(Label="Greek, Ancient")]
        GreekAncient = 59,

        [FieldOption(Label="Gujarati")]
        Gujarati = 3,

        [FieldOption(Label="Hebrew")]
        Hebrew = 27,

        [FieldOption(Label="Hindi")]
        Hindi = 8,

        [FieldOption(Label="Hungarian")]
        Hungarian = 28,

        [FieldOption(Label="Icelandic")]
        Icelandic = 63,

        [FieldOption(Label="Indonesian")]
        Indonesian = 53,

        [FieldOption(Label="Irish")]
        Irish = 56,

        [FieldOption(Label="Italian")]
        Italian = 43,

        [FieldOption(Label="Japanese")]
        Japanese = 38,

        [FieldOption(Label="Javanese")]
        Javanese = 12,

        [FieldOption(Label="Kannada")]
        Kannada = 5,

        [FieldOption(Label="Korean")]
        Korean = 41,

        [FieldOption(Label="Lithuanian")]
        Lithuanian = 50,

        [FieldOption(Label="Latin")]
        Latin = 46,

        [FieldOption(Label="Latvian")]
        Latvian = 62,

        [FieldOption(Label="Malay")]
        Malay = 33,

        [FieldOption(Label="Malayalam")]
        Malayalam = 58,

        [FieldOption(Label="Manx")]
        Manx = 57,

        [FieldOption(Label="Marathi")]
        Marathi = 9,

        [FieldOption(Label="Norwegian")]
        Norwegian = 48,

        [FieldOption(Label="Polish")]
        Polish = 45,

        [FieldOption(Label="Portuguese")]
        Portuguese = 34,

        [FieldOption(Label="Brazilian Portuguese (BP)")]
        BrazilianPortuguese = 52,

        [FieldOption(Label="Punjabi")]
        Punjabi = 14,

        [FieldOption(Label="Romanian")]
        Romanian = 30,

        [FieldOption(Label="Russian")]
        Russian = 16,

        [FieldOption(Label="Scottish Gaelic")]
        ScottishGaelic = 24,

        [FieldOption(Label="Sanskrit")]
        Sanskrit = 60,

        [FieldOption(Label="Serbian")]
        Serbian = 31,

        [FieldOption(Label="Slovenian")]
        Slovenian = 54,

        [FieldOption(Label="Spanish")]
        Spanish = 4,

        [FieldOption(Label="Castilian Spanish")]
        CastilianSpanish = 55,

        [FieldOption(Label="Swedish")]
        Swedish = 40,

        [FieldOption(Label="Tagalog")]
        Tagalog = 29,

        [FieldOption(Label="Tamil")]
        Tamil = 11,

        [FieldOption(Label="Telugu")]
        Telugu = 10,

        [FieldOption(Label="Thai")]
        Thai = 7,

        [FieldOption(Label="Turkish")]
        Turkish = 42,

        [FieldOption(Label="Ukrainian")]
        Ukrainian = 25,

        [FieldOption(Label="Urdu")]
        Urdu = 15,

        [FieldOption(Label="Vietnamese")]
        Vietnamese = 13,

        [FieldOption(Label="Other")]
        Other = 47,
    }

    public enum MyAnonamouseFreeleechWedgeAction
    {
        [FieldOption(Label = "Never", Hint = "Do not buy as freeleech")]
        Never = 0,

        [FieldOption(Label = "Preferred", Hint = "Buy and use wedge if possible")]
        Preferred = 1,

        [FieldOption(Label = "Required", Hint = "Abort download if unable to buy wedge")]
        Required = 2,
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
        [JsonProperty(PropertyName = "personal_freeleech")]
        public bool PersonalFreeLeech { get; set; }
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
        public IReadOnlyCollection<MyAnonamouseTorrent> Data { get; set; }
        public string Message { get; set; }
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
