# Aleatora — Markov melodies: plan

What to build, in what order, and what "done" means for each milestone: Aleatora brought up to
the framework, its hex grid rebuilt on three dimensions, and the melody statistics in
`Tools/Markov/Source/` (scale-degree frequencies and Markov transition counts, orders 1 to 4, per
mode) loaded, sampled and shown. This plan is written for Claude Code working on Jon's machine, in
`C:\Users\jonjg\Documents\Aurumworks\Unity\`. Paths below are relative to `Unity/` unless they
start inside `Aleatora/`.

**What Aleatora is** (Jon, 2026-11-15): a MIDI and OSC controller and composition tool. **It makes
no sound of its own**: no synthesiser, no audio output, no mixer. What it composes leaves as MIDI
and OSC (how is its own plan, later; *Decisions for Jon*, 7).

Written 2026-11-15 from what was on disk that day. **Aleatora has not been brought up to the
framework yet:** its three scripts (`HexGrid`, `HexDisc`, `SelectionManager`) are in the global
namespace under `Assets/Scripts/`, it takes no framework package, it has no git repository, no
`CLAUDE.md` and no docs but this one. It is on Unity 6000.0.85f2, URP 17.0.4, Cinemachine 3.1.7,
ProBuilder, the VFX Graph, Test Framework 1.6.0, and **Input System 1.20.1** (the other games are
on 1.19.0; Lead & Bone is on 1.20.0). The grid was thrown together to get something rendering
while Jon experiments with the look, which he isn't happy with yet: M0 rebuilds it, and keeps
every part of the look tunable.

---

## Read first

1. `CLAUDE.md` (the folder of projects).
2. `Framework/README.md`: the packages, the rules for shared code, *Code style*, *Conventions for
   every game*, *UI kit*, *Menu shell*, *Self-test*. They govern here.
3. `Framework/Template/README.md`: *Bringing an existing project up* (M0 follows it).
4. `Framework/Docs/framework-plan.md`: *Where things stand*, *Risks*.
5. `Deepglow/CLAUDE.md`: the working agreement this project copies (everything in code, tuning
   assets, EditMode tests, the menu check that grows, the editor checklist). From Deepglow, read
   and never change: `Assets/Aurumworks/Scripts/Game/Hud.cs` (a screen built with the kit outside
   the menus), `Game/DeepglowTuning.cs` and `Editor/TuningMaker.cs` (the tuning asset),
   `Game/Deepglow.cs` (a bootstrap made as the scene loads), `Editor/Tests/` (the test style), and
   `Docs/Deepglow_Tech.md` *Building in code*, *Checks*.
6. For the grid: Amit Patel's *Hexagonal Grids* (redblobgames.com/grids/hexagons) for cube and
   axial coordinates, layouts and rounding; MathWorld's *Hexagonal Grid Graph* for the k × m × n
   shape (M0, step 7).
7. `Aleatora/Tools/Markov/Source/README.md`: what the data is, how it was made, and its limits.
8. This plan's *Decisions for Jon*, then the milestone in hand.

## How we work

- **One milestone at a time.** At the end of each: stop and report what was built, which tests and
  checks prove it, deviations from this plan, and open questions. Wait for Jon's go-ahead.
- **Hard stops:** after M0 (Jon opens the brought-up project, tries the new grid and tunes its
  look) and after M3 (Jon looks at the *Melodies* page).
- **Don't invent design.** Where this plan is silent or a *Decision for Jon* is still open, ask.
  Values marked **(default)** may be built as written; flag them in the report. Once Jon rules,
  write the ruling into `Docs/Aleatora_Design.md` (*Decided by Jon*, dated) in the same change.
  **The look is Jon's:** expose it on the tuning asset; don't choose it.
- **Hand-off doc.** `Docs/Aleatora_Handoff.md` holds the state, what comes next and what to read
  for it. Keep it current at the end of every turn that changes state, so a fresh chat can start
  from it. What was built goes under its section as "As built" in `Docs/Aleatora_Tech.md`.
- **Git.** One repository, branch `main`, Git LFS for binaries, LF text. One branch per milestone
  (`m0-grid`, `m1-tables`, …), small commits, merged after Jon approves. A framework change is
  committed in `Framework/` first. No remote until Jon asks; when he does, it is private.
- **End every change with the editor checklist** (`Deepglow/CLAUDE.md`, *After every change*): it
  compiles; Inspector fields and assets Unity will create; what to look at in play. Say plainly
  what couldn't be verified.

## Engineering rules

- **No sound.** No AudioSource, AudioMixer, `OnAudioFilterRead`, synthesiser or sound cue
  anywhere in Aleatora, and no hover or press cues in its UI theme.
- **Plain C# where it can be.** The grid's coordinates, shapes and layout, and everything under
  `Scripts/Music/` (tables, reader, generator, voicing, random numbers) have no `UnityEngine` in
  them, so EditMode tests run them whole.
- **Deterministic.** Every random draw comes from the project's own seeded generator (`Music/Rng`),
  never `UnityEngine.Random` or `System.Random`, so one seed is one melody in the Editor, a player
  and a test.
- **The reader never throws** on a bad table: it returns the table or its errors, with line
  numbers. A table that fails is logged with `Debug.LogError` (which fails a self-test) and left
  out.
- **Everything is built in code** (Deepglow's way): the scene holds the camera, the Cinemachine
  camera, the lights and little else; the grid and the pages are made as the game runs; tuning
  lives on a ScriptableObject in `Assets/Aurumworks/Resources/`; pages are built with the
  framework's UI kit, their look the game's `UiTheme`. No new colours beyond the theme's roles
  unless Jon asks.
- **No material instances at run time.** Per-cell colour goes through a `MaterialPropertyBlock`,
  never `renderer.material` (which copies the material and leaks it).
- **Never edit** `.meta` files, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, the generated
  `*.csproj` / `*.slnx`. `Packages/manifest.json`, `ProjectSettings/` and serialized assets
  (`.unity`, `.prefab`, `.asset`, `.inputactions`) only where a step below says so (Jon's go-ahead
  on this plan approves those steps' edits), each shown to Jon in the report.
- **No package upgrades or downgrades.** Input System stays at 1.20.1 (core asks for 1.19.0 or
  later, so 1.20.1 satisfies it, as Lead & Bone's 1.20.0 does).
- **Nothing goes into the framework** until a second game needs it. The grid and the Markov code
  stay here.
- **NUnit's `Assert.That` model only**, C# 9 (Unity's).

## Decisions for Jon

Each has the default this plan builds if Jon doesn't say otherwise.

1. **Bring-up.** The template's menus, theme, menu check and conventions, **without** its mixer,
   volume sliders or UI sounds, since Aleatora makes no sound (**default**). Or minimal: namespace,
   folders and git only.
2. **The grid's orientation.** Both are built and switchable on the tuning asset. **Default:**
   flat-topped hexes, as now, with the k axis running north–south. The other is pointy-topped
   hexes with the k axis horizontal, as in MathWorld's figure.
3. **The grid's starting dimensions.** 5 × 5 × 5 (61 cells, a regular hexagon) (**default**), on
   the tuning asset.
4. **Where the *Melodies* view lives.** A page opened through the menu stack, from a *Melodies*
   choice on the main menu and a button on the session menu (**default**: Back, Cancel, focus and
   `MenuFocus` come for free). Or an overlay in play, over the grid.
5. **Which sets ship.** All 17 (**default**: 1.8 MB of text in `Resources`). Or only the five
   `european_*` sets.
6. **Which octave a degree is played in.** The tables hold pitch classes only (the data's README,
   *Method and caveats*). Nearest to the note before, inside a range (**default**, M2). Or a
   contour table built from the source (a new analysis run, outside this plan).
7. **Outside this plan, each its own plan later:** what the grid means musically (the three axes
   are ready to carry, say, an interval each), how the generator drives it, and how notes leave
   Aleatora as MIDI and OSC.

## The data

`Aleatora/Tools/Markov/Source/` holds the analysis's output, its README and its scripts
(`scripts/extract.py`, `analyze.py`, `sample_melody.py`): about 10,000 folk melodies from the
Essen collection (German and Chinese Han), O'Neill's *Music of Ireland* and Ryan's Mammoth
Collection, all bundled with music21. The tonic is each tune's final note, and the mode is the
diatonic mode that best fits its pitches. One JSON per set, `<corpus>_<mode>`:

| Corpus | Modes |
|---|---|
| `european` (German + Irish, pooled) | `ionian`, `aeolian`, `dorian`, `mixolydian`, `harmonic_minor` |
| `german` | `ionian`, `aeolian`, `dorian`, `mixolydian`, `harmonic_minor` |
| `irish` | `ionian`, `aeolian`, `dorian`, `mixolydian` |
| `chinese_han` | `gong`, `zhi`, `yu` (pentatonic; ≈ ionian, mixolydian, aeolian finals) |

What the data can't say: octave or contour (degrees are pitch classes), rhythm (every note counts
once), where a phrase ends. Phrygian and Lydian are absent.

## The tables on disk

`Aleatora/Tools/markov.py` (written and checked 2026-11-15; Python 3, standard library) turns the
JSON into text tables:

    python -I Tools/markov.py Tools/Markov/Source Assets/Aurumworks/Resources/Markov

It writes `<set>.txt` per set and `Index.txt` (the sets, one per line), reads each back and checks
it, and exits 1 if a check fails. It doesn't rewrite a file that is already identical. Format
(its docstring is the reference):

    aleatora-markov 1
    set european_ionian
    corpus european
    mode ionian
    tunes 7136
    notes 481199
    unigram 103918 342 72637 419 86194 47118 1456 91139 311 41377 726 35562
    first 2098 0 76 8 800 94 1 3929 0 79 6 45
    order 1 12
    1 | 22635 42 19489 53 15678 2175 38 12094 14 8066 237 16261
    ...
    order 4 4529
    5 4 3 2 | 3014 12 462 3 993 153 4 366 0 49 3 57
    ...

- Degrees are written `1 b2 2 b3 3 4 #4 5 b6 6 b7 7` (semitones 0–11 above the tonic), and every
  row of 12 counts is in that order. A context lists its degrees oldest first.
