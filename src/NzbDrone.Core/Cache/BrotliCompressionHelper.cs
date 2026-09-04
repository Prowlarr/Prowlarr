using System;
using System.IO;
using System.IO.Compression;

namespace NzbDrone.Core.Cache
{
    public static class BrotliCompressionHelper
    {
        public static byte[] Compress(byte[] data, CompressionLevel level = CompressionLevel.Fastest)
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            using var outputStream = new MemoryStream();
            using (var brotliStream = new BrotliStream(outputStream, level))
            {
                brotliStream.Write(data, 0, data.Length);
            }

            return outputStream.ToArray();
        }

        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
            {
                return Array.Empty<byte>();
            }

            using var inputStream = new MemoryStream(compressedData);
            using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            brotliStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
