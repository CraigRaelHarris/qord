using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qord;

public readonly record struct ChordEntry(string Symbol, ChordEncoding Encoding, bool IsUserDefined);

public sealed class ChordLibrary
{
    private const int FileVersion = 1;

    private static readonly Lazy<Dictionary<string, ChordEncoding>> BuiltInChords = new(CreateBuiltInChords);

    private readonly string? _path;
    private readonly Dictionary<string, ChordEncoding> _userChords = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _skippedSymbols = new();

    public string? CorruptLibraryBackupPath { get; private set; }

    public IReadOnlyList<string> SkippedSymbols => _skippedSymbols;

    public ChordLibrary()
        : this(DefaultPath)
    {
    }

    public ChordLibrary(string? userLibraryPath)
    {
        _path = userLibraryPath;
        if (_path is not null && File.Exists(_path))
            LoadUserChords();
    }

    public static string DefaultPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".qord",
            "chords.json");

    public bool TryGet(string symbol, [NotNullWhen(true)] out ChordEncoding? encoding)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (_userChords.TryGetValue(symbol, out ChordEncoding? user))
        {
            encoding = user;
            return true;
        }
        if (BuiltInChords.Value.TryGetValue(symbol, out ChordEncoding? builtIn))
        {
            encoding = builtIn;
            return true;
        }
        encoding = null;
        return false;
    }

    public IReadOnlyList<ChordEntry> List()
    {
        var merged = new Dictionary<string, ChordEntry>(StringComparer.OrdinalIgnoreCase);
        foreach ((string symbol, ChordEncoding encoding) in BuiltInChords.Value)
            merged[symbol] = new ChordEntry(symbol, encoding, IsUserDefined: false);
        foreach ((string symbol, ChordEncoding encoding) in _userChords)
            merged[symbol] = new ChordEntry(symbol, encoding, IsUserDefined: true);
        return merged.Values
            .OrderBy(entry => entry.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Add(string symbol, ChordEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(encoding);
        if (_path is null)
            throw new InvalidOperationException("No user library file is configured.");
        symbol = symbol.Trim();
        if (symbol.Length == 0)
            throw new ArgumentException("The chord symbol must not be empty.", nameof(symbol));
        _userChords[symbol] = encoding;
        SaveUserChords();
    }

    private void LoadUserChords()
    {
        ChordLibraryFile? file;
        try
        {
            using FileStream stream = File.OpenRead(_path!);
            file = JsonSerializer.Deserialize(stream, ChordLibraryJsonContext.Default.ChordLibraryFile);
        }
        catch (JsonException)
        {
            string backup = _path + ".corrupt";
            try
            {
                File.Copy(_path!, backup, overwrite: true);
                CorruptLibraryBackupPath = backup;
            }
            catch (IOException)
            {
            }
            return;
        }
        if (file?.Chords is null)
            return;
        if (file.Version != FileVersion)
            throw new InvalidOperationException($"Unsupported chord library file version {file.Version} (expected {FileVersion}): {_path}");
        foreach ((string symbol, string text) in file.Chords)
        {
            if (ChordEncoding.TryParse(text, out ChordEncoding? encoding))
                _userChords[symbol] = encoding;
            else
                _skippedSymbols.Add(symbol);
        }
    }

    private void SaveUserChords()
    {
        string? directory = Path.GetDirectoryName(_path!);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var file = new ChordLibraryFile(
            FileVersion,
            _userChords.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase));
        string tempPath = _path + ".tmp";
        using (FileStream stream = File.Create(tempPath))
            JsonSerializer.Serialize(stream, file, ChordLibraryJsonContext.Default.ChordLibraryFile);
        File.Move(tempPath, _path!, overwrite: true);
    }

    private static Dictionary<string, ChordEncoding> CreateBuiltInChords()
    {
        string[] roots = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        (string Suffix, int[] Intervals)[] qualities =
        {
            ("", new[] { 0, 4, 7 }),
            ("maj", new[] { 0, 4, 7 }),
            ("m", new[] { 0, 3, 7 }),
            ("7", new[] { 0, 4, 7, 10 }),
            ("m7", new[] { 0, 3, 7, 10 }),
            ("maj7", new[] { 0, 4, 7, 11 }),
            ("dim", new[] { 0, 3, 6 }),
            ("aug", new[] { 0, 4, 8 }),
            ("sus2", new[] { 0, 2, 7 }),
            ("sus4", new[] { 0, 5, 7 }),
            ("5", new[] { 0, 7 }),
        };

        var chords = new Dictionary<string, ChordEncoding>(StringComparer.OrdinalIgnoreCase);
        int anchorPitchClassC = PianoNote.MiddleC % 12;
        int anchorPitchClassF = PianoNote.FBelowMiddleC % 12;
        for (int rootPitchClass = 0; rootPitchClass < 12; rootPitchClass++)
        {
            int offsetFromC = (rootPitchClass - anchorPitchClassC + 12) % 12;
            int offsetFromF = (rootPitchClass - anchorPitchClassF + 12) % 12;
            KeyLayout layout = offsetFromC <= offsetFromF ? KeyLayout.C : KeyLayout.F;
            int rootOffset = layout == KeyLayout.C ? offsetFromC : offsetFromF;
            string root = roots[rootPitchClass];
            foreach ((string suffix, int[] intervals) in qualities)
                chords[root + suffix] = ChordEncoding.FromKeyOffsets(
                    layout,
                    octave: 0,
                    intervals.Select(interval => rootOffset + interval));
        }
        return chords;
    }
}

internal sealed record ChordLibraryFile(int Version, Dictionary<string, string> Chords);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ChordLibraryFile))]
internal sealed partial class ChordLibraryJsonContext : JsonSerializerContext
{
}
