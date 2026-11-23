namespace Qord;

internal static class PixelFont
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int Scale = 2;
    private const int Spacing = 2;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
        ['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
        ['2'] = new[] { "01110", "10001", "00001", "00110", "01000", "10000", "11111" },
        ['3'] = new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
        ['4'] = new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
        ['5'] = new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
        ['6'] = new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
        ['7'] = new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
        ['8'] = new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
        ['9'] = new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" },
        ['-'] = new[] { "00000", "00000", "00000", "11111", "00000", "00000", "00000" },
    };

    public static int MeasureWidth(string text) => text.Length * GlyphWidth * Scale + Math.Max(0, text.Length - 1) * Spacing;

    public static IEnumerable<(int X, int Y)> Pixels(string text, int x, int y)
    {
        int cursor = x;
        foreach (char c in text)
        {
            string[] glyph = Glyphs[c];
            for (int row = 0; row < GlyphHeight; row++)
            {
                for (int column = 0; column < GlyphWidth; column++)
                {
                    if (glyph[row][column] != '1')
                        continue;
                    for (int scaleY = 0; scaleY < Scale; scaleY++)
                        for (int scaleX = 0; scaleX < Scale; scaleX++)
                            yield return (cursor + column * Scale + scaleX, y + row * Scale + scaleY);
                }
            }
            cursor += GlyphWidth * Scale + Spacing;
        }
    }
}
