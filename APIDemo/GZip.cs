using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace AvventoAPILibrary
{
    public static class GZip
    {
        public static byte[] Decompress(byte[] data)
        {
            try
            {
                using (MemoryStream inStream = new MemoryStream())
                {
                    int dataLength = BitConverter.ToInt32(data, 0);
                    inStream.Write(data, 4, data.Length - 4);
                    byte[] decompressed = new byte[dataLength];
                    inStream.Position = 0;
                    using (GZipStream zip = new GZipStream(inStream, CompressionMode.Decompress))
                    {
                        zip.Read(decompressed, 0, decompressed.Length);
                    }
                    return decompressed;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] CompressNoLength(byte[] data)
        {
            using (MemoryStream inStream = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(inStream, CompressionMode.Compress, true))
                {
                    zip.Write(data, 0, data.Length);
                }
                inStream.Position = 0;
                byte[] compressed = new byte[inStream.Length];
                inStream.Read(compressed, 0, compressed.Length);
                return compressed;
            }
        }

        public static byte[] DecompressNoLength(byte[] data)
        {
            using (MemoryStream inStream = new MemoryStream())
            {
                inStream.Write(data, 0, data.Length);
                byte[] decompressed = new byte[10000000];
                inStream.Position = 0;
                using (GZipStream zip = new GZipStream(inStream, CompressionMode.Decompress))
                {
                    zip.Read(decompressed, 0, decompressed.Length);
                }
                return decompressed;
            }
        }
    }
}