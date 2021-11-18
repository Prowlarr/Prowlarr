using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            string result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", ":", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "-", "", "" };

            // Replace a colon followed by a space with space dash space for a better appearance
            if (replace)
            {
                result = result.Replace(": ", " - ");
            }

            for (int i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], replace ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }

        public static string StripNonAlphaNumeric(this string str, string replacement = "") =>
            StripRegex(str, "[^a-zA-Z0-9 -]", replacement);

        public static string StripRegex(string str, string regex, string replacement = "")
        {
            var rgx = new Regex(regex);
            str = rgx.Replace(str, replacement);
            return str;
        }

        // replaces culture specific characters with the corresponding base characters (e.g. è becomes e).
        public static string RemoveDiacritics(string s)
        {
            var normalizedString = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < normalizedString.Length; i++)
            {
                var c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        public static string FromBase64(string str) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(str));

        /// <summary>
        /// Convert an array of bytes to a string of hex digits
        /// </summary>
        /// <param name="bytes">array of bytes</param>
        /// <returns>String of hex digits</returns>
        public static string HexStringFromBytes(byte[] bytes) =>
            string.Join("", bytes.Select(b => b.ToString("X2")));

        /// <summary>
        /// Compute hash for string encoded as UTF8
        /// </summary>
        /// <param name="s">String to be hashed</param>
        /// <returns>40-character hex string</returns>
        public static string HashSHA1(string s)
        {
            var sha1 = SHA1.Create();

            var bytes = Encoding.UTF8.GetBytes(s);
            var hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }

        public static string Hash(string s)
        {
            // Use input string to calculate MD5 hash
            var md5 = System.Security.Cryptography.MD5.Create();

            var inputBytes = System.Text.Encoding.ASCII.GetBytes(s);
            var hashBytes = md5.ComputeHash(inputBytes);

            return HexStringFromBytes(hashBytes);
        }

        // Is never used
        // remove in favor of Exception.ToString() ?
        public static string GetExceptionDetails(this Exception exception)
        {
            var properties = exception.GetType()
                                    .GetProperties();
            var fields = properties
                             .Select(property => new
                             {
                                 Name = property.Name,
                                 Value = property.GetValue(exception, null)
                             })
                             .Select(x => string.Format(
                                 "{0} = {1}",
                                 x.Name,
                                 x.Value != null ? x.Value.ToString() : string.Empty));
            return string.Join("\n", fields);
        }

        private static char[] MakeValidFileName_invalids;

        /// <summary>Replaces characters in <c>text</c> that are not allowed in
        /// file names with the specified replacement character.</summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
        {
            var sb = new StringBuilder(text.Length);
            var invalids = MakeValidFileName_invalids ?? (MakeValidFileName_invalids = Path.GetInvalidFileNameChars());
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

        /// <summary>
        /// Converts a NameValueCollection to an appropriately formatted query string.
        /// Duplicate keys are allowed in a NameValueCollection, but are stored as a csv string in Value.
        /// This function handles leaving the values together in the csv string or splitting the value into separate keys
        /// </summary>
        /// <param name="collection">The NameValueCollection being converted</param>
        /// <param name="encoding">The Encoding to use in url encoding Value</param>
        /// <param name="duplicateKeysIfMulti">Duplicate keys are handled as true => {"Key=Val1", "Key=Val2} or false => {"Key=Val1,Val2"}</param>
        /// <param name="separator">The string used to separate each query value</param>
        /// <returns>A web encoded string of key=value parameters separated by the separator</returns>
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

        public static string GenerateRandom(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var randBytes = new byte[length];

            using (var rngCsp = RandomNumberGenerator.Create())
            {
                rngCsp.GetBytes(randBytes);
                var key = "";
                foreach (var b in randBytes)
                {
                    key += chars[b % chars.Length];
                }

                return key;
            }
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
