namespace Qord;

public static class ChordRenderer
{
    public static void Render(ChordEncoding chord, string path, ChordImageFormat format)
    {
        KeyboardPlan plan = KeyboardPlan.Create(chord);
        using FileStream stream = File.Create(path);
        Render(plan, stream, format);
    }

    public static void Render(ChordEncoding chord, Stream output, ChordImageFormat format)
    {
        if (chord is null)
            throw new ArgumentNullException(nameof(chord));
        if (output is null)
            throw new ArgumentNullException(nameof(output));
        Render(KeyboardPlan.Create(chord), output, format);
    }

    private static void Render(KeyboardPlan plan, Stream output, ChordImageFormat format)
    {
        switch (format)
        {
            case ChordImageFormat.Png:
                PngRenderer.Render(plan, output);
                break;
            case ChordImageFormat.Svg:
                SvgRenderer.Render(plan, output);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
}
