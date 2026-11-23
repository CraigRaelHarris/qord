namespace Qord;

internal static class QadroPngWriter
{
    public static void Render(QadroPlan plan, Stream output)
    {
        var buffer = new byte[plan.Width * plan.Height * 3];
        FillRect(buffer, plan.Width, 0, 0, plan.Width, plan.Height, Palette.Background);

        foreach (int lineX in plan.VerticalLineXs)
            FillRect(buffer, plan.Width, lineX - 1, plan.LineTop, 2, plan.LineBottom - plan.LineTop, Palette.QadroLine);

        foreach (QadroCircle circle in plan.Circles)
        {
            switch (circle.Kind)
            {
                case QadroCircleKind.Filled:
                    FillCircle(buffer, plan.Width, circle.X, circle.Y, QadroGeometry.Radius, Palette.BlackKey);
                    break;
                case QadroCircleKind.Hollow:
                    FillCircle(buffer, plan.Width, circle.X, circle.Y, QadroGeometry.Radius, Palette.Border);
                    FillCircle(buffer, plan.Width, circle.X, circle.Y, QadroGeometry.Radius - 2, Palette.WhiteKey);
                    break;
                case QadroCircleKind.Dot:
                    FillCircle(buffer, plan.Width, circle.X, circle.Y, QadroGeometry.DotRadius, Palette.BlackKey);
                    break;
            }
        }

        if (plan.HasTopRow)
        {
            DrawOctaveText(buffer, plan.Width, plan.OctaveText!, plan.OctaveTextCenterX, plan.OctaveTextTop);
            DrawArrow(buffer, plan.Width, plan.ArrowCenterX, plan.ArrowY, plan.ArrowDirection);
        }

        PngEncoder.Encode(output, buffer, plan.Width, plan.Height);
    }

    private static void DrawOctaveText(byte[] buffer, int stride, string text, int centerX, int top)
    {
        int x = centerX - PixelFont.MeasureWidth(text) / 2;
        foreach ((int pixelX, int pixelY) in PixelFont.Pixels(text, x, top))
            SetPixel(buffer, stride, pixelX, pixelY, Palette.Border);
    }

    private static void DrawArrow(byte[] buffer, int stride, int centerX, int arrowY, int direction)
    {
        int halfLength = QadroGeometry.ArrowLength / 2;
        FillRect(buffer, stride, centerX - halfLength, arrowY - 1, QadroGeometry.ArrowLength, 2, Palette.Border);
        for (int i = 0; i <= QadroGeometry.ArrowHeadDepth; i++)
        {
            int halfHeight = QadroGeometry.ArrowHeadHalfHeight * (QadroGeometry.ArrowHeadDepth - i) / QadroGeometry.ArrowHeadDepth;
            int headX = direction >= 0 ? centerX + halfLength + i : centerX - halfLength - i;
            FillRect(buffer, stride, headX, arrowY - halfHeight, 1, halfHeight * 2 + 1, Palette.Border);
        }
    }

    private static void FillCircle(byte[] buffer, int stride, int centerX, int centerY, int radius, Rgb color)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                    SetPixel(buffer, stride, x, y, color);
            }
        }
    }

    private static void FillRect(byte[] buffer, int stride, int x, int y, int width, int height, Rgb color)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
                SetPixel(buffer, stride, column, row, color);
        }
    }

    private static void SetPixel(byte[] buffer, int stride, int x, int y, Rgb color)
    {
        int pixelStart = (y * stride + x) * 3;
        buffer[pixelStart] = color.R;
        buffer[pixelStart + 1] = color.G;
        buffer[pixelStart + 2] = color.B;
    }
}
