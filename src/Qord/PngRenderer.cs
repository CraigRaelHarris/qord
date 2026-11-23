namespace Qord;

internal static class PngRenderer
{
    public static void Render(KeyboardPlan plan, Stream output)
    {
        byte[] pixels = Rasterize(plan);
        PngEncoder.Encode(output, pixels, plan.Width, plan.Height);
    }

    private static byte[] Rasterize(KeyboardPlan plan)
    {
        var buffer = new byte[plan.Width * plan.Height * 3];
        FillRect(buffer, plan.Width, 0, 0, plan.Width, plan.Height, Palette.Background);
        foreach (KeyBox key in plan.KeysInPaintOrder)
            FillKey(buffer, plan.Width, key);
        return buffer;
    }

    private static void FillKey(byte[] buffer, int stride, KeyBox key)
    {
        int border = KeyboardGeometry.Border;
        FillRect(buffer, stride, key.X, key.Y, key.Width, key.Height, Palette.Border);
        FillRect(buffer, stride, key.X + border, key.Y + border, key.Width - border * 2, key.Height - border * 2, Palette.FillOf(key));
    }

    private static void FillRect(byte[] buffer, int stride, int x, int y, int width, int height, Rgb color)
    {
        for (int row = y; row < y + height; row++)
        {
            int lineStart = row * stride * 3;
            for (int column = x; column < x + width; column++)
            {
                int pixelStart = lineStart + column * 3;
                buffer[pixelStart] = color.R;
                buffer[pixelStart + 1] = color.G;
                buffer[pixelStart + 2] = color.B;
            }
        }
    }
}
