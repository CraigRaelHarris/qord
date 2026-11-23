using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Qord;

public sealed class ChordEncoding : IEquatable<ChordEncoding>
{
    public KeyLayout Layout { get; }
    public int Octave { get; }
    public IReadOnlyList<bool> Keys { get; }

    public int BaseMidi => PianoNote.AnchorFor(Layout) + 12 * Octave;

    public ChordEncoding(KeyLayout layout, int octave, IReadOnlyList<bool> keys)
    {
        if (keys is null)
            throw new ArgumentNullException(nameof(keys));
        if (keys.Count == 0)
            throw new ArgumentException("At least one key bit is required.", nameof(keys));
        if (!keys[^1])
            throw new ArgumentException("The last key bit must be set: the bit string ends at the highest used key.", nameof(keys));
        long baseMidi = PianoNote.AnchorFor(layout) + 12L * octave;
        if (baseMidi < PianoNote.Lowest || baseMidi > PianoNote.Highest)
            throw new ArgumentException($"Octave {octave} puts the starting key ({layout} layout) outside the 88-key piano range.", nameof(octave));
        if (baseMidi + keys.Count - 1 > PianoNote.Highest)
            throw new ArgumentException($"The highest key (offset {keys.Count - 1}) is outside the 88-key piano range.", nameof(keys));
        Layout = layout;
        Octave = octave;
        Keys = keys.ToArray();
    }

    public static ChordEncoding FromKeyOffsets(KeyLayout layout, int octave, IEnumerable<int> usedKeyOffsets)
    {
        if (usedKeyOffsets is null)
            throw new ArgumentNullException(nameof(usedKeyOffsets));
        int[] offsets = usedKeyOffsets.OrderBy(offset => offset).ToArray();
        if (offsets.Length == 0)
            throw new ArgumentException("At least one key offset is required.", nameof(usedKeyOffsets));
        if (offsets[0] < 0)
            throw new ArgumentException("Key offsets must not be negative.", nameof(usedKeyOffsets));
        var keys = new bool[offsets[^1] + 1];
        foreach (int offset in offsets)
            keys[offset] = true;
        return new ChordEncoding(layout, octave, keys);
    }

    public static ChordEncoding Parse(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));
        string[] parts = text.Split(',');
        if (parts.Length != 3)
            throw new FormatException($"Expected '<layout>,<octave>,<keys>' with exactly 3 comma-separated fields, got {parts.Length}.");
        KeyLayout layout = ParseLayout(parts[0]);
        int octave = ParseOctave(parts[1]);
        bool[] keys = ParseKeys(parts[2]);
        try
        {
            return new ChordEncoding(layout, octave, keys);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException(exception.Message);
        }
    }

    public static bool TryParse(string? text, [NotNullWhen(true)] out ChordEncoding? encoding)
    {
        if (text is null)
        {
            encoding = null;
            return false;
        }
        try
        {
            encoding = Parse(text);
            return true;
        }
        catch (FormatException)
        {
            encoding = null;
            return false;
        }
    }

    private static KeyLayout ParseLayout(string part)
    {
        string trimmed = part.Trim();
        return trimmed switch
        {
            "0" => KeyLayout.F,
            "1" => KeyLayout.C,
            _ => throw new FormatException($"Layout must be 0 (from F) or 1 (from C), got '{trimmed}'."),
        };
    }

    private static int ParseOctave(string part)
    {
        string trimmed = part.Trim();
        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int octave))
            throw new FormatException($"Octave must be an integer, got '{trimmed}'.");
        return octave;
    }

    private static bool[] ParseKeys(string part)
    {
        string trimmed = part.Trim();
        if (trimmed.Length == 0)
            throw new FormatException("Keys must not be empty.");
        if (trimmed.Length > PianoNote.KeyCount)
            throw new FormatException($"Keys must not exceed {PianoNote.KeyCount} digits (the 88-key piano range).");
        var keys = new bool[trimmed.Length];
        for (int i = 0; i < trimmed.Length; i++)
        {
            keys[i] = trimmed[i] switch
            {
                '0' => false,
                '1' => true,
                _ => throw new FormatException($"Keys must contain only '0' and '1' digits, found '{trimmed[i]}' at position {i}."),
            };
        }
        if (!keys[^1])
            throw new FormatException("Keys must end with '1': trailing zeros are not allowed, the bit string ends at the highest used key.");
        return keys;
    }

    public IReadOnlyList<PianoNote> GetNotes()
    {
        int baseMidi = BaseMidi;
        var notes = new List<PianoNote>();
        for (int i = 0; i < Keys.Count; i++)
        {
            if (Keys[i])
                notes.Add(new PianoNote(baseMidi + i));
        }
        return notes;
    }

    public ChordEncoding WithOctave(int octave) => new(Layout, octave, Keys);

    public ChordEncoding TranscodeTo(KeyLayout targetLayout)
    {
        if (targetLayout == Layout)
            return this;
        int[] noteMidi = GetNotes().Select(note => note.Midi).ToArray();
        int anchor = PianoNote.AnchorFor(targetLayout);
        int lowest = noteMidi.Min();
        int octave = (int)Math.Floor((lowest - anchor) / 12.0);
        int baseMidi = anchor + 12 * octave;
        return FromKeyOffsets(targetLayout, octave, noteMidi.Select(midi => midi - baseMidi));
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append((int)Layout).Append(',').Append(Octave).Append(',');
        foreach (bool used in Keys)
            builder.Append(used ? '1' : '0');
        return builder.ToString();
    }

    public bool Equals(ChordEncoding? other) =>
        other is not null
        && Layout == other.Layout
        && Octave == other.Octave
        && Keys.Count == other.Keys.Count
        && Keys.SequenceEqual(other.Keys);

    public override bool Equals(object? obj) => obj is ChordEncoding other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Layout);
        hash.Add(Octave);
        foreach (bool used in Keys)
            hash.Add(used);
        return hash.ToHashCode();
    }
}