- Lines end in LF. **A comment starts with `//`, never `#`**: a context can begin with `#4` (the
  converter's first draft treated those lines as comments, and its own check caught it).
- **Checks that hold in every set** (the reader makes them too, M1): the unigram sums to `notes`;
  `first` sums to `tunes`; order k's counts sum to `notes − k · tunes` (every tune has at least 8
  notes); the header's context count matches; no context repeats; each context has k known
  degrees. And, since every tune ends on its tonic: **a degree's order-1 row sums to its unigram
  count, less `tunes` for degree 1** (in `european_ionian`, row `7` sums to 35,562 and row `1` to
  103,918 − 7,136 = 96,782).

## Code layout

All in Assembly-CSharp (no asmdefs, as Deepglow), namespace `Aurumworks.Aleatora`, under
`Assets/Aurumworks/Scripts/`. Tests are in `Editor/Tests/`, namespace `Aurumworks.Aleatora.Tests`.

| Folder | Holds | Unity in it? |
|---|---|---|
| `Grid/` | `HexCoord`, `HexDirection`, `HexShape`, `HexLayout` (plain C#); `HexGrid`, `HexCellView`, `GridSelection` | The last three only |
| `Music/` | `Degree`, `ModeScale`, `MarkovTable`, `MarkovTableReader`, `Rng`, `GeneratorSettings`, `MelodyGenerator`, `MelodyStep`, `Voicing` | No |
| `Game/` | The bootstrap (`Aleatora`), `AleatoraTuning` and its sections (`GridTuning`, `MelodyTuning`), `MarkovLibrary` (loads the tables from `Resources`) | Yes |
| `View/` | `MelodiesPage`, `DegreeBars`, `TransitionGrid`, `MelodyStrip` | Yes |
| (root) | The template's `MainMenu`, `SessionMenu`, `SettingsMenu`, `MenuCheck` | Yes |
| `Editor/`, `Editor/Tests/` | `PlayerBuild`, `TuningMaker` (*Aleatora > Make Tuning*), EditMode tests | Editor |

---

## M0 — Aleatora brought up, the grid rebuilt (`main`, then branch `m0-grid`)

**Tasks**

1. **Check no Editor has the project open** (`Aleatora/Temp/UnityLockfile`). Throughout, run
   Unity headless only when it is absent.
2. **Git first, before anything changes:** the template's `.gitignore` and `.gitattributes`,
   `git init -b main`, `git lfs install`, and commit the project as it is ("As found"). Every
   change after it is a diff Jon can read.
3. **Bring it up** as `Framework/Template/README.md`, *Bringing an existing project up*, says, but
   with no sound (decision 1):
   - Copy in the template's scripts, `Resources/Menus.prefab`, `ScriptableObjects/UI Theme.asset`,
     `Tools/menucheck.sh` and `Assets/TextMesh Pro/` (Aleatora has none), each with its `.meta`
     and the `.meta` of each new folder. **Not** the mixer. Check first that none of those GUIDs
     is already in Aleatora.
   - `SettingsMenu` lists no volumes (Settings is Fullscreen and Back), in its code default and on
     `Menus.prefab`; `MenuCheck` loses its mixer check. Note the difference from the template in
     `Docs/Aleatora_Tech.md`.
   - `Packages/manifest.json`: add `"com.aurumworks.core": "file:../../Framework/com.aurumworks.core"`.
     Nothing else changes in it.
   - The `Menu` action, with the template's ids and its two bindings (`<Keyboard>/escape`,
     `<Gamepad>/start`), into the `UI` map of Aleatora's own `Assets/InputSystem_Actions.inputactions`.
     Aleatora also has a generated `InputSystem_Actions.cs`: if *Generate C# Class* is on for the
     asset, Unity regenerates it on import; don't edit it by hand. Check the asset is the
     project-wide actions.
   - Player settings: company name Aurumworks, Fullscreen Window, Resizable Window on, Active Input
     Handling: Input System Package. Product Name *Aleatora* (the project was once *Aleatoria*: its
     `.vs/Aleatoria.slnx` remains; check the product name, the save folder follows it).
   - Renames: namespace `Aurumworks.Aleatora` in the template's scripts; flags
     `-aleatoraMenuCheck`, `-aleatoraScreenshots` (`MenuCheck.cs`), `-aleatoraBuildPath`
     (`PlayerBuild.cs`); `product`, `namespace` and `game` at the top of `Tools/menucheck.sh`;
     Title on `MainMenu` in `Menus.prefab`. The main menu is drawn over Aleatora's scene,
     `Assets/Scenes/SampleScene.unity` (point `MainMenu` at it, and add it to the build if it isn't).
4. **Run the menu check** (`bash Tools/menucheck.sh m0-setup`). The first build may rewrite the
   three URP files: commit them as left, and check a second build leaves the tree clean. Commit the
   bring-up on `main`.
5. **Record the grid as found**, on branch `m0-grid`, before changing it: every Inspector value on
   the grid's objects in `SampleScene.unity` and the two hexagon prefabs (rows 7, columns 11, the
   prefab, the layer, the selected colour, the input action references, the Cinemachine camera),
   which materials and meshes they use, and a screenshot from the menu check's Play step. These
   become the tuning asset's defaults (step 7), so the first run of the new grid looks as the old
   one did but for its shape.
