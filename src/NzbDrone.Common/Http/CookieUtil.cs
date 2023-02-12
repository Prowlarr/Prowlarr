using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NzbDrone.Common.Http
{
    public static class CookieUtil
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
        // NOTE: we are not checking non-ascii characters and we should
        private static readonly Regex CookieRegex = new (@"([^\(\)<>@,;:\\""/\[\]\?=\{\}\s]+)=([^,;\\""\s]+)");
        private static readonly string[] FilterProps = { "COMMENT", "COMMENTURL", "DISCORD", "DOMAIN", "EXPIRES", "MAX-AGE", "PATH", "PORT", "SECURE", "VERSION", "HTTPONLY", "SAMESITE" };
        private static readonly char[] InvalidKeyChars = { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '/', '[', ']', '?', '=', '{', '}', ' ', '\t', '\n' };
        private static readonly char[] InvalidValueChars = { '"', ',', ';', '\\', ' ', '\t', '\n' };

        public static Dictionary<string, string> CookieHeaderToDictionary(string cookieHeader)
        {
            var cookieDictionary = new Dictionary<string, string>();
            if (cookieHeader == null)
            {
                return cookieDictionary;
            }

            var matches = CookieRegex.Match(cookieHeader);
            while (matches.Success)
            {
                if (matches.Groups.Count > 2 && !FilterProps.Contains(matches.Groups[1].Value.ToUpperInvariant()))
                {
                    cookieDictionary[matches.Groups[1].Value] = matches.Groups[2].Value;
                }

                matches = matches.NextMatch();
            }

            return cookieDictionary;
        }

        public static string CookieDictionaryToHeader(Dictionary<string, string> cookieDictionary)
        {
            if (cookieDictionary == null)
            {
                return "";
            }

            foreach (var kv in cookieDictionary)
            {
                if (kv.Key.IndexOfAny(InvalidKeyChars) > -1 || kv.Value.IndexOfAny(InvalidValueChars) > -1)
                {
                    throw new FormatException($"The cookie '{kv.Key}={kv.Value}' is malformed.");
                }
            }

            return string.Join("; ", cookieDictionary.Select(kv => kv.Key + "=" + kv.Value));
        }
    }
}
