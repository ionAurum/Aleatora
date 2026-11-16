# Aleatora: how it is built

One section per system, each with what was built under "As built". The plans are
`Aleatora_Markov_Plan.md` (finished) and `Aleatora_Midi_Plan.md`; Jon's rulings are in
`Aleatora_Design.md`.

## Contents

- *Code layout*
- *Building in code*
- *Differences from the template*
- *Grid* (as found, the review, as built)
- *Melody tables* (as built)
- *Melody generator* (as built)
- *Melodies page* (as built)
- *Cell look* (the gel pad shader, as built)
- *MIDI* (as built)
- *Checks*

## Code layout

All in Assembly-CSharp (no asmdefs), namespace `Aurumworks.Aleatora`, under
`Assets/Aurumworks/Scripts/`. Tests are in `Editor/Tests/`, namespace `Aurumworks.Aleatora.Tests`.

| Folder | Holds | Unity in it? |
|---|---|---|
| `Grid/` | `HexCoord`, `HexDirection` (with `HexDirections`), `HexShape`, `HexLayout` (with `HexOrientation`, `HexPoint`): plain C#. `HexGrid`, `HexCellView`, `GridSelection` | The last three only |
| `Music/` (M1; M2 from `Rng` on) | `Degree`, `ModeScale`, `MarkovTable`, `MarkovTableReader`, `Rng`, `GeneratorSettings`, `MelodyGenerator`, `MelodyStep`, `Voicing` | No |
| `Midi/` (MIDI-1) | `MidiMessage`, `IMidiOutput`, `MidiPorts`, `RecordingOutput`; internal `WinMm`, `WinMmOutput`; `NotePlayer`, `INoteSource`, `MelodySource` (MIDI-2, 3); `IMidiInput`, `TimedMessage`, `FeedInput`, `HeldNotes`, internal `WinMmInput` (IN-1); `IBeatSource`, `InternalBeatSource`, `ClockFollower`, `ClockSender` (IN-2) | No |
| `Game/` | The bootstrap `Aleatora`, `AleatoraTuning`, `GridTuning`, `MarkovLibrary`, `MelodyTuning` (M2), `MelodyViewTuning` (M3), `MidiTuning`, `MidiPlayback` (MIDI-1) | Yes |
| `View/` (M3) | `MelodiesPage`, `DegreeBars`, `TransitionGrid`, `TransitionGraph`, `MelodyStrip`; the static `ViewText` and `ViewMesh`; the static `MidiPage` (MIDI-3), `MidiMonitorPage` (IN-3) | Yes |
| (root) | `MainMenu`, `SessionMenu`, `SettingsMenu`, `MenuCheck` | Yes |
| `Editor/`, `Editor/Tests/` | `PlayerBuild`, `TuningMaker` (*Aleatora > Make Tuning*), EditMode tests | Editor |

## Building in code

The scene (`Assets/Scenes/SampleScene.unity`) holds the Main Camera (with the Cinemachine brain),
the `OrbitCamera` Cinemachine camera (orbital follow, rotation composer, an input controller on
`Player/Orbit` and `Player/Zoom`), the directional light, a global volume and the `Emitter`
object (the cone above the board).