6. **Review the grid's code** and write the findings into `Docs/Aleatora_Tech.md` (*Grid*, "As
   found") before rewriting it. Known so far:
   - `IsCoordinateInbounds` compares `row` with `columns` and `column` with `rows`.
   - `Neighbours` adds `hexagons[row, column]` (the cell itself) once per neighbour, not the
     neighbour.
   - `RelativeCoordinatesFromCardinalDirection` gives `(-1, -1)` for South, SouthWest and
     NorthWest alike, so it doesn't invert `CardinalDirectionFromRelativeCoordinate`.
   - The offset coordinates make every neighbour rule depend on the column's parity, which
     `NeighbourCoordinates` ignores, so some neighbours are wrong in every other column.
   - `NeighbourAtRelativeCoordinate` treats a relative coordinate as absolute.
   - `SelectionManager` sets `renderer.material.color`, which copies the material on every
     selection; it reads `Camera.main` and `Mouse.current` every frame; it doesn't wait for menus.
   - `HexDisc` is unfinished and unused (check the scene and prefabs for its GUID).
   - Public fields for everything, no namespace, `GameObject[,]` as the model.
7. **Rebuild it on three dimensions.** The board is MathWorld's k × m × n hexagonal grid graph: k
   hexagons along the horizontal axis, m along the northeast and n along the northwest; a hexagon
   with opposite sides of k, m and n cells. k = m = n is a regular hexagon (what `HexDisc` was
   for); n = 1 is a k × m parallelogram; 1 × 1 × 1 is one cell.
   - **`Grid/HexCoord`** (plain C#, readonly struct): cube coordinates `Q`, `R`, `S` with
     `Q + R + S = 0` (stored as `Q`, `R`; `S` computed), equality and hashing, `+`, `-`,
     `Neighbour(HexDirection)`, `Distance`, `static HexCoord Round(double q, double r)` (cube
     rounding).
   - **`Grid/HexDirection`** (enum), named by the three dimensions so it reads the same in either
     orientation: `PlusK` (+1, 0), `PlusM` (+1, −1), `PlusN` (0, −1), `MinusK` (−1, 0), `MinusM`
     (−1, +1), `MinusN` (0, +1), as (ΔQ, ΔR), in turning order. The opposite of `d` is
     `(d + 3) % 6`.
   - **`Grid/HexShape`** (plain C#): `HexShape(int k, int m, int n)` (each ≥ 1). A cell is in it
     when **0 ≤ R ≤ m + n − 2, −(m − 1) ≤ Q ≤ k − 1 and −(k + n − 2) ≤ S ≤ 0**. Its sides of
     constant R have k cells, its sides of constant S m cells, its sides of constant Q n cells.
     `Count = k·m + m·n + n·k − k − m − n + 1`. `Contains`, `Cells` (in a fixed order: R, then Q),
     `IndexOf(HexCoord)` (dense 0 … Count − 1, for arrays), `Centre` (the mean of the cells'
     positions, for centring the board). These were checked in Python for every k, m and n from 1
     to 7 (2026-11-15): the count, the side lengths and connectedness.
   - **`Grid/HexLayout`** (plain C#, `float`): `Orientation` (`Flat` or `Pointy`, decision 2),
     `Size` (centre to corner), `Gap` (between neighbours). `Position(HexCoord)` gives x and z on
     the board's plane; `Pick(x, z)` gives the coordinate under a point (the inverse, then
     `HexCoord.Round`). Pointy puts the k axis on +x; Flat is Pointy turned 90°, so the k axis runs
     along +z. With `Gap` 0 and `Size` 1, Flat spaces cells as the old grid did (1.5 apart one
     way, √3 the other).
   - **`Grid/HexGrid`** (MonoBehaviour, made by the bootstrap, not in the scene): builds the board
     from the tuning's *Grid* section, centred on the origin. One `HexCellView` per cell (the
     tuning's prefab, as now), indexed by `HexShape.IndexOf`. Changing the dimensions,
     orientation, size or gap in play rebuilds it, and the look's values (colours, height,
     prefab) apply live, so Jon can tune the look with the game running (as Deepglow's tuning,
     "Tunable in play"). `TryGetCell(HexCoord, out HexCellView)`, `Shape`, `Layout`.
   - **`Grid/HexCellView`** (MonoBehaviour): its coordinate, and its colour through a
     `MaterialPropertyBlock` (URP Lit's `_BaseColor`). States: at rest and selected (more when Jon
     asks).
   - **`Grid/GridSelection`** (replaces `SelectionManager`): a click picks by casting the
     pointer's ray onto the board's plane at the cells' top and `HexLayout.Pick`, so cells need no
     colliders (**default**; the collider mesh asset stays until Jon says it can go). Focus points
     the Cinemachine camera at the selected cell, or the board's centre. It reads the same input
     actions as now, from the project-wide actions by name, and does nothing while
     `MenuFocus.IsAnyOpen`.
   - **`Game/AleatoraTuning`** (Deepglow's `DeepglowTuning` pattern: a ScriptableObject in
     `Resources`, a `[Serializable]` section per system, `Load()` falling back to the defaults
     with a warning) with its *Grid* section, `GridTuning`: k, m and n (decision 3), orientation,
     size, gap, the cell prefab, the colours (at rest, selected), the cells' height; its defaults
     from step 5. *Aleatora > Make Tuning* (`Editor/TuningMaker`) makes the asset if it's missing
     and never overwrites it. Making the asset is approved.
   - **`Game/Aleatora`**: the bootstrap, made as the scene loads (as Deepglow's `Deepglow`), which
     loads the tuning and makes `HexGrid` and `GridSelection`.
   - **The scene:** the old grid object and its components come out of `SampleScene.unity`; the
     camera, the Cinemachine camera and the lights stay. Approved as an edit to a serialized
     asset; show Jon the scene's diff.
   - **Deleting:** the three old scripts are replaced, not moved. Delete `HexGrid.cs`,
     `HexDisc.cs`, `SelectionManager.cs` and their `.meta` files once nothing references their
     GUIDs, and `Assets/Scripts/` if it is then empty (approved). The `CardinalDirection` enum goes
     with them.
8. **Tests** (`Editor/Tests/HexCoordTests`, `HexShapeTests`, `HexLayoutTests`):
   - `HexCoord`: `Q + R + S = 0`; each direction's neighbour is at distance 1 and its opposite
     comes back; the six neighbours are distinct; distance is symmetric and obeys the triangle
     inequality; rounding a cell's own centre gives the cell, and points near a corner round to
     one of the three cells that meet there.
   - `HexShape`: the count formula for every k, m and n from 1 to 7, against `Cells`; 1 × 1 × 1
     is one cell; k × m × 1 is k·m; the six sides have k, m, n, k, m, n cells; `Contains` and
     `Cells` agree; `IndexOf` is dense and unique; every cell is connected to every other;
     neighbourliness is symmetric.
   - `HexLayout`, in both orientations: `Pick(Position(c)) == c` for every cell of a 7 × 5 × 3
     board; neighbours sit `√3 · Size + Gap` apart; the board's centre is the origin; Flat with
     `Size` 1 and `Gap` 0 matches the old grid's spacing.
9. **The menu check grows:** after Play, the grid has 61 cells at the default; a pick at the
   screen position of three known cells selects each (`GridSelection` called with that point);
   under the session menu a click selects nothing; screenshots of the default board, of 7 × 4 × 1
   and of 3 × 5 × 7, set through the tuning in the run (`3-Grid`, `3-Grid-Parallelogram`,
   `3-Grid-Uneven`), and of the other orientation (`3-Grid-Pointy`).
10. **Docs**, in Deepglow's style: `CLAUDE.md` (what Aleatora is, as above; the layout;
    never-edit; Unity rules, with *No sound* among them; code style; menu check; EditMode tests;
    editor checklist; workflow), `README.md`, `Docs/Aleatora_Handoff.md`, `Docs/Aleatora_Design.md`
    (*Decided by Jon*: what Aleatora is, and his answers to *Decisions for Jon*),
    `Docs/Aleatora_Tech.md` (*Code layout*, *Grid* with its "As found" and "As built", *Checks*).
    This plan stays in `Docs/`.
11. **The other repositories' docs:** add Aleatora to `Unity/CLAUDE.md`'s list, and to
    `Framework/Docs/framework-plan.md`'s *Where things stand* (it takes `core`, has no sound, is on
    Input System 1.20.1). Commit in `Framework/`.

