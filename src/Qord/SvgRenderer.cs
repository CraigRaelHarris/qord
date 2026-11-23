using System.Text;

namespace Qord;

internal static class SvgRenderer
{
    public static void Render(KeyboardPlan plan, Stream output)
    {
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(plan.Width)
            .Append("\" height=\"").Append(plan.Height)
            .Append("\" viewBox=\"0 0 ").Append(plan.Width).Append(' ').Append(plan.Height).Append("\">\n");
        AppendRect(svg, 0, 0, plan.Width, plan.Height, Palette.Background);
        svg.Append("  <g stroke=\"").Append(Palette.Border.Hex)
            .Append("\" stroke-width=\"").Append(KeyboardGeometry.Border * 2).Append("\">\n");
        foreach (KeyBox key in plan.KeysInPaintOrder)
            AppendRect(svg, key.X, key.Y, key.Width, key.Height, Palette.FillOf(key), indent: "    ");
        svg.Append("  </g>\n</svg>\n");

        byte[] bytes = Encoding.UTF8.GetBytes(svg.ToString());
        output.Write(bytes, 0, bytes.Length);
    }

    private static void AppendRect(StringBuilder svg, int x, int y, int width, int height, Rgb fill, string indent = "  ")
    {
        svg.Append(indent).Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y)
            .Append("\" width=\"").Append(width).Append("\" height=\"").Append(height)
            .Append("\" fill=\"").Append(fill.Hex).Append("\" rx=\"2\"/>\n");
    }
}