**As built (M0, 2026-11-15):** `Game/Aleatora` is made `AfterSceneLoad` and again on each single
scene load (Deepglow's pattern), so Quit to Menu gives a fresh one. It loads `AleatoraTuning`
from `Resources` (a warning and the code's defaults if the asset is missing), makes a `HexGrid`
child at the origin and adds `HexGrid` and `GridSelection` to it. *Aleatora > Make Tuning*
(`Editor/TuningMaker`) makes `Assets/Aurumworks/Resources/AleatoraTuning.asset` if there is none,
with the found cell prefab, and never overwrites it. The asset in git was made that way, headless.

## Differences from the template

Aleatora was brought up as `Framework/Template/README.md` (*Bringing an existing project up*)
says, on 2026-11-15, without sound:

- No `Audio/Mixer/`. `SettingsMenu` has no mixer and no volume list (Settings is Fullscreen and
  Back), in code and on `Menus.prefab`.
- `MenuCheck` has no mixer check. It checks that `Settings.Volumes` is empty, that Settings has
  no slider, and that no AudioSource exists.
- The theme has no hover or press cue (the template's has none either).
- Aleatora's input actions had only a `Player` map (Orbit, Zoom, Focus, Left Click). The
  template's whole `UI` map (11 actions with `Menu`, its ids and bindings) was added to
  `Assets/InputSystem_Actions.inputactions`; Unity regenerated `InputSystem_Actions.cs`.
- Input System is 1.20.1 (the template's is 1.19.0). The manifest gained `com.aurumworks.core`
  and nothing else.
- Player settings: only Resizable Window changed (off to on); the rest already matched. The
  default window became 1920 × 1080 in M4 (Design, ruling 23).
- The first build rewrote `PC_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset` and
  `packages-lock.json`; committed as left, and later builds leave the tree clean.

## Grid

### As found (2026-11-15, commit "As found")

`Docs/AsFound/Grid-11x11.png` is the grid as it drew, from the menu check `m0-setup`. The paths
below are as found; on 2026-11-15 Jon moved these folders under `Assets/Aurumworks/`, the
AudioListener left the Main Camera and `Hexagon.prefab` was deleted.

- **Scene object `HexGrid`** (layer 10, *Hexagons*), at the origin, with:
  - `HexGrid`: Cinemachine camera `OrbitCamera`; rows 11, columns 11 (the code's default rows
    was 7); hexagon prefab `Assets/Prefabs/Rounded Hexagonal Prism.prefab`.
  - `SelectionManager`: select action `Player/Left Click`, focus action `Player/Focus` (Tab), the
    same camera, layer mask 1024 (*Hexagons*), selected colour (0.267, 0.839, 0.173), which is
    (68, 214, 44) / 255.
- The Cinemachine camera's tracking target was this object's transform; `HexGrid.Start` set it
  to the centre cell.
- **`Rounded Hexagonal Prism.prefab`:** a variant of `Assets/Meshes/Rounded Hexagonal Prism.fbx`
  with material `Hexagon-Leather035D_1K` (URP Lit, base colour white, smoothness 0.7, leather
  maps), Receive GI set to light probes, static, and a non-convex MeshCollider on
  `Mesh-Hexagon-Collider.asset` (63 vertices; extents 1 × 0.5 × 0.866, so flat-topped, one unit
  to a corner, one unit high).
- **`Hexagon.prefab`** (unused): the collider mesh drawn with material `Plastic`, a convex
  MeshCollider and a Rigidbody with everything but x frozen.
- `Hexagon-Plastic010_1K.mat` is used by nothing; `Plastic.mat` by `Hexagon.prefab` and the
  `Emitter Cone`.
- Spacing: columns 1.5 apart in x, cells √3 apart in z, even columns half a cell off.
- `HexDisc` was referenced by no scene or prefab.

### The review of the old code

- `IsCoordinateInbounds` compared `row` with `columns` and `column` with `rows`: wrong on any
  board that isn't square (the scene's was 11 × 11, which hid it).
- `Neighbours` added `hexagons[row, column]`, the cell itself, once per neighbour.
- `RelativeCoordinatesFromCardinalDirection` gave (−1, −1) for South, SouthWest and NorthWest
  alike, so it didn't invert `CardinalDirectionFromRelativeCoordinate`.
- The coordinates were offset coordinates, where a neighbour's step depends on the column's
  parity; `NeighbourCoordinates` used one rule for every column, so some neighbours were wrong in
  every other column.
- `NeighbourCardinalDirections` passed absolute coordinates to a function that wants relative
  ones, so it returned `Unknown` almost everywhere.
- `NeighbourAtRelativeCoordinate` treated a relative coordinate as absolute.
- The grid was centred by `columns × 1.5` and `rows × √3`, half a cell off the cells' true middle.
- `SelectionManager` set `renderer.material.color`, copying the material on every selection and
  never destroying the copy; read `Camera.main` and `Mouse.current` every frame; had
  `RequireComponent(HexGrid)` and a public `hexGrid` field it overwrote in `Awake`; didn't wait
  for menus.
- `HexDisc` was unfinished and unused.
- Public fields for everything, no namespace, `GameObject[,]` as the model.

### As built (M0, 2026-11-15, branch `m0-grid`)

The board is MathWorld's k × m × n hexagonal grid graph, in cube coordinates (Amit Patel's
*Hexagonal Grids*).

- **`HexCoord`** (readonly struct): `Q`, `R`, `S = −Q − R`; `+`, `−`, equality, hashing;
  `Neighbour(HexDirection)`; `Distance`; `Round(q, r)` (cube rounding).
- **`HexDirection`**: `PlusK` (+1, 0), `PlusM` (+1, −1), `PlusN` (0, −1), `MinusK`, `MinusM`,
  `MinusN`, as (ΔQ, ΔR), in turning order. `HexDirections.Offset` and `Opposite` ((d + 3) % 6).
- **`HexShape(k, m, n)`**: a cell is in it when 0 ≤ R ≤ m + n − 2, −(m − 1) ≤ Q ≤ k − 1 and
  −(k + n − 2) ≤ S ≤ 0. `Count`, `Contains`, `Cells` (by R, then Q), `IndexOf` (dense, −1 off the
  board), `CentreQ` and `CentreR` (the mean of the cells' coordinates; the layout turns them into
  a position). A side below 1 throws.
- **`HexLayout`** (readonly struct, `float` out, `double` inside): `Orientation`, `Size`, `Gap`,
  `Spacing` (√3 · Size + Gap, centre to neighbour's centre); `Position(cell)` gives a `HexPoint`
  (x, z); `Pick(x, z)` is its inverse with `HexCoord.Round`. `HexLayout.Centred(shape, …)` puts
  the board's middle at the origin. Pointy: +K is +x, +M north-east, +N north-west. Flat is
  Pointy turned a quarter turn, (x, z) → (−z, x): +K is +z.
- **`HexGrid`**: `Begin(GridTuning)` builds the board on its own transform: one instance of the
  tuning's prefab per cell with a `HexCellView` added, scaled (Size, Height, Size), turned 90°
  about y in Pointy (the prefab is flat-topped). Each `Update` it compares the tuning with what it
  built from: k, m, n, orientation, size, gap or prefab changed rebuilds and raises `Rebuilt`;
  height or a colour changed re-applies the look. `TopHeight` is read from the first cell's
  renderer bounds. `Shape`, `Layout`, `CellCount`, `TryGetCell`, `TopOf(coord)`, `Centre` (a
  transform at the board's middle). With no prefab on the tuning it logs an error once and draws
  nothing.
- **`HexCellView`**: `Coord`, `IsSelected`; colour through one shared `MaterialPropertyBlock`
  (`_BaseColor`) on the cell's renderers. States: at rest, selected. Since 2026-11-15 also
  `_Glow` and `_GlowColor` on a material that has them (*Cell look*).
- **`GridSelection`**: a press of `Player/Left Click` casts the pointer's ray onto the plane at
  `TopHeight` and asks `Layout.Pick`; a point off the board deselects. `Player/Focus` has the
  Cinemachine camera follow the selected cell, or `Centre` with none. `PickAt(screenPoint)`,
  `Select`, `Deselect`, `Focus` are public (the menu check uses them). While
  `MenuFocus.IsAnyOpen` it does nothing and switches the scene's `CinemachineInputAxisController`
  off, so orbit and zoom wait for menus too. After a rebuild the selection stays if its cell is
  still on the board.
- **`GridTuning`** (a section of `AleatoraTuning`): `K`, `M`, `N` (1 to 40; default 5, 5, 5),
  `Orientation` (Flat), `Size` (1), `Gap` (0; hidden and always 0 since M4, Design ruling 22), `CellPrefab` (the found prefab), `Height` (1),
  `RestColor` (white), `SelectedColor` (the found green). These reproduce the found look but for
  the board's shape. `RestGlow` (0) and `SelectedGlow` (1) came later (*Cell look*).
- **Not carried over:** cells are on the prefab's layer, not *Hexagons* (Jon agreed); the old
  grid's half-cell centring error is gone.
- **The cell's collider** stays on the prefab (Design, ruling 14): a non-convex MeshCollider on
  `Mesh-Hexagon-Collider.asset`, a full hexagon one unit to a corner, so at `Gap` 0 the colliders
  tile without the visual gap. Picking doesn't use it. It scales with the cell; `Gap` is held at 0
  (ruling 22), so the colliders always tile.
- Unity's audio is disabled in Project Settings, and the *Hexagons* layer is gone (2026-11-15).

## Melody tables

### As built (M1, 2026-11-15, branch `m1-tables`)

- **The tables** are in `Assets/Aurumworks/Resources/Markov/`: 17 sets and `Index.txt`, 1.8 MB of
  LF text, in git as text. `python -I Tools/markov.py Tools/Markov/Source
  Assets/Aurumworks/Resources/Markov` writes them again (it rewrites nothing that is identical).
  Every set has orders 1 to 4. `german_dorian` has 11 order-1 rows, not 12: a degree never
  played has no row.
- **`Music/Degree`**: `Count`, `IsValid`, `Name` (as on disk), `Display` (♭ and ♯), `TryParse`
  for a string and for a span.
- **`Music/ModeScale`**: `Mask(mode)`, `Has(mask, degree)`, `Contains(mode, degree)`, `All`
  (4095, an unknown mode's).
- **`Music/MarkovTable`**: as the plan's M1, task 4. A context is packed base 12, oldest degree
  most significant; per order a `Dictionary<int, int>` gives its row's offset in one flat
  `int[]`. Beyond the plan: **an empty context is order 0** (`TryGetCounts` gives the unigram,
  `Total` gives `Notes`, `Contexts(0)` the one empty context), ready for M2's back-off;
  `ContextCount(order)`; `HighestOrder` = 8 (12⁸ still packs into an int).
- **`Music/MarkovTableReader`**: `Read(text, out table)` never throws; errors carry line numbers
  and stop being listed after 20. It makes every check of the plan's *The tables on disk*, and
  also refuses: a header line missing, twice, or after the first `order`; orders out of turn
  (they run 1, 2, …); an unknown line. Blank lines and spaces at a line's end are passed over.
  `ReadIndex(text)` reads `Index.txt`.
  **What it can't tell:** a file cut exactly at the end of an order reads as a whole table of
  fewer orders (the format has no last line). The menu check and the tests ask for four orders.
- **`Game/MarkovLibrary`**: `Names`, `TryGet(name, out table)`. A set the index doesn't list is
  not found and logs nothing. A set whose file is missing, fails to read, or whose `set` line is
  not its file's name logs one error and is not tried again.

### As built (TH-1, 2026-11-15, branch `th-1-tables`)

Four sets written from theory (`Docs/Aleatora_Theory_Plan.md`; Design, rulings 50 and 51):
`virtuoso_lydian`, `virtuoso_phrygian`, `virtuoso_double_harmonic`, `virtuoso_hindu_pentatonic`,
21 sets in all.

- **`Tools/Markov/Theory/virtuoso.py`** writes 20,000 phrases a mode by the plan's rules and
  counts them into `Tools/Markov/Theory/Output/virtuoso_<mode>.json` (the folk JSON's shape; its
  docstring is the reference). `virtuoso.json` holds the modes, weights, seed (2112) and phrase
  count. Each mode has its own `random.Random`, seeded from `"<seed>:<mode>"`, so a mode added
  leaves the others as they were; the same seed writes the same files (checked).
  Then `python -I Tools/markov.py Tools/Markov/Source Tools/Markov/Theory/Output
  Assets/Aurumworks/Resources/Markov`: `markov.py` now takes any number of source folders, lists
  every set by name in `Index.txt`, and refuses two sets of one name. The 17 folk tables came out
  byte for byte as they were.
- **Readings where the plan was silent:** a phrase is laid out on scale steps from two under the
  low tonic to two over the high one, and starts in the middle octave; a figure's first note is a
  step or a third away (2 : 1), its direction even, flipped if it would leave the range; a
  sequence is two to four groups; a pedal's line starts one to three steps away and stays inside
  the octave (in the pentatonic, an octave would repeat the pedal's degree); a turn goes either
  way; the start is 1, 5, the 3rd or the colour at 3 : 2 : 2 : 2; a pull goes to the nearest
  placement of its target the way it goes; the ending is stepwise to a tonic, through a pull into
  1 at the pull's 70% where the mode has one (not Lydian), otherwise from the nearer side. The
  chromatic weight is **0.23 per whole step in a run** (0.2 gave 2.6%): Lydian 3.1%, Phrygian
  2.9%, double harmonic 0.6% (one whole step, 4 to 5), the pentatonic 1.6%.
- **The script's checks** (it writes nothing if one fails): every phrase ends on 1 with 8 notes
  or more; no degree twice running; nothing outside the mode but passing notes, each between two
  scale notes a whole step apart; each pulled degree's likeliest next note is a target of its
  pull; the sums. As run: after ♯4 in Lydian, 5 39% and 3 33% (the weakest pull); after ♭2 in
  Phrygian, 1 54%; double harmonic ♭2 → 1 51%, 7 → 1 48%, ♭6 → 5 40% (7 37%); the pentatonic
  ♭7 → 1 44% (5 35%), 4 → 3 45%.
- About 27 to 28 notes a phrase, 547k to 565k notes a set; the tables are 25 to 88 KB (narrower
  than a folk set: the rules make fewer contexts).
- **`ModeScale`**: `lydian`, `phrygian`, `double_harmonic`, `hindu_pentatonic` (the last
  1 3 4 5 ♭7, ruling 49). **`ViewText.SetName`** knows them: "Virtuoso · double harmonic".

## Cell look

### As built (2026-11-15, outside the milestones; merged with M1)

Built at Jon's request by Claude in the desktop app (not C2) from a study of the Intuitive
Instruments Exquis pads: frosted silicone hex caps that glow from an LED beneath.

- **`Assets/Aurumworks/Shaders/GelHexPad.shader`** (`Aurumworks/GelHexPad`), hand-written URP
  HLSL, desktop only. Passes: ForwardLit, ShadowCaster, DepthOnly, DepthNormals (SSAO). Main and
  additional lights with shadows, Forward+ (`_FORWARD_PLUS` before Unity 6.1,
  `_CLUSTER_LIGHT_LOOP` after), SSAO, reflection probes, fog. Two layers:
  - *Gel surface*, always on: wrapped diffuse with a subsurface tint (a soft terminator),
    back-light transmission through the thin top edges, rough GGX "frost" specular with a little
    normal noise, rim scatter at silhouettes, contact darkening at the base.
  - *LED glow*: a virtual LED on the cell's axis. Its light falls off as e^(−density·d)/d through
    the gel, normalised so the top centre is 1: `_GlowIntensity` is the HDR brightness there at
    `_Glow` 1, and `_ScatterDensity` only sets the contrast. The brightest part turns toward white
    (`Hot Core *`; light glows stay saturated), the silhouettes darken (`Glow Rim`), and a thin
    highlight follows the rounded top edge (`Bevel Edge Catch`, picked by object-space normal.y).
- **Object space is the shader's frame**: +Y up, the cell on the Y axis. `_CapBase` is the
  object-space y of the cell's bottom, `_CapHeight` its height, `_CapApothem` centre to flat side.
  The cell's scale (Size, Height) doesn't change object space, so the glow keeps its shape. Batching
  that bakes meshes into world space would break it; the cells are made at run time and aren't
  batched.
- **`Rounded Hexagonal Prism.fbx`, measured** (from the file): Y up, pivot at the centre,
  x ±0.929 (corners, rounded), z ±0.823 (flats), y ±0.5, rounded top and bottom edges, 1,092
  vertices. Normals are calculated on import (smoothing angle 60°).
- **`Assets/Aurumworks/Materials/Gel Hex Pad.mat`** is on `Rounded Hexagonal Prism.prefab` (its one
  material slot) in place of `Hexagon-Leather035D_1K`, which stays, unused. Where it differs from
  the shader's defaults: `_CapBase` −0.5, `_CapHeight` 1, `_CapApothem` 0.82, `_LedHeight` 0.35
  (inside the cell: the prism is tall, and with the LED under it the side walls' bases were nearer
  the LED than the top and burned out white) and `_ScatterDensity` 1.8. Every look value is on the
  material, for Jon to tune in the Inspector, live.
- **Per cell**, `HexCellView` writes the shared `MaterialPropertyBlock`: `_BaseColor` = the
  material's own base colour × `RestColor`; `_GlowColor` = the state's colour (`RestColor` or
  `SelectedColor`); `_Glow` = `RestGlow` or `SelectedGlow`. It reads each renderer's
  `sharedMaterial` once, in `Place`. On a material without `_Glow` and `_GlowColor` it does what it
  did: the selected colour replaces the base colour. One change for every material: at rest the
  base colour is now the material's times `RestColor`, as the tooltip always said, instead of
  `RestColor` alone (the same at white).
- **`GridTuning`** gained `RestGlow` (0) and `SelectedGlow` (1), 0 to 1, read live; added to
  `AleatoraTuning.asset` with those values.
- **What the glow wants from the scene:** Bloom, which Jon turned on in `SampleSceneProfile`
  (intensity 15, 2026-11-15), and Dithering on the Main Camera (on since M4, ruling 24), or the
  smooth gradients band. HDR is already on in `PC_RPAsset`. Jon also set the `Emitter` object inactive in
  the scene.
- **Not modelled:** light from a lit cell onto its neighbours (a small, dim, shadowless point
  light under a lit cell would do it).
- **`.meta` files:** `GelHexPad.shader.meta` (`72dc08d87857432990edacf3ef687e69`) and `Gel Hex Pad.mat.meta` (`0d53381a64324f70b2563f1e5f0b4a65`)
  were written by hand with new GUIDs, against the rule in `CLAUDE.md`, because the prefab had to
  name the material before Unity had imported it. Don't regenerate them; the prefab and material
  reference those GUIDs. Unity made `Assets/Aurumworks/Shaders.meta`.
- **Verified:** every pass and keyword variant compiles with DXC against the URP 17.0 shader
  library (`6000.0/staging`); the C# compiles against stand-ins for UnityEngine; the material
  values were chosen from a software render of the shader's maths on the measured mesh. Seen by Jon
  in the Editor; the 69 EditMode tests pass with it. **Not verified:** the menu check.
- **Left out of the package it came from:** `GelPadVisual` (a per-pad driver that would fight
  `HexCellView` for the property block) and `HexCapMeshGenerator` (the prefab has its own mesh).

## Melody generator

### As built (M2, 2026-11-15, branch `m2-generator`)

- **`Music/Rng`**: xoshiro128**, seeded by SplitMix64 from a `ulong`. `NextUInt`, `NextDouble`
  (53 bits from two draws), `Next(count)` (Lemire's, unbiased), `Reset(seed)`, `Seed`. Whole-number
  arithmetic only; the tests pin the first numbers of three seeds to a reference worked out apart
  from it.
- **`Music/GeneratorSettings`**: a struct with public fields `Order`, `Temperature`, `Smoothing`,
  `MinimumContext`, `ChromaticWeight`; `Default` (the plan's: 2, 1, 0, 3, 1) and `Clamped()`
  (order 0–4, temperature at least 0.01, the rest at least 0, a NaN to its default).
- **`Music/MelodyGenerator`**: as the plan's M2, task 3. The weights are the counts divided by the
  largest first, then raised to 1/T, so temperature 0.01 can't overflow. A context is used only if
  the table has it, it was heard at least `MinimumContext` times (order 0 always is) and some weight
  is above 0; otherwise one order shorter. If even the unigram weighs nothing, every degree of the
  mode as likely (it can't happen with the shipped sets). Every step makes one `NextDouble`, so
  steps stay in step with the seed. `Settings` can be changed between steps.
  Beyond the plan: **`MelodyStep.IsFirst`** (the first note, from `First`: `OrderUsed` 0,
  `OrderAsked` 0, `ContextTotal` = the set's melodies); **`SetHistory(degrees)`**, to ask what
  follows a given run (the tests' back-off cases, and the view later); `MaxOrder` (the table's).
- **`Music/Voicing`**: `Voicing(tonic, low, high)`, `Place(degree)` (stateful: nearest the note
  before, the tonic for the first), `Placement(degree, reference)` (no state), `Reset()`,
  `Previous`. Beyond the plan: values are kept within 0–127, a backward range is swapped, and a
  range narrower than 11 semitones is widened upward (downward at the top), so every degree has a
  placement in it.
  **Leaps: an octave at most** (Design, ruling 20, replacing the plan's tritone). From a note
  near an edge, the nearest placement can be outside the range, and moving it an octave inward
  makes a leap of 7 to 11 semitones (from G5 at the top of 55–79, degree 6 is A5 = 81, out, so
  A4 = 69: a leap of 10). `LeapsAreAnOctaveAtMost` asks for no leap above an octave anywhere, and
  none above a tritone from a note at least a tritone inside both edges.
- **`Game/MelodyTuning`**, on `AleatoraTuning` as `Melodies` (added to `AleatoraTuning.asset`):
  `Set` (`european_ionian`), the generator's five values as their own Inspector fields with
  tooltips and ranges (`Generator` builds the struct; `Music/` can't use Unity's attributes),
  `Tonic` 60, `Low` 55, `High` 79, `Seed` (a `long` in the Inspector, **default 2112**, Design
  ruling 21; it was 1 until M4), `MakeVoicing()`. Nothing reads it in play yet; M3's page will.
- **Stepping allocates nothing**, checked with the Unity Test Framework's `AllocatingGCMemory`
  constraint (Unity's GC.Alloc recorder). The plan's `GC.GetAllocatedBytesForCurrentThread`
  counts nothing on Unity's Mono; the test also checks that the recorder catches a 4 KB array.

## Melodies page

### As built (M3, 2026-11-15, branch `m3-view`)

- **`View/MelodiesPage`**: `MelodiesPage.Open(menus)` opens it with `MenuStack.Open` and adds the
  component to the panel. From the main menu (*Melodies*, under *Play*) and the session menu
  (*Melodies*, after *Settings*). It works on `MelodyTuning.Copy()`; the asset is never written.
  Controls at the left, 560 px wide: *Set* (`ViewText.SetName`, read from the set's name so
  listing them reads no table), *Order* (the kit's slider set to 0–4, whole numbers), *Temperature*
  (0.25 × 16^v, 0.25–4), *Tonic* (C … B; keeps the tonic's octave), *Seed* (`InputField: Seed`,
  a whole number from 0; anything else puts the seed back), *Generate* (accent), *Step*, *Back*.
  At the right: the graph takes the height above the strip; *The set*, *Next note* and
  *Transitions* are stacked at its left; *Melody* runs along the bottom.
  Beyond the plan: **it makes a melody from the seed as it opens**; **its backdrop is the theme's
  Background, opaque** (the stack's scrim let the main menu show through the panels); changing
  *Set* makes a new melody from the seed; *Order* and *Temperature* apply from the next step;
  *Tonic* places the melody so far again; *Next note*'s caption names the context used
  ("after 5 3 · order 2 of 2 · 17,602 notes"; the first note "first note · from how 7,136
  melodies begin"); a clicked row reads "after 5 · order 1 · 91,139 notes".
- **`View/DegreeBars`** (a `MaskableGraphic`): twelve bars **scaled to the tallest** (the plan
  says "the height its probability"; at full scale the unigram's tallest bar would be a fifth of
  the panel); the share over each, the name under, the caption under all. In-mode bars in accent,
  the rest muted; the chosen degree framed in the text colour.
- **`View/TransitionGrid`**: as the plan. Names along both edges at 15 pt (in-mode in the text
  colour, the rest muted); a row the table never heard is all panel colour. `RowAt` and
  `OnPointerClick`.
- **`View/TransitionGraph`** (a `MaskableGraphic`; ruling 18): nodes on a chromatic circle as
  large as the panel allows once room is kept for the loops and their shares. **Arcs bow inward**:
  a step clockwise (up to six semitones; six from the lower degree) by `ArcBend` of its chord,
  the way back by twice it. Bowed outward to the right of travel (the first try), an arc between
  degrees two apart ran through the node between them, its share on top of that node. Arc width
  `Lerp(ThinnestArc, ThickestArc, share)`; ordinary arcs in muted at 60% alpha, the framed node's
  in accent and drawn after; each share on a patch of the panel colour. A loop is the part of a
  circle outside its node. `NodeAt`, `NodeCentre`, `ArcCount`, `SmallestShare`.
- **`View/MelodyStrip`** (a `MaskableGraphic`): as the plan, the newest note's column framed and
  the tonic's rows named at the left ("C4").
- **`View/ViewText`**: shares as ".42", "<.01", "0", "1"; set, tonic and note names; counts with
  commas. **Flat and sharp signs only if the font has them**: the theme sets no fonts, and
  TextMeshPro's default LiberationSans has no ♭ or ♯, so the page writes `b3` and `#4`.
  **`View/ViewMesh`**: rectangles, frames, thick polylines, discs, rings, triangles.
- **`Game/MelodyViewTuning`**, on `AleatoraTuning` as `MelodyView` (added to the asset), read
  live (the graph rebuilds when a value changes): `MinimumArcShare` 0.05, `NodeRadius` 24,
  `ArcBend` 0.16, `ThinnestArc` 1.5, `ThickestArc` 9, `ArrowLength` 12, `ArcLabelSize` 15,
  `NodeLabelSize` 20. Colours are the theme's.
- `MelodyTuning` gained `Copy()` and a setter on `Tonic`.
- **A `MaskableGraphic` of ours needs `[RequireComponent(typeof(CanvasRenderer))]`** (as `Image`
  has). `Graphic` doesn't require one, and its getter adds one only when `GetComponent` gives a
  true null: a player does, the Editor gives a fake null and adds none, so the raycaster threw
  `MissingComponentException` as the pointer moved (found by Jon in the Editor; the menu check
  moves no pointer and runs a player).

## MIDI

### As built (MIDI-1, 2026-11-15, branch `midi-1-port`)

Our own wrapper over `winmm.dll` (Design, ruling 27), plain C# in `Midi/`.

- **`MidiMessage`**: short messages packed as `midiOutShortMsg` takes them (status lowest, then
  the two data bytes); channels 1–16, clamped, as are the data to 0–127. `NoteOn`, `NoteOff`
  (release velocity 0 unless given), `ControlChange`, `AllNotesOff` (CC 123), and `Panic`: the
  2,048 note offs (every note, every channel; ruling 32 and the plan's decision 12), made once.
  Readers (`Kind`, `Channel`, `Data1`, `Data2`, `IsNoteOn`, `IsNoteOff`, `IsAllNotesOff`) and
  `Describe` for logs.
- **`IMidiOutput`**: `Name`, `Send(int)`, `Dispose`. Sends may come from two threads.
- **`WinMm`** (internal): `midiOutGetNumDevs`, `midiOutGetDevCapsW`, `midiOutOpen` (no
  callback), `midiOutShortMsg`, `midiOutReset`, `midiOutClose`, and `ErrorText` over
  `midiOutGetErrorTextW`. `timeBeginPeriod` comes with MIDI-2's clock.
- **`WinMmOutput`** (internal; not in the plan, which had the port inside `MidiPorts`): an open
  port. A lock keeps sends and closing apart; `Dispose` resets (ending every note on every
  channel) and closes; a finalizer closes a port never disposed. A failed send keeps Windows'
  words in `LastError`.
- **`MidiPorts`**: `Outputs` lists afresh each time, in Windows' order, without any port whose
  name starts "Microsoft GS Wavetable" (ruling 28; `IsHidden`). `TryOpen(name, …)` opens the
  first listed port of that name; a name not listed gives "Greedo (not found)", one that won't
  open gives Windows' words. Off Windows, or without winmm, nothing is listed or opened.
- **`RecordingOutput`**: keeps each message with its time (its own `Stopwatch`, or a clock
  given), and what is sounding (note on adds; note off, note on at 0 or CC 123 on its channel
  removes; `Dispose` clears). Thread-safe.
- **`Game/MidiTuning`**, on `AleatoraTuning` as `Midi` (added to the asset): `Channel` 1, `Bpm` 120
  (20–300), `Velocity` 100, `NoteLength` 0.9
  (0.05–1; a share of the beat, called `Gate` until Jon asked for a clearer name).
- **`Game/MidiPlayback`**, added to the `Aleatora` object by the bootstrap (`Aleatora.Midi`):
  `Begin` takes the tuning's channel and opens the saved port (`Aurumworks.Midi.Output`, "" for
  None), **but none in a self-test**. `Choose(name)` saves (and `Settings.Save`s) and opens;
  `Use(output)` puts a stand-in in, unsaved; `Error` says why the chosen port isn't open;
  `Panic`; `Close` sends All Notes Off on `Channel`, then disposes. `OnDisable` (which leaving
  play mode, a script reload and Quit to Menu's scene load all come through) and
  `OnApplicationQuit` close.
- **Run In Background is on** (ruling 31; `ProjectSettings.asset`, `runInBackground: 1`).
- **Jon's machine** (2026-11-15): loopMIDI ports "Greedo" and "Han", renamed at IN-4 to
  "Aleatora to Reaper" and "Reaper to Aleatora" (ruling 45), with "Aleatora Test". The names
  "Greedo" and "Han" in this doc, the code's comments and the tests are examples only. Windows describes them as it
  does any port (manufacturer 1, product 102, MIDI port), so nothing in winmm says "virtual";
  loopMIDI keeps its ports' names under `HKCU\Software\Tobias Erichsen\loopMIDI\Ports`, which
  is how the EditMode test finds one to open. Windows MIDI Services is installed (`wdmaud2.drv`,
  `MidisrvTransferComplete`), and winmm sees the ports through it.

### As built (MIDI-2, 2026-11-15, branch `midi-2-player`)

- **`Midi/NotePlayer`**: the schedule, no clock of its own; every call says the time (seconds).
  Its own lock, so the clock thread and the main thread may both call it. A copy of the notes;
  `SetBpm` (20–300), `SetChannel`, `Velocity`, `NoteLength`, `Loop`, `Endless` (each turns the
  other off), `Source`; `Play(now)`, `Pause(output)`, `Stop(output)`, `Release(output)`,
  `Panic(output)`, `Advance(now, output)`; `IsPlaying`, `IsPaused`, `Position`, `Sounding`,
  `Count`, `NoteAt`. The limits and defaults are its constants (`MidiTuning` refers to them, so
  `Midi/` needs nothing from `Game/`). Readings where the plan was silent, accepted by Jon in
  MIDI-4 (Design, ruling 35):
  - *Pause*, then *Play*: the next note goes on at once, and the beats run from it.
  - *BPM* while playing: the next note goes on a beat at the new tempo after the last note
    went on (at once if that time is past); the note sounding keeps its length unless the next
    note comes first.
  - A note on found more than a beat late (the clock held up) goes on now and the beats run from
    it, rather than the missed notes in a burst.
  - `SetNotes(notes)` starts from the first on the next beat (*Generate*);
    `SetNotes(notes, keepPosition: true)` plays on from the playhead (*Step*, *Tonic*). The note
    sounding goes to its note off either way.
  - The end (loop off) is found at the beat after the last note: playing until then, then
    `Position` −1 and *Play* starts from the first.
  - `Position` is the note whose beat it is: framed through its gap to the next beat, kept while
    paused, −1 when stopped.
- **`Midi/INoteSource`**: `TryNext(out note)`, *Endless*'s next note. Called **on the clock
  thread inside the player's lock**, past the last note, once a beat. The page's source (MIDI-3)
  needs its own lock around the generator and voicing, a queue of what it made for the main
  thread to show, and must never call the player while holding that lock (the lock order is
  `MidiPlayback`'s, then the player's, then the source's or the output's).
- **`Game/MidiPlayback`** gains the player and the clock: `Player` (to read, and for what sends
  nothing), `Now` (its `Stopwatch`), `SetNotes`, `Bpm`, `Channel` (a change ends the note on the
  old channel), `Play`, `Pause`, `Stop`, `Panic` (the player's: playing goes on), `Shutdown`
  (stops the player, ends the thread, closes; `OnDisable` and quitting call it), `IsClockRunning`.
  The thread starts on *Play* and ends by itself once the player isn't playing: background,
  above normal priority, `timeBeginPeriod(1)` for its life (`WinMm.BeginMillisecondTimer`),
  `Advance` under the playback's lock, then `Sleep(1)`. An exception stops the player and is
  logged from `Update`. A change of port (`Choose`, `Use`) ends the note on the old port and plays
  on, on the new one. `Begin` takes the tuning's BPM, velocity and note length too.
- **Measured** (EditMode, 2026-11-15, Jon's machine): 16 notes at 300 BPM through the thread,
  mean 0.57 ms late, worst 1.18 ms.

### As built (MIDI-3, 2026-11-15, branch `midi-3-page`)

- **`Midi/MelodySource`** (not named in the plan): the page's generator and voicing behind their
  own lock, Endless's `INoteSource`. `Begin(generator, voicing, mask)`, `SetVoicing`,
  `SetSettings`, `Step()` (the page's), `TryNext` (the clock thread's). Every note made, from
  either, goes into one queue in the order made; `Drain(list, distribution)` hands them to the
  page with the last note's distribution. It never calls the player.
- **`NotePlayer.SyncRoot`** (new): the player's lock. The page holds it while it changes its melody
  and the player's notes together (*Generate*, *Step*, *Tonic*: take what Endless made, change,
  `SetNotes`), so Endless can't add a note between them. Lock order: `MidiPlayback`'s, the
  player's, the source's or the output's. Each frame the page drains the queue (no player lock)
  and grows the strip, the bars and the lists; its melody and the player's are the same note for
  note (tested on made-up times and on a real thread).
- **`View/MidiPage`** (static, a dialog): *MIDI*, *Output*, then *None* and each port as buttons,
  the saved one in accent (so it is selected as the page opens); a saved port not listed shows
  as "Greedo (not found)"; `MidiPlayback.Error` in the danger colour under the list; *Refresh*,
  *Back*. Pressing a port `Choose`s it, then the page **closes and opens again**, built afresh
  (*Refresh* the same, with *Refresh* selected). `OutputName`: the open output's name, "None", or
  the saved name with "(not open)" or "(not found)".
- **Settings**: a *MIDI* row (`SettingsPage.More`), a button reading "MIDI: Greedo" that opens the
  MIDI page and reads the choice again as it closes. Only with the game's `MidiPlayback`.
- **The transport** on `MelodiesPage`, a row across the top of the *Melody* panel: *Play* (reads
  *Pause* while playing), *Stop*, *Loop*, *Endless*, *Panic!*, *Channel* (1–16), *BPM*
  (`InputField: BPM`; 20–300, anything else puts the last back), *Output* (the port's name only,
  the button's object `Button: Output`). Widths are fixed but *Output*'s, which takes the rest
  (about 170 px of the 1,920 canvas). The panels above gave height: *The set* 0.805–1, *Next note*
  0.61–0.795, *Transitions* and the graph from 0.28, *Melody* 0–0.27. The page's controls start as
  the player stands: Loop, Endless, Channel and BPM live on `MidiPlayback` for the run (the
  tuning's at start), not saved. *Panic!* is a plain button, not the danger colour. Closing the
  page (`OnDestroy`) stops playback and takes the source off the player; the MIDI page over it
  doesn't. *Step* and *Tonic* play on from the playhead; *Generate* and a new *Set* start from
  the first note on the next beat. The *Output* label follows the open output each frame.
- **`MelodyStrip`**: `SetPlayhead(index in the melody)`, `Playhead`, `PlayheadColumn`; the note
  playing framed 3 px in the theme's text colour, a little outside the strip; the newest note's
  frame stays in Line. A note off the strip's left edge plays with nothing framed.

### As built (IN-1, 2026-11-15, branch `in-1-input`)

MIDI in, the base layer (the MIDI in plan; Design, rulings 38 to 43). Nothing in Aleatora
responds to what comes in yet: it is read, held and queued.

- **`MidiMessage`** gains the system messages: `Clock`, `Start`, `Continue`, `Stop`,
  `SongPosition(sixteenths)` (0–16,383, clamped; the low seven bits first) and their readers
  (`IsClock`, `IsStart`, `IsContinue`, `IsStop`, `IsSongPosition`, `SongPositionOf`,
  `IsActiveSensing`), `IsSystem` (0xF0 and up: no channel; `Kind` gives a system message's whole
  status byte), `NoteName` (C4 = 60, sharps as `C♯4`, the sign the *Melodies* page uses), and
  constants for the other channel kinds, CC 64 and CC 120. **`Describe` changed** for the
  monitor: "Note on E4 96", "Note off E4", "CC 64 127", "Program 5", "Pitch bend 8192",
  "Clock: Start", "Song position 16", without the channel; `DescribeWithChannel` puts "ch 3 " in
  front for logs (`RecordingOutput.Sent.ToString`, `TimedMessage.ToString`).
- **`IMidiInput`**: `Name`, `Signal` (an auto-reset event, never disposed, so a waiter never holds
  a closed handle), `TryRead(out TimedMessage)`, `Dispose`. **`TimedMessage`**: the message, its
  time in seconds on the clock given, its port's name.
- **`WinMm`** gains `midiInGetNumDevs`, `midiInGetDevCapsW`, `midiInOpen` (with
  `CALLBACK_FUNCTION`), `midiInStart`, `midiInStop`, `midiInReset`, `midiInClose` and
  `InputErrorText` over `midiInGetErrorTextW`.
- **`WinMmInput`** (internal): an open input. The callback is static, marked
  `[AOT.MonoPInvokeCallback]`, kept alive in a static field, and finds its input through a
  **weak** `GCHandle` passed as the instance (so a lost input can still be finalized and closed).
  For `MIM_DATA` only, it stamps the time, queues under the queue's lock (at most 4,096; past that
  the oldest go, counted in `Dropped`), sets `Signal`, and catches everything. `Dispose`: stop,
  reset, close (a lock of its own for the handle, never the queue's, so the callback can't
  deadlock a close), then the handle is freed. No SysEx buffers are given, so SysEx never arrives.
- **`MidiPorts`**: `Inputs` (all listed; GS Wavetable is an output only) and
  **`TryOpenInput(name, clock, out input, out error)`**: the plan's signature plus the clock to
  stamp with (`MidiPlayback.Now` in the game). "Han (not found)" or Windows' words as for outputs.
- **`FeedInput`**: the stand-in. `Feed(message)` stamps now on its clock, `Feed(message, time)`
  with the time given; `Pending`; `IsDisposed`; nothing taken once disposed.
- **`HeldNotes`** (decision 7): per port and channel, keys down with velocities and the pedal (CC
  64, down from 64). A key let go under the pedal is sustained until it comes up; struck again it
  is down again (the new velocity). Velocity 0 is a note off. **Not in the plan:** All Notes Off
  (CC 123) lets go of every key on its channel, the pedal still holding; All Sound Off (CC 120)
  ends everything on its channel. `IsDown`, `IsHeld`, `IsSustained`, `Velocity`, `IsPedalDown`,
  `Count`, `CopyTo(list)` (lowest note first; `Held.ToString` is "E4" or "E4 (sustained)"),
  `Clear(port)`, `Version` (up by one with each change). Thread-safe.
- **`MidiPlayback`**: `InputsKey` (`Aurumworks.Midi.Inputs`, names one to a line). `Begin` opens
  the saved inputs after the output, none in a self-test. `ChooseInput(name, on)` saves and opens
  or closes; `UseInput(input)` puts a stand-in in, unsaved; `CloseInput`, `CloseInputs`;
  `InputNames`, `ChosenInputs`, `IsInputOpen`, `InputError` ("Greedo is the output", "Han (not
  found)", Windows' words); `Held`; `DrainReceived(list)`, the monitor's queue (at most 256, the
  oldest dropped), with every message but clock ticks and Active Sensing. **The output's port is
  never an input** (ruling 38): `ChooseInput` and `UseInput` refuse the open output's name (or the
  saved one's with none open), and `Choose`ing an output that is an open input closes that input
  (its choice stays saved; `Begin` skips it while it is the output). `Shutdown` closes the inputs
  after the output.
- **The clock thread** starts on *Play* or on opening an input and ends once nothing plays and no
  input is open. Each turn, under the playback's lock, it reads every input (to `Held` and the
  queue), then advances the player; then it waits on the inputs' signals for up to 1 ms
  (`WaitHandle.WaitAny`; `Sleep(1)` with no input). It holds `timeBeginPeriod(1)` for its life,
  so while an input is open the 1 ms timer is held the whole time. Closing an input takes it out
  of the list under the lock, then disposes it outside the lock (a port's close waits for Windows'
  callback). Lock order: the playback's, the player's, then the source's, the output's or an
  input's queue; the callback takes only its queue's.
- **Measured** (EditMode, 2026-11-15, Jon's machine): 20 fed notes held after mean 0.15 ms, worst
  1.26 ms. The menu check's fed chord was held within 5 ms. Inputs listed: Greedo, Han, SE49,
  MIDIIN2 (SE49); nothing opened them. With Jon's "Aleatora Test" port made (the same day; Greedo and Han were
  no longer listed), a CC and a tick sent through it came back in 1.00 ms and 1.03 ms.

### As built (IN-2, 2026-11-15, branch `in-2-clock`)

The clock (the MIDI in plan's decisions 4 to 6; rulings 39 and 40). Nothing on screen yet: the
*MIDI* page's *Clock* and *Send clock* and the *BPM* box come in IN-3.

- **`NotePlayer` runs in beats.** Its API is as in MIDI-2 (every call still says the time in
  seconds) and every MIDI-2 test passes unchanged. Note ons are scheduled in beats; a note's off is
  in seconds, NoteLength of a beat at the tempo it went on at. Two ways of keeping time:
  - **Its own** (`InternalBeatSource`: a line through one time and beat at the BPM). *Play* puts
    beat 0 (or, resuming, the next note's beat) at now; *BPM* moves the line so the next note
    keeps ruling 35's time; a note more than a beat late moves it to now. The notes play in order,
    as before.
  - **Following** a `ClockFollower` (`Follow(follower, now, output)`; null goes back). Note *n*
    goes on as the clock's beat *n* arrives, counted from the song's start; *Loop* plays *n* mod
    the count; *Endless* asks the source for notes until it has note *n* (the same notes as playing
    from the start; tested to beat 200); with neither, nothing past the last note, the playhead
    −1, and it stays *playing*, so the clock going back to the start plays again. The clock's Start
    or Continue plays it (after Aleatora's own *Stop* too); its Stop, or half a second without a
    tick, ends the note sounding and holds the playhead. A position between beats waits for the
    next whole beat. Held up by more than a beat, it plays the note for the beat it is now.
  - New: `Tempo` (the followed clock's measured tempo, else `Bpm`), `Following`, `Sender`,
    `Follow`, `SetSender`.
- **`IBeatSource`**: `BeatAt(now)`, `Tempo`. The transport (Starts, Jumps) the player reads from
  the `ClockFollower` itself.
- **`ClockFollower`**: `Receive(message, time)` takes Clock, Start, Continue, Stop and SPP and
  leaves the rest; `Check(now)` stops it after `Timeout` (0.5 s) without a tick while running.
  `Tempo`: the average over the last 24 tick gaps (one beat), from every tick, running or not; Start,
  Continue or a gap over 0.5 s starts the average afresh, the last tempo standing until two ticks
  come (0 until the first two ever). `BeatAt`: the last tick's position plus the time since at the
  tempo, never reaching the next tick, so note *n* goes on with tick 24*n*, not before it. Before
  the first tick after Start, Continue or SPP, just short of the position (the first tick after a
  Start is beat 0). Ticks while stopped move nothing. `IsRunning`, `Starts`, `Jumps`, `JumpBeat`,
  `Timeouts`, `Position`, `Reset` (the counts kept). Thread-safe.
- **`ClockSender`**: driven by the player on its own beats, under the player's lock, not by a BPM
  of its own (the plan had `Advance(now, output)`; it is `Advance(beat, output)`), so tick 24*n*
  goes out just before note *n*'s note on whatever the tempo did. *Play* from the first note: SPP 0
  and Start; a resume: SPP to the next note's beat and Continue. Either waits for its first tick,
  and goes with it. *Pause*, *Stop* and the end of the melody send Stop (nothing if Start hadn't
  gone yet). The beats count from *Play*, so Reaper's bar 1 is where Aleatora's *Play* was.
- **`MidiPlayback`**: `ClockKey` (`Aurumworks.Midi.Clock`: "" Aleatora's own, else the input's
  name) and `SendClockKey` (`Aurumworks.Midi.SendClock`: "1" on). `ChooseClock`/`UseClock`,
  `ChooseSendClock`/`UseSendClock` (saved or not, as `Choose`/`Use`); `Begin` takes the saved ones,
  not in a self-test. `ClockName`, `Followed`, `IsFollowing`, `SendClock`, `ChosenClock`,
  `ChosenSendClock`, `Follower`, `Tempo`. The clock thread feeds the followed input's messages to
  the follower, checks its timeout, then advances the player. The sender is on only with Send clock
  on and Aleatora's own clock.
- **Readings where the plan was silent**, for Jon at IN-4:
  - **A followed input that isn't open** (not found, or closed) leaves Aleatora on its own clock
    until it opens; the choice stays saved.
  - **Changing the clock while playing:** to a clock, it joins on the next beat if the clock runs,
    else waits for its Start; back to its own, the next note goes on at once. The note sounding ends
    either way.
  - **While following**, *Pause* is like *Stop* (silent until *Play* or the clock's next Start or
    Continue) but keeps the playhead. "Until Reaper next starts" counts Continue, which Reaper
    sends when it plays from mid-project.
  - **While following**, *Generate* and *Step* change the notes and note *n* still goes on beat
    *n*: a new melody is heard from the note for the next beat, not from its first, keeping
    decision 4's rule that a note always lands on the same beat.
  - **The first note after the first Start** sounds for NoteLength of a beat at Aleatora's own BPM,
    as no tempo has been measured yet (one tick); every later note at the measured tempo.
  - **Send clock and *BPM* while playing:** ruling 35 moves the next note (at once, if the new
    beat is past), and the ticks go with the notes, so they bunch or spread around that note.
    Turning Send clock on while playing joins at the next note with SPP and Continue. Changing the
    output port while sending sends no Stop to the old one.
- **Measured** (EditMode, 2026-11-15, Jon's machine): following a fed clock at 300 BPM on the real
  thread, 12 note ons after their ticks by mean 0.16 ms, worst 1.06 ms, the tempo read 300.0;
  ±1 ms of jitter at 120 BPM read at most 0.472 BPM off (the worst a ±1 ms jitter can make is
  0.48); MIDI-2's 16 notes at 300 BPM on its own clock, mean 0.63 ms late, worst 1.96 ms.

### As built (IN-3, 2026-11-15, branch `in-3-page`)

The screens (the MIDI in plan's decisions 2 to 5, 8 and 10). Nothing new is saved beyond IN-1's
and IN-2's keys.

- **`View/MidiPage`** is now 0.6 of the screen wide (the stack's `DialogWidth`, put back after, as
  the framework's Credits page does), in **two equal columns**: *Output* at the left as before;
  *Inputs* at the right, then *Clock* and *Send clock*; under both a row of *Monitor*, *Refresh*
  and *Back*. The chosen output port still has the selection as it opens.
  - *Inputs* are toggles (`MidiPage.InputRows`): each port listed, then each open input not listed
    (a stand-in), then each chosen input neither listed nor open as "Han (not found)". A toggle is
    on while its input is open or chosen; flipping it is `ChooseInput`, then the page opens again
    (as a port's button does) with that toggle selected. The output's own port reads "Aleatora
    Test (the output)" in the muted colour, can't be used, and navigation skips it
    (`MidiPlayback.IsOutputName`, now public). Why a chosen input didn't open shows under the
    toggles in the danger colour.
  - *Clock* is a dropdown (`Dropdown: Clock`): *Aleatora*, then each open input; a chosen clock
    whose input isn't open is listed after them as "Han (not open)" and stays chosen. Picking one
    is `ChooseClock`; the page doesn't rebuild.
  - *Send clock* is a toggle, `ChooseSendClock`.
- **`View/MidiMonitorPage`** (new; a MonoBehaviour on its dialog, 0.5 of the screen wide): *Clock*,
  *Held*, *Messages* and *Back*. Each frame it drains `MidiPlayback.DrainReceived` (it is the only
  reader, so opening it shows what came since it was last open, up to the queue's 256), keeps the
  newest 16, and lists them newest first in columns: port, channel (blank for a system message),
  what it is (`MidiMessage.Describe`), the time in seconds on `MidiPlayback.Now` (muted). *Held*:
  the notes on every input, lowest first, sustained ones muted; "None" with none. `ClockText`:
  "Following Han: 150.0 BPM, playing, beat 3", "Following Han: no clock yet" before a tempo is
  measured, "Aleatora: 120 BPM", with ", sending clock" or " (Han isn't open)". For the menu check:
  `ClockLine`, `HeldLine` ("C4 E4 G4 (sustained)"), `MessageLines` ("Keys · ch 1 · Note on C4 100 ·
  12.003"), and the static `Describe`.
- **`MelodiesPage`**: while following, the *BPM* box shows `MidiPlayback.Tempo` to one decimal
  (Aleatora's own BPM until two ticks have come), can't be used (its content type goes to Standard
  and its limit off, so "150.0" fits), and reads Aleatora's BPM again, editable, when it stops
  following. Checked each frame with the playhead.
- **Readings where the plan was silent**, for Jon at IN-4:
  - **The beat in the monitor counts from 1** at the song's start, as Reaper's bar 1, beat 1 is the
    first beat (Aleatora's note 0 plays on it).
  - **The monitor's clock line adds** ", sending clock" with Send clock on, and "(Han isn't open)"
    when the chosen clock's input isn't open.
  - **The greyed output's port** is greyed by its label only; the kit's toggle box looks the same
    switched off (`ButtonLook`). A box look for switched-off toggles would be a framework change.

## Checks

- **EditMode tests** (`Editor/Tests/`), 21 as of M0, 69 as of M1, 107 as of M2, 131 as of M3,
  166 as of MIDI-1, 193 as of MIDI-2, 200 as of MIDI-3, 241 as of IN-1, 265 as of IN-2, 268 as of IN-3,
  291 as of TH-1:
  - `HexCoordTests`: Q + R + S = 0; the directions as named; a neighbour is one step away and its
    opposite comes back; six distinct neighbours; distance symmetric with the triangle
    inequality; equality and hashing; rounding a centre gives the cell; points round a corner
    round to one of its three cells.
  - `HexShapeTests`, for every k, m, n from 1 to 7: the count formula against `Cells`; 1 × 1 × 1,
    5 × 5 × 5 = 61, k × m × 1 = k·m; a side below 1 refused; the six sides have k, m, n, k, m, n
    cells; `Contains` and `Cells` agree; `IndexOf` dense and in order; connected; neighbourliness
    symmetric.
  - `HexLayoutTests`, both orientations: `Pick(Position(c)) == c` on 7 × 5 × 3, and for a point
    inside each cell; neighbours √3 · Size + Gap apart; a centred board's middle at the origin;
    +K east in Pointy and north in Flat; Flat at Size 1, Gap 0 spaced as the old grid.
  - `DegreeTests` (M1): names as on disk and back; what is no degree; ♭ and ♯; each mode's
    degrees; an unknown mode has all twelve.
  - `MarkovTableTests` (M1): all 17 sets load from `Resources`, through the reader and the
    library; `european_ionian`'s known values; the invariants for every set and order, through
    the table's own API; contexts outside the table; 24 malformed tables, each with the error it
    must name; no text; the tiny table cut at every length; the cap on errors; CRLF as LF; the
    index.
  - `RngTests` (M2): the first numbers of three seeds and a `NextDouble` against the reference;
    `Reset`; `NextDouble` in [0, 1) with mean ½; `Next(12)` even within 0.005; a count of 0 refused.
  - `MelodyGeneratorTests` (M2), on the shipped tables: the plan's list (same seed, same 1,000
    steps and distributions, another seed others, `Reset` repeats; order 0 as the unigram over
    200,000 draws; after 7, 0.458 to 1, as handed back and as drawn over 40,000; no smoothing plays
    only pairs heard; at order 4 each context used heard at least `MinimumContext` times; back-off
    from a context never heard and from one heard twice; temperature 0.01 picks the likeliest
    after each degree; `ChromaticWeight` 0 stays in the mode in all 17 sets; the distribution sums
    to 1 and is the context's counts over their total; no allocation over 10,000 steps), and
    beyond it: the first step from `First`, smoothing lets every note follow, bad input refused,
    settings clamped, the history keeps four.
  - `VoicingTests` (M2): in range and in pitch class over six ranges (to 0 and 127); the first note
    nearest the tonic; a tritone tie toward the middle; a worked melody with an octave move at the
    top; narrow, backward and out-of-MIDI ranges made whole; leaps (see *Melody generator*).
  - `MelodiesPageTests` (M3): shares in hundredths at the edges; set names, and every shipped
    set has a corpus and a mode; the temperature slider's ends, middle and round trip; each drawn
    view has a CanvasRenderer (fails without the attribute, checked).
  - `MidiMessageTests` (MIDI-1): note on and off and CC 123 bytes on channels 1 and 16; channel
    and data clamped; the readers; velocity 0 ends a note; the panic's 2,048, each note and
    channel once.
  - `MidiPortsTests` (MIDI-1): listing doesn't throw; GS Wavetable never listed nor opened; a port
    not there, or no name, doesn't open and says so; **a loopMIDI port** (found through loopMIDI's
    registry key) opens, takes an All Notes Off, closes and opens again (skipped without one;
    ruling 33). It is the only test that opens a port, and it sends no note. Changed in IN-1: it
    opens only "Aleatora Test" (ruling 42), without the registry.
  - `MidiPlaybackTests` (MIDI-1): `RecordingOutput` keeps what, when and what is sounding;
    `MidiPlayback` with a recording: `Panic`, `Close`'s All Notes Off on the channel in use,
    another output closing the first, the channel clamped; `MidiTuning`'s defaults. `Begin` is
    never called, so no saved port opens.
  - `NotePlayerTests` (MIDI-2), on made-up times advanced every millisecond: four notes at 120
    on at 0, 0.5, 1, 1.5 and off 0.45 after each; channel and velocity in the bytes; a repeated
    note off then on at the same time; loop off ends with nothing sounding and *Play* restarts;
    loop on wraps; *Loop* and *Endless* exclusive; *Pause* and *Stop* leave nothing sounding,
    *Play* resumes or restarts; *Endless* plays 12 notes past 4 from the generator, the same as
    *Step* from the seed; no source ends it; *Panic* leaves the next note on its beat; *BPM*
    slower and faster; clamped; a channel change ends the note on the old; new notes from the
    next beat, or from the playhead; a note found late; no output; nothing to play.
  - `MidiPlaybackTests` (MIDI-2): the clock thread plays 16 notes at 300 BPM, each within 5 ms
    (the spread logged); *Pause* and *Stop* end the thread with nothing sounding, *Play* resumes;
    `Shutdown` while playing; a change of port while playing.
  - `MelodySourceTests` (MIDI-3): `Step` queues what the generator and voicing make, in order,
    with the last distribution; `TryNext` the same notes; not ready makes nothing and Endless
    ends; `Begin` drops what waited; voicing and settings from the next note; the page's melody
    stays the player's with *Step* between Endless's notes, on made-up times and on a real thread
    at 300 BPM (19 notes, 15 from *Step*, on the first run).
  - IN-1: `MidiMessageTests` adds the clock's one-byte messages, SPP's bytes, value and clamping,
    system messages having no channel, note names (C4 = 60) and `Describe`. `HeldNotesTests`: on
    and off, velocity 0, the pedal holding and releasing (down from 64), a key struck again under
    it, two channels and two ports apart, All Notes Off and All Sound Off, what isn't a note
    changes nothing, the list's order, a port's notes going as it closes. `MidiPortsTests`:
    `Inputs` doesn't throw; an input not there or unnamed doesn't open and says why; **only the
    loopMIDI port "Aleatora Test"** is ever opened (ruling 42; the MIDI-1 test that opened the
    first loopMIDI port, "Greedo", now opens only it): its output opens, takes an All Notes Off on
    channel 16 and closes, and a CC 119 on channel 16 and a tick sent come back in order, within
    0.1 s; both skipped without the port. `MidiInputTests`, with `FeedInput`s: the feed's order
    and closing; a fed note held within 5 ms through the real thread (20 notes, the spread
    logged); the monitor's queue without ticks or Active Sensing, and keeping the newest 256; two
    inputs apart and one closed; the last input closed ends the thread, playing keeps it; the
    output's name refused; `Shutdown` closes the inputs.
  - IN-2: `NotePlayerTests` unchanged, through the player's own beats. `ClockTests`, on made-up
    times advanced every millisecond: 120 BPM reads 120.0, a change to 150 reads within a beat,
    ±1 ms of jitter stays under 0.5 BPM, a gap starts the average afresh; following, Start plays
    note 0 on the first tick and note *n* on tick 24*n* (note offs at the measured tempo), SPP 16
    puts note 4 on the first tick, SPP 17 waits for beat 5, Stop ends the note and holds the place,
    Continue resumes at SPP's beat or where it stopped, half a second without ticks is a Stop (and
    ends a long note), *Loop* wraps, nothing past the end until the clock goes back, *Endless*
    steps to beat 200 with the notes playing from the start makes, Aleatora's *Stop* silences until
    the clock's next Start and *Play* joins on the next beat, going back to its own clock plays on
    at once, `Tempo` is the clock's; the sender: SPP 0, Start, then 24 ticks a beat 20.83 ms apart
    with each note's tick just before it, Stop on *Pause*, SPP 12 and Continue on resuming, Stop
    on *Stop* and at the melody's end, nothing while following, joining at the next note when
    turned on mid-play. `ClockPlaybackTests`, through `MidiPlayback` with a `FeedInput`: following
    a fed clock at 300 BPM on the real thread, each note on within 2 ms after its tick (the spread
    logged) and nothing sounding after Stop; a clock input not open leaves Aleatora on its own
    until it opens and after it closes; Send clock sends with its own clock (48 ticks for two
    notes, Stop at the end), not while following.
  - IN-3: `MidiMonitorTests`: a message's line (port, channel, what, time; no channel for a system
    message); the clock's line on Aleatora's own clock, with Send clock, with a clock chosen whose
    input isn't open, and following a fed clock before its tempo is known, playing on beat 3 at
    150.0 and stopped.
  - TH-1: `TheoryTableTests`, for each of the four theory sets: corpus, mode, 20,000 phrases,
    four orders; no phrase begins outside the mode; every order-1 row sums to its unigram (less
    the phrases for 1: every phrase ends on 1); under 5% of notes outside the mode; each colour
    note's likeliest next note is its pull (seven cases); an order-4 melody at ChromaticWeight 0
    stays in the mode and finds its order-4 context on more than 90% of 4,000 steps (a Python
    re-run of the generator found 99.9%). `DegreeTests` gains the four modes' masks (an unknown
    mode is now `locrian`, as `phrygian` was); `MelodiesPageTests` their names; the set count is
    21.
  - Only the M2 tests draw random numbers, from `Rng` with fixed seeds.
- **The menu check** (`MenuCheck.cs`), as of M0 (10 screenshots): the template's walk (main menu,
  Settings, Play, session menu and its Settings, Quit to Menu), with no volumes and no
  AudioSource in place of the mixer check; then `CheckGrid`: the bootstrap made the grid; the
  tuning is 5 × 5 × 5 flat; 61 cells; picks at the screen points of (0, 0), (0, 4) and (4, 4)
  select each; a pick off the board selects nothing; the board rebuilds as 7 × 4 × 1 (28),
  3 × 5 × 7 (57) and 5 × 5 × 5 pointy (61), set through the tuning (`3-Grid`,
  `3-Grid-Parallelogram`, `3-Grid-Uneven`, `3-Grid-Pointy`); and under the session menu a pick
  selects nothing.
  As of M1, `CheckTables` after `CheckGrid`: the index lists 17 sets; each reads with four
  orders; `european_ionian`'s melodies, notes and notes after `7`; an unlisted set is not found.
  As of M2, `CheckGenerator` after `CheckTables`: the tuning has its Melodies section; its set
  reads; 200 steps twice from its seed are the same; every degree, order and note in range. It logs
  the first 16 (`european_ionian`, seed 2112, order 2: `5/55 5/55 4/65 3/64 3/64 5/67 …`).
  As of M3, `CheckMelodies`, from the main menu (all through) and from the session menu (opens,
  then the gamepad's East): it opens over the menu with *Generate* selected, nothing under it
  usable, navigation reaching all 8 controls and nothing else, 32 notes; seed 1 and *Generate*
  twice make the same 32; the newest note's row and node framed; the graph's arcs (58 for
  `european_ionian`), none under the minimum (`4-Melodies`); *Order* 4 and eight *Steps*, each
  `OrderUsed` ≤ 4, `OrderAsked` 4, the bars summing to 1, the note outlined; a click on the node
  of 5 and on the grid's row of ♭3 show those rows; *Tonic* D moves every note; *Set*
  `irish_dorian` (52 arcs, "Irish · dorian"; `4-Dorian`); the asset's set unchanged; Escape
  closes it and the selection returns to *Play* (*Resume* from the session menu). 12 screenshots.
  As of MIDI-1, `CheckMidi` after `CheckGenerator`: the bootstrap made `MidiPlayback`; it opened
  no port; the outputs (logged) don't list GS Wavetable; with a `RecordingOutput` in place,
  *Panic* sends 2,048 note offs and leaves nothing sounding; `Close` sends All Notes Off on the
  channel in use and disposes it.
  As of MIDI-3, `CheckMidiPage` from Settings (*MIDI*): it opens over Settings; *None* first,
  *Refresh* and *Back*, no GS Wavetable, one button in accent; navigation stays in it (`5-Midi`);
  *Refresh* builds it again with *Refresh* selected; Escape closes it to Settings' *Back*. In
  `CheckMelodies` (all through), `CheckTransport` after `4-Melodies`, with a `RecordingOutput`:
  *Output* reads its name; *Channel* 3, *BPM* 300 (301 puts 300 back); *Play*: it reads *Pause*,
  the first four note ons are the melody's first four on channel 3, 0.2 s apart (to 15 ms), each
  note off 0.18 s on; the strip frames the player's note (`5-Playing`); *Stop* leaves nothing
  sounding or framed; *Loop* with four notes wraps; *Endless* turns Loop off and plays past note
  32, the page's melody the player's; *Panic!* sends 2,048 and playing goes on; *Output* opens the
  MIDI page over the page (as from Settings) and playing goes on; *Stop*, *Generate* again. At
  the end, Escape while playing closes the page with nothing sounding, and the recording is
  closed for `CheckMidi`. 14 screenshots.
  As of IN-1, `CheckMidi` goes on: no input opened in a self-test; the inputs logged; a
  `FeedInput` opens and starts the thread; C4, E4 and G4 fed show as held ("C4 E4 G4"), and go to
  the monitor's queue; closing the input forgets them. 149 checks.
  As of IN-3, `CheckMidiIn` after `CheckTransport`, with a `RecordingOutput` named after a listed
  input port ("Aleatora Test" when listed) and two `FeedInput`s, a keyboard and a clock: the MIDI
  page lists every input, the fed ones on and the output's port greyed; *Clock* offers Aleatora
  and each open input; *Send clock* off; *Monitor*; every control reachable (`6-MidiIn`). C4, E4,
  G4, the pedal and G4 off: the monitor lists the five newest first and holds "C4 E4 G4
  (sustained)" (`6-Monitor`). *Clock* on the fed clock: the *BPM* box can't be used; a fed clock at
  150 BPM (ticks fed on a thread of its own, each stamped with its due time) reads 150.0, the
  monitor would say "Following …: 150.0 BPM, playing, beat 3", the playhead moves from note 2 to 4
  (`6-Following`); eight beats: eight note ons, the melody's first eight, each 0 to 5 ms after its
  beat's tick; the fed Stop leaves nothing sounding. Then *Clock* on Aleatora, *Send clock* on,
  *BPM* 150, *Play* for a second, *Stop*: SPP 0, Start, about 63 ticks 16.67 ms apart on average
  (each within 5 ms), Stop, nothing sounding. The feeds close and Send clock goes off.
  `CheckReachesAll` (the Melodies page's old check, made a helper) runs on the MIDI page from
  Settings and the monitor too. 185 checks, 17 screenshots.
  As of TH-1, `CheckTables` counts 21 sets and makes a 200-note melody from each at order 4 and
  chromatic weight 0, every note in its mode; the four theory sets' first 24 are logged. 186
  checks.
  `menucheck.sh` takes `WIDTH` and `HEIGHT` (1920 × 1080 unless given).
- **Runs (2026-11-15):** `m0-setup` and `m0-setup2` on `main` (exit 0; the second left the tree
  clean); EditMode tests 21 of 21; `m0-grid` on `m0-grid` (exit 0, 47 checks held, tree clean). M1: EditMode tests 69 of 69;
  `m1-tables` on `m1-tables` (exit 0, 51 checks held, tree clean). Before merging M1 (with the
  gel pads): EditMode tests 69 of 69. M2: EditMode tests 107 of 107; `m2-generator` on
  `m2-generator` (exit 0, 55 checks held, tree clean). M3: EditMode tests 128 of 128, then 131 of 131 after the CanvasRenderer fix; `m3` at
  1920 × 1080 and `m3-small` at 1280 × 720 on `m3-view` (exit 0 each, 94 checks held, tree clean). MIDI-1: EditMode tests 166 of 166 (the loopMIDI test
  opened "Greedo"); `midi1` on `midi-1-port` (exit 0, 99 checks held, 12 screenshots). MIDI-2: EditMode tests 193 of 193; `midi2` on `midi-2-player` (exit 0, 99 checks held, unchanged). MIDI-3: EditMode tests 200 of 200 (16 notes at 300 BPM, worst 1.14 ms); `midi3-final` at 1920 × 1080 and `midi3-final-small` at 1280 × 720 on `midi-3-page` (exit 0 each, 144 checks held, 14 screenshots). IN-1: EditMode tests 239 of 241, the two "Aleatora Test" tests skipped (no such port yet), then 241 of 241 once Jon made it; `in1` at 1920 × 1080 and `in1-720` at 1280 × 720 on `in-1-input` (exit 0 each, 149 checks held, 14 screenshots). IN-2: EditMode tests 265 of 265 (none skipped); `in2` at 1920 × 1080 on `in-2-clock` (exit 0, 149 checks held, unchanged: the clock has no screen until IN-3). IN-3: EditMode tests 268 of 268; `in3-final` at 1920 × 1080 and `in3-final-720` at 1280 × 720 on `in-3-page` (exit 0 each, 185 checks held, 17 screenshots; following at 150 BPM, note ons 0.5 to 0.6 ms after their ticks; sent ticks 16.66 ms apart, worst 2.07 ms off).
