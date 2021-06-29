using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class Anthelion : TorrentIndexerBase<AnthelionSettings>
    {
        public override string Name => "Anthelion";
        public override string[] IndexerUrls => new string[] { "https://anthelion.me/" };
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override string Description => "A movies tracker";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Anthelion(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnthelionRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnthelionParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
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
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("keeplogged", "1")
                .AddFormParameter("login", "Log+In!")
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var headers = new NameValueCollection
            {
                { "Referer", LoginUrl }
            };

            authLoginRequest.Headers.Add(headers);

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var errorMessage = dom.QuerySelector("form#loginform").TextContent.Trim();

                throw new IndexerAuthException(errorMessage);
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("Anthelion authentication succeeded.");
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
                                       MovieSearchParam.Q
                                   }
            };

            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.Movies, "Film/Feature");
            caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.Movies, "Film/Short");
            caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.TV, "TV/Miniseries");
            caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.Other, "Other");

            return caps;
        }
    }

    public class AnthelionRequestGenerator : IIndexerRequestGenerator
    {
        public AnthelionSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public AnthelionRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            // TODO: IMDB search is available but it requires to parse the details page
            var qc = new NameValueCollection
            {
                { "order_by", "time" },
                { "order_way", "desc" },
                { "action", "basic" },
                { "searchsubmit", "1" },
                { "searchstr", imdbId.IsNotNullOrWhiteSpace() ? imdbId : term }
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            foreach (var cat in catList)
            {
                qc.Add($"filter_cat[{cat}]", "1");
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public class AnthelionParser : IParseIndexerResponse
    {
        private readonly AnthelionSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnthelionParser(AnthelionSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table.torrent_table > tbody > tr.torrent");
            foreach (var row in rows)
            {
                var qDetailsLink = row.QuerySelector("a.torrent_name");
                var year = qDetailsLink.NextSibling.TextContent.Replace("[", "").Replace("]", "").Trim();
                var tags = row.QuerySelector("div.torrent_info").FirstChild.TextContent.Replace(" / ", " ").Trim();
                var title = $"{qDetailsLink.TextContent} {year} {tags}";
                var description = row.QuerySelector("div.tags").TextContent.Trim();
                var details = _settings.BaseUrl + qDetailsLink.GetAttribute("href");
                var torrentId = qDetailsLink.GetAttribute("href").Split('=').Last();
                var link = _settings.BaseUrl + "torrents.php?action=download&id=" + torrentId;
                var posterStr = qDetailsLink.GetAttribute("data-cover");
                var poster = !string.IsNullOrWhiteSpace(posterStr) ? new Uri(qDetailsLink.GetAttribute("data-cover")) : null;

                var files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(3)").TextContent);
                var publishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(4)").TextContent);
                var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(5)").FirstChild.TextContent);
                var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent);
                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

                var dlVolumeFactor = row.QuerySelector("strong.tl_free") != null ? 0 : 1;

                var cat = row.QuerySelector("td.cats_col > div").GetAttribute("class").Replace("tooltip cats_", "");
                var category = new List<IndexerCategory>
                {
                    cat switch
                    {
                        "featurefilm" => NewznabStandardCategory.Movies,
                        "shortfilm" => NewznabStandardCategory.Movies,
                        "miniseries" => NewznabStandardCategory.TV,
                        "other" => NewznabStandardCategory.Other,
                        _ => throw new Exception($"Unknown category: {cat}")
                    }
                };

                // TODO: TMDb is also available
                var qImdb = row.QuerySelector("a[href^=\"https://www.imdb.com\"]");
                var imdb = qImdb != null ? ParseUtil.GetImdbID(qImdb.GetAttribute("href").Split('/').Last()) : null;

                var release = new TorrentInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200,
                    Description = description,
                    Title = title,
                    PublishDate = publishDate,
                    Categories = category,
                    DownloadUrl = link,
                    InfoUrl = details,
                    Guid = link,
                    ImdbId = imdb.GetValueOrDefault(),
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    Size = size,
                    Grabs = grabs,
                    Files = files,
                    DownloadVolumeFactor = dlVolumeFactor,
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnthelionSettingsValidator : AbstractValidator<AnthelionSettings>
    {
        public AnthelionSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class AnthelionSettings : IIndexerSettings
    {
        private static readonly AnthelionSettingsValidator Validator = new AnthelionSettingsValidator();

        public AnthelionSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
