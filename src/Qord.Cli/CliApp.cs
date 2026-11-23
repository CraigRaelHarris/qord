using System.Globalization;
using System.Text;
using Qord;

namespace Qord.Cli;

public static class CliApp
{
    public static int Run(IReadOnlyList<string> args) =>
        Run(args, Console.Out, Console.Error, ResolveLibraryPath());

    public static int Run(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr, string? libraryPath)
    {
        ArgumentNullException.ThrowIfNull(args);
        try
        {
            if (args.Count == 0)
            {
                PrintUsage(stderr);
                return 1;
            }

            var rest = args.Skip(1).ToList();
            switch (args[0].ToLowerInvariant())
            {
                case "render":
                    return Render(rest, stdout, stderr, libraryPath);
                case "show":
                    return Show(rest, stdout, stderr, libraryPath);
                case "list":
                    return List(rest, stdout, stderr, libraryPath);
                case "add":
                    return Add(rest, stdout, stderr, libraryPath);
                case "help":
                case "--help":
                case "-h":
                    PrintUsage(stdout);
                    return 0;
                default:
                    stderr.WriteLine($"error: unknown command '{args[0]}'");
                    PrintUsage(stderr);
                    return 1;
            }
        }
        catch (Exception exception)
        {
            stderr.WriteLine("error: " + exception.Message);
            return 1;
        }
    }

    private static string? ResolveLibraryPath()
    {
        string? overridden = Environment.GetEnvironmentVariable("QORD_LIBRARY");
        return string.IsNullOrWhiteSpace(overridden) ? ChordLibrary.DefaultPath : overridden;
    }

