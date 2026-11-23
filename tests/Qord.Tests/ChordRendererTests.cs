using System.Text;
using Qord;

namespace Qord.Tests;

public class ChordRendererTests : IDisposable
{
    private readonly string _directory;

    public ChordRendererTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "qord-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Render_Png_WritesPngMagic()
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), stream, ChordImageFormat.Png);
        byte[] bytes = stream.ToArray();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Render_Png_HasExpectedDimensions()
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), stream, ChordImageFormat.Png);
        byte[] png = stream.ToArray();
        Assert.Equal(116, ReadBigEndianUInt32(png, 16));
        Assert.Equal(96, ReadBigEndianUInt32(png, 20));
    }

    [Fact]
    public void Render_Svg_SpansFromAnchorToHighestKey()
    {
        string svg = RenderSvg("1,0,10001001");
        Assert.StartsWith("<svg", svg);
        Assert.Equal(9, CountRects(svg));
        Assert.Equal(3, CountOccurrences(svg, "#A9CDF0"));
        Assert.Equal(0, CountOccurrences(svg, "#2E86DE"));
    }

    [Fact]
    public void Render_Svg_HighlightsBlackKeys()
    {
        string svg = RenderSvg("0,0,010001001");
        Assert.Equal(10, CountRects(svg));
        Assert.Equal(3, CountOccurrences(svg, "#2E86DE"));
        Assert.Equal(0, CountOccurrences(svg, "#A9CDF0"));
    }

    [Fact]
    public void Render_Svg_DrawsBlackKeysAboveWhiteKeys()
    {
        string svg = RenderSvg("0,0,010001001");
        Assert.True(svg.LastIndexOf("#FFFFFF", StringComparison.Ordinal) < svg.IndexOf("#171717", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_ToFile_CreatesSvgFile()
    {
        string path = Path.Combine(_directory, "chord.svg");
        ChordRenderer.Render(ChordEncoding.Parse("0,0,010001001"), path, ChordImageFormat.Svg);
        Assert.True(File.Exists(path));
        Assert.StartsWith("<svg", File.ReadAllText(path));
    }

    private static string RenderSvg(string text)
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse(text), stream, ChordImageFormat.Svg);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static int ReadBigEndianUInt32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static int CountRects(string svg) => CountOccurrences(svg, "<rect");

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
