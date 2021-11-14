using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentValidation;
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
    public class NorBits : TorrentIndexerBase<NorBitsSettings>
    {
        public override string Name => "NorBits";
        public override string[] IndexerUrls => new string[] { "https://norbits.net/" };
        public override string Description => "NorBits is a Norwegian Private site for MOVIES / TV / GENERAL";
        public override string Language => "nb-NO";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public NorBits(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NorBitsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NorBitsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var indexPage = await ExecuteAuth(requestBuilder.Build());

            var loginUrl = string.Format("{0}/{1}", Settings.BaseUrl.TrimEnd('/'), "login.php");

            var requestBuilder2 = new HttpRequestBuilder(loginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var authLoginRequest = requestBuilder2
                .SetCookies(indexPage.GetCookies())
                .Build();

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            await ExecuteAuth(authLoginRequest);

            var requestBuilder3 = new HttpRequestBuilder(string.Format("{0}/{1}", Settings.BaseUrl.TrimEnd('/'), "takelogin.php"))
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.POST
            };

            var authLoginCheckRequest = requestBuilder3
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetCookies(indexPage.GetCookies())
                .SetHeader("Referer", loginUrl)
                .Build();

            var loginResponse = await ExecuteAuth(authLoginCheckRequest);

            if (!loginResponse.GetCookies().ContainsKey("uid"))
            {
                // Default error message
                var message = "Error during attempt !";

                // Oops, unable to login
                _logger.Info("NorBits - Login failed: " + message, "error");

                throw new IndexerAuthException(message);
            }

            var cookies = loginResponse.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("NorBits authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("logout.php"))
            {
                return true;
            }

            return false;
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

            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=49", NewznabStandardCategory.MoviesUHD, "Filmer - UHD-2160p");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=19", NewznabStandardCategory.MoviesHD, "Filmer - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=20", NewznabStandardCategory.MoviesHD, "Filmer - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=22", NewznabStandardCategory.MoviesSD, "Filmer - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=49", NewznabStandardCategory.TVUHD, "TV - UHD-2160p");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=19", NewznabStandardCategory.TVHD, "TV - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=20", NewznabStandardCategory.TVHD, "TV - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=22", NewznabStandardCategory.TVSD, "TV - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=3", NewznabStandardCategory.PC, "Programmer");
            caps.Categories.AddCategoryMapping("main_cat[]=4", NewznabStandardCategory.Console, "Spill");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=42", NewznabStandardCategory.AudioMP3, "Musikk - 192");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=43", NewznabStandardCategory.AudioMP3, "Musikk - 256");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=44", NewznabStandardCategory.AudioMP3, "Musikk - 320");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=45", NewznabStandardCategory.AudioMP3, "Musikk - VBR");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=46", NewznabStandardCategory.AudioLossless, "Musikk - Lossless");
            caps.Categories.AddCategoryMapping("main_cat[]=6", NewznabStandardCategory.Books, "Tidsskrift");
            caps.Categories.AddCategoryMapping("main_cat[]=7", NewznabStandardCategory.AudioAudiobook, "Lydbøker");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=19", NewznabStandardCategory.AudioVideo, "Musikkvideoer - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=20", NewznabStandardCategory.AudioVideo, "Musikkvideoer - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=22", NewznabStandardCategory.AudioVideo, "Musikkvideoer - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=40", NewznabStandardCategory.AudioOther, "Podcasts");

            return caps;
        }
    }

    public class NorBitsRequestGenerator : IIndexerRequestGenerator
    {
        public NorBitsSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public NorBitsRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/browse.php", Settings.BaseUrl.TrimEnd('/'));

            var parameters = new NameValueCollection();
            var categoriesList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            var searchterm = term;

            // Building our tracker query
            parameters.Add("incldead", "1");
            parameters.Add("fullsearch", Settings.UseFullSearch ? "1" : "0");
            parameters.Add("scenerelease", "0");

            // If search term provided
            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                searchterm = "imdbsearch=" + imdbId;
            }
            else if (!string.IsNullOrWhiteSpace(term))
            {
                searchterm = "search=" + term.UrlEncode(Encoding.GetEncoding(28591));
            }
            else
            {
                // Showing all torrents (just for output function)
                searchterm = "search=";
            }

            var catQryStr = "";

            foreach (var cat in categoriesList)
            {
                catQryStr += "&" + cat;
            }

            // Building our query
            searchUrl += "?" + searchterm + "&" + parameters.GetQueryString() + "&" + catQryStr;

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public class NorBitsParser : IParseIndexerResponse
    {
        private readonly NorBitsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public NorBitsParser(NorBitsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var firstPageRows = dom.QuerySelectorAll("#torrentTable > tbody > tr").Skip(1).ToCollection();

            // If pagination available
            int nbResults;

            // Check if we have a minimum of one result
            if (firstPageRows?.Length >= 1)
            {
                // Retrieve total count on our alone page
                nbResults = firstPageRows.Count();
            }
            else
            {
                // No result found for this query
                return torrentInfos;
            }

            var torrentDetailsUrl = _settings.BaseUrl + "details.php?id={id}";
            var torrentDownloadUrl = _settings.BaseUrl + "download.php?id={id}&passkey={passkey}";

            // Loop on results
            foreach (var row in firstPageRows)
            {
                var id = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").GetAttribute("href").Split('=').Last();                  // ID
                var name = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").GetAttribute("title");                                 // Release Name
                var categoryName = row.QuerySelector("td:nth-of-type(1) > div > a:nth-of-type(1)").GetAttribute("title");                   // Category
                var mainCat = row.QuerySelector("td:nth-of-type(1) > div > a:nth-of-type(1)").GetAttribute("href").Split('?').Last();
                var qSubCat2 = row.QuerySelector("td:nth-of-type(1) > div > a[href^=\"/browse.php?sub2_cat[]=\"]");
                var cat = mainCat;
                if (qSubCat2 != null)
                {
                    cat += '&' + qSubCat2.GetAttribute("href").Split('?').Last();
                }

                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent);                                      // Seeders
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent);                                    // Leechers
                var regexObj = new Regex(@"[^\d]");                                                                                         // Completed
                var completed2 = row.QuerySelector("td:nth-of-type(8)").TextContent;
                var completed = ParseUtil.CoerceInt(regexObj.Replace(completed2, ""));
                var qFiles = row.QuerySelector("td:nth-of-type(3) > a");                                                                    // Files
                var files = qFiles != null ? ParseUtil.CoerceInt(Regex.Match(qFiles.TextContent, @"\d+").Value) : 1;
                var humanSize = row.QuerySelector("td:nth-of-type(7)").TextContent.ToLowerInvariant();                                      // Size
                var size = ParseUtil.GetBytes(humanSize);                                                                                 // Date
                var dateTimeOrig = row.QuerySelector("td:nth-of-type(5)").TextContent;
                var dateTime = Regex.Replace(dateTimeOrig, @"<[^>]+>|&nbsp;", "").Trim();
                var date = DateTime.ParseExact(dateTime, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                var details = new Uri(torrentDetailsUrl.Replace("{id}", id.ToString()));                                                    // Description Link
                var passkey = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(2)").GetAttribute("href");                               // Download Link
                var key = Regex.Match(passkey, "(?<=passkey\\=)([a-zA-z0-9]*)");
                var downloadLink = new Uri(torrentDownloadUrl.Replace("{id}", id.ToString()).Replace("{passkey}", key.ToString()));

                // Building release infos
                var release = new TorrentInfo
                {
                    Categories = _categories.MapTrackerCatToNewznab(cat),
                    Title = name,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    PublishDate = date,
                    Size = size,
                    Files = files,
                    Grabs = completed,
                    Guid = details.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    DownloadUrl = downloadLink.AbsoluteUri,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                var genres = row.QuerySelector("span.genres")?.TextContent;
                if (!string.IsNullOrEmpty(genres))
                {
                    release.Description = genres;
                }

                // IMDB
                var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                release.ImdbId = ParseUtil.GetImdbID(imdbLink) ?? 0;

                if (row.QuerySelector("img[title=\"100% freeleech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[title=\"Halfleech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else if (row.QuerySelector("img[title=\"90% Freeleech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.1;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                release.UploadVolumeFactor = 1;

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NorBitsSettingsValidator : AbstractValidator<NorBitsSettings>
    {
        public NorBitsSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class NorBitsSettings : IIndexerSettings
    {
        private static readonly NorBitsSettingsValidator Validator = new NorBitsSettingsValidator();

        public NorBitsSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Use Full Search", HelpText = "Use Full Search from Site", Type = FieldType.Checkbox)]
        public bool UseFullSearch { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
