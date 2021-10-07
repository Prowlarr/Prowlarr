using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace NzbDrone.Core.Parser
{
    public static class ParseUtil
    {
        private static readonly Regex InvalidXmlChars =
            new Regex(
                @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
                RegexOptions.Compiled);
        private static readonly Regex ImdbId = new Regex(@"^(?:tt)?(\d{1,8})$", RegexOptions.Compiled);

        public static string NormalizeSpace(string s) => s.Trim();

        public static string NormalizeMultiSpaces(string s) =>
            new Regex(@"\s+").Replace(NormalizeSpace(s), " ");

        public static string NormalizeNumber(string s)
        {
            s = (s.Length == 0) ? "0" : s.Replace(",", ".");

            s = NormalizeSpace(s).Replace("-", "0");

            if (s.Count(c => c == '.') > 1)
            {
                var lastOcc = s.LastIndexOf('.');
                s = s.Substring(0, lastOcc).Replace(".", string.Empty) + s.Substring(lastOcc);
            }

            return s;
        }

        public static string RemoveInvalidXmlChars(string text) => string.IsNullOrEmpty(text) ? "" : InvalidXmlChars.Replace(text, "");

        public static double CoerceDouble(string str) => double.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static float CoerceFloat(string str) => float.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static int CoerceInt(string str) => int.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static long CoerceLong(string str) => long.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static bool TryCoerceDouble(string str, out double result) => double.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceFloat(string str, out float result) => float.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceInt(string str, out int result) => int.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceLong(string str, out long result) => long.TryParse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static long? GetLongFromString(string str)
        {
            if (str == null)
            {
                return null;
            }

            var idRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
            var idMatch = idRegEx.Match(str);
            if (!idMatch.Success)
            {
                return null;
            }

            var id = idMatch.Groups[1].Value;
            return CoerceLong(id);
        }

        public static int? GetImdbID(string imdbstr)
        {
            if (imdbstr == null)
            {
                return null;
            }

            var match = ImdbId.Match(imdbstr);
            if (!match.Success)
            {
                return null;
            }

            return int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static string GetFullImdbId(string imdbstr)
        {
            var imdbid = GetImdbID(imdbstr);
            if (imdbid == null)
            {
                return null;
            }

            return "tt" + ((int)imdbid).ToString("D7");
        }

        public static string GetArgumentFromQueryString(string url, string argument)
        {
            if (url == null || argument == null)
            {
                return null;
            }

            var qsStr = url.Split(new char[] { '?' }, 2)[1];
            qsStr = qsStr.Split(new char[] { '#' }, 2)[0];
            var qs = QueryHelpers.ParseQuery(qsStr);
            return qs[argument].FirstOrDefault();
        }

        public static long GetBytes(string str)
        {
            var valStr = new string(str.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            var unit = new string(str.Where(char.IsLetter).ToArray());
            var val = CoerceFloat(valStr);
            return GetBytes(unit, val);
        }

        public static long GetBytes(string unit, float value)
        {
            unit = unit.Replace("i", "").ToLowerInvariant();
            if (unit.Contains("kb"))
            {
                return BytesFromKB(value);
            }

            if (unit.Contains("mb"))
            {
                return BytesFromMB(value);
            }

            if (unit.Contains("gb"))
            {
                return BytesFromGB(value);
            }

            if (unit.Contains("tb"))
            {
                return BytesFromTB(value);
            }

            return (long)value;
        }

        public static long BytesFromTB(float tb) => BytesFromGB(tb * 1024f);

        public static long BytesFromGB(float gb) => BytesFromMB(gb * 1024f);

        public static long BytesFromMB(float mb) => BytesFromKB(mb * 1024f);

        public static long BytesFromKB(float kb) => (long)(kb * 1024f);
    }
}
