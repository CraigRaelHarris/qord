namespace Qord;

internal readonly record struct Rgb(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
}

internal static class Palette
{
    public static readonly Rgb Background = new(0xF2, 0xF2, 0xF0);
    public static readonly Rgb Border = new(0x2B, 0x2B, 0x2B);
    public static readonly Rgb WhiteKey = new(0xFF, 0xFF, 0xFF);
    public static readonly Rgb BlackKey = new(0x17, 0x17, 0x17);
    public static readonly Rgb WhiteKeyUsed = new(0xA9, 0xCD, 0xF0);
    public static readonly Rgb BlackKeyUsed = new(0x2E, 0x86, 0xDE);
    public static readonly Rgb QadroLine = new(0x8C, 0x8C, 0x8C);

    public static Rgb FillOf(KeyBox key) => (key.IsBlack, key.IsUsed) switch
    {
        (false, false) => WhiteKey,
        (false, true) => WhiteKeyUsed,
        (true, false) => BlackKey,
        (true, true) => BlackKeyUsed,
    };
}