    private static int Render(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr, string? libraryPath)
    {
        string? symbol = null;
        int? octave = null;
        KeyLayout? layout = null;
        ChordImageFormat format = ChordImageFormat.Png;
        VisualLayout visual = VisualLayout.Qadro;
        string? outputPath = null;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--octave":
                    if (++i >= args.Count)
                        return Fail(stderr, "--octave requires a value");
                    if (!int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedOctave))
                        return Fail(stderr, $"'{args[i]}' is not a valid octave");
                    octave = parsedOctave;
                    break;
                case "--layout":
                    if (++i >= args.Count)
                        return Fail(stderr, "--layout requires a value");
                    if (!TryParseLayout(args[i], out KeyLayout parsedLayout))
                        return Fail(stderr, $"'{args[i]}' is not a valid layout (expected C or F)");
                    layout = parsedLayout;
                    break;
                case "--format":
                    if (++i >= args.Count)
                        return Fail(stderr, "--format requires a value");
                    string formatText = args[i].ToLowerInvariant();
                    if (formatText != "png" && formatText != "svg")
                        return Fail(stderr, $"'{args[i]}' is not a valid format (expected png or svg)");
                    format = formatText == "png" ? ChordImageFormat.Png : ChordImageFormat.Svg;
                    break;
                case "--visual":
                    if (++i >= args.Count)
                        return Fail(stderr, "--visual requires a value");
                    string visualText = args[i].ToLowerInvariant();
                    if (visualText != "traditional" && visualText != "qadro")
                        return Fail(stderr, $"'{args[i]}' is not a valid visual layout (expected traditional or qadro)");
                    visual = visualText == "traditional" ? VisualLayout.Traditional : VisualLayout.Qadro;
                    break;
                case "--out":
                    if (++i >= args.Count)
                        return Fail(stderr, "--out requires a value");
                    outputPath = args[i];
                    break;
                default:
                    if (arg.StartsWith('-'))
                        return Fail(stderr, $"unknown option '{arg}'");
                    if (symbol is not null)
                        return Fail(stderr, $"unexpected argument '{arg}'");
                    symbol = arg;
                    break;
            }
        }

        if (symbol is null)
            return Fail(stderr, "render requires a chord symbol");

        var library = OpenLibrary(libraryPath, stderr);
        if (!library.TryGet(symbol, out ChordEncoding? encoding))
        {
            stderr.WriteLine($"error: chord '{Safe(symbol)}' is not in the library; add it with: qord add {Safe(symbol)} <layout>,<octave>,<keys>");
            return 2;
        }

        if (layout is not null)
            encoding = encoding.TranscodeTo(layout.Value);
        if (octave is not null)
            encoding = encoding.WithOctave(octave.Value);

        outputPath ??= SanitizeFileName(symbol) + ExtensionFor(format);
        ChordRenderer.Render(encoding, outputPath, format, visual);
        stdout.WriteLine($"Wrote {Safe(outputPath)} ({DescribeNotes(encoding)})");
        return 0;
    }

    private static int Show(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr, string? libraryPath)
    {
        if (args.Count != 1)
            return Fail(stderr, "show requires exactly one chord symbol");
        if (args[0].StartsWith('-'))
            return Fail(stderr, $"unknown option '{args[0]}'");

        var library = OpenLibrary(libraryPath, stderr);
        if (!library.TryGet(args[0], out ChordEncoding? encoding))
        {
            stderr.WriteLine($"error: chord '{Safe(args[0])}' is not in the library");
            return 2;
        }
        stdout.WriteLine($"{Safe(args[0])}: {encoding} ({DescribeNotes(encoding)})");
        return 0;
    }

    private static int List(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr, string? libraryPath)
    {
        if (args.Count > 0)
            return Fail(stderr, "list takes no arguments");

        var library = OpenLibrary(libraryPath, stderr);
        foreach (ChordEntry entry in library.List())
            stdout.WriteLine(
                $"{Safe(entry.Symbol).PadRight(9)}{entry.Encoding.ToString().PadRight(25)}{DescribeNotes(entry.Encoding)}{(entry.IsUserDefined ? "  (user)" : "")}");
        return 0;
    }

    private static int Add(IReadOnlyList<string> args, TextWriter stdout, TextWriter stderr, string? libraryPath)
    {
        if (args.Count != 2)
            return Fail(stderr, "add requires a chord symbol and an encoding, e.g. qord add Csus2 1,0,10100001");
        if (args[0].StartsWith('-') || args[1].StartsWith('-'))
            return Fail(stderr, "unknown option");

        if (!ChordEncoding.TryParse(args[1], out ChordEncoding? encoding))
        {
            stderr.WriteLine($"error: '{args[1]}' is not a valid chord encoding (expected <layout>,<octave>,<keys>)");
            return 1;
        }

        var library = OpenLibrary(libraryPath, stderr);
        library.Add(args[0], encoding);
        stdout.WriteLine($"Added {Safe(args[0].Trim())} = {encoding} ({DescribeNotes(encoding)})");
        return 0;
    }

    private static ChordLibrary OpenLibrary(string? libraryPath, TextWriter stderr)
    {
        var library = new ChordLibrary(libraryPath);
        if (library.CorruptLibraryBackupPath is not null)
            stderr.WriteLine($"warning: the chord library file was not valid JSON; a copy was saved as {Safe(library.CorruptLibraryBackupPath)} and the file is being ignored");
        if (library.SkippedSymbols.Count > 0)
            stderr.WriteLine($"warning: ignored invalid chord entries: {string.Join(", ", library.SkippedSymbols.Select(Safe))}");
        return library;
    }

    private static string DescribeNotes(ChordEncoding encoding) =>
        string.Join(" ", encoding.GetNotes().Select(note => note.Name));

    private static string Safe(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
            if (!char.IsControl(c))
                builder.Append(c);
        return builder.ToString();
    }

    private static bool TryParseLayout(string text, out KeyLayout layout)
    {
        switch (text.Trim().ToUpperInvariant())
        {
            case "C":
                layout = KeyLayout.C;
                return true;
            case "F":
                layout = KeyLayout.F;
                return true;
            default:
                layout = default;
                return false;
        }
    }

    private static string ExtensionFor(ChordImageFormat format) => format switch
    {
        ChordImageFormat.Png => ".png",
        ChordImageFormat.Svg => ".svg",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private static string SanitizeFileName(string symbol)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(symbol.Length);
        foreach (char c in symbol)
            builder.Append(invalid.Contains(c) ? '_' : c);
        return builder.ToString();
    }

    private static int Fail(TextWriter stderr, string message)
    {
        stderr.WriteLine("error: " + message);
        stderr.WriteLine("run 'qord help' for usage");
        return 1;
    }

    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("usage: qord <command> [options]");
        writer.WriteLine();
        writer.WriteLine("commands:");
        writer.WriteLine("  render <symbol> [--octave N] [--layout C|F] [--format png|svg] [--visual traditional|qadro] [--out PATH]");
        writer.WriteLine("        render the chord's image (default: png, qadro visual layout, written to the current directory)");
        writer.WriteLine("  show <symbol>");
        writer.WriteLine("        print the chord's text representation and notes");
        writer.WriteLine("  list");
        writer.WriteLine("        list every chord in the library (built-in and user-defined)");
        writer.WriteLine("  add <symbol> <encoding>");
        writer.WriteLine("        add or override a chord, e.g. qord add Csus2 1,0,10100001");
        writer.WriteLine();
        writer.WriteLine("text representation: <layout>,<octave>,<keys>");
        writer.WriteLine("  layout  1 = from C, 0 = from F");
        writer.WriteLine("  octave  0 = middle register, -1 = one octave lower, 2 = two octaves higher");
        writer.WriteLine("  keys    one 0/1 per key stepping up from the starting key, e.g. 10001001 = C-E-G");
    }
}