**Done when** the menu check passes with no errors after the bring-up (on `main`) and after the
grid (on `m0-grid`), the grid's tests pass headless (`Deepglow/CLAUDE.md`, *EditMode tests*, with
Aleatora's paths and `$TEMP/aleatora-runs`), the build leaves the tree clean, and the docs exist.
**Hard stop:** Jon opens Aleatora in the Editor, tries shapes and both orientations, selects and
focuses cells, and tunes the look on the tuning asset. `m0-grid` merges when he is happy with how
it works (the look can keep changing after).

---

## M1 — The tables in (branch `m1-tables`)

**Tasks**

1. **Run the converter** into `Assets/Aurumworks/Resources/Markov/` (decision 5 decides which
   sets; to ship fewer, list them in `Index.txt` and leave the rest out). Unity writes the `.meta`
   files on import. Commit the `.txt` files as text (not LFS).
2. **`Music/Degree`** (static): `Count = 12`; `Name(int)` (`"1"`, `"b2"`, … `"7"`, as on disk);
   `TryParse(string, out int)`; `Display(int)` (`"♭2"`, `"♯4"` for the view).
3. **`Music/ModeScale`** (static): the degrees each mode has, as a 12-bit mask, for colouring
   in-mode degrees in the view and for the generator's `ChromaticWeight`: ionian {1 2 3 4 5 6 7},
   aeolian {1 2 ♭3 4 5 ♭6 ♭7}, dorian {1 2 ♭3 4 5 6 ♭7}, mixolydian {1 2 3 4 5 6 ♭7},
   harmonic_minor {1 2 ♭3 4 5 ♭6 7}, gong {1 2 3 5 6}, zhi {1 2 4 5 6}, yu {1 ♭3 4 5 ♭7}. An
   unknown mode has all twelve.
