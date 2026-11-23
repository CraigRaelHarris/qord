using System.IO.Compression;

namespace Qord;

internal static class PngEncoder
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly uint[] CrcTable = BuildCrcTable();

    public static void Encode(Stream output, byte[] rgb, int width, int height)
    {
        output.Write(PngSignature);

        var header = new byte[13];
        WriteBigEndianUInt32(header, 0, (uint)width);
        WriteBigEndianUInt32(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 2;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(output, "IHDR", header);

        byte[] rawData = BuildRawScanlines(rgb, width, height);
        byte[] compressed = ZlibCompress(rawData);
        WriteChunk(output, "IDAT", compressed);

        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static byte[] BuildRawScanlines(byte[] rgb, int width, int height)
    {
        int stride = width * 3;
        var raw = new byte[height * (stride + 1)];
        for (int y = 0; y < height; y++)
        {
            raw[y * (stride + 1)] = 0;
            Array.Copy(rgb, y * stride, raw, y * (stride + 1) + 1, stride);
        }
        return raw;
    }

    private static byte[] ZlibCompress(byte[] data)
    {
        using var deflate = new MemoryStream();
        using (var compressor = new DeflateStream(deflate, CompressionLevel.Optimal, leaveOpen: true))
            compressor.Write(data, 0, data.Length);

        byte[] deflateData = deflate.ToArray();
        var result = new byte[2 + deflateData.Length + 4];
        result[0] = 0x78;
        result[1] = 0x01;
        deflateData.CopyTo(result, 2);
        WriteBigEndianUInt32(result, result.Length - 4, Adler32(data));
        return result;
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> chunkHeader = stackalloc byte[8];
        WriteBigEndianUInt32(chunkHeader, 0, (uint)data.Length);
        for (int i = 0; i < 4; i++)
            chunkHeader[4 + i] = (byte)type[i];
        output.Write(chunkHeader);
        output.Write(data);

        uint crc = 0xFFFFFFFF;
        foreach (char c in type)
            crc = CrcTable[(crc ^ (byte)c) & 0xFF] ^ (crc >> 8);
        foreach (byte b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);

        Span<byte> crcBytes = stackalloc byte[4];
        WriteBigEndianUInt32(crcBytes, 0, crc ^ 0xFFFFFFFF);
        output.Write(crcBytes);
    }

    private static void WriteBigEndianUInt32(Span<byte> target, int offset, uint value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1;
        uint b = 0;
        foreach (byte value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
