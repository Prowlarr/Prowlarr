using System;
using System.Security.Cryptography;
using System.Text;

namespace NzbDrone.Common
{
    public static class HashUtil
    {
        public static string CalculateCrc(string input)
        {
            var mCrc = 0xffffffff;
            var bytes = Encoding.UTF8.GetBytes(input);
            foreach (var myByte in bytes)
            {
                mCrc ^= (uint)myByte << 24;
                for (var i = 0; i < 8; i++)
                {
                    if ((Convert.ToUInt32(mCrc) & 0x80000000) == 0x80000000)
                    {
                        mCrc = (mCrc << 1) ^ 0x04C11DB7;
                    }
                    else
                    {
                        mCrc <<= 1;
                    }
                }
            }

            return $"{mCrc:x8}";
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            var hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            return Convert.ToHexString(hashBytes);
        }

        public static string CalculateMd5(string s)
        {
            // Use input string to calculate MD5 hash
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(s);
                var hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }

        public static string AnonymousToken()
        {
            var seed = $"{Environment.ProcessorCount}_{Environment.OSVersion.Platform}_{Environment.MachineName}_{Environment.UserName}";
            return HashUtil.CalculateCrc(seed);
        }
    }
}
