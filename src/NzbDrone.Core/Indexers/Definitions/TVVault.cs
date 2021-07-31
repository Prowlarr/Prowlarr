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
using NzbDrone.Common.Extensions;
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
    public class TVVault : TorrentIndexerBase<TVVaultSettings>
    {
        public override string Name => "TVVault";
        public override string[] IndexerUrls => new[] { "https://tv-vault.me/" };
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override string Description => "TV-Vault is a very unique tracker dedicated for old TV shows, TV movies and documentaries.";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TVVault(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TVVaultRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TVVaultParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            requestBuilder.Method = HttpMethod.POST;

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

            _logger.Debug("TVVault authentication succeeded.");
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
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.FreeLeech
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Movies);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVSD);

            return caps;
        }
    }

    public class TVVaultRequestGenerator : IIndexerRequestGenerator
    {
        public TVVaultSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public TVVaultRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            var qc = new NameValueCollection
            {
                { "order_by", "s3" },
                { "order_way", "DESC" },
                { "disablegrouping", "1" }
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("action", "advanced");
                qc.Add("imdbid", imdbId);
            }
            else if (!string.IsNullOrWhiteSpace(term))
            {
                qc.Add("searchstr", StripSearchString(term));
            }

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
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

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

        private string StripSearchString(string term)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // AND filter the result later to achieve the proper result
            term = Regex.Replace(term, @"[S|E]\d\d", string.Empty);
            return term.Trim();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TVVaultParser : IParseIndexerResponse
    {
        private readonly TVVaultSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TVVaultParser(TVVaultSettings settings, IndexerCapabilitiesCategories categories)
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
                var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                var title = qDetailsLink.TextContent;

                var description = qDetailsLink.NextSibling.TextContent.Trim();
                title += " " + description;
                var details = _settings.BaseUrl + qDetailsLink.GetAttribute("href");
                var torrentId = qDetailsLink.GetAttribute("href").Split('=').Last();
                var link = _settings.BaseUrl + "torrents.php?action=download&id=" + torrentId;

                var files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(3)").TextContent);
                var publishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(4)").TextContent);
                var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(5)").FirstChild.TextContent);
                var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent);
                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

                var dlVolumeFactor = row.QuerySelector("strong.freeleech_normal") != null ? 0 : 1;

                var category = new List<IndexerCategory> { TvCategoryFromQualityParser.ParseTvShowQuality(description) };

                var release = new TorrentInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 0,
                    Description = description,
                    Title = title,
                    PublishDate = publishDate,
                    Categories = category,
                    DownloadUrl = link,
                    InfoUrl = details,
                    Guid = link,
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

    public class TVVaultSettingsValidator : AbstractValidator<TVVaultSettings>
    {
        public TVVaultSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class TVVaultSettings : IIndexerSettings
    {
        private static readonly TVVaultSettingsValidator Validator = new TVVaultSettingsValidator();

        public TVVaultSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
