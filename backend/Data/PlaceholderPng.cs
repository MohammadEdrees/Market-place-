using System.IO.Compression;
using System.Text;

namespace MarketWorkplace.Api.Data;

/// <summary>
/// Dependency-free PNG encoder used to generate the placeholder gallery images that
/// <c>DbInitializer</c> seeds into <c>wwwroot/images</c> at startup (gradient, checker
/// and stripe patterns in colours derived from the listing).
/// </summary>
public static class PlaceholderPng
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Encodes an 8-bit truecolour PNG (no interlace, scanline filter None).</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="from">First RGB colour.</param>
    /// <param name="to">Second RGB colour the pattern blends towards.</param>
    /// <param name="style">0 = diagonal gradient, 1 = checkerboard, 2 = stripes.</param>
    public static byte[] Generate(int width, int height, byte[] from, byte[] to, int style)
    {
        var stride = width * 3;
        var raw = new byte[height * (stride + 1)];
        for (var y = 0; y < height; y++)
        {
            var row = y * (stride + 1);
            raw[row] = 0; // scanline filter: None
            for (var x = 0; x < width; x++)
            {
                var t = Pattern(width, height, style, x, y);
                var edge = x < 5 || y < 5 || x >= width - 5 || y >= height - 5 ? 0.65 : 1.0;
                var i = row + 1 + x * 3;
                raw[i] = (byte)(Mix(from[0], to[0], t) * edge);
                raw[i + 1] = (byte)(Mix(from[1], to[1], t) * edge);
                raw[i + 2] = (byte)(Mix(from[2], to[2], t) * edge);
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write(Signature);

        var header = new byte[13];
        WriteBigEndian(header, 0, (uint)width);
        WriteBigEndian(header, 4, (uint)height);
        header[8] = 8; // bit depth
        header[9] = 2; // colour type: truecolour RGB
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    /// <summary>Blends [0..1] across the image according to the style; also darkens the border.</summary>
    private static double Pattern(int width, int height, int style, int x, int y)
    {
        var u = width <= 1 ? 0 : x / (double)(width - 1);
        var v = height <= 1 ? 0 : y / (double)(height - 1);
        return style % 3 switch
        {
            1 => (x / 48 + y / 48) % 2 == 0 ? u : 1 - u,
            2 => ((x + y) / 64) % 2 == 0 ? v : 1 - v,
            _ => (u + v) / 2,
        };
    }

    private static byte Mix(byte from, byte to, double t) =>
        (byte)Math.Clamp(from + (to - from) * t, byte.MinValue, byte.MaxValue);

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeAndData = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type, 0, 4, typeAndData, 0);
        data.CopyTo(typeAndData, 4);

        WriteBigEndian(stream, (uint)data.Length);
        stream.Write(typeAndData);
        WriteBigEndian(stream, Crc32(typeAndData));
    }

    private static void WriteBigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static void WriteBigEndian(Stream stream, uint value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    /// <summary>Standard CRC-32 required by every PNG chunk.</summary>
    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? crc >> 1 ^ 0xEDB88320u : crc >> 1;
            }
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
