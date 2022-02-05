using System;
using System.Collections.Generic;
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
    public class PornoLab : TorrentIndexerBase<PornoLabSettings>
    {
        public override string Name => "PornoLab";
        private string LoginUrl => Settings.BaseUrl + "forum/login.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public PornoLab(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PornoLabRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PornoLabParser(Settings, Capabilities.Categories, _logger);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .AddFormParameter("login_username", Settings.Username)
                .AddFormParameter("login_password", Settings.Password)
                .AddFormParameter("login", "Login")
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var errorMessage = "Unknown error message, please report";
                var loginResultParser = new HtmlParser();
                var loginResultDocument = loginResultParser.ParseDocument(response.Content);
                var errormsg = loginResultDocument.QuerySelector("h4[class=\"warnColor1 tCenter mrg_16\"]");
                if (errormsg != null)
                {
                    errorMessage = errormsg.TextContent;
                }

                throw new IndexerAuthException(errorMessage);
            }

            UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("PornoLab authentication succeeded");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("Вы зашли как:"))
            {
                return true;
            }

            return false;
        }
    }

    public class PornoLabRequestGenerator : IIndexerRequestGenerator
    {
        public PornoLabSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public PornoLabRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/forum/tracker.php", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            // NameValueCollection don't support cat[]=19&cat[]=6
            var qc = new List<KeyValuePair<string, string>>
            {
                { "o", "1" },
                { "s", "2" }
            };

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                qc.Add("nm", searchString);
            }
            else
            {
                // use the normal search
                searchString = searchString.Replace("-", " ");
                qc.Add("nm", searchString);
            }

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                qc.Add("f[]", cat);
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

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

    public class PornoLabParser : IParseIndexerResponse
    {
        private readonly PornoLabSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly Logger _logger;
        private static readonly Regex StripRussianRegex = new Regex(@"(\([А-Яа-яЁё\W]+\))|(^[А-Яа-яЁё\W\d]+\/ )|([а-яА-ЯЁё \-]+,+)|([а-яА-ЯЁё]+)");

        public PornoLabParser(PornoLabSettings settings, IndexerCapabilitiesCategories categories, Logger logger)
        {
            _settings = settings;
            _categories = categories;
            _logger = logger;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var rowsSelector = "table#tor-tbl > tbody > tr";

            var searchResultParser = new HtmlParser();
            var searchResultDocument = searchResultParser.ParseDocument(indexerResponse.Content);
            var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
            foreach (var row in rows)
            {
                try
                {
                    var qDownloadLink = row.QuerySelector("a.tr-dl");

                    // Expects moderation
                    if (qDownloadLink == null)
                    {
                        continue;
                    }

                    var qForumLink = row.QuerySelector("a.f");
                    var qDetailsLink = row.QuerySelector("a.tLink");
                    var qSize = row.QuerySelector("td:nth-child(6) u");
                    var link = new Uri(_settings.BaseUrl + "forum/" + qDetailsLink.GetAttribute("href"));
                    var seederString = row.QuerySelector("td:nth-child(7) b").TextContent;
                    var seeders = string.IsNullOrWhiteSpace(seederString) ? 0 : ParseUtil.CoerceInt(seederString);

                    var timestr = row.QuerySelector("td:nth-child(11) u").TextContent;
                    var forum = qForumLink;
                    var forumid = forum.GetAttribute("href").Split('=')[1];
                    var title = _settings.StripRussianLetters
                        ? StripRussianRegex.Replace(qDetailsLink.TextContent, "")
                        : qDetailsLink.TextContent;
                    var size = ParseUtil.GetBytes(qSize.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);
                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));
                    var release = new TorrentInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        Title = title,
                        InfoUrl = link.AbsoluteUri,
                        Description = qForumLink.TextContent,
                        DownloadUrl = link.AbsoluteUri,
                        Guid = link.AbsoluteUri,
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Grabs = grabs,
                        PublishDate = publishDate,
                        Categories = _categories.MapTrackerCatToNewznab(forumid),
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = 1
                    };

                    torrentInfos.Add(release);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Pornolab: Error while parsing row '{0}':\n\n{1}", row.OuterHtml, ex));
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class PornoLabSettings : UserPassTorrentBaseSettings
    {
        public PornoLabSettings()
        {
        }

        [FieldDefinition(4, Label = "Strip Russian Letters", HelpLink = "Strip Cyrillic letters from release names", Type = FieldType.Checkbox)]
        public bool StripRussianLetters { get; set; }
    }
}
