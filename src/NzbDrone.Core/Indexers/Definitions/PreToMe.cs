using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
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
    public class PreToMe : TorrentIndexerBase<PreToMeSettings>
    {
        public override string Name => "PreToMe";
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public PreToMe(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PreToMeRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PreToMeParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var loginPage = await ExecuteAuth(new HttpRequest(Settings.BaseUrl + "login.php"));

            requestBuilder.Method = HttpMethod.Post;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(loginPage.GetCookies());

            var authLoginRequest = requestBuilder
                .AddFormParameter("returnto", "%2F")
                .AddFormParameter("login_pin", Settings.Pin)
                .AddFormParameter("login", "Login")
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && response.Content.Contains("logout.php"))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));
                _logger.Debug("PreToMe authentication succeeded");
            }
            else
            {
                throw new IndexerAuthException("PreToMe authentication failed");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.HasHttpRedirect || !httpResponse.Content.Contains("logout.php"))
            {
                return true;
            }

            return false;
        }
    }

    public class PreToMeRequestGenerator : IIndexerRequestGenerator
    {
        public PreToMeSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public PreToMeRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/browse.php", Settings.BaseUrl.TrimEnd('/'));

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "st", "1" } // search in title
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", imdbId);
                qc.Add("sd", "1"); // search in description
            }
            else
            {
                qc.Add("search", term);
            }

            // parse categories and tags
            var catGroups = new HashSet<string>(); // HashSet instead of List to avoid duplicates
            var tagGroups = new HashSet<string>();
            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in cats)
            {
                // "cat[]=7&tags=x264"
                var cSplit = cat.Split('&');

                var gSplit = cSplit[0].Split('=');
                if (gSplit.Length > 1)
                {
                    catGroups.Add(gSplit[1]); // category = 7
                }

                if (cSplit.Length > 1)
                {
                    var tSplit = cSplit[1].Split('=');
                    if (tSplit.Length > 1)
                    {
                        tagGroups.Add(tSplit[1]); // tag = x264
                    }
                }
            }

            // add categories
            foreach (var cat in catGroups)
            {
                qc.Add("cat[]", cat);
            }

            // do not include too many tags as it'll mess with their servers
            if (tagGroups.Count < 7)
            {
                qc.Add("tags", string.Join(",", tagGroups));

                // if tags are specified match any
                // if no tags are specified match all, with any we get random results
                qc.Add("tf", tagGroups.Any() ? "any" : "all");
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

    public class PreToMeParser : IParseIndexerResponse
    {
        private readonly PreToMeSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public PreToMeParser(PreToMeSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("table > tbody > tr.browse");
            foreach (var row in rows)
            {
                var qLink = row.Children[1].QuerySelector("a");
                var title = qLink.GetAttribute("title");
                if (qLink.QuerySelectorAll("span").Length == 1 && title.StartsWith("NEW! |"))
                {
                    title = title.Substring(6).Trim();
                }

                // TODO: Asses if we should be throwing this out
                //if (!query.MatchQueryStringAND(title))
                //{
                //    continue; // we have to skip bad titles due to tags + any word search
                //}
                var details = _settings.BaseUrl + qLink.GetAttribute("href");
                var link = _settings.BaseUrl + row.Children[2].QuerySelector("a").GetAttribute("href");
                var dateStr = Regex.Replace(row.Children[5].InnerHtml, @"\<br[\s]{0,1}[\/]{0,1}\>", " ");
                var publishDate = DateTimeUtil.FromTimeAgo(dateStr);
                var files = ParseUtil.CoerceInt(row.Children[3].TextContent);
                var size = ParseUtil.GetBytes(row.Children[7].TextContent);
                var grabs = ParseUtil.CoerceInt(row.Children[8].TextContent);
                var seeders = ParseUtil.CoerceInt(row.Children[9].TextContent);
                var leechers = ParseUtil.CoerceInt(row.Children[10].TextContent);
                var cat = row.FirstElementChild.FirstElementChild.GetAttribute("href").Replace("browse.php?", string.Empty);

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = details,
                    Guid = details,
                    DownloadUrl = link,
                    PublishDate = publishDate,
                    Size = size,
                    Categories = _categories.MapTrackerCatToNewznab(cat),
                    Files = files,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    MinimumRatio = 0.75,
                    MinimumSeedTime = 216000, // 60 hours
                    DownloadVolumeFactor = 0, // ratioless
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class PreToMeSettingsValidator : AbstractValidator<PreToMeSettings>
    {
        public PreToMeSettingsValidator()
        {
            RuleFor(c => c.Pin).NotEmpty();
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class PreToMeSettings : NoAuthTorrentBaseSettings
    {
        private static readonly PreToMeSettingsValidator Validator = new PreToMeSettingsValidator();

        public PreToMeSettings()
        {
            Pin = "";
            Username = "";
            Password = "";
        }

        [FieldDefinition(2, Label = "Pin", HelpText = "Site Pin", Privacy = PrivacyLevel.Password)]
        public string Pin { get; set; }

        [FieldDefinition(3, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(4, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
