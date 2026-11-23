using Qord;

namespace Qord.Tests;

public class ChordLibraryTests : IDisposable
{
    private readonly string _directory;

    public ChordLibraryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "qord-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string LibraryPath => Path.Combine(_directory, "chords.json");

    [Fact]
    public void BuiltIns_IncludeSpecExamples()
    {
        var library = new ChordLibrary(LibraryPath);
        Assert.True(library.TryGet("Cmaj", out ChordEncoding? cmaj));
        Assert.Equal("1,0,10001001", cmaj!.ToString());
        Assert.True(library.TryGet("F#maj", out ChordEncoding? fsharp));
        Assert.Equal("0,0,010001001", fsharp!.ToString());
        Assert.True(library.TryGet("F#m", out ChordEncoding? fsharpMinor));
        Assert.Equal("0,0,010010001", fsharpMinor!.ToString());
    }

    [Fact]
    public void Lookup_IsCaseInsensitive()
    {
        var library = new ChordLibrary(LibraryPath);
        Assert.True(library.TryGet("cMAJ", out ChordEncoding? chord));
        Assert.Equal("1,0,10001001", chord!.ToString());
    }

    [Fact]
    public void TryGet_UnknownSymbolFails()
    {
        var library = new ChordLibrary(LibraryPath);
        Assert.False(library.TryGet("Zzz", out ChordEncoding? missing));
        Assert.Null(missing);
    }

    [Fact]
    public void Add_PersistsToDisk()
    {
        var library = new ChordLibrary(LibraryPath);
        ChordEncoding encoding = ChordEncoding.Parse("1,0,10100001");
        library.Add("Csus2", encoding);

        Assert.True(File.Exists(LibraryPath));
        Assert.False(File.Exists(LibraryPath + ".tmp"));
        var reloaded = new ChordLibrary(LibraryPath);
        Assert.True(reloaded.TryGet("Csus2", out ChordEncoding? loaded));
        Assert.Equal(encoding, loaded);
    }

    [Fact]
    public void CorruptFile_IsBackedUpAndIgnored()
    {
        File.WriteAllText(LibraryPath, "{ not valid json");
        var library = new ChordLibrary(LibraryPath);
        Assert.Equal(LibraryPath + ".corrupt", library.CorruptLibraryBackupPath);
        Assert.True(File.Exists(LibraryPath + ".corrupt"));
        Assert.True(library.TryGet("Cmaj", out ChordEncoding? _));
        Assert.Empty(library.SkippedSymbols);
    }

    [Fact]
    public void InvalidEntries_AreCollectedAsSkipped()
    {
        File.WriteAllText(LibraryPath, "{\"Version\":1,\"Chords\":{\"Zbad\":\"1,0,100\",\"Zgood\":\"1,0,1\"}}");
        var library = new ChordLibrary(LibraryPath);
        Assert.Equal(new[] { "Zbad" }, library.SkippedSymbols);
        Assert.True(library.TryGet("Zgood", out ChordEncoding? good));
        Assert.Equal("1,0,1", good!.ToString());
    }

    [Fact]
    public void Add_OverridesBuiltIns()
    {
        var library = new ChordLibrary(LibraryPath);
        ChordEncoding custom = ChordEncoding.Parse("1,2,10001001");
        library.Add("Cmaj", custom);

        Assert.True(library.TryGet("Cmaj", out ChordEncoding? chord));
        Assert.Equal(custom, chord);

        var reloaded = new ChordLibrary(LibraryPath);
        Assert.True(reloaded.TryGet("Cmaj", out ChordEncoding? persisted));
        Assert.Equal(custom, persisted);
    }

    [Fact]
    public void Add_CreatesMissingDirectories()
    {
        string nested = Path.Combine(_directory, "nested", "deep", "chords.json");
        var library = new ChordLibrary(nested);
        library.Add("X", ChordEncoding.Parse("1,0,1"));
        Assert.True(File.Exists(nested));
    }

    [Fact]
    public void List_MergesBuiltInsAndUserChords()
    {
        var library = new ChordLibrary(LibraryPath);
        library.Add("Xcustom", ChordEncoding.Parse("1,0,10100001"));

        IReadOnlyList<ChordEntry> entries = library.List();
        Assert.Contains(entries, entry => entry.Symbol == "Cmaj" && !entry.IsUserDefined);
        Assert.Contains(entries, entry => entry.Symbol == "Xcustom" && entry.IsUserDefined);
        Assert.True(entries.Count >= 132);
    }
}
