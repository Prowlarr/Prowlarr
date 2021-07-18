using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
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
    public class Animedia : TorrentIndexerBase<AnimediaSettings>
    {
        public override string Name => "Animedia";
        public override string[] IndexerUrls => new string[] { "https://tt.animedia.tv/" };
        public override string Description => "Animedia is russian anime voiceover group and eponymous anime tracker.";
        public override string Language => "ru-ru";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Animedia(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimediaRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimediaParser(Settings, Capabilities.Categories) { HttpClient = _httpClient, Logger = _logger };
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
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "TV Anime");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "OVA/ONA/Special");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TV, "Dorama");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Movies, "Movies");
            return caps;
        }
    }

    public class AnimediaRequestGenerator : IIndexerRequestGenerator
    {
        public AnimediaSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public AnimediaRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var requestUrl = string.Empty;

            if (string.IsNullOrWhiteSpace(term))
            {
                requestUrl = Settings.BaseUrl;
            }
            else
            {
                var queryCollection = new NameValueCollection
                {
                    // Remove season and episode info from search term cause it breaks search
                    { "keywords", Regex.Replace(term, @"(?:[SsEe]?\d{1,4}){1,2}$", "").TrimEnd() },
                    { "limit", "20" },
                    { "orderby_sort", "entry_date|desc" }
                };

                requestUrl = string.Format("{0}/ajax/search_result/P0?{1}", Settings.BaseUrl.TrimEnd('/'), queryCollection.GetQueryString());
            }

            var request = new IndexerRequest(requestUrl, HttpAccept.Html);
            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
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

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        // Animedia doesn't support music, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        // Animedia doesn't support books, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimediaParser : IParseIndexerResponse
    {
        private readonly AnimediaSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private static readonly Regex EpisodesInfoQueryRegex = new Regex(@"сери[ия] (\d+)(?:-(\d+))? из.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ResolutionInfoQueryRegex = new Regex(@"качество (\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SizeInfoQueryRegex = new Regex(@"размер:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ReleaseDateInfoQueryRegex = new Regex(@"добавлен:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieMovieRegex = new Regex(@"Фильм", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieOVARegex = new Regex(@"ОВА|OVA|ОНА|ONA|Special", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieDoramaRegex = new Regex(@"Дорама", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public IHttpClient HttpClient { get; set; }
        public Logger Logger { get; set; }

        public AnimediaParser(AnimediaSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        private string composeTitle(AngleSharp.Html.Dom.IHtmlDocument dom, AngleSharp.Dom.IElement t, AngleSharp.Dom.IElement tr)
        {
            var name_ru = dom.QuerySelector("div.media__post__header > h1").TextContent.Trim();
            var name_en = dom.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(1) > div > span").TextContent.Trim();
            var name_orig = dom.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(2) > div > span").TextContent.Trim();

            var title = name_ru + " / " + name_en;
            if (name_en != name_orig)
            {
                title += " / " + name_orig;
            }

            var tabName = t.TextContent;
            tabName = tabName.Replace("Сезон", "Season");
            if (tabName.Contains("Серии"))
            {
                tabName = "";
            }

            var heading = tr.QuerySelector("h3.tracker_info_bold").TextContent;

            // Parse episodes info from heading if episods info present
            var match = EpisodesInfoQueryRegex.Match(heading);
            heading = tabName;
            if (match.Success)
            {
                if (string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    heading += " E" + match.Groups[1].Value;
                }
                else
                {
                    heading += string.Format(" E{0}-{1}", match.Groups[1].Value, match.Groups[2].Value);
                }
            }

            return title + " - " + heading + " [" + getResolution(tr) + "p]";
        }

        private string getResolution(AngleSharp.Dom.IElement tr)
        {
            var resolution = tr.QuerySelector("div.tracker_info_left").TextContent;
            return ResolutionInfoQueryRegex.Match(resolution).Groups[1].Value;
        }

        private long getReleaseSize(AngleSharp.Dom.IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left").TextContent;
            return ReleaseInfo.GetBytes(SizeInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }

        private DateTime getReleaseDate(AngleSharp.Dom.IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left").TextContent;
            return DateTime.Parse(ReleaseDateInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }

        private ICollection<IndexerCategory> MapCategories(AngleSharp.Html.Dom.IHtmlDocument dom, AngleSharp.Dom.IElement t, AngleSharp.Dom.IElement tr)
        {
            var rName = t.TextContent;
            var rDesc = tr.QuerySelector("h3.tracker_info_bold").TextContent;
            var type = dom.QuerySelector("div.releases-date:contains('Тип:')").TextContent;

            // Check OVA first cause OVA looks like anime with OVA in release name or description
            if (CategorieOVARegex.IsMatch(rName) || CategorieOVARegex.IsMatch(rDesc))
            {
                return _categories.MapTrackerCatDescToNewznab("OVA/ONA/Special");
            }

            // Check movies then, cause some of releases could be movies dorama and should go to movies category
            if (CategorieMovieRegex.IsMatch(rName) || CategorieMovieRegex.IsMatch(rDesc))
            {
                return _categories.MapTrackerCatDescToNewznab("Movies");
            }

            // Check dorama. Most of doramas are flaged as doramas in type info, but type info could have a lot of types at same time (movie, etc)
            if (CategorieDoramaRegex.IsMatch(rName) || CategorieDoramaRegex.IsMatch(type))
            {
                return _categories.MapTrackerCatDescToNewznab("Dorama");
            }

            return _categories.MapTrackerCatDescToNewznab("TV Anime");
        }

        private IList<TorrentInfo> ParseRelease(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            foreach (var t in dom.QuerySelectorAll("ul.media__tabs__nav > li > a"))
            {
                var tr_id = t.Attributes["href"].Value;
                var tr = dom.QuerySelector("div" + tr_id);
                var seeders = int.Parse(tr.QuerySelector("div.circle_green_text_top").TextContent);
                var url = indexerResponse.HttpRequest.Url.ToString();

                var release = new TorrentInfo
                {
                    Title = composeTitle(dom, t, tr),
                    InfoUrl = url,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,

                    Guid = url + tr_id,
                    Seeders = seeders,
                    Peers = seeders + int.Parse(tr.QuerySelector("div.circle_red_text_top").TextContent),
                    Grabs = int.Parse(tr.QuerySelector("div.circle_grey_text_top").TextContent),
                    Categories = MapCategories(dom, t, tr),
                    PublishDate = getReleaseDate(tr),
                    DownloadUrl = tr.QuerySelector("div.download_tracker > a.btn__green").Attributes["href"].Value,
                    MagnetUrl = tr.QuerySelector("div.download_tracker > a.btn__d-gray").Attributes["href"].Value,
                    Size = getReleaseSize(tr),
                    Resolution = getResolution(tr)
                };
                torrentInfos.Add(release);
            }

            return torrentInfos;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var links = dom.QuerySelectorAll("a.ads-list__item__title");
            foreach (var link in links)
            {
                var url = link.GetAttribute("href");

                // Some URLs in search are broken
                if (url.StartsWith("//"))
                {
                    url = "https:" + url;
                }

                var releaseRequest = new IndexerRequest(url, HttpAccept.Html);
                var releaseResponse = new IndexerResponse(releaseRequest, HttpClient.Execute(releaseRequest.HttpRequest));

                // Throw common http errors here before we try to parse
                if (releaseResponse.HttpResponse.HasHttpError)
                {
                    if ((int)releaseResponse.HttpResponse.StatusCode == 429)
                    {
                        throw new TooManyRequestsException(releaseRequest.HttpRequest, releaseResponse.HttpResponse);
                    }
                    else
                    {
                        throw new IndexerException(releaseResponse, "Http error code: " + releaseResponse.HttpResponse.StatusCode);
                    }
                }

                torrentInfos.AddRange(ParseRelease(releaseResponse));
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimediaSettingsValidator : AbstractValidator<AnimediaSettings>
    {
        public AnimediaSettingsValidator()
        {
        }
    }

    public class AnimediaSettings : IIndexerSettings
    {
        private static readonly AnimediaSettingsValidator Validator = new AnimediaSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
