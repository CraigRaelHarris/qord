namespace Qord;

internal sealed record KeyBox(bool IsBlack, bool IsUsed, int X, int Y, int Width, int Height);

internal sealed class KeyboardPlan
{
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<KeyBox> WhiteKeys { get; }
    public IReadOnlyList<KeyBox> BlackKeys { get; }

    public IEnumerable<KeyBox> KeysInPaintOrder => WhiteKeys.Concat(BlackKeys);

    private KeyboardPlan(int width, int height, IReadOnlyList<KeyBox> whiteKeys, IReadOnlyList<KeyBox> blackKeys)
    {
        Width = width;
        Height = height;
        WhiteKeys = whiteKeys;
        BlackKeys = blackKeys;
    }

    public static KeyboardPlan Create(ChordEncoding chord)
    {
        IReadOnlyList<PianoNote> notes = chord.GetNotes();
        var used = new HashSet<int>(notes.Select(note => note.Midi));
        int first = chord.BaseMidi;
        int last = used.Max();

        var whiteKeys = new List<KeyBox>();
        var blackKeys = new List<KeyBox>();
        int whiteIndex = 0;
        for (int midi = first; midi <= last; midi++)
        {
            bool isUsed = used.Contains(midi);
            if (PianoNote.IsBlack(midi))
            {
                blackKeys.Add(new KeyBox(
                    IsBlack: true,
                    isUsed,
                    KeyboardGeometry.Padding + whiteIndex * KeyboardGeometry.WhiteKeyWidth - KeyboardGeometry.BlackKeyWidth / 2,
                    KeyboardGeometry.Padding,
                    KeyboardGeometry.BlackKeyWidth,
                    KeyboardGeometry.BlackKeyHeight));
            }
            else
            {
                whiteKeys.Add(new KeyBox(
                    IsBlack: false,
                    isUsed,
                    KeyboardGeometry.Padding + whiteIndex * KeyboardGeometry.WhiteKeyWidth,
                    KeyboardGeometry.Padding,
                    KeyboardGeometry.WhiteKeyWidth,
                    KeyboardGeometry.WhiteKeyHeight));
                whiteIndex++;
            }
        }

        int width = KeyboardGeometry.Padding * 2 + whiteIndex * KeyboardGeometry.WhiteKeyWidth;
        int height = KeyboardGeometry.Padding * 2 + KeyboardGeometry.WhiteKeyHeight;
        return new KeyboardPlan(width, height, whiteKeys, blackKeys);
    }
}
