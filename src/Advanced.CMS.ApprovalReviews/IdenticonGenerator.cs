using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Advanced.CMS.ApprovalReviews;

internal class IdenticonGenerator
{
    private const int GridSize = 5;
    private const int CellSize = 20;

    public byte[] CreateIdenticon(string userName, int size = 100)
    {
        var hash = GetHash(userName);

        // Extract color from hash
        var r = hash[0];
        var g = hash[1];
        var b = hash[2];

        // Create a 5x5 grid, mirrored horizontally for symmetry
        var grid = new bool[GridSize, GridSize];
        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < (GridSize + 1) / 2; col++)
            {
                var byteIndex = 3 + row * 3 + col;
                var isColored = (hash[byteIndex % hash.Length] & 1) == 1;
                grid[row, col] = isColored;
                grid[row, GridSize - 1 - col] = isColored; // Mirror
            }
        }

        // Generate PNG
        var imageSize = GridSize * CellSize;
        var pixels = new byte[imageSize * imageSize * 3];

        for (var y = 0; y < imageSize; y++)
        {
            for (var x = 0; x < imageSize; x++)
            {
                var gridRow = y / CellSize;
                var gridCol = x / CellSize;
                var pixelIndex = (y * imageSize + x) * 3;

                if (grid[gridRow, gridCol])
                {
                    pixels[pixelIndex] = r;
                    pixels[pixelIndex + 1] = g;
                    pixels[pixelIndex + 2] = b;
                }
                else
                {
                    pixels[pixelIndex] = 240;
                    pixels[pixelIndex + 1] = 240;
                    pixels[pixelIndex + 2] = 240;
                }
            }
        }

        return CreatePng(pixels, imageSize, imageSize);
    }

    private static byte[] GetHash(string input)
    {
        using var md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(input));
    }

    private static byte[] CreatePng(byte[] rgb, int width, int height)
    {
        using var ms = new MemoryStream();

        // PNG signature
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        WriteChunk(ms, "IHDR", writer =>
        {
            writer.WriteBigEndian(width);
            writer.WriteBigEndian(height);
            writer.Write((byte)8);  // bit depth
            writer.Write((byte)2);  // color type (RGB)
            writer.Write((byte)0);  // compression
            writer.Write((byte)0);  // filter
            writer.Write((byte)0);  // interlace
        });

        // IDAT chunk (image data)
        WriteChunk(ms, "IDAT", writer =>
        {
            using var compressedStream = new MemoryStream();
            using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, true))
            {
                for (var y = 0; y < height; y++)
                {
                    deflate.WriteByte(0); // filter byte (none)
                    for (var x = 0; x < width; x++)
                    {
                        var i = (y * width + x) * 3;
                        deflate.WriteByte(rgb[i]);
                        deflate.WriteByte(rgb[i + 1]);
                        deflate.WriteByte(rgb[i + 2]);
                    }
                }
            }

            // zlib header
            writer.Write((byte)0x78);
            writer.Write((byte)0x9C);

            var compressed = compressedStream.ToArray();
            writer.Write(compressed);

            // Adler-32 checksum
            var adler = ComputeAdler32(rgb, width, height);
            writer.WriteBigEndian(adler);
        });

        // IEND chunk
        WriteChunk(ms, "IEND", _ => { });

        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, Action<BinaryWriter> writeData)
    {
        using var dataStream = new MemoryStream();
        using var dataWriter = new BinaryWriter(dataStream);
        writeData(dataWriter);
        dataWriter.Flush();
        var data = dataStream.ToArray();

        using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
        writer.WriteBigEndian(data.Length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes);
        writer.Write(data);

        var crc = ComputeCrc32(typeBytes, data);
        writer.WriteBigEndian((int)crc);
    }

    private static uint ComputeCrc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFF;
        foreach (var b in type) crc = UpdateCrc(crc, b);
        foreach (var b in data) crc = UpdateCrc(crc, b);
        return crc ^ 0xFFFFFFFF;
    }

    private static uint UpdateCrc(uint crc, byte b)
    {
        crc ^= b;
        for (var i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        return crc;
    }

    private static int ComputeAdler32(byte[] rgb, int width, int height)
    {
        uint a = 1, b = 0;
        for (var y = 0; y < height; y++)
        {
            // Filter byte
            a = (a + 0) % 65521;
            b = (b + a) % 65521;

            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                a = (a + rgb[i]) % 65521;
                b = (b + a) % 65521;
                a = (a + rgb[i + 1]) % 65521;
                b = (b + a) % 65521;
                a = (a + rgb[i + 2]) % 65521;
                b = (b + a) % 65521;
            }
        }
        return (int)((b << 16) | a);
    }
}

internal static class BinaryWriterExtensions
{
    public static void WriteBigEndian(this BinaryWriter writer, int value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }
}
