namespace Qord;

public readonly record struct PianoNote(int Midi)
{
    public const int MiddleC = 60;
    public const int FBelowMiddleC = 53;
    public const int Lowest = 21;
    public const int Highest = 108;
    public const int KeyCount = Highest - Lowest + 1;

    private static readonly string[] Names =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B",
    };

    public string Name => Names[Midi % 12] + (Midi / 12 - 1);

    public bool IsBlackKey => IsBlack(Midi);

    public static bool IsBlack(int midi) => midi % 12 is 1 or 3 or 6 or 8 or 10;

    public static int AnchorFor(KeyLayout layout) => layout switch
    {
        KeyLayout.C => MiddleC,
        KeyLayout.F => FBelowMiddleC,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };

    public override string ToString() => Name;
}
