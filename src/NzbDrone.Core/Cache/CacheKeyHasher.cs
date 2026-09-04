using System;
using System.Security.Cryptography;
using System.Text;

namespace NzbDrone.Core.Cache
{
    public static class CacheKeyHasher
    {
        public static string Hash(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
        }
    }
}
