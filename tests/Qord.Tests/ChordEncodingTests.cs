using Qord;

namespace Qord.Tests;

public class ChordEncodingTests
{
    private static string Bits(ChordEncoding chord) =>
        string.Concat(chord.Keys.Select(key => key ? '1' : '0'));

    private static int[] Midis(ChordEncoding chord) =>
        chord.GetNotes().Select(note => note.Midi).ToArray();

    private static string[] Names(ChordEncoding chord) =>
        chord.GetNotes().Select(note => note.Name).ToArray();

    [Fact]
    public void Parse_CMajorSpecExample()
    {
        ChordEncoding chord = ChordEncoding.Parse("1,0,10001001");
        Assert.Equal(KeyLayout.C, chord.Layout);
        Assert.Equal(0, chord.Octave);
        Assert.Equal("10001001", Bits(chord));
        Assert.Equal(60, chord.BaseMidi);
        Assert.Equal(new[] { 60, 64, 67 }, Midis(chord));
        Assert.Equal(new[] { "C4", "E4", "G4" }, Names(chord));
    }

    [Fact]
    public void Parse_FSharpMajorSpecExample()
    {
        ChordEncoding chord = ChordEncoding.Parse("0,0,010001001");
        Assert.Equal(KeyLayout.F, chord.Layout);
        Assert.Equal(0, chord.Octave);
        Assert.Equal("010001001", Bits(chord));
        Assert.Equal(53, chord.BaseMidi);
        Assert.Equal(new[] { 54, 58, 61 }, Midis(chord));
        Assert.Equal(new[] { "F#3", "A#3", "C#4" }, Names(chord));
    }

    [Theory]
    [InlineData("1,0,10001001")]
    [InlineData("0,0,010001001")]
    [InlineData("1,-1,10001001")]
    [InlineData("1,2,10001001")]
    [InlineData("1,0,1000100100001")]
    public void ToString_RoundTrips(string text)
    {
        Assert.Equal(text, ChordEncoding.Parse(text).ToString());
    }

    [Theory]
    [InlineData("1,0,1", 60)]
    [InlineData("0,0,1", 53)]
    [InlineData("1,-1,1", 48)]
    [InlineData("0,-1,1", 41)]
    [InlineData("1,2,1", 84)]
    [InlineData("0,2,1", 77)]
    public void Octave_ShiftsBaseKey(string text, int expectedBaseMidi)
    {
        Assert.Equal(expectedBaseMidi, ChordEncoding.Parse(text).BaseMidi);
    }

    [Fact]
    public void Keys_MaySpanMoreThanOneOctave()
    {
        ChordEncoding chord = ChordEncoding.Parse("1,0,1000100100001");
        Assert.Equal(new[] { 60, 64, 67, 72 }, Midis(chord));
    }

    [Theory]
    [InlineData("2,0,10001001")]
    [InlineData("01,0,10001001")]
    [InlineData("1,0")]
    [InlineData("1,0,10001001,extra")]
    [InlineData("1,x,10001001")]
    [InlineData("1,0.5,10001001")]
    [InlineData("1,0,")]
    [InlineData("1,0,100")]
    [InlineData("1,0,100010010")]
    [InlineData("1,0,1000a001")]
    [InlineData("1,0,1 1")]
    [InlineData("1,9,1")]
    [InlineData("1,-9,1")]
    [InlineData("1,4,1001")]
    [InlineData("0,-4,1")]
    public void Parse_RejectsInvalidText(string text)
    {
        Assert.Throws<FormatException>(() => ChordEncoding.Parse(text));
    }

    [Fact]
    public void Parse_RejectsKeysLongerThanThePiano()
    {
        Assert.Throws<FormatException>(() => ChordEncoding.Parse("1,0," + new string('1', 89)));
    }

    [Fact]
    public void Parse_RejectsOctavesThatOverflowTheAnchor()
    {
        Assert.Throws<FormatException>(() => ChordEncoding.Parse("1,357913942,1"));
        Assert.Throws<FormatException>(() => ChordEncoding.Parse("0,-357913944,1"));
        Assert.False(ChordEncoding.TryParse("1,357913942,1", out ChordEncoding? overflowed));
        Assert.Null(overflowed);
    }

    [Fact]
    public void Constructor_RejectsOutOfPianoRange()
    {
        Assert.Throws<ArgumentException>(() => new ChordEncoding(KeyLayout.C, 9, new[] { true }));
        Assert.Throws<ArgumentException>(() => new ChordEncoding(KeyLayout.C, 0, new bool[50]));
    }

    [Fact]
    public void WithOctave_KeepsLayoutAndKeys()
    {
        ChordEncoding chord = ChordEncoding.Parse("1,0,10001001").WithOctave(2);
        Assert.Equal("1,2,10001001", chord.ToString());
        Assert.Equal(new[] { 84, 88, 91 }, Midis(chord));
    }

    [Fact]
    public void TranscodeTo_SameLayoutReturnsSameInstance()
    {
        ChordEncoding chord = ChordEncoding.Parse("1,0,10001001");
        Assert.Same(chord, chord.TranscodeTo(KeyLayout.C));
    }

    [Fact]
    public void TranscodeTo_FLayoutPreservesNotes()
    {
        ChordEncoding chord = ChordEncoding.Parse("1,0,10001001").TranscodeTo(KeyLayout.F);
        Assert.Equal("0,0,000000010001001", chord.ToString());
        Assert.Equal(new[] { 60, 64, 67 }, Midis(chord));
    }

    [Fact]
    public void TranscodeTo_CLayoutPreservesNotes()
    {
        ChordEncoding chord = ChordEncoding.Parse("0,0,010001001").TranscodeTo(KeyLayout.C);
        Assert.Equal("1,-1,00000010001001", chord.ToString());
        Assert.Equal(new[] { 54, 58, 61 }, Midis(chord));
    }

    [Fact]
    public void FromKeyOffsets_AcceptsUnsortedOffsets()
    {
        ChordEncoding chord = ChordEncoding.FromKeyOffsets(KeyLayout.C, 0, new[] { 7, 0, 4 });
        Assert.Equal("1,0,10001001", chord.ToString());
    }

    [Fact]
    public void FromKeyOffsets_RejectsNegativeOffsets()
    {
        Assert.Throws<ArgumentException>(() => ChordEncoding.FromKeyOffsets(KeyLayout.C, 0, new[] { -1, 4 }));
    }

    [Fact]
    public void Equality_ComparesAllFields()
    {
        Assert.Equal(ChordEncoding.Parse("1,0,10001001"), ChordEncoding.Parse("1,0,10001001"));
        Assert.NotEqual(ChordEncoding.Parse("1,0,10001001"), ChordEncoding.Parse("0,0,010001001"));
        Assert.Equal(ChordEncoding.Parse("1,0,10001001").GetHashCode(), ChordEncoding.Parse("1,0,10001001").GetHashCode());
    }

    [Fact]
    public void TryParse_ReportsValidity()
    {
        Assert.True(ChordEncoding.TryParse("1,0,10001001", out ChordEncoding? parsed));
        Assert.Equal("1,0,10001001", parsed!.ToString());
        Assert.False(ChordEncoding.TryParse("nonsense", out ChordEncoding? failed));
        Assert.Null(failed);
    }
}
