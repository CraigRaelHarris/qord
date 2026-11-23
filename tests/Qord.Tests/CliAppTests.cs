using Qord;
using Qord.Cli;

namespace Qord.Tests;

public class CliAppTests : IDisposable
{
    private readonly string _directory;

    public CliAppTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "qord-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string LibraryPath => Path.Combine(_directory, "chords.json");

    private (int Exit, string Output, string Error) Run(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int exit = CliApp.Run(args, stdout, stderr, LibraryPath);
        return (exit, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public void Show_PrintsEncodingAndNotes()
    {
        (int exit, string output, string error) = Run("show", "Cmaj");
        Assert.Equal(0, exit);
        Assert.Equal("", error);
        Assert.Contains("Cmaj: 1,0,10001001", output);
        Assert.Contains("C4 E4 G4", output);
    }

    [Fact]
    public void Show_UnknownChordFails()
    {
        (int exit, string output, string error) = Run("show", "Zzz");
        Assert.Equal(2, exit);
        Assert.Contains("not in the library", error);
    }

    [Fact]
    public void Render_WritesPngByDefault()
    {
        string path = Path.Combine(_directory, "out.png");
        (int exit, string output, string error) = Run("render", "Cmaj", "--visual", "traditional", "--out", path);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(path));
        Assert.Contains("Wrote", output);

        byte[] header = new byte[4];
        using (FileStream stream = File.OpenRead(path))
            stream.ReadExactly(header);
        Assert.Equal(0x89, header[0]);
        Assert.Equal((byte)'P', header[1]);
    }

    [Fact]
    public void Render_WritesSvgWithFormatFlag()
    {
        string path = Path.Combine(_directory, "out.svg");
        (int exit, string output, string error) = Run("render", "F#maj", "--format", "svg", "--visual", "traditional", "--out", path);
        Assert.Equal(0, exit);
        Assert.StartsWith("<svg", File.ReadAllText(path));
    }

    [Fact]
    public void Render_RespectsOctaveAndLayoutFlags()
    {
        string path = Path.Combine(_directory, "out.svg");
        (int exit, string output, string error) = Run("render", "Cmaj", "--octave", "-1", "--layout", "F", "--format", "svg", "--visual", "traditional", "--out", path);
        Assert.Equal(0, exit);
        Assert.Contains("C3 E3 G3", output);
    }

    [Fact]
    public void Render_UnknownChordFails()
    {
        (int exit, string output, string error) = Run("render", "Zzz");
        Assert.Equal(2, exit);
        Assert.Contains("not in the library", error);
    }

    [Fact]
    public void Render_OutOfRangeOctaveFails()
    {
        string path = Path.Combine(_directory, "x.png");
        (int exit, string output, string error) = Run("render", "Cmaj", "--octave", "9", "--out", path);
        Assert.Equal(1, exit);
        Assert.Contains("piano range", error);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Add_ThenShowAndList_RoundTrips()
    {
        (int exit, string output, string error) = Run("add", "Csus2", "1,0,10100001");
        Assert.Equal(0, exit);
        Assert.True(File.Exists(LibraryPath));

        (int showExit, string showOutput, string showError) = Run("show", "Csus2");
        Assert.Equal(0, showExit);
        Assert.Contains("1,0,10100001", showOutput);

        (int listExit, string listOutput, string listError) = Run("list");
        Assert.Equal(0, listExit);
        Assert.Contains("Csus2", listOutput);
        Assert.Contains("(user)", listOutput);
    }

    [Fact]
    public void Add_InvalidEncodingFails()
    {
        (int exit, string output, string error) = Run("add", "Bad", "1,0,100");
        Assert.Equal(1, exit);
        Assert.Contains("not a valid chord encoding", error);
        Assert.False(File.Exists(LibraryPath));
    }

    [Fact]
    public void Render_DefaultsToQadroVisual()
    {
        string path = Path.Combine(_directory, "default.png");
        (int exit, string output, string error) = Run("render", "Cmaj", "--out", path);
        Assert.Equal(0, exit);
        Assert.Equal("", error);
        Assert.True(File.Exists(path));

        byte[] header = new byte[4];
        using (FileStream stream = File.OpenRead(path))
            stream.ReadExactly(header);
        Assert.Equal(0x89, header[0]);
    }

    [Fact]
    public void Render_QadroVisual_Succeeds()
    {
        string path = Path.Combine(_directory, "qadro.svg");
        (int exit, string output, string error) = Run("render", "F#maj", "--visual", "qadro", "--format", "svg", "--out", path);
        Assert.Equal(0, exit);
        Assert.StartsWith("<svg", File.ReadAllText(path));
    }

    [Fact]
    public void Render_QadroAm6ExampleOctaveDown()
    {
        string path = Path.Combine(_directory, "am6.svg");
        (int exit, string output, string error) = Run("render", "Am6", "--octave", "-1", "--format", "svg", "--out", path);
        Assert.Equal(0, exit);
        string svg = File.ReadAllText(path);
        Assert.Contains(">-1</text>", svg);
        Assert.Contains("A2 C3 E3 F#3", output);
    }

    [Fact]
    public void Render_TraditionalVisual_StillRenders()
    {
        string path = Path.Combine(_directory, "traditional.png");
        (int exit, string output, string error) = Run("render", "Cmaj", "--visual", "traditional", "--out", path);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Render_InvalidVisualFails()
    {
        (int exit, string output, string error) = Run("render", "Cmaj", "--visual", "fancy");
        Assert.Equal(1, exit);
        Assert.Contains("not a valid visual layout", error);
    }

    [Fact]
    public void CorruptLibrary_WarnsButStillSucceeds()
    {
        File.WriteAllText(LibraryPath, "{ not valid json");
        (int exit, string output, string error) = Run("show", "Cmaj");
        Assert.Equal(0, exit);
        Assert.Contains("1,0,10001001", output);
        Assert.Contains("warning", error);
        Assert.True(File.Exists(LibraryPath + ".corrupt"));
    }

    [Fact]
    public void InvalidLibraryEntries_ProduceWarning()
    {
        File.WriteAllText(LibraryPath, "{\"Version\":1,\"Chords\":{\"Zbad\":\"1,0,100\"}}");
        (int exit, string output, string error) = Run("list");
        Assert.Equal(0, exit);
        Assert.Contains("Zbad", error);
        Assert.DoesNotContain("Zbad", output);
    }

    [Fact]
    public void NoArgs_PrintsUsage()
    {
        (int exit, string output, string error) = Run();
        Assert.Equal(1, exit);
        Assert.Contains("usage", error);
    }

    [Fact]
    public void UnknownCommand_Fails()
    {
        (int exit, string output, string error) = Run("frobnicate");
        Assert.Equal(1, exit);
        Assert.Contains("unknown command", error);
    }

    [Fact]
    public void Help_PrintsUsage()
    {
        (int exit, string output, string error) = Run("help");
        Assert.Equal(0, exit);
        Assert.Contains("usage", output);
    }

    [Fact]
    public void List_ShowsBuiltIns()
    {
        (int exit, string output, string error) = Run("list");
        Assert.Equal(0, exit);
        Assert.Contains("Cmaj", output);
        Assert.Contains("1,0,10001001", output);
    }
}
