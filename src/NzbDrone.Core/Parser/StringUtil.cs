using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Parser
{
    public static class StringUtil
    {
        private static readonly Regex WordDelimiterRegex = new Regex(@"(\s|\.|,|_|-|=|'|\|)+", RegexOptions.Compiled);
        private static readonly Regex SpecialCharRegex = new Regex(@"(\&|\:|\\|\/)+", RegexOptions.Compiled);
        private static readonly Regex PunctuationRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex CommonWordRegex = new Regex(@"\b(a|an|the|and|or|of)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DuplicateSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        public static string CleanFileName(string name, bool replace = true)
        {
            var result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", ":", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "-", "", "" };

            // Replace a colon followed by a space with space dash space for a better appearance
            if (replace)
            {
                result = result.Replace(": ", " - ");
            }

            for (var i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], replace ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }

        private static char[] MakeValidFileName_invalids;

        public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
        {
            var sb = new StringBuilder(text.Length);
            var invalids = MakeValidFileName_invalids ??= Path.GetInvalidFileNameChars();
            var changed = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (invalids.Contains(c))
                {
                    changed = true;
                    var repl = replacement ?? '\0';
                    if (fancy)
                    {
                        if (c == '"')
                        {
                            repl = '”'; // U+201D right double quotation mark
                        }
                        else if (c == '\'')
                        {
                            repl = '’'; // U+2019 right single quotation mark
                        }
                        else if (c == '/')
                        {
                            repl = '⁄'; // U+2044 fraction slash
                        }
                    }

                    if (repl != '\0')
                    {
                        sb.Append(repl);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length == 0)
            {
                return "_";
            }

            return changed ? sb.ToString() : text;
        }

        public static string GetQueryString(this NameValueCollection collection,
                                            Encoding encoding = null,
                                            bool duplicateKeysIfMulti = false,
                                            string separator = "&") =>
            collection.ToEnumerable(duplicateKeysIfMulti).GetQueryString(encoding, separator);

        public static string GetQueryString(this IEnumerable<KeyValuePair<string, string>> collection,
                                            Encoding encoding = null,
                                            string separator = "&") =>
            string.Join(separator,
                        collection.Select(a => $"{a.Key}={a.Value.UrlEncode(encoding ?? Encoding.UTF8)}"));

        public static void Add(this ICollection<KeyValuePair<string, string>> collection, string key, string value) => collection.Add(new KeyValuePair<string, string>(key, value));

        public static IEnumerable<KeyValuePair<string, string>> ToEnumerable(
            this NameValueCollection collection, bool duplicateKeysIfMulti = false)
        {
            foreach (string key in collection.Keys)
            {
                var value = collection[key];
                if (duplicateKeysIfMulti)
                {
                    foreach (var val in value.Split(','))
                    {
                        yield return new KeyValuePair<string, string>(key, val);
                    }
                }
                else
                {
                    yield return new KeyValuePair<string, string>(key, value);
                }
            }
        }

        public static string ToHtmlPretty(this IElement element)
        {
            if (element == null)
            {
                return "<NULL>";
            }

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var formatter = new PrettyMarkupFormatter();
            element.ToHtml(sw, formatter);
            return sb.ToString();
        }

        public static string NormalizeTitle(this string title)
        {
            title = WordDelimiterRegex.Replace(title, " ");
            title = PunctuationRegex.Replace(title, string.Empty);
            title = CommonWordRegex.Replace(title, string.Empty);
            title = DuplicateSpacesRegex.Replace(title, " ");
            title = SpecialCharRegex.Replace(title, string.Empty);

            return title.Trim().ToLower();
        }
    }
}
