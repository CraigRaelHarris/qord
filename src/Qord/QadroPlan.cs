using System.Globalization;

namespace Qord;

internal enum QadroCircleKind
{
    Filled,
    Hollow,
    Dot,
}

internal sealed record QadroCircle(int X, int Y, QadroCircleKind Kind);

internal sealed class QadroPlan
{
    public int Width { get; }
    public int Height { get; }
    public string? OctaveText { get; }
    public int OctaveTextCenterX { get; }
    public int OctaveTextTop { get; }
    public int ArrowY { get; }
    public int ArrowCenterX { get; }
    public int ArrowDirection { get; }
    public IReadOnlyList<QadroCircle> Circles { get; }
    public IReadOnlyList<int> VerticalLineXs { get; }
    public int LineTop { get; }
    public int LineBottom { get; }

    public bool HasTopRow => OctaveText is not null;

    private QadroPlan(
        int width,
        int height,
        string? octaveText,
        int octaveTextCenterX,
        int octaveTextTop,
        int arrowY,
        int arrowCenterX,
        int arrowDirection,
        IReadOnlyList<QadroCircle> circles,
        IReadOnlyList<int> verticalLineXs,
        int lineTop,
        int lineBottom)
    {
        Width = width;
        Height = height;
        OctaveText = octaveText;
        OctaveTextCenterX = octaveTextCenterX;
        OctaveTextTop = octaveTextTop;
        ArrowY = arrowY;
        ArrowCenterX = arrowCenterX;
        ArrowDirection = arrowDirection;
        Circles = circles;
        VerticalLineXs = verticalLineXs;
        LineTop = lineTop;
        LineBottom = lineBottom;
    }

    public static QadroPlan Create(ChordEncoding chord)
    {
        IReadOnlyList<bool> keys = chord.Keys;
        int lastUsed = keys.Count - 1;
        int baseMidi = chord.BaseMidi;

        bool IsBlack(int offset) => PianoNote.IsBlack(baseMidi + offset);
        bool IsWhite(int offset) => !IsBlack(offset);
        bool IsBoundary(int offset) => IsWhite(offset) && IsWhite(offset + 1);

        var whiteOffsets = new List<int>();
        for (int offset = 0; offset <= lastUsed; offset++)
        {
            if (IsWhite(offset))
                whiteOffsets.Add(offset);
        }

        int blackMax = 12 * (lastUsed / 12 + 1) - 1;
        var blackOffsets = new List<int>();
        for (int offset = 0; offset <= blackMax; offset++)
        {
            if (IsBlack(offset))
                blackOffsets.Add(offset);
        }

        int maxOffset = Math.Max(whiteOffsets[^1], blackMax);

        var x = new int[maxOffset + 1];
        for (int offset = 1; offset <= maxOffset; offset++)
            x[offset] = x[offset - 1] + QadroGeometry.Semitone + (IsBoundary(offset - 1) ? QadroGeometry.BoundaryGap : 0);

        string? octaveText = chord.Octave == 0 ? null : chord.Octave.ToString(CultureInfo.InvariantCulture);
        int width = QadroGeometry.OriginX + x[maxOffset] + QadroGeometry.Radius + QadroGeometry.PadX;
        int height = QadroGeometry.PadY
            + (octaveText is null ? 0 : QadroGeometry.TopArea)
            + QadroGeometry.Radius * 4
            + QadroGeometry.RowGap
            + QadroGeometry.PadY;

        int bottomCenterY = height - QadroGeometry.PadY - QadroGeometry.Radius;
        int middleCenterY = bottomCenterY - QadroGeometry.RowGap - QadroGeometry.Radius * 2;

        var circles = new List<QadroCircle>();
        foreach (int offset in blackOffsets)
        {
            bool used = offset < keys.Count && keys[offset];
            circles.Add(new QadroCircle(
                QadroGeometry.OriginX + x[offset],
                middleCenterY,
                used ? QadroCircleKind.Filled : QadroCircleKind.Hollow));
        }
        foreach (int offset in whiteOffsets)
        {
            circles.Add(new QadroCircle(
                QadroGeometry.OriginX + x[offset],
                bottomCenterY,
                keys[offset] ? QadroCircleKind.Filled : QadroCircleKind.Dot));
        }

        int cStart = chord.Layout == KeyLayout.F ? 7 : 0;
        var lineXs = new List<int>();
        for (int c = cStart; c <= maxOffset; c += 12)
            lineXs.Add(QadroGeometry.OriginX + x[c] - (QadroGeometry.Semitone + QadroGeometry.BoundaryGap) / 2);

        return new QadroPlan(
            width,
            height,
            octaveText,
            width / 2,
            QadroGeometry.PadY + 2,
            QadroGeometry.PadY + 26,
            width / 2,
            Math.Sign(chord.Octave),
            circles,
            lineXs,
            middleCenterY - QadroGeometry.Radius,
            bottomCenterY + QadroGeometry.Radius);
    }
}