4. **`Music/MarkovTable`** (immutable): `Name`, `Corpus`, `Mode`, `Tunes`, `Notes`, `MaxOrder`;
   `int Unigram(int degree)`, `int First(int degree)`; `bool TryGetCounts(ReadOnlySpan<int> context,
   out ReadOnlySpan<int> counts)` (the context's length is its order; its last element is the most
   recent note); `int Total(ReadOnlySpan<int> context)`; `IEnumerable<int[]> Contexts(int order)`
   for the view. Storage: per order a `Dictionary<int, int>` from the context packed base 12 (order
   4 is under 12⁴ = 20,736) to an offset into one flat `int[]` of counts. No string keys at run
   time.
5. **`Music/MarkovTableReader`**: `static IReadOnlyList<string> Read(string text, out MarkovTable
   table)`. Never throws; on any error `table` is null and the list says what and on which line.
   It makes every check in *The tables on disk*, the row-sum one included. Accepts CRLF too.
6. **`Game/MarkovLibrary`**: reads `Resources/Markov/Index.txt`, then each set as a `TextAsset`
   the first time it is asked for (`TryGet(string name, out MarkovTable)`), caching it; `Names`
   lists what the index lists. A set that fails to read is logged with `Debug.LogError`, once.
   Statics reset in a `SubsystemRegistration` method.
