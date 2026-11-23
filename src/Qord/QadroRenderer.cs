namespace Qord;

internal static class QadroRenderer
{
    public static void Render(ChordEncoding chord, Stream output, ChordImageFormat format)
    {
        QadroPlan plan = QadroPlan.Create(chord);
        Render(plan, output, format);
    }

    public static void Render(ChordEncoding chord, string path, ChordImageFormat format)
    {
        QadroPlan plan = QadroPlan.Create(chord);
        using FileStream stream = File.Create(path);
        Render(plan, stream, format);
    }

    private static void Render(QadroPlan plan, Stream output, ChordImageFormat format)
    {
        switch (format)
        {
            case ChordImageFormat.Png:
                QadroPngWriter.Render(plan, output);
                break;
            case ChordImageFormat.Svg:
                QadroSvgWriter.Render(plan, output);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
}
