using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public abstract class BrazilianPublicTorrentBase : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        protected abstract BrazilianPublicTorrentSite Site { get; }

        public override string Name => Site.Name;
        public override string[] IndexerUrls => new[] { Site.BaseUrl };
        public override string[] LegacyUrls => Site.LegacyUrls;
        public override string Language => "pt-BR";
        public override string Description => "Brazilian public torrent indexer";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override bool SupportsRss => false;

        protected BrazilianPublicTorrentBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BrazilianPublicTorrentRequestGenerator(Settings, Site);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BrazilianPublicTorrentNoopParser();
        }

        protected override async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var context = BrazilianPublicTorrentParser.ExtractSearchContext(request.Url.FullUri);
            var cookies = new Dictionary<string, string>();
            HttpResponse response;

            if (Site.RequiresSearchToken)
            {
                var homeRequest = BuildRequest(Settings.BaseUrl, null, null, true);
                var homeResponse = await ExecuteRequest(homeRequest);
                cookies = MergeCookies(cookies, homeResponse.GetCookies());

                var token = BrazilianPublicTorrentParser.ExtractToken(homeResponse.Content);
                var query = Uri.EscapeDataString(context.Query);
                var searchUrl = $"{Settings.BaseUrl.TrimEnd('/')}/index.php?hp_bot_check=&token={Uri.EscapeDataString(token ?? string.Empty)}&busca={query}";
                response = await ExecuteRequest(BuildRequest(searchUrl, cookies, Settings.BaseUrl, false));
            }
            else
            {
                if (Site.UsesHdrRoutes && context.Route == BrazilianPublicTorrentRoute.SearchPage)
                {
                    var movieContext = WithRoute(context, BrazilianPublicTorrentRoute.Movies);
                    var seriesContext = WithRoute(context, BrazilianPublicTorrentRoute.Series);

                    var movieResponse = await ExecuteRequest(BuildRequest(BuildSearchUrl(movieContext), cookies, Settings.BaseUrl, false));
                    cookies = MergeCookies(cookies, movieResponse.GetCookies());

                    var seriesResponse = await ExecuteRequest(BuildRequest(BuildSearchUrl(seriesContext), cookies, Settings.BaseUrl, false));
                    cookies = MergeCookies(cookies, seriesResponse.GetCookies());

                    var combinedReleases = await ParseSearchResponses(new[] { (movieResponse, movieContext), (seriesResponse, seriesContext) }, context, cookies);

                    return new IndexerQueryResult
                    {
                        Releases = combinedReleases,
                        Response = movieResponse
                    };
                }

                response = await ExecuteRequest(BuildRequest(BuildSearchUrl(context), cookies, Settings.BaseUrl, false));
            }

            cookies = MergeCookies(cookies, response.GetCookies());

            var releases = await ParseSearchResponses(new[] { (response, context) }, context, cookies);

            return new IndexerQueryResult
            {
                Releases = releases,
                Response = response
            };
        }

        protected abstract IEnumerable<BrazilianPublicTorrentSearchItem> ParseSearchItems(IDocument document, BrazilianPublicTorrentSearchContext context);

        protected virtual string GetMagnetOptionLabel(IElement anchor)
        {
            return anchor.TextContent;
        }

        private async Task<List<ReleaseInfo>> ParseSearchResponses(IEnumerable<(HttpResponse Response, BrazilianPublicTorrentSearchContext Context)> responses, BrazilianPublicTorrentSearchContext originalContext, IDictionary<string, string> cookies)
        {
            var items = new List<BrazilianPublicTorrentSearchItem>();

            foreach (var (response, context) in responses)
            {
                var searchDocument = BrazilianPublicTorrentParser.ParseDocument(response.Content, response.Request.Url.FullUri);
                items.AddRange(ParseSearchItems(searchDocument, context).Where(item => item.Url.IsNotNullOrWhiteSpace()));
            }

            items = items
                .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var releases = new List<ReleaseInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var detailResponse = await ExecuteRequest(BuildRequest(item.Url, cookies, Settings.BaseUrl, true));
                var detailDocument = BrazilianPublicTorrentParser.ParseDocument(detailResponse.Content, item.Url);
                var detail = BrazilianPublicTorrentParser.ParseDetail(detailDocument, GetMagnetOptionLabel);

                if (!BrazilianPublicTorrentParser.MatchesSearchContext(item, detail, originalContext))
                {
                    continue;
                }

                foreach (var option in detail.MagnetOptions)
                {
                    var infoHash = BrazilianPublicTorrentParser.ExtractInfoHash(option.MagnetUrl);
                    var dedupeKey = infoHash ?? option.MagnetUrl;

                    if (dedupeKey.IsNullOrWhiteSpace() || !seen.Add(dedupeKey))
                    {
                        continue;
                    }

                    var categories = GetCategories(detail, item);
                    var title = BrazilianPublicTorrentParser.BuildTitle(item, detail, option, originalContext);

                    releases.Add(new TorrentInfo
                    {
                        Guid = infoHash.IsNotNullOrWhiteSpace() ? $"{Name}-{infoHash}" : option.MagnetUrl,
                        Title = title,
                        InfoUrl = item.Url,
                        MagnetUrl = option.MagnetUrl,
                        InfoHash = infoHash,
                        Size = option.Size ?? GetSingleOptionSize(detail),
                        PublishDate = detail.PublishDate,
                        Categories = categories,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    });
                }
            }

            return releases;
        }

        private static long? GetSingleOptionSize(BrazilianPublicTorrentDetail detail)
        {
            return detail.MagnetOptions.Count == 1 ? detail.Size : null;
        }

        private static BrazilianPublicTorrentSearchContext WithRoute(BrazilianPublicTorrentSearchContext context, BrazilianPublicTorrentRoute route)
        {
            return new BrazilianPublicTorrentSearchContext
            {
                Kind = context.Kind,
                Route = route,
                Query = context.Query,
                Season = context.Season,
                Episode = context.Episode,
                Year = context.Year
            };
        }

        private List<IndexerCategory> GetCategories(BrazilianPublicTorrentDetail detail, BrazilianPublicTorrentSearchItem item)
        {
            var categoryText = $"{detail.Title} {detail.CategoryText} {item.Title}";

            if (BrazilianPublicTorrentParser.IsTvCompatibleText(categoryText))
            {
                return new List<IndexerCategory> { NewznabStandardCategory.TV };
            }

            if (categoryText.Contains("filme", StringComparison.OrdinalIgnoreCase) ||
                categoryText.Contains("movie", StringComparison.OrdinalIgnoreCase))
            {
                return new List<IndexerCategory> { NewznabStandardCategory.Movies };
            }

            if (item.Route == BrazilianPublicTorrentRoute.Series)
            {
                return new List<IndexerCategory> { NewznabStandardCategory.TV };
            }

            return new List<IndexerCategory> { NewznabStandardCategory.Movies };
        }

        private string BuildSearchUrl(BrazilianPublicTorrentSearchContext context)
        {
            if (Site.UsesHdrRoutes)
            {
                var segment = context.Route == BrazilianPublicTorrentRoute.Series ? "series" : "filmes";
                var slug = BrazilianPublicTorrentParser.ToHdrSlug(context.Query);

                return $"{Settings.BaseUrl.TrimEnd('/')}/{segment}/{slug}/";
            }

            throw new InvalidOperationException($"{Name} does not define a direct search route.");
        }

        private HttpRequest BuildRequest(string url, IDictionary<string, string> cookies, string referer, bool allowRedirect)
        {
            var builder = new HttpRequestBuilder(url)
            {
                AllowAutoRedirect = allowRedirect
            };

            if (cookies != null)
            {
                builder.SetCookies(cookies);
            }

            if (referer.IsNotNullOrWhiteSpace())
            {
                builder.SetHeader("Referer", referer);
            }

            var httpRequest = builder.Build();
            httpRequest.Encoding = Encoding;
            return httpRequest;
        }

        private async Task<HttpResponse> ExecuteRequest(HttpRequest httpRequest)
        {
            if (httpRequest.RateLimit < RateLimit)
            {
                httpRequest.RateLimit = RateLimit;
            }

            return await _httpClient.ExecuteProxiedAsync(httpRequest, Definition);
        }

        private static Dictionary<string, string> MergeCookies(IDictionary<string, string> current, IDictionary<string, string> next)
        {
            var merged = new Dictionary<string, string>(current ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);

            if (next == null)
            {
                return merged;
            }

            foreach (var cookie in next)
            {
                merged[cookie.Key] = cookie.Value;
            }

            return merged;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                }
            };

            caps.Categories.AddCategoryMapping(2000, NewznabStandardCategory.Movies, "Filmes");
            caps.Categories.AddCategoryMapping(5000, NewznabStandardCategory.TV, "Series");

            return caps;
        }
    }
}