7. **Tests** (`Editor/Tests/MarkovTableTests`):
   - Every set in `Index.txt` loads from `Resources` with no errors.
   - `european_ionian`, known values: tunes 7136, notes 481199; unigram of `1` is 103,918; first
     of `5` is 3,929; after `7`, 16,296 of 35,562 go to `1`; after `#4` (a context that begins
     with `#`), 1,026 of 1,456 go to `5`; after `5 4 3 2`, 3,014 go to `1`.
   - The invariants, for every set and every order.
   - Malformed input returns errors and doesn't throw: an empty string, a wrong first line, 11
     counts in a row, a negative count, an unknown degree, a repeated context, a context of the
     wrong length, a header count that disagrees, totals that disagree, a truncated file.
   - CRLF text reads the same as LF.

**Done when** the tests pass headless and the menu check still passes.

---

## M2 — The generator (branch `m2-generator`)

**Tasks**

1. **`Music/Rng`**: a small seeded generator of the project's own (SplitMix64 to seed, then
   xoshiro128** or similar), `NextDouble()`, `Next(int)`; the same numbers on every runtime.
2. **`Music/GeneratorSettings`** (a struct, serializable on the tuning asset): `Order` (0–4;
   **default** 2), `Temperature` (> 0; 1 is as the data has it, toward 0 the likeliest note,
   above 1 flatter; **default** 1), `Smoothing` (add-k on the 12 counts of the context used;
   **default** 0: never a transition the data hasn't heard), `MinimumContext` (back off when a
   context has fewer notes behind it than this; **default** 3), `ChromaticWeight` (multiplies the
   weight of degrees outside the mode; 0 keeps a melody in the mode; **default** 1).
