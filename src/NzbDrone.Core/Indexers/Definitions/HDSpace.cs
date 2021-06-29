using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class HDSpace : TorrentIndexerBase<HDSpaceSettings>
    {
        public override string Name => "HD-Space";
        public override string[] IndexerUrls => new string[] { "https://hd-space.org/" };
        private string LoginUrl => Settings.BaseUrl + "index.php?page=login";
        public override string Description => "Sharing The Universe";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public HDSpace(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDSpaceRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDSpaceParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var loginPage = await _httpClient.ExecuteAsync(new HttpRequest(LoginUrl));

            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;

            var authLoginRequest = requestBuilder
                .AddFormParameter("uid", Settings.Username)
                .AddFormParameter("pwd", Settings.Password)
                .SetCookies(loginPage.GetCookies())
                .SetHeader("Content-Type", "multipart/form-data")
                .SetHeader("Referer", LoginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var errorStr = "Login Failed: You have {0} remaining login attempts";
                var remainingAttemptSpan = new Regex(string.Format(errorStr, "(.*?)"))
                                           .Match(loginPage.Content).Groups[1].ToString();
                var attempts = Regex.Replace(remainingAttemptSpan, "<.*?>", string.Empty);
                var errorMessage = string.Format(errorStr, attempts);

                throw new IndexerAuthException(errorMessage);
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("HDSpace authentication succeeded.");
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
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                                   },
                MovieSearchParams = new List<MovieSearchParam>
                                   {
                                       MovieSearchParam.Q, MovieSearchParam.ImdbId
                                   },
                MusicSearchParams = new List<MusicSearchParam>
                                   {
                                       MusicSearchParam.Q
                                   }
            };

            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.MoviesBluRay, "Movie / Blu-ray");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.MoviesHD, "Movie / 1080p");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.MoviesHD, "Movie / 720p");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.MoviesHD, "Movie / Remux");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.MoviesHD, "Movie / HD-DVD");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.MoviesUHD, "Movie / 4K UHD");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.TVHD, "TV Show / 720p HDTV");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.TVHD, "TV Show / 1080p HDTV");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVDocumentary, "Documentary / 720p");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.TVDocumentary, "Documentary / 1080p");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.TVAnime, "Animation / 720p");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.TVAnime, "Animation / 1080p");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.AudioLossless, "Music / HQ Audio");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.AudioVideo, "Music / Videos");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.XXX, "XXX / 720p");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.XXX, "XXX / 1080p");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.MoviesOther, "Trailers");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.PC, "Software");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Other, "Others");

            return caps;
        }
    }

    public class HDSpaceRequestGenerator : IIndexerRequestGenerator
    {
        public HDSpaceSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public HDSpaceRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdb = null)
        {
            var searchUrl = string.Format("{0}/index.php?page=torrents&", Settings.BaseUrl.TrimEnd('/'));

            var queryCollection = new NameValueCollection
            {
                { "active", "0" },
                { "category", string.Join(";", Capabilities.Categories.MapTorznabCapsToTrackers(categories)) }
            };

            if (imdb != null)
            {
                queryCollection.Add("options", "2");
                queryCollection.Add("search", imdb);
            }
            else
            {
                queryCollection.Add("options", "0");
                queryCollection.Add("search", term);
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

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
            return new IndexerPageableRequestChain();
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

    public class HDSpaceParser : IParseIndexerResponse
    {
        private readonly HDSpaceSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public HDSpaceParser(HDSpaceSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var resultParser = new HtmlParser();
            var searchResultDocument = resultParser.ParseDocument(indexerResponse.Content);
            var rows = searchResultDocument.QuerySelectorAll("table.lista > tbody > tr");

            foreach (var row in rows)
            {
                // this tracker has horrible markup, find the result rows by looking for the style tag before each one
                var prev = row.PreviousElementSibling;
                if (prev == null || !string.Equals(prev.NodeName, "style", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var release = new TorrentInfo();
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 86400; // 24 hours

                var qLink = row.Children[1].FirstElementChild;
                release.Title = qLink.TextContent.Trim();
                release.InfoUrl = _settings.BaseUrl + qLink.GetAttribute("href");
                release.Guid = release.InfoUrl;

                var imdbLink = row.Children[1].QuerySelector("a[href*=imdb]");
                if (imdbLink != null)
                {
                    release.ImdbId = ParseUtil.GetImdbID(imdbLink.GetAttribute("href").Split('/').Last()).GetValueOrDefault();
                }

                var qDownload = row.Children[3].FirstElementChild;
                release.DownloadUrl = _settings.BaseUrl + qDownload.GetAttribute("href");

                var dateStr = row.Children[4].TextContent.Trim();

                //"July 11, 2015, 13:34:09", "Today|Yesterday at 20:04:23"
                release.PublishDate = DateTimeUtil.FromUnknown(dateStr);
                var sizeStr = row.Children[5].TextContent;
                release.Size = ReleaseInfo.GetBytes(sizeStr);
                release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent) + release.Seeders;
                var grabs = row.QuerySelector("td:nth-child(10)").TextContent;
                grabs = grabs.Replace("---", "0");
                release.Grabs = ParseUtil.CoerceInt(grabs);

                if (row.QuerySelector("img[title=\"FreeLeech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[src=\"images/sf.png\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[title=\"Half FreeLeech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                release.UploadVolumeFactor = 1;
                var qCat = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]");
                var cat = qCat.GetAttribute("href").Split('=')[2];
                release.Categories = _categories.MapTrackerCatToNewznab(cat);
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class HDSpaceSettingsValidator : AbstractValidator<HDSpaceSettings>
    {
        public HDSpaceSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class HDSpaceSettings : IIndexerSettings
    {
        private static readonly HDSpaceSettingsValidator Validator = new HDSpaceSettingsValidator();

        public HDSpaceSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", Privacy = PrivacyLevel.UserName, HelpText = "Site Username")]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password, HelpText = "Site Password")]
        public string Password { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
