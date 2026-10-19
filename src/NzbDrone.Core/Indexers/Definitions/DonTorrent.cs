using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class DonTorrent : TorrentIndexerBase<DonTorrentSettings>
    {
        public override string Name => "DonTorrent";
        public override string[] IndexerUrls => new[]
        {
            "https://todotorrents.org/",
            "https://tomadivx.net/",
            "https://verdetorrent.com/",
            "https://seriesblanco.one/",
            "https://naranjatorrent.com/"
        };

        public override string[] LegacyUrls => new[]
        {
            "https://dontorrent.haus/",
            "https://dontorrent.news/",
            "https://dontorrent.institute/",
            "https://dontorrent.jetzt/",
            "https://dontorrent.loan/",
            "https://dontorrent.graphics/",
            "https://dontorrent.international/",
            "https://dontorrent.irish/",
            "https://dontorrent.lighting/",
            "https://dontorrent.istanbul/",
            "https://dontorrent.onl/",
            "https://dontorrent.kids/",
            "https://dontorrent.kiwi/",
            "https://dontorrent.live/",
            "https://dontorrent.phd/",
            "https://dontorrent.gripe/",
            "https://dontorrent.promo/"
        };

        public override string Description => "DonTorrent is a SPANISH Public tracker for MOVIES / TV / MUSIC / GAMES";
        public override string Language => "es-ES";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.1);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public DonTorrent(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DonTorrentRequestGenerator(Settings, RateLimit);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DonTorrentParser(Definition, Settings, Capabilities.Categories, RateLimit, _httpClient);
        }

        public override async Task<IndexerDownloadResponse> Download(Uri link)
        {
            var downloadUrl = link.ToString();
            if (downloadUrl.Contains("cdn.pizza") ||
                downloadUrl.Contains("blazing.network") ||
                downloadUrl.Contains("tor.cat") ||
                downloadUrl.Contains("cdndelta.com") ||
                downloadUrl.Contains("cdnbeta.in") ||
                downloadUrl.Contains("/torrents/series/"))
            {
                return await base.Download(link);
            }

            var request = new HttpRequestBuilder(downloadUrl)
                .WithRateLimit(RateLimit.TotalSeconds)
                .Accept(HttpAccept.Html)
                .Build();

            var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(new IndexerResponse(new IndexerRequest(request), response),
                    $"Unexpected response status {response.StatusCode} code from indexer request");
            }

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.Content);
            var dlNode = dom.QuerySelector("div.text-center > p > a");
            downloadUrl = dlNode != null ? $"https:{dlNode.GetAttribute("href")}" : null;

            if (downloadUrl.IsNullOrWhiteSpace())
            {
                throw new IndexerException(new IndexerResponse(new IndexerRequest(request), response),
                    "Failed to locate download link on details page.");
            }

            return await base.Download(new Uri(downloadUrl));
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var result = base.CleanupReleases(releases, searchCriteria);

            if (!Settings.MatchWords || searchCriteria.SearchTerm.IsNullOrWhiteSpace())
            {
                return result;
            }

            var normalizedTerm = DonTorrentHelper.NormalizeSearchTerm(searchCriteria.SanitizedSearchTerm);
            return result.Where(r => DonTorrentHelper.CheckTitleMatchWords(normalizedTerm, r.Title)).ToList();
        }

        private static IndexerCapabilities SetCapabilities()
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
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Pelicula, NewznabStandardCategory.Movies, "Pelicula");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.PeliculaHD, NewznabStandardCategory.MoviesHD, "Peliculas HD");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Pelicula4K, NewznabStandardCategory.MoviesUHD, "Peliculas 4K");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Serie, NewznabStandardCategory.TVSD, "Serie");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.SerieHD, NewznabStandardCategory.TVHD, "Serie HD");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Musica, NewznabStandardCategory.Audio, "Música");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Documental, NewznabStandardCategory.TVDocumentary, "Documental");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Juego, NewznabStandardCategory.PCGames, "Juegos PC");
            caps.Categories.AddCategoryMapping(DonTorrentHelper.DonTorrentCatType.Juego, NewznabStandardCategory.Console, "Juegos consola");

            return caps;
        }
    }

    public class DonTorrentSettings : NoAuthTorrentBaseSettings
    {
        public DonTorrentSettings()
        {
            MatchWords = true;
        }

        [FieldDefinition(2, Type = FieldType.Checkbox, Label = "Match words in title", HelpText = "Only return releases that contain all search words.")]
        public bool MatchWords { get; set; }
    }

    internal static class DonTorrentHelper
    {
        internal static class DonTorrentCatType
        {
            public static string Pelicula => "pelicula";
            public static string PeliculaHD => "peliculahd";
            public static string Pelicula4K => "pelicula4k";
            public static string Serie => "serie";
            public static string SerieHD => "seriehd";
            public static string Documental => "documental";
            public static string Musica => "musica";
            public static string Juego => "juego";
        }

        internal const string NewTorrentsUrl = "ultimos";
        internal const string SearchUrl = "buscar/";

        internal static readonly Dictionary<string, string> CategoriesMap = new()
        {
            { "/pelicula/", DonTorrentCatType.Pelicula },
            { "/serie/", DonTorrentCatType.Serie },
            { "/documental/", DonTorrentCatType.Documental },
            { "/musica/", DonTorrentCatType.Musica },
            { "/juego/", DonTorrentCatType.Juego },
        };

        /// <summary>
        /// Normalizes a search term to better match site search behavior:
        /// strips punctuation, removes trailing year, and drops language tokens.
        /// </summary>
        internal static string NormalizeSearchTerm(string input)
        {
            var searchTerm = input ?? string.Empty;
            searchTerm = Regex.Replace(searchTerm, @"[-._\(\)@/\\\[\]\+\%]", " ");
            searchTerm = Regex.Replace(searchTerm, @"\s+", " ").Trim();

            var r = new Regex("([ ]+([0-9]{4}))$", RegexOptions.IgnoreCase);
            var m = r.Match(searchTerm);
            if (m.Success)
            {
                searchTerm = searchTerm.Replace(m.Groups[1].Value, "");
            }

            searchTerm = Regex.Replace(searchTerm, @"\b(espa[ñn]ol|spanish|castellano|spa)\b", "", RegexOptions.IgnoreCase);
            searchTerm = Regex.Replace(searchTerm, @"\s+", " ").Trim();
            return searchTerm;
        }

        /// <summary>
        /// Converts a series/episode label into a scene-like release title and
        /// appends language/quality tags inferred from the series title.
        /// </summary>
        internal static string ParseSeriesTitle(string title, string episodeTitle)
        {
            var newTitle = title.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            var newEpisodeTitle = episodeTitle.Trim();
            var matches = Regex.Matches(newEpisodeTitle, "([0-9]+)x([0-9]+)", RegexOptions.IgnoreCase);
            if (matches.Count > 1)
            {
                newEpisodeTitle = "";
                foreach (Match m in matches)
                {
                    if (string.IsNullOrEmpty(newEpisodeTitle))
                    {
                        newEpisodeTitle += "S" + m.Groups[1].Value.PadLeft(2, '0') +
                                           "E" + m.Groups[2].Value.PadLeft(2, '0');
                    }
                    else
                    {
                        newEpisodeTitle += "-E" + m.Groups[2].Value.PadLeft(2, '0');
                    }
                }
            }
            else
            {
                var m = Regex.Match(newEpisodeTitle, "^([0-9]+)x([0-9]+)[^0-9]+([0-9]+)[.]?$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    newEpisodeTitle = "S" + m.Groups[1].Value.PadLeft(2, '0') +
                                      "E" + m.Groups[2].Value.PadLeft(2, '0') + "-" +
                                      "E" + m.Groups[3].Value.PadLeft(2, '0');
                }
                else
                {
                    m = Regex.Match(newEpisodeTitle, "^([0-9]+)x([0-9]+)(.*)$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        newEpisodeTitle = "S" + m.Groups[1].Value.PadLeft(2, '0') +
                                          "E" + m.Groups[2].Value.PadLeft(2, '0');
                        if (!string.IsNullOrEmpty(m.Groups[3].Value))
                        {
                            newEpisodeTitle += " " + m.Groups[3].Value.Replace(" -", "").Trim();
                        }
                    }
                }
            }

            var finalTitle = newTitle + " " + newEpisodeTitle;
            finalTitle += " SPANISH";

            if (title.ToLowerInvariant().Contains("es-en"))
            {
                finalTitle += " ENGLISH";
            }

            if (title.ToLowerInvariant().Contains("720p"))
            {
                finalTitle += " 720p";
            }
            else if (title.ToLowerInvariant().Contains("1080p"))
            {
                finalTitle += " 1080p";
            }
            else
            {
                finalTitle += " SDTV";
            }

            if (title.ToLowerInvariant().Contains("hdtv"))
            {
                finalTitle += " HDTV";
            }

            if (title.ToLowerInvariant().Contains("x265"))
            {
                finalTitle += " x265";
            }
            else
            {
                finalTitle += " x264";
            }

            return finalTitle;
        }

        internal static int GetEpisodeCountFromTitle(string title)
        {
            var matches = Regex.Matches(title, "E([0-9]{1,3})", RegexOptions.IgnoreCase);
            var count = matches.Count;
            if (count == 0)
            {
                return 0;
            }

            if (count == 2)
            {
                var firstMatch = matches[0].Groups[1].Value;
                var lastMatch = matches[1].Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(firstMatch) && !string.IsNullOrWhiteSpace(lastMatch))
                {
                    var firstEp = int.Parse(firstMatch);
                    var lastEp = int.Parse(lastMatch);
                    return lastEp - firstEp + 1;
                }
            }

            return count;
        }

        internal static string GetCategory(string title, string url)
        {
            var cat = GetCategoryFromUrl(url);
            switch (cat)
            {
                case "pelicula":
                case "pelicula4k":
                    if (title.Contains("4K"))
                    {
                        cat = DonTorrentCatType.Pelicula4K;
                    }
                    else if (title.Contains("720p") || title.Contains("1080p"))
                    {
                        cat = DonTorrentCatType.PeliculaHD;
                    }

                    break;
                case "serie":
                case "seriehd":
                    if (title.Contains("720p") || title.Contains("1080p"))
                    {
                        cat = DonTorrentCatType.SerieHD;
                    }

                    break;
            }

            return cat;
        }

        internal static string GetCategoryFromUrl(string url)
        {
            return CategoriesMap
                .Where(categoryMap => url.Contains(categoryMap.Key))
                .Select(categoryMap => categoryMap.Value)
                .FirstOrDefault();
        }

        internal static DateTime TryToParseDate(string dateToParse, DateTime dateDefault)
        {
            try
            {
                return DateTime.ParseExact(dateToParse.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch
            {
                return dateDefault;
            }
        }

        internal static bool CheckTitleMatchWords(string queryStr, string title)
        {
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLowerInvariant()));

            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLowerInvariant()));

            var titleSet = titleWords.ToArray();
            return queryWords.All(word => titleSet.Contains(word));
        }
    }

    public class DonTorrentRequestGenerator : IIndexerRequestGenerator
    {
        private readonly DonTorrentSettings _settings;
        private readonly TimeSpan _rateLimit;

        public DonTorrentRequestGenerator(DonTorrentSettings settings, TimeSpan rateLimit)
        {
            _settings = settings;
            _rateLimit = rateLimit;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria) =>
            BuildRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria) =>
            BuildRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria) =>
            BuildRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria) =>
            new IndexerPageableRequestChain();

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria) =>
            BuildRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);

        private IndexerPageableRequestChain BuildRequests(SearchCriteriaBase searchCriteria, string sanitizedTerm)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var baseUrl = _settings.BaseUrl.TrimEnd('/');

            if (searchCriteria.IsRssSearch)
            {
                var url = $"{baseUrl}/{DonTorrentHelper.NewTorrentsUrl}";
                var request = new HttpRequestBuilder(url)
                    .WithRateLimit(_rateLimit.TotalSeconds)
                    .Accept(HttpAccept.Html)
                    .Build();

                pageableRequests.Add(new[] { new IndexerRequest(request) });
                return pageableRequests;
            }

            var term = DonTorrentHelper.NormalizeSearchTerm(sanitizedTerm);
            var searchUrl = $"{baseUrl}/{DonTorrentHelper.SearchUrl}{Uri.EscapeDataString(term)}";
            var searchRequest = new HttpRequestBuilder(searchUrl)
                .WithRateLimit(_rateLimit.TotalSeconds)
                .SetHeader("Referer", searchUrl)
                .Accept(HttpAccept.Html)
                .Build();

            pageableRequests.Add(new[] { new IndexerRequest(searchRequest) });
            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DonTorrentParser : IParseIndexerResponse
    {
        private readonly ProviderDefinition _definition;
        private readonly DonTorrentSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly TimeSpan _rateLimit;
        private readonly IIndexerHttpClient _httpClient;

        public DonTorrentParser(ProviderDefinition definition, DonTorrentSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient)
        {
            _definition = definition;
            _settings = settings;
            _categories = categories;
            _rateLimit = rateLimit;
            _httpClient = httpClient;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            var url = indexerResponse.Request.Url.FullUri ?? string.Empty;
            var isNewest = url.Contains($"/{DonTorrentHelper.NewTorrentsUrl}");
            return isNewest ? ParseNewest(indexerResponse) : ParseSearch(indexerResponse);
        }

        /// <summary>
        /// Parses the newest items list. Used by indexer test and initial setup to check that the connection works.
        /// </summary>
        private IList<ReleaseInfo> ParseNewest(IndexerResponse indexerResponse)
        {
            var maxNewestItems = 10;
            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(indexerResponse.Content);

            var rows = doc.QuerySelector("div.seccion#ultimos_torrents > div.card > div.card-body > div");
            if (rows == null)
            {
                return releases;
            }

            var parsedDetailsLink = new HashSet<string>();
            string rowTitle = null;
            string rowDetailsLink = null;
            string rowQuality = null;
            string currentCategory = null;
            DateTime? rowPublish = null;
            var addedCount = 0;

            foreach (var row in rows.Children)
            {
                if (row.TagName.Equals("DIV"))
                {
                    var header = row.TextContent.Trim().ToLowerInvariant();
                    if (header.Contains("peliculas"))
                    {
                        currentCategory = DonTorrentHelper.DonTorrentCatType.Pelicula;
                    }
                    else if (header.Contains("series"))
                    {
                        currentCategory = DonTorrentHelper.DonTorrentCatType.Serie;
                    }
                    else if (header.Contains("documentales"))
                    {
                        currentCategory = DonTorrentHelper.DonTorrentCatType.Documental;
                    }
                    else if (header.Contains("música") || header.Contains("musica"))
                    {
                        currentCategory = DonTorrentHelper.DonTorrentCatType.Musica;
                    }
                    else if (header.Contains("juegos"))
                    {
                        currentCategory = DonTorrentHelper.DonTorrentCatType.Juego;
                    }

                    continue;
                }

                if (row.TagName.Equals("A"))
                {
                    rowTitle = row.TextContent.Trim();
                    rowDetailsLink = CombineUrl(_settings.BaseUrl, row.GetAttribute("href"));
                }

                if (row.TagName.Equals("SPAN"))
                {
                    var rowText = row.TextContent.Trim();
                    if (Regex.IsMatch(rowText, @"^\d{4}-\d{2}-\d{2}$"))
                    {
                        rowPublish = DonTorrentHelper.TryToParseDate(rowText, DateTime.UtcNow);
                    }
                    else if (Regex.IsMatch(rowText, "([()])"))
                    {
                        rowQuality = rowText;
                    }
                }

                if (row.TagName.Equals("BR"))
                {
                    if (!parsedDetailsLink.Contains(rowDetailsLink) && rowTitle != null)
                    {
                        var cat = DonTorrentHelper.GetCategory(rowTitle, rowDetailsLink);
                        if (cat.IsNullOrWhiteSpace())
                        {
                            cat = currentCategory;
                        }

                        if (cat.IsNotNullOrWhiteSpace())
                        {
                            var normalizedLabel = $"{rowTitle} {rowQuality}".ToLowerInvariant();
                            if (normalizedLabel.Contains("4k") && cat == DonTorrentHelper.DonTorrentCatType.Pelicula)
                            {
                                cat = DonTorrentHelper.DonTorrentCatType.Pelicula4K;
                            }

                            if (normalizedLabel.Contains("720p") || normalizedLabel.Contains("1080p"))
                            {
                                if (cat == DonTorrentHelper.DonTorrentCatType.Pelicula)
                                {
                                    cat = DonTorrentHelper.DonTorrentCatType.PeliculaHD;
                                }
                                else if (cat == DonTorrentHelper.DonTorrentCatType.Serie)
                                {
                                    cat = DonTorrentHelper.DonTorrentCatType.SerieHD;
                                }
                            }
                        }

                        if (IsSupportedCategory(cat))
                        {
                            var publishDate = rowPublish ?? DateTime.UtcNow;
                            var release = GenerateReleaseFromNewest(rowTitle, rowDetailsLink, cat, rowQuality, publishDate);
                            if (release != null)
                            {
                                releases.Add(release);
                                addedCount++;
                            }

                            parsedDetailsLink.Add(rowDetailsLink);
                        }
                    }

                    rowTitle = null;
                    rowDetailsLink = null;
                    rowQuality = null;
                    rowPublish = null;
                }

                if (addedCount >= maxNewestItems)
                {
                    break;
                }
            }

            return releases;
        }

        private IList<ReleaseInfo> ParseSearch(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(indexerResponse.Content);

            var rows = doc.QuerySelectorAll("div.seccion#buscador > div.card > div.card-body > p");
            if (rows.Length == 0)
            {
                return releases;
            }

            if (rows.First().TextContent.Contains("Introduce alguna palabra para buscar con al menos 2 letras."))
            {
                return releases;
            }

            foreach (var row in rows.Skip(2))
            {
                var anchor = row.QuerySelector("p > span > a");
                if (anchor == null)
                {
                    continue;
                }

                var link = CombineUrl(_settings.BaseUrl, anchor.GetAttribute("href"));
                var title = anchor.TextContent.Trim();
                var cat = DonTorrentHelper.GetCategory(title, link);
                var quality = string.Empty;

                switch (DonTorrentHelper.GetCategoryFromUrl(link))
                {
                    case "pelicula":
                    case "serie":
                        var qualityNode = row.QuerySelector("p > span > span");
                        if (qualityNode != null)
                        {
                            quality = Regex.Replace(qualityNode.TextContent, "([()])", "").Trim();
                        }

                        break;
                }

                if (IsSupportedCategory(cat))
                {
                    ParseRelease(releases, link, title, cat, quality);
                }
            }

            return releases;
        }

        private void ParseRelease(ICollection<ReleaseInfo> releases, string link, string title, string category, string quality)
        {
            if (link.IsNullOrWhiteSpace() || title.IsNullOrWhiteSpace() || category.IsNullOrWhiteSpace())
            {
                return;
            }

            title = title.Trim();
            if (title.EndsWith("."))
            {
                title = title.Remove(title.Length - 1).Trim();
            }

            switch (category)
            {
                case "pelicula":
                case "peliculahd":
                case "pelicula4k":
                    ParseMovieRelease(releases, link, title, quality);
                    break;
                case "serie":
                case "seriehd":
                    ParseSeriesRelease(releases, link, title);
                    break;
                case "musica":
                    ParseMusicRelease(releases, link, title);
                    break;
            }
        }

        private void ParseMusicRelease(ICollection<ReleaseInfo> releases, string link, string title)
        {
            var doc = FetchDocument(link);
            if (doc == null)
            {
                return;
            }

            var data = doc.QuerySelector("div.descargar > div.card > div.card-body");
            if (data == null)
            {
                return;
            }

            var data3 = data.QuerySelectorAll("div.text-center > div.d-inline-block");
            if (data3.Length < 2)
            {
                return;
            }

            var publishStr = data3[0].TextContent;
            var sizeStr = data3[1].TextContent;

            var publishDate = DonTorrentHelper.TryToParseDate(publishStr, DateTime.UtcNow);
            var size = ParseUtil.GetBytes(sizeStr);

            var release = GenerateRelease(title, link, link, DonTorrentHelper.GetCategory(title, link), publishDate, size);
            if (release != null)
            {
                releases.Add(release);
            }
        }

        private void ParseSeriesRelease(ICollection<ReleaseInfo> releases, string link, string title)
        {
            var doc = FetchDocument(link);
            if (doc == null)
            {
                return;
            }

            var data = doc.QuerySelector("div.descargar > div.card > div.card-body");
            if (data == null)
            {
                return;
            }

            var rows = data.QuerySelectorAll("div.d-inline-block > table.table > tbody > tr");
            foreach (var row in rows)
            {
                var episodeData = row.QuerySelectorAll("td");
                if (episodeData.Length < 3)
                {
                    continue;
                }

                var episodeTitle = episodeData[0].TextContent;
                var downloadHref = episodeData[1].QuerySelector("a")?.GetAttribute("href");
                var downloadLink = downloadHref.IsNullOrWhiteSpace() ? null : "https:" + downloadHref;
                var episodePublishStr = episodeData[2].TextContent;
                var episodePublish = DonTorrentHelper.TryToParseDate(episodePublishStr, DateTime.UtcNow);

                if (downloadLink.IsNullOrWhiteSpace())
                {
                    continue;
                }

                episodeTitle = DonTorrentHelper.ParseSeriesTitle(title, episodeTitle);

                var size = 512.Megabytes();
                if (episodeTitle.ToLowerInvariant().Contains("720p"))
                {
                    size = 1.Gigabytes();
                }

                if (episodeTitle.ToLowerInvariant().Contains("1080p"))
                {
                    size = 4.Gigabytes();
                }

                size *= DonTorrentHelper.GetEpisodeCountFromTitle(episodeTitle);

                var release = GenerateRelease(episodeTitle, link, downloadLink, DonTorrentHelper.GetCategory(title, link), episodePublish, size);
                if (release != null)
                {
                    releases.Add(release);
                }
            }
        }

        private void ParseMovieRelease(ICollection<ReleaseInfo> releases, string link, string title, string quality)
        {
            var doc = FetchDocument(link);
            if (doc == null)
            {
                return;
            }

            var release = ParseMovieReleaseFromDocument(link, title, quality, doc);
            if (release != null)
            {
                releases.Add(release);
            }
        }

        internal ReleaseInfo ParseMovieReleaseFromDocument(string link, string title, string quality, AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            var tags = "";
            var queryMatches = Regex.Matches(title, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
            foreach (Match m in queryMatches)
            {
                var tag = m.Groups[1].Value.Trim().ToUpperInvariant();

                if (tag.Equals("4K"))
                {
                    quality = "(UHD 4K 2160p)";
                }
                else if (tag.Equals("FULLBLURAY"))
                {
                    quality = "(COMPLETE BLURAY)";
                }
                else
                {
                    tags += " " + tag;
                }

                title = title.Replace(m.Groups[0].Value, "");
            }

            title = title.Trim();

            if (quality != null)
            {
                var queryMatch = Regex.Match(quality, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
                if (queryMatch.Success)
                {
                    quality = queryMatch.Groups[1].Value;
                }

                quality = quality.Trim().Replace("-", " ");
                quality = Regex.Replace(quality, "HDRip", "BDRip", RegexOptions.IgnoreCase);
            }

            var releaseYear = doc.QuerySelector("div.d-inline-block.ml-2 > p:contains('Año') > a")?.TextContent.Trim();
            if (releaseYear.IsNotNullOrWhiteSpace() && Regex.IsMatch(releaseYear, @"^((?:19|20)\d{2})$"))
            {
                title += $" {releaseYear}";
            }

            title += tags;
            title += " SPANISH";
            if (quality != null)
            {
                title += " " + quality;
            }

            var info = doc.QuerySelectorAll("div.descargar > div.card > div.card-body").FirstOrDefault();
            var moreinfo = info?.QuerySelectorAll("div.text-center > div.d-inline-block").ToArray() ?? Array.Empty<AngleSharp.Dom.IElement>();

            long size;
            if (moreinfo.Length == 2)
            {
                size = ParseUtil.GetBytes(moreinfo[1].QuerySelector("p")?.TextContent ?? "");
            }
            else if (title.ToLowerInvariant().Contains("4k"))
            {
                size = 50.Gigabytes();
            }
            else if (title.ToLowerInvariant().Contains("1080p"))
            {
                size = 4.Gigabytes();
            }
            else if (title.ToLowerInvariant().Contains("720p"))
            {
                size = 1.Gigabytes();
            }
            else
            {
                size = 512.Megabytes();
            }

            var cat = DonTorrentHelper.GetCategory(title, link);
            if (cat.IsNullOrWhiteSpace())
            {
                if (title.Contains("4K"))
                {
                    cat = DonTorrentHelper.DonTorrentCatType.Pelicula4K;
                }
                else if (title.Contains("720p") || title.Contains("1080p"))
                {
                    cat = DonTorrentHelper.DonTorrentCatType.PeliculaHD;
                }
                else
                {
                    cat = DonTorrentHelper.DonTorrentCatType.Pelicula;
                }
            }

            return GenerateRelease(title, link, link, cat, DateTime.UtcNow, size);
        }

        private ReleaseInfo GenerateReleaseFromNewest(string title, string link, string cat, string quality, DateTime publishDate)
        {
            var normalizedTitle = title.Trim();
            var size = 512.Megabytes();
            var qualityLabel = quality ?? string.Empty;
            var combinedLabel = $"{normalizedTitle} {qualityLabel}".ToLowerInvariant();

            if (combinedLabel.Contains("4k"))
            {
                size = 50.Gigabytes();
            }
            else if (combinedLabel.Contains("1080p"))
            {
                size = 4.Gigabytes();
            }
            else if (combinedLabel.Contains("720p"))
            {
                size = 1.Gigabytes();
            }
            else if (cat == DonTorrentHelper.DonTorrentCatType.Pelicula4K)
            {
                size = 50.Gigabytes();
            }
            else if (cat == DonTorrentHelper.DonTorrentCatType.PeliculaHD || cat == DonTorrentHelper.DonTorrentCatType.SerieHD)
            {
                size = 4.Gigabytes();
            }

            if (cat is "serie" or "seriehd")
            {
                var parts = normalizedTitle.Split(':');
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    var seriesTitle = parts[0].Trim();
                    var episodeTitle = string.Join(":", parts.Skip(1)).Trim();
                    if (!string.IsNullOrWhiteSpace(episodeTitle))
                    {
                        normalizedTitle = DonTorrentHelper.ParseSeriesTitle(seriesTitle, episodeTitle);
                    }
                    else
                    {
                        normalizedTitle += " SPANISH";
                    }
                }
                else
                {
                    normalizedTitle += " SPANISH";
                }
            }
            else if (cat is "pelicula" or "peliculahd" or "pelicula4k")
            {
                normalizedTitle += " SPANISH";
                if (!qualityLabel.IsNullOrWhiteSpace())
                {
                    normalizedTitle += " " + qualityLabel.Replace("(", "").Replace(")", "").Trim();
                }
            }
            else
            {
                normalizedTitle += " SPANISH";
            }

            return GenerateRelease(normalizedTitle, link, link, cat, publishDate, size);
        }

        internal ReleaseInfo GenerateRelease(string title, string infoUrl, string downloadUrl, string cat, DateTime publishDate, long size)
        {
            var categories = _categories.MapTrackerCatToNewznab(cat);
            if (categories == null || !categories.Any())
            {
                return null;
            }

            var release = new TorrentInfo
            {
                Title = title,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Guid = downloadUrl,
                Categories = categories.ToList(),
                PublishDate = publishDate,
                Size = size,
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            return release;
        }

        private AngleSharp.Html.Dom.IHtmlDocument FetchDocument(string url)
        {
            var request = new HttpRequestBuilder(url)
                .WithRateLimit(_rateLimit.TotalSeconds)
                .SetHeader("Referer", _settings.BaseUrl)
                .Accept(HttpAccept.Html)
                .Build();

            var indexerRequest = new IndexerRequest(request);
            var response = new IndexerResponse(indexerRequest, _httpClient.ExecuteProxied(indexerRequest.HttpRequest, _definition));

            if (response.HttpResponse.HasHttpError)
            {
                if (response.HttpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TooManyRequestsException(response.HttpRequest, response.HttpResponse);
                }

                if ((int)response.HttpResponse.StatusCode >= 500)
                {
                    return null;
                }

                throw new IndexerException(response, $"HTTP Error - {response.HttpResponse.StatusCode}. {url}");
            }

            var parser = new HtmlParser();
            return parser.ParseDocument(response.Content);
        }

        private static string CombineUrl(string baseUrl, string relative)
        {
            if (relative.IsNullOrWhiteSpace())
            {
                return baseUrl;
            }

            if (relative.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return relative;
            }

            if (relative.StartsWith("//"))
            {
                return "https:" + relative;
            }

            return baseUrl.TrimEnd('/') + "/" + relative.TrimStart('/');
        }

        private static bool IsSupportedCategory(string cat)
        {
            return cat is "pelicula" or "peliculahd" or "pelicula4k" or "serie" or "seriehd" or "musica" or "documental" or "juego";
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
