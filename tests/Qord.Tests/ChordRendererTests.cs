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
        ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), stream, ChordImageFormat.Png, VisualLayout.Traditional);
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
        ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), stream, ChordImageFormat.Png, VisualLayout.Traditional);
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
        ChordRenderer.Render(ChordEncoding.Parse("0,0,010001001"), path, ChordImageFormat.Svg, VisualLayout.Traditional);
        Assert.True(File.Exists(path));
        Assert.StartsWith("<svg", File.ReadAllText(path));
    }

    [Fact]
    public void Render_DefaultsToQadro()
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse("0,0,010001001"), stream, ChordImageFormat.Png);
        byte[] png = stream.ToArray();
        Assert.Equal(0x89, png[0]);
        Assert.Equal(195, ReadBigEndianUInt32(png, 16));
        Assert.Equal(66, ReadBigEndianUInt32(png, 20));
    }

    [Fact]
    public void Render_ExplicitTraditionalVisual_MatchesTraditional()
    {
        using var implicitStream = new MemoryStream();
        using var traditionalStream = new MemoryStream();
        ChordEncoding chord = ChordEncoding.Parse("1,0,10001001");
        ChordRenderer.Render(chord, implicitStream, ChordImageFormat.Png, VisualLayout.Traditional);
        ChordRenderer.Render(chord, traditionalStream, ChordImageFormat.Png, VisualLayout.Traditional);
        Assert.Equal(implicitStream.ToArray(), traditionalStream.ToArray());
    }

    [Fact]
    public void Render_Qadro_Png_HasExpectedDimensions()
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse("0,0,010001001"), stream, ChordImageFormat.Png, VisualLayout.Qadro);
        byte[] png = stream.ToArray();
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal(195, ReadBigEndianUInt32(png, 16));
        Assert.Equal(66, ReadBigEndianUInt32(png, 20));
    }

    [Fact]
    public void Render_Qadro_Png_OctaveRowExtendsHeight()
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse("1,1,10001001"), stream, ChordImageFormat.Png, VisualLayout.Qadro);
        byte[] png = stream.ToArray();
        Assert.Equal(195, ReadBigEndianUInt32(png, 16));
        Assert.Equal(110, ReadBigEndianUInt32(png, 20));
    }

    [Fact]
    public void Render_Qadro_ToFile_CreatesPngFile()
    {
        string path = Path.Combine(_directory, "qadro.png");
        ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), path, ChordImageFormat.Png, VisualLayout.Qadro);
        Assert.True(File.Exists(path));
        byte[] header = new byte[4];
        using (FileStream stream = File.OpenRead(path))
            stream.ReadExactly(header);
        Assert.Equal(0x89, header[0]);
    }

    [Fact]
    public void Render_Qadro_Svg_FSharpMajor()
    {
        string svg = RenderQadroSvg("0,0,010001001");
        Assert.StartsWith("<svg", svg);
        Assert.Contains("width=\"195\" height=\"66\"", svg);
        Assert.DoesNotContain("<text", svg);
        Assert.Equal(10, CountOccurrences(svg, "<circle"));
        Assert.Equal(8, CountOccurrences(svg, "#171717"));
        Assert.Equal(2, CountOccurrences(svg, "#FFFFFF"));
        Assert.Equal(1, CountOccurrences(svg, "<line"));
        Assert.Contains("x1=\"112\"", svg);
    }

    [Fact]
    public void Render_Qadro_Svg_CMajorOctaveUp()
    {
        string svg = RenderQadroSvg("1,1,10001001");
        Assert.Contains("width=\"195\" height=\"110\"", svg);
        Assert.Contains(">1</text>", svg);
        Assert.Contains("<polygon points=\"115,30 115,42 123,36\"", svg);
        Assert.Equal(10, CountOccurrences(svg, "<circle"));
        Assert.Equal(5, CountOccurrences(svg, "#171717"));
        Assert.Equal(5, CountOccurrences(svg, "#FFFFFF"));
        Assert.Equal(2, CountOccurrences(svg, "<line"));
    }

    [Fact]
    public void Render_Qadro_Svg_Am6OctaveDownSpansTwoOctaves()
    {
        string svg = RenderQadroSvg("0,-1,00001001000101");
        Assert.Contains("width=\"379\" height=\"110\"", svg);
        Assert.Contains(">-1</text>", svg);
        Assert.Contains("<polygon points=\"171,30 171,42 163,36\"", svg);
        Assert.Equal(18, CountOccurrences(svg, "<circle"));
        Assert.Equal(9, CountOccurrences(svg, "#171717"));
        Assert.Equal(9, CountOccurrences(svg, "#FFFFFF"));
        Assert.Equal(3, CountOccurrences(svg, "<line"));
    }

    private static string RenderQadroSvg(string text)
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse(text), stream, ChordImageFormat.Svg, VisualLayout.Qadro);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void Render_InvalidVisualLayout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ChordRenderer.Render(ChordEncoding.Parse("1,0,10001001"), new MemoryStream(), ChordImageFormat.Png, (VisualLayout)99));
    }

    private static string RenderSvg(string text)
    {
        using var stream = new MemoryStream();
        ChordRenderer.Render(ChordEncoding.Parse(text), stream, ChordImageFormat.Svg, VisualLayout.Traditional);
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
