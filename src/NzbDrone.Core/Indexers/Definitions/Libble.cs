using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    internal class Libble : TorrentIndexerBase<LibbleSettings>
    {
        public override string Name => "Libble";
        public override string[] IndexerUrls => new string[] { "https://libble.me/" };
        public override string Description => "Libble is a Private Torrent Tracker for MUSIC";
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Libble(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new LibbleRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new LibbleParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                Method = HttpMethod.Post,
                AllowAutoRedirect = true
            };

            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("code", Settings.TwoFactorAuthCode)
                .AddFormParameter("keeplogged", "1")
                .AddFormParameter("login", "Login")
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
                var errorMessage = dom.QuerySelector("#loginform > .warning")?.TextContent.Trim();

                throw new IndexerAuthException($"Libble authentication failed. Error: \"{errorMessage}\"");
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("Libble authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return !httpResponse.Content.Contains("logout.php");
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                    {
                        MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album, MusicSearchParam.Label, MusicSearchParam.Year, MusicSearchParam.Genre
                    }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.AudioVideo);

            return caps;
        }
    }

    public class LibbleRequestGenerator : IIndexerRequestGenerator
    {
        public LibbleSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public LibbleRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
        {
            var term = searchCriteria.SanitizedSearchTerm.Trim();

            parameters.Add("order_by", "time");
            parameters.Add("order_way", "desc");
            parameters.Add("searchstr", term);

            var queryCats = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (queryCats.Count > 0)
            {
                foreach (var cat in queryCats)
                {
                    parameters.Add($"filter_cat[{cat}]", "1");
                }
            }

            if (searchCriteria.Offset.HasValue && searchCriteria.Limit.HasValue && searchCriteria.Offset > 0 && searchCriteria.Limit > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                parameters.Add("page", page.ToString());
            }

            var searchUrl = string.Format("{0}/torrents.php?{1}", Settings.BaseUrl.TrimEnd('/'), parameters.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
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

            if (searchCriteria.Label.IsNotNullOrWhiteSpace())
            {
                parameters.Add("recordlabel", searchCriteria.Label);
            }

            if (searchCriteria.Year.HasValue)
            {
                parameters.Add("year", searchCriteria.Year.ToString());
            }

            if (searchCriteria.Genre.IsNotNullOrWhiteSpace())
            {
                parameters.Add("taglist", searchCriteria.Genre);
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

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

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class LibbleParser : IParseIndexerResponse
    {
        private readonly LibbleSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public LibbleParser(LibbleSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table#torrent_table > tbody > tr.group:has(strong > a[href*=\"torrents.php?id=\"])");

            var releaseYearRegex = new Regex(@"\[(\d{4})\]$");

            foreach (var row in rows)
            {
                var albumLinkNode = row.QuerySelector("strong > a[href*=\"torrents.php?id=\"]");
                var groupId = ParseUtil.GetArgumentFromQueryString(albumLinkNode.GetAttribute("href"), "id");

                var artistsNodes = row.QuerySelectorAll("strong > a[href*=\"artist.php?id=\"]");

                var releaseArtist = "Various Artists";
                if (artistsNodes.Count() > 0)
                {
                    releaseArtist = artistsNodes.Select(artist => artist.TextContent.Trim()).ToList().Join(", ");
                }

                var releaseAlbumName = row.QuerySelector("strong > a[href*=\"torrents.php?id=\"]")?.TextContent.Trim();

                var title = row.QuerySelector("td:nth-child(4) > strong")?.TextContent.Trim();
                var releaseAlbumYear = releaseYearRegex.Match(title);

                var releaseDescription = row.QuerySelector("div.tags")?.TextContent.Trim();
                var releaseThumbnailUrl = row.QuerySelector(".thumbnail")?.GetAttribute("title").Trim();

                var releaseGenres = new List<string>();
                if (!string.IsNullOrEmpty(releaseDescription))
                {
                    releaseGenres = releaseGenres.Union(releaseDescription.Split(',').Select(tag => tag.Trim()).ToList()).ToList();
                }

                var cat = row.QuerySelector("td.cats_col div.cat_icon")?.GetAttribute("class").Trim();

                var matchCategory = Regex.Match(cat, @"\bcats_(.*?)\b");
                if (matchCategory.Success)
                {
                    cat = matchCategory.Groups[1].Value.Trim();
                }

                var category = new List<IndexerCategory>
                {
                    cat switch
                    {
                        "music" => NewznabStandardCategory.Audio,
                        "libblemixtapes" => NewznabStandardCategory.Audio,
                        "musicvideos" => NewznabStandardCategory.AudioVideo,
                        _ => NewznabStandardCategory.Other,
                    }
                };

                var releaseRows = doc.QuerySelectorAll(string.Format("table#torrent_table > tbody > tr.group_torrent.groupid_{0}:has(a[href*=\"torrents.php?id=\"])", groupId));

                foreach (var releaseRow in releaseRows)
                {
                    var release = new TorrentInfo();

                    var detailsNode = releaseRow.QuerySelector("a[href^=\"torrents.php?id=\"]");
                    var downloadLink = _settings.BaseUrl + releaseRow.QuerySelector("a[href^=\"torrents.php?action=download&id=\"]").GetAttribute("href").Trim();

                    var releaseTags = detailsNode.FirstChild.TextContent.Trim(' ', '/');

                    release.Title = string.Format("{0} - {1} {2} {3}", releaseArtist, releaseAlbumName, releaseAlbumYear, releaseTags).Trim();
                    release.Categories = category;
                    release.Description = releaseDescription;
                    release.Genres = releaseGenres;
                    release.PosterUrl = releaseThumbnailUrl;

                    release.InfoUrl = _settings.BaseUrl + detailsNode.GetAttribute("href").Trim();
                    release.DownloadUrl = downloadLink;
                    release.Guid = release.InfoUrl;

                    release.Size = ParseUtil.GetBytes(releaseRow.QuerySelector("td:nth-child(4)").TextContent.Trim());
                    release.Files = ParseUtil.CoerceInt(releaseRow.QuerySelector("td:nth-child(2)").TextContent);
                    release.Grabs = ParseUtil.CoerceInt(releaseRow.QuerySelector("td:nth-child(5)").TextContent);
                    release.Seeders = ParseUtil.CoerceInt(releaseRow.QuerySelector("td:nth-child(6)").TextContent);
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(releaseRow.QuerySelector("td:nth-child(7)").TextContent);

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 259200; // 72 hours

                    try
                    {
                        release.PublishDate = DateTime.ParseExact(
                            releaseRow.QuerySelector("td:nth-child(3) > span[title]").GetAttribute("title").Trim(),
                            "MMM dd yyyy, HH:mm",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal);
                    }
                    catch (Exception)
                    {
                    }

                    switch (releaseRow.QuerySelector("a[href^=\"torrents.php?id=\"] strong")?.TextContent.Trim())
                    {
                        case "Neutral!":
                            release.DownloadVolumeFactor = 0;
                            release.UploadVolumeFactor = 0;
                            break;
                        case "Freeleech!":
                            release.DownloadVolumeFactor = 0;
                            release.UploadVolumeFactor = 1;
                            break;
                        default:
                            release.DownloadVolumeFactor = 1;
                            release.UploadVolumeFactor = 1;
                            break;
                    }

                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class LibbleSettings : UserPassTorrentBaseSettings
    {
        public LibbleSettings()
        {
            TwoFactorAuthCode = "";
        }

        [FieldDefinition(4, Label = "2FA code", Type = FieldType.Textbox, HelpText = "Only fill in the <b>2FA code</b> box if you have enabled <b>2FA</b> on the Libble Web Site. Otherwise just leave it empty.")]
        public string TwoFactorAuthCode { get; set; }
    }
}