3. **`Music/MelodyGenerator`**: `MelodyGenerator(MarkovTable table, GeneratorSettings settings,
   ulong seed)`; `MelodyStep Next(Span<float> distribution)`; `Reset(ulong seed)`; `History` (the
   last `MaxOrder` degrees).
   - The first note is drawn from `First`; after that, from the longest context of the history up
     to `Order` that the table has with at least `MinimumContext` behind it, backing off one
     order at a time down to the unigram (order 0).
   - Weights: `(count + Smoothing)^(1 / Temperature)`, times `ChromaticWeight` outside the mode.
     All zero (only possible with `ChromaticWeight` 0 and a chromatic-only row) backs off further.
   - **`MelodyStep`** (struct): `Degree`, `OrderUsed`, `OrderAsked`, `ContextTotal` (the notes
     behind the choice); the 12 probabilities it drew from are written into the caller's
     `distribution`, so stepping allocates nothing and the view can draw exactly what was chosen
     from.
4. **`Music/Voicing`**: degree to MIDI note number, ready for MIDI output later. `Tonic` (MIDI;
   **default** 60, C4), `Low` and `High` (**default** 55 and 79). The first note is the degree's
   placement nearest `Tonic`; each next is the placement nearest the note before (ties toward the
   middle of the range); a placement outside the range moves an octave inward (decision 6).
5. **The tuning asset's *Melodies* section**, `MelodyTuning`: the set's name (**default**
   `european_ionian`), the `GeneratorSettings`, the voicing, the seed.
6. **Tests** (`Editor/Tests/MelodyGeneratorTests`, `VoicingTests`):
   - The same seed gives the same 1,000 steps; another seed gives others.
   - Order 0: 200,000 draws match the unigram within 0.005 for each degree.
   - Order 1: after `7` in `european_ionian`, the share going to `1` is 0.458 ± 0.01.
   - With `Smoothing` 0, every generated pair at order 1 has a count above 0 in the table; at
     order 4, every step's `OrderUsed` context exists with at least `MinimumContext` behind it.
   - Back-off: a history whose order-4 context is absent reports `OrderUsed` below 4.
   - Temperature 0.01 picks the likeliest note; `ChromaticWeight` 0 never leaves the mode.
   - The distribution handed back sums to 1 and is the one drawn from.
   - Stepping allocates nothing (`GC.GetAllocatedBytesForCurrentThread` around 10,000 steps).
   - Voicing: always in range and within 0–127; the first note nearest the tonic; with a range of
     at least an octave and a half, no leap larger than a tritone.

**Done when** the tests pass headless and the menu check still passes.

---

## M3 — The view (branch `m3-view`)

The *Melodies* page (decision 4), built with `UiKit` in reference pixels on 1920 × 1080, opened
with `MenuStack.Open` (an empty menu with its own layout), so Back, Cancel, the selection and
`MenuFocus` work as for every menu. It edits a working copy of `MelodyTuning`, never the asset. It
shows and steps melodies; it plays nothing.

**Tasks**

1. **`View/MelodiesPage`**: the page and its controls, in a column at the left: *Set* (dropdown of
   `MarkovLibrary.Names`, written as "European · ionian"), *Order* (slider 0–4, showing the
   number), *Temperature* (slider on a log scale, 0.25–4), *Tonic* (dropdown C … B), *Seed* (text
   field), *Generate* (the main action, accent: a new melody of 32 notes from the seed), *Step*
   (one more note), *Back*. The kit names them (`Dropdown: Set`, `Slider: Order`, `Button:
   Generate`) for the self-test. Opened from a *Melodies* choice on the main menu and a button on
   the session menu.
2. **`View/DegreeBars`**: twelve bars, one per degree, the height its probability; in-mode degrees
   (`ModeScale`) in the theme's accent, the rest in muted; the degree's name under each and its
   share over it. Used twice: the set's unigram, and *Next note*: the distribution the last step
   drew from, with the chosen degree outlined and a line under it, "order 2 of 4 · 1,526 notes"
   (`OrderUsed`, `OrderAsked`, `ContextTotal`), so a back-off shows.
