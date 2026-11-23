# qord

qord maps piano chords between a compact **text representation** and a **small keyboard image**. The visual representation is designed to be easy to read at a glance.

It ships as two projects:

- **Qord** — a .NET class library that parses and validates chord encodings, maintains a chord library, and renders chord images.
- **Qord.Cli** — a command-line tool (`qord`) for interacting with the library using conventional chord symbols.

## Text representation

Every chord in the library is described by three comma-separated values:

    <layout>,<octave>,<keys>

| Field | Meaning |
|---|---|
| `layout` | `1` = the chord is laid out from the **C** key, `0` = from the **F** key. |
| `octave` | Octave offset from the middle register. `0` = middle C (C4) as reference, `-1` = one octave lower, `2` = two octaves higher. |
| `keys` | A string of `0`/`1` digits — one digit per key stepping **upwards from the starting key** (F or C), counting **both black and white keys**. `1` = the key is used in the chord. These keys are semitones.|

Rules:

- Each digit is one semitone step from the starting key.
- The bit string ends at the **highest used key**: the last digit is always `1`, and trailing zeros are rejected, so every chord has exactly one canonical encoding.
- The bit string is **unbounded**: chords may span more than one octave (more than 12 digits).
- For the F layout, octave `0` starts from the F **just below middle C** (F3), so C-rooted and F-rooted chords at octave 0 sit in the same register.
- The whole span from the starting key to the highest used key must fit on an 88-key piano (A0–C8); anything beyond is rejected as out of range.

### Examples

A standard (middle) C chord:

    1,0,10001001

Stepping up from C: `C=1, C#=0, D=0, D#=0, E=1, F=0, F#=0, G=1` → **C–E–G** (C4, E4, G4).

An F# chord:

    0,0,010001001

Stepping up from F: `F=0, F#=1, G=0, G#=0, A=0, A#=1, B=0, C=0, C#=1` → **F#–A#–C#** (F#3, A#3, C#4).

A C suspended 2nd (C–D–G), which spans a 7th:

    1,0,10100001

## Chord library

- Built-in defaults cover common chords for every root: `maj` (also the bare root, e.g. `C`), `m`, `7`, `m7`, `m6`, `maj7`, `dim`, `aug`, `sus2`, `sus4` and `5`. Roots C through E use the C layout; roots F through B use the F layout.
- Your own chords are persisted in `~/.qord/chords.json` (set the `QORD_LIBRARY` environment variable to use a different file) as a map from chord symbol to the 3-number encoding. User entries override built-ins.
- Entries whose encodings are invalid are ignored with a warning, and a file that is not valid JSON is backed up (as `chords.json.corrupt`) and ignored, so a bad library file never blocks the tool.
- Manage entries from the CLI with `add` / `list`, or by editing the JSON file directly.

## Rendering

Images come in two **visual layouts**, each available in both formats:

- **qadro** — the default visual layout: a three-row schematic of the chord.
  - **Top row** (only shown when the octave is not 0): the octave number centered, with an arrow beneath it — pointing left when the chord drops octaves, right when it rises. Nothing is drawn for octave 0.
  - **Middle row** — the black keys: three circles, a gap, then two circles per octave from F; two, a gap, then three from C. Used keys are filled black; unused ones are white circles. This row always shows every circle of each spanned octave.
  - **Bottom row** — the white keys: four circles, a gap, then three per octave from F; three, a gap, then four from C. Used keys are filled black; unused ones shrink to dots; nothing is drawn to the right of the chord's last used key.
  - A thin vertical gray line marks each C — in the B–C gap for F-based rows, at the left edge and each octave start for C-based rows.
  - Chords spanning more than one octave repeat the circle pattern per octave, extending to the right.
- **traditional** — a small piano-keyboard diagram with the chord's keys highlighted. The diagram spans from the layout's **starting key** (which may itself be unused, e.g. the F in the F# example) to the chord's **highest used key**; used keys are highlighted in blue.

Formats (selectable with `--format`):

- **png** — raster image, written by a built-in dependency-free PNG encoder (default)
- **svg** — vector graphic; scales cleanly and is easy to hand-edit

## CLI usage

    qord render <symbol> [--octave N] [--layout C|F] [--format png|svg] [--visual traditional|qadro] [--out PATH]
    qord show   <symbol>
    qord list
    qord add    <symbol> <encoding>

| Option | Meaning |
|---|---|
| `--octave N` | Override the encoding's octave field. |
| `--layout C\|F` | Re-express the chord in the other key layout (same notes). |
| `--format png\|svg` | Output format (default `png`). |
| `--visual traditional\|qadro` | Visual layout of the image (default `qadro`). |
| `--out PATH` | Output file (default `<symbol>.<ext>` in the current directory). |

Examples:

    qord render Cmaj                          # qadro PNG of middle-register C major
    qord render F#m --octave -1 --visual traditional --format svg --out fsm.svg
    qord show Cmaj                            # Cmaj: 1,0,10001001 (C4 E4 G4)
    qord add Csus2 1,0,10100001
    qord list

Exit codes: `0` success, `1` usage or validation error, `2` chord not found.

## Building

    dotnet build qord.sln
    dotnet test qord.sln
    dotnet run --project src/Qord.Cli -- render Cmaj

The solution and projects target .NET 8.

## Project layout

    src/Qord       the library (encodings, chord library, PNG/SVG rendering)
    src/Qord.Cli   the qord command-line tool
    tests/Qord.Tests
