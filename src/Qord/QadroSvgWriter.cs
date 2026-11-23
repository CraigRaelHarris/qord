using System.Text;

namespace Qord;

internal static class QadroSvgWriter
{
    public static void Render(QadroPlan plan, Stream output)
    {
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(plan.Width)
            .Append("\" height=\"").Append(plan.Height)
            .Append("\" viewBox=\"0 0 ").Append(plan.Width).Append(' ').Append(plan.Height).Append("\">\n");
        AppendRect(svg, 0, 0, plan.Width, plan.Height, Palette.Background);

        foreach (int lineX in plan.VerticalLineXs)
        {
            svg.Append("  <line x1=\"").Append(lineX).Append("\" y1=\"").Append(plan.LineTop)
                .Append("\" x2=\"").Append(lineX).Append("\" y2=\"").Append(plan.LineBottom)
                .Append("\" stroke=\"").Append(Palette.QadroLine.Hex).Append("\" stroke-width=\"2\"/>\n");
        }

        foreach (QadroCircle circle in plan.Circles)
        {
            switch (circle.Kind)
            {
                case QadroCircleKind.Filled:
                    AppendCircle(svg, circle, QadroGeometry.Radius, Palette.BlackKey);
                    break;
                case QadroCircleKind.Hollow:
                    svg.Append("    <circle cx=\"").Append(circle.X).Append("\" cy=\"").Append(circle.Y)
                        .Append("\" r=\"").Append(QadroGeometry.Radius)
                        .Append("\" fill=\"").Append(Palette.WhiteKey.Hex)
                        .Append("\" stroke=\"").Append(Palette.Border.Hex).Append("\" stroke-width=\"2\"/>\n");
                    break;
                case QadroCircleKind.Dot:
                    AppendCircle(svg, circle, QadroGeometry.DotRadius, Palette.BlackKey);
                    break;
            }
        }

        if (plan.HasTopRow)
        {
            svg.Append("  <text x=\"").Append(plan.OctaveTextCenterX).Append("\" y=\"").Append(plan.OctaveTextTop + 11)
                .Append("\" font-family=\"Consolas, monospace\" font-size=\"14\" fill=\"").Append(Palette.Border.Hex)
                .Append("\" text-anchor=\"middle\">").Append(plan.OctaveText).Append("</text>\n");

            int halfLength = QadroGeometry.ArrowLength / 2;
            int lineStart = plan.ArrowCenterX - halfLength;
            int lineEnd = plan.ArrowCenterX + halfLength;
            svg.Append("  <line x1=\"").Append(lineStart).Append("\" y1=\"").Append(plan.ArrowY)
                .Append("\" x2=\"").Append(lineEnd).Append("\" y2=\"").Append(plan.ArrowY)
                .Append("\" stroke=\"").Append(Palette.Border.Hex).Append("\" stroke-width=\"2\"/>\n");
            int baseX = plan.ArrowDirection >= 0 ? lineEnd : lineStart;
            int tipX = plan.ArrowDirection >= 0 ? lineEnd + QadroGeometry.ArrowHeadDepth : lineStart - QadroGeometry.ArrowHeadDepth;
            svg.Append("  <polygon points=\"").Append(baseX).Append(',').Append(plan.ArrowY - QadroGeometry.ArrowHeadHalfHeight)
                .Append(' ').Append(baseX).Append(',').Append(plan.ArrowY + QadroGeometry.ArrowHeadHalfHeight)
                .Append(' ').Append(tipX).Append(',').Append(plan.ArrowY)
                .Append("\" fill=\"").Append(Palette.Border.Hex).Append("\"/>\n");
        }

        svg.Append("</svg>\n");

        byte[] bytes = Encoding.UTF8.GetBytes(svg.ToString());
        output.Write(bytes, 0, bytes.Length);
    }

    private static void AppendCircle(StringBuilder svg, QadroCircle circle, int radius, Rgb fill)
    {
        svg.Append("    <circle cx=\"").Append(circle.X).Append("\" cy=\"").Append(circle.Y)
            .Append("\" r=\"").Append(radius)
            .Append("\" fill=\"").Append(fill.Hex).Append("\"/>\n");
    }

    private static void AppendRect(StringBuilder svg, int x, int y, int width, int height, Rgb fill)
    {
        svg.Append("  <rect x=\"").Append(x).Append("\" y=\"").Append(y)
            .Append("\" width=\"").Append(width).Append("\" height=\"").Append(height)
            .Append("\" fill=\"").Append(fill.Hex).Append("\"/>\n");
    }
}