3. **`View/TransitionGrid`**: the order-1 table, 12 × 12, rows *from* and columns *to*, each row
   normalised. A 12 × 12 `Texture2D` (point filtered, made once per set, destroyed with the page)
   in a `RawImage`, the colour of a cell `Color.Lerp(panel, accent, sqrt(p))` from the theme;
   degree names along both edges. A click on a row (pointer only; it isn't in the keyboard
   navigation) shows that row's bars in *Next note* with its context. The row of the newest note
   is framed.
4. **`View/TransitionGraph`** (Jon, 2026-11-15; Design ruling 18): beside the grid, the same
   order-1 table as a directed graph. Twelve nodes on a chromatic circle, 1 at the top and
   clockwise, each labelled with its degree's name; in-mode nodes in accent, the rest muted. An
   arc from a node to each degree that follows it whose row share is at least
   `MelodyViewTuning.MinimumArcShare` (default 0.05), curved so the two directions between a pair
   don't overlap, with an arrowhead at its end and its share as a label (`.42`); a repeated note
   is a small loop outside its node. Drawn with a `MaskableGraphic` in reference pixels, made once
   per set. A click on a node (pointer only, like the grid's rows) shows its row in *Next note*;
   the newest note's node is framed and its arcs drawn in front.
5. **`View/MelodyStrip`**: the last 32 notes as a piano roll, one column a note, one row a
   semitone over the voicing's range, the tonic's rows marked, in-mode notes in accent and
   chromatic ones in danger; the newest at the right, with its MIDI note number.
6. **The menu check grows** (`MenuCheck.cs`): open *Melodies* from the main menu; it opens with
   *Generate* selected; set the seed to 1 and press *Generate*; 32 notes in the strip; the graph
   draws at least one arc and none below the minimum; screenshot (`4-Melodies`); *Order* to 4 and
   *Step* eight times, each step's `OrderUsed` ≤ 4 and the bars' sum 1; switch *Set* to `irish_dorian`, screenshot (`4-Dorian`); Cancel closes the page and hands
   the selection back; open it from the session menu too. `UiProbe.Reachable` reaches every
   control from *Generate*; `UiProbe.CheckWheel` if anything scrolls. At 1920 × 1080 and a smaller
   window (as the menu check runs them).
7. **Look at the screenshots** before reporting: labels legible, nothing overlapping at the small
   size.

**Done when** the menu check passes with its new checks and screenshots. **Hard stop:** Jon
looks at the page and rules on its layout and colours.

---

## M4 — Jon tries it

Jon tries the sets, orders and temperatures. His rulings go into `Docs/Aleatora_Design.md`;
tuning he likes goes on the tuning asset; `m3-view` merges to `main`. Then decision 7's plans.

---

## Milestone summary

| # | Branch | Lands | Checked by |
|---|---|---|---|
| M0 | `main`, `m0-grid` | Git, the framework's conventions and menus (no sound); the grid rebuilt on k × m × n in both orientations; the tuning asset; docs | Menu check after each part; grid tests; **Jon in the Editor, tuning the look** |
| M1 | `m1-tables` | The text tables, `MarkovTable`, its reader, `MarkovLibrary` | EditMode tests |
| M2 | `m2-generator` | `Rng`, the generator with back-off, voicing to MIDI notes | EditMode tests |
| M3 | `m3-view` | The *Melodies* page: bars, the transition grid and graph, the strip | Menu check with screenshots; **Jon looks** |
| M4 | merge | Rulings and tuning | Jon |

## Risks

- **Aleatora isn't in git yet.** Nothing before M0's first commit can be undone; that commit
  comes before any other change.
- **GUIDs.** The template's files bring the template's GUIDs; a clash with one already in
  Aleatora breaks references. Check before copying (M0, step 3).
- **The scene edit.** Taking the old grid out of `SampleScene.unity` is a serialized edit; read
  the YAML for every reference to the old scripts' GUIDs first, and show Jon the diff.
- **The look changes underfoot.** Jon is still tuning it. Everything visual goes on the tuning
  asset, read live; nothing visual is a constant in code. The defaults from M0's step 5 keep his
  current look as the starting point.
- **The generated input class.** Adding `Menu` to the actions changes `InputSystem_Actions.cs`;
  code that uses the class by name still compiles, but check the Console.
- **Input System 1.20.1** is newer than the template's 1.19.0. The menu shell reads actions by
  name and Lead & Bone runs 1.20.0 without trouble, but the menu check is the proof.
- **The data's limits show:** no contour, no rhythm, no phrase ends. Higher orders on small sets
  (`chinese_han_zhi`, `german_mixolydian`) back off often; the view's "order k of n" line is how
  to see it. Treat rare chromatic transitions with some suspicion: some are encoding errors in the
  corpora.
- **Headless runs while Jon's Editor is open.** Check `Temp/UnityLockfile` every time; with it
  present, ask, or work on a scratch copy as Deepglow's `CLAUDE.md` describes.
