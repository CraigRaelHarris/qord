namespace Qord;

public static class ChordRenderer
{
    public static void Render(ChordEncoding chord, string path, ChordImageFormat format, VisualLayout visual = VisualLayout.Qadro)
    {
        if (chord is null)
            throw new ArgumentNullException(nameof(chord));
        switch (visual)
        {
            case VisualLayout.Traditional:
            {
                KeyboardPlan plan = KeyboardPlan.Create(chord);
                using FileStream stream = File.Create(path);
                Render(plan, stream, format);
                break;
            }
            case VisualLayout.Qadro:
                QadroRenderer.Render(chord, path, format);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(visual), visual, null);
        }
    }

    public static void Render(ChordEncoding chord, Stream output, ChordImageFormat format, VisualLayout visual = VisualLayout.Qadro)
    {
        if (chord is null)
            throw new ArgumentNullException(nameof(chord));
        if (output is null)
            throw new ArgumentNullException(nameof(output));
        switch (visual)
        {
            case VisualLayout.Traditional:
                Render(KeyboardPlan.Create(chord), output, format);
                break;
            case VisualLayout.Qadro:
                QadroRenderer.Render(chord, output, format);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(visual), visual, null);
        }
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
