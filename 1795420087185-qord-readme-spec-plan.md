# qord — README + Spec Review Plan

## Goal
Create `README.md` at repo root for the qord project (C# library `Qord` + CLI `Qord.Cli`), documenting the chord text encoding, image rendering, chord library, and CLI usage. Repo is currently empty; this plan contains the full README content plus the spec review findings that shaped it.

## Spec review findings (resolved with user)
1. **Examples verified correct.** `1,0,10001001` = C–E–G from C; `0,0,010001001` = F#–A#–C# from F (bits 2, 6, 9). Each digit = one semitone step from the root key; string ends after the highest used key.
2. **F-key octave base clarified:** octave 0 from F means F just below middle C (F3, with middle C = C4), so C- and F-rooted chords at octave 0 sit in the same register.
3. **Bit length is unbounded** — chords may span more than 12 semitones; no fixed cap.
4. **CLI format:** conventional chord symbols (e.g. `Cmaj`, `F#m`), with `--octave` / `--layout` options.
5. **Image output:** piano-keyboard diagram with chord keys highlighted; PNG (generated in-code) or SVG, selectable via `--format`.
6. **Library persistence:** JSON file (`~/.qord/chords.json`) layered over built-in common-chord defaults; entries map chord symbol → 3-number encoding.
7. **CLI surface:** `render`, `list`, `add`, `show`. Solution: `qord.sln` with `Qord` class library and `Qord.Cli` console app, .NET 8+.

## README.md content (to be written at C:\code\qord\README.md)

# qord

qord maps piano chords between a compact **text representation** and a **small keyboard image**. It ships as two projects:

- **Qord** — a .NET class library that parses/validates chord encodings, maintains a chord library, and renders chord images.
- **Qord.Cli** — a command-line tool (`qord`) for interacting with the library, using conventional chord symbols.

## Text representation

Every chord in the library is described by three comma-separated values:

```
<layout>,<octave>,<keys>
```

| Field | Meaning |
|---|---|
| `layout` | `1` = chord is laid out from the **C** key; `0` = laid out from the **F** key. |
| `octave` | Octave offset from the middle register. `0` = middle C (C4) as reference; `-1` = one octave lower; `2` = two octaves higher. |
| `keys` | A string of `0`/`1` digits — one per semitone stepping **upwards from the starting key** (F or C), counting **both black and white keys**. `1` = the key is used in the chord. |

Notes:
- The bit string ends at the **highest used key** — trailing unused keys are omitted.
- The string is **unbounded**; chords may span more than one octave (more than 12 digits).
- For F-layout, octave `0` uses the F **just below middle C** (F3), so C- and F-rooted chords at octave 0 sit in the same register.

### Examples

```
1,0,10001001      C major (middle register)
```
Stepping from C: `C=1, C#=0, D=0, D#=0, E=1, F=0, F#=0, G=1` → **C–E–G**.

```
0,0,010001001     F# major (middle register)
```
Stepping from F: `F=0, F#=1, G=0, G#=0, A=0, A#=1, B=0, C=0, C#=1` → **F#–A#–C#**.

## Chord library

- Built-in defaults cover common chords (maj, min, 7, m7, dim, aug, ...).
- User chords are persisted in `~/.qord/chords.json` as a map from chord symbol to the 3-number encoding; user entries override built-ins.
- Managed from the CLI via `add` / `list` (or by editing the JSON file directly).

## Rendering

The image is a small piano-keyboard diagram with the chord's keys highlighted. Available formats:
- **PNG** — raster, generated in-code
- **SVG** — vector, scales cleanly and is hand-editable

## CLI usage

```
qord render <symbol> [--octave N] [--layout C|F] [--format png|svg] [--out PATH]
qord show   <symbol>                  # print the 3-number encoding
qord list                               # list all library chords
qord add    <symbol> <encoding>        # add/override a library entry, e.g.
                                        # qord add Csus2 1,0,1001001
```

Examples:

```
qord render Cmaj                      # PNG of middle-register C major
qord render F#m --octave -1 --format svg --out fsm.svg
```

## Building

```
dotnet build qord.sln
dotnet run --project Qord.Cli -- render Cmaj
```

## Implementation outline (for the implementing agent)

1. Create `Qord` class library (.NET 8): `ChordEncoding` model (layout, octave, key bits) with strict parse/validate + `ToString`; `ChordLibrary` (built-in defaults + JSON overlay at `~/.qord/chords.json`); `ChordRenderer` (keyboard diagram → PNG via ImageSharp or System.Drawing, and a hand-rolled SVG writer).
2. Create `Qord.Cli` console app referencing `Qord`; implement `render` / `show` / `list` / `add` verbs with System.CommandLine or hand-rolled arg parsing.
3. Write the README content above to `C:\code\qord\README.md` (verbatim, minus this outline section).
4. Validation: unit tests for encoding round-trips (both spec examples, >12-bit chords, F-layout F3 base) and a CLI smoke test (`render Cmaj`, `add` + `list`, `--format svg`).

## Validation plan
- README examples must decode exactly as documented (verified during spec review).
- Implementation must round-trip `1,0,10001001` ↔ C–E–G and `0,0,010001001` ↔ F#–A#–C#.

## Out of scope
- Playback/audio, MIDI export, chord inversion naming conventions, NuGet packaging.
