using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions
{
    public static class BrazilianPublicTorrentParser
    {
        private static readonly Regex SizeRegex = new(@"(?<!\d)(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>[KMGT]B)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SeasonRegex = new(@"(?<!\d)(?<season>\d{1,2})\s*(?:[ªº°a]\s*)?temporada\b|\bS(?<season2>\d{1,2})(?:E\d{1,2})?\b|\bseason\s*(?<season3>\d{1,2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TokenRegex = new(@"name\s*=\s*[""']token[""'][^>]*value\s*=\s*[""'](?<token>[^""']+)[""']|value\s*=\s*[""'](?<token>[^""']+)[""'][^>]*name\s*=\s*[""']token[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IDocument ParseDocument(string html, string address)
        {
            var parser = new HtmlParser();
            return parser.ParseDocument(html ?? string.Empty);
        }

        public static BrazilianPublicTorrentSearchContext ExtractSearchContext(string url)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);

            return new BrazilianPublicTorrentSearchContext
            {
                Query = query["query"] ?? string.Empty,
                Kind = ParseEnum(query["kind"], BrazilianPublicTorrentSearchKind.Basic),
                Route = ParseEnum(query["route"], BrazilianPublicTorrentRoute.SearchPage),
                Season = ParseNullableInt(query["season"]),
                Episode = query["episode"],
                Year = ParseNullableInt(query["year"])
            };
        }

        public static string ExtractToken(string html)
        {
            if (html.IsNullOrWhiteSpace())
            {
                return null;
            }

            var match = TokenRegex.Match(html);
            return match.Success ? WebUtility.HtmlDecode(match.Groups["token"].Value) : null;
        }

        public static string ToHdrSlug(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = RemoveDiacritics(title).ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_");
            normalized = Regex.Replace(normalized, @"_+", "_");

            return normalized.Trim('_');
        }

        public static string AbsoluteUrl(string baseUrl, string href)
        {
            if (href.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            {
                return absolute.AbsoluteUri;
            }

            return new Uri(new Uri(baseUrl), href).AbsoluteUri;
        }

        public static BrazilianPublicTorrentDetail ParseDetail(IDocument document, Func<IElement, string> getMagnetLabel)
        {
            var values = ExtractMetadata(document);
            var title = First(values, "Título", "Titulo", "Título Traduzido", "Titulo Traduzido", "Título Original", "Titulo Original");
            var category = First(values, "Categoria", "Tipo");
            var explicitSize = TryExtractExplicitSize(First(values, "Tamanho", "Tamanho do Arquivo")) ?? ExtractSpecSize(document);

            var options = document.QuerySelectorAll("a[href^='magnet:?']")
                .OfType<IElement>()
                .Select(anchor =>
                {
                    var magnet = anchor.GetAttribute("href");
                    var label = getMagnetLabel(anchor);

                    if (label.IsNullOrWhiteSpace())
                    {
                        label = anchor.TextContent;
                    }

                    return new BrazilianPublicTorrentMagnetOption
                    {
                        MagnetUrl = magnet,
                        Label = CleanText(label),
                        Size = TryExtractOptionSize(anchor, label, magnet),
                        Element = anchor
                    };
                })
                .Where(option => option.MagnetUrl.IsNotNullOrWhiteSpace())
                .ToList();

            return new BrazilianPublicTorrentDetail
            {
                Title = CleanText(title),
                CategoryText = CleanText(category),
                Quality = NormalizeValue(First(values, "Qualidade", "Qualidade de Vídeo", "Qualidade de Video")),
                Audio = NormalizeValue(First(values, "Áudio", "Audio", "Qualidade de Áudio", "Qualidade de Audio")),
                Language = NormalizeValue(First(values, "Idioma")),
                Subtitle = NormalizeValue(First(values, "Legenda")),
                Genre = CleanText(First(values, "Gênero", "Genero")),
                Year = ExtractYear(First(values, "Ano de Lançamento", "Ano de Lancamento", "Lançamento", "Lancamento")),
                PublishDate = ExtractPublishDate(document),
                Size = explicitSize,
                MagnetOptions = options
            };
        }

        public static string ExtractInfoHash(string magnetUrl)
        {
            if (magnetUrl.IsNullOrWhiteSpace())
            {
                return null;
            }

            var match = Regex.Match(WebUtility.UrlDecode(magnetUrl), @"xt=urn:btih:(?<hash>[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["hash"].Value.ToLowerInvariant() : null;
        }

        public static string ExtractMagnetDisplayName(string magnetUrl)
        {
            if (magnetUrl.IsNullOrWhiteSpace())
            {
                return null;
            }

            var queryStart = magnetUrl.IndexOf('?');
            if (queryStart < 0)
            {
                return null;
            }

            var query = HttpUtility.ParseQueryString(magnetUrl.Substring(queryStart + 1));
            return CleanText(query["dn"]);
        }

        public static long? TryExtractExplicitSize(string text)
        {
            if (text.IsNullOrWhiteSpace())
            {
                return null;
            }

            var match = SizeRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            return ParseUtil.GetBytes($"{match.Groups["value"].Value} {match.Groups["unit"].Value}");
        }

        private static long? TryExtractOptionSize(IElement anchor, string label, string magnetUrl)
        {
            var size = TryExtractMagnetSize(magnetUrl) ?? TryExtractExplicitSize(label);
            if (size.HasValue)
            {
                return size;
            }

            foreach (var text in GetNearbyMagnetTexts(anchor))
            {
                size = TryExtractExplicitSize(text);
                if (size.HasValue)
                {
                    return size;
                }
            }

            return null;
        }

        private static long? TryExtractMagnetSize(string magnetUrl)
        {
            if (magnetUrl.IsNullOrWhiteSpace())
            {
                return null;
            }

            var queryStart = magnetUrl.IndexOf('?');
            if (queryStart < 0)
            {
                return null;
            }

            var query = HttpUtility.ParseQueryString(WebUtility.HtmlDecode(magnetUrl.Substring(queryStart + 1)));
            var rawSize = query["xl"];

            if (long.TryParse(rawSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) && size > 0)
            {
                return size;
            }

            return null;
        }

        private static long? ExtractSpecSize(IDocument document)
        {
            foreach (var element in document.QuerySelectorAll(".spec-card-glass, .spec-card, .specs-grid-premium > *, .info-card, .detail-card"))
            {
                var text = CleanText(element.TextContent);

                if (text.IsNullOrWhiteSpace() || !text.Contains("tamanho", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var size = TryExtractExplicitSize(text);
                if (size.HasValue)
                {
                    return size;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetNearbyMagnetTexts(IElement anchor)
        {
            var current = anchor.ParentElement;
            var inspected = 0;

            while (current != null && current.LocalName != "body" && inspected < 4)
            {
                inspected++;

                if (current.Matches(".download-row, .download-item, .lista-download, .link-download, .links, .download, li, tr") ||
                    current.QuerySelectorAll("a[href^='magnet:?']").Length == 1)
                {
                    yield return current.TextContent;
                }

                current = current.ParentElement;
            }
        }

        public static string BuildTitle(BrazilianPublicTorrentSearchItem item, BrazilianPublicTorrentDetail detail, BrazilianPublicTorrentMagnetOption option, BrazilianPublicTorrentSearchContext context)
        {
            var title = ExtractMagnetDisplayName(option.MagnetUrl);

            if (title.IsNullOrWhiteSpace())
            {
                var parts = new[]
                {
                    detail.Title,
                    item.Title,
                    detail.Year?.ToString(CultureInfo.InvariantCulture),
                    option.Label,
                    detail.Quality,
                    detail.Audio,
                    detail.Language,
                    detail.Subtitle
                };

                title = string.Join(" ", parts.Where(p => p.IsNotNullOrWhiteSpace()));
            }

            title = CleanReleaseTitle(title);
            title = CanonicalizePortugueseSeason(title);

            if (context.Season.HasValue && IsTvCompatibleText(title))
            {
                title = EnsureSeasonToken(title, context.Season.Value);
            }

            return title;
        }

        public static bool MatchesSearchContext(BrazilianPublicTorrentSearchItem item, BrazilianPublicTorrentDetail detail, BrazilianPublicTorrentSearchContext context)
        {
            var haystack = RemoveDiacritics($"{item.Title} {detail.Title}").ToLowerInvariant();
            var words = NormalizeWords(context.Query);

            if (words.Any() && words.Count(word => haystack.Contains(word)) < Math.Min(words.Count, 2))
            {
                return false;
            }

            if (context.Kind == BrazilianPublicTorrentSearchKind.Tv || context.Route == BrazilianPublicTorrentRoute.Series)
            {
                if (context.Season.HasValue && !MatchesSeason($"{item.Title} {detail.Title}", context.Season.Value))
                {
                    return false;
                }

                return IsTvCompatibleText($"{item.Title} {detail.Title} {detail.CategoryText}");
            }

            if (context.Kind == BrazilianPublicTorrentSearchKind.Movie || context.Route == BrazilianPublicTorrentRoute.Movies)
            {
                return !IsTvCompatibleText($"{item.Title} {detail.Title} {detail.CategoryText}");
            }

            return true;
        }

        public static bool IsTvCompatibleText(string text)
        {
            return text.IsNotNullOrWhiteSpace() &&
                   (SeasonRegex.IsMatch(text) || text.Contains("série", StringComparison.OrdinalIgnoreCase) || text.Contains("serie", StringComparison.OrdinalIgnoreCase));
        }

        public static string CleanText(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            value = WebUtility.HtmlDecode(value);
            value = Regex.Replace(value, @"\s+", " ");

            return value.Trim();
        }

        public static string CleanReleaseTitle(string value)
        {
            value = CleanText(value);

            if (value.IsNullOrWhiteSpace())
            {
                return value;
            }

            value = value.Replace('.', ' ').Replace('_', ' ');
            value = Regex.Replace(value, @"\b(download|torrent|magnet|link)\b", "", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"\s+", " ");

            return value.Trim(' ', '-', '|');
        }

        private static IDictionary<string, string> ExtractMetadata(IDocument document)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in document.QuerySelectorAll("#informacoes p, .infos p, .informacoes p, p"))
            {
                var html = element.InnerHtml;
                foreach (var part in Regex.Split(html, @"<br\s*/?>", RegexOptions.IgnoreCase))
                {
                    var text = StripTags(part);
                    var separator = text.IndexOf(':');

                    if (separator <= 0)
                    {
                        continue;
                    }

                    var key = CleanText(text.Substring(0, separator));
                    var value = CleanText(text.Substring(separator + 1));

                    if (key.IsNotNullOrWhiteSpace() && value.IsNotNullOrWhiteSpace() && !values.ContainsKey(key))
                    {
                        values[key] = value;
                    }
                }
            }

            return values;
        }

        private static string StripTags(string html)
        {
            if (html.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            return CleanText(Regex.Replace(html, "<.*?>", " ")) ?? string.Empty;
        }

        private static string First(IDictionary<string, string> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        private static DateTime ExtractPublishDate(IDocument document)
        {
            var raw = document.QuerySelector("meta[property='article:published_time'], meta[name='article:published_time']")?.GetAttribute("content") ??
                      document.QuerySelector("[itemprop='datePublished']")?.GetAttribute("datetime") ??
                      document.QuerySelector("[itemprop='datePublished']")?.TextContent;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date.ToUniversalTime();
            }

            return DateTime.Today;
        }

        private static int? ExtractYear(string text)
        {
            if (text.IsNullOrWhiteSpace())
            {
                return null;
            }

            var match = Regex.Match(text, @"\b(19|20)\d{2}\b");
            return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
        }

        private static string NormalizeValue(string value)
        {
            value = CleanText(value);

            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return value
                .Replace("Dual Áudio", "Dual", StringComparison.OrdinalIgnoreCase)
                .Replace("Dual Audio", "Dual", StringComparison.OrdinalIgnoreCase)
                .Replace("Full HD", "1080p", StringComparison.OrdinalIgnoreCase)
                .Replace("4K", "2160p", StringComparison.OrdinalIgnoreCase)
                .Replace("Ultra HD", "2160p", StringComparison.OrdinalIgnoreCase);
        }

        private static string CanonicalizePortugueseSeason(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return title;
            }

            return Regex.Replace(
                title,
                @"(?<!\d)(?<season>\d{1,2})\s*(?:[ªº°a]\s*)?temporada\b",
                match =>
                {
                    var season = int.Parse(match.Groups["season"].Value, CultureInfo.InvariantCulture);
                    return $"S{season:00}";
                },
                RegexOptions.IgnoreCase);
        }

        private static bool MatchesSeason(string text, int season)
        {
            var match = SeasonRegex.Match(text ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var value = match.Groups["season"].Success ? match.Groups["season"].Value :
                match.Groups["season2"].Success ? match.Groups["season2"].Value :
                match.Groups["season3"].Value;

            return int.TryParse(value, out var parsed) && parsed == season;
        }

        private static string EnsureSeasonToken(string title, int season)
        {
            if (Regex.IsMatch(title, $@"\bS{season:00}\b", RegexOptions.IgnoreCase))
            {
                return title;
            }

            if (MatchesSeason(title, season))
            {
                return Regex.Replace(title, SeasonRegex.ToString(), $"S{season:00}", RegexOptions.IgnoreCase);
            }

            return title;
        }

        private static List<string> NormalizeWords(string query)
        {
            return Regex.Split(RemoveDiacritics(query ?? string.Empty).ToLowerInvariant(), @"[^a-z0-9]+")
                .Where(word => word.Length > 2)
                .Take(6)
                .ToList();
        }

        private static T ParseEnum<T>(string value, T fallback)
            where T : struct
        {
            return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
        }

        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        private static string RemoveDiacritics(string text)
        {
            if (text.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
