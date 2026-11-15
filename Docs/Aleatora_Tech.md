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
| `Midi/` (MIDI-1) | `MidiMessage`, `IMidiOutput`, `MidiPorts`, `RecordingOutput`; internal `WinMm`, `WinMmOutput` | No |
| `Game/` | The bootstrap `Aleatora`, `AleatoraTuning`, `GridTuning`, `MarkovLibrary`, `MelodyTuning` (M2), `MelodyViewTuning` (M3), `MidiTuning`, `MidiPlayback` (MIDI-1) | Yes |
| `View/` (M3) | `MelodiesPage`, `DegreeBars`, `TransitionGrid`, `TransitionGraph`, `MelodyStrip`; the static `ViewText` and `ViewMesh` | Yes |
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
  (20–300), `Velocity` 100, `Gate` 0.9 (0.05–1).
- **`Game/MidiPlayback`**, added to the `Aleatora` object by the bootstrap (`Aleatora.Midi`):
  `Begin` takes the tuning's channel and opens the saved port (`Aurumworks.Midi.Output`, "" for
  None), **but none in a self-test**. `Choose(name)` saves (and `Settings.Save`s) and opens;
  `Use(output)` puts a stand-in in, unsaved; `Error` says why the chosen port isn't open;
  `Panic`; `Close` sends All Notes Off on `Channel`, then disposes. `OnDisable` (which leaving
  play mode, a script reload and Quit to Menu's scene load all come through) and
  `OnApplicationQuit` close.
- **Run In Background is on** (ruling 31; `ProjectSettings.asset`, `runInBackground: 1`).
- **Jon's machine** (2026-11-15): loopMIDI ports "Greedo" and "Han". Windows describes them as it
  does any port (manufacturer 1, product 102, MIDI port), so nothing in winmm says "virtual";
  loopMIDI keeps its ports' names under `HKCU\Software\Tobias Erichsen\loopMIDI\Ports`, which
  is how the EditMode test finds one to open. Windows MIDI Services is installed (`wdmaud2.drv`,
  `MidisrvTransferComplete`), and winmm sees the ports through it.

## Checks

- **EditMode tests** (`Editor/Tests/`), 21 as of M0, 69 as of M1, 107 as of M2, 131 as of M3,
  166 as of MIDI-1:
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
    ruling 33). It is the only test that opens a port, and it sends no note.
  - `MidiPlaybackTests` (MIDI-1): `RecordingOutput` keeps what, when and what is sounding;
    `MidiPlayback` with a recording: `Panic`, `Close`'s All Notes Off on the channel in use,
    another output closing the first, the channel clamped; `MidiTuning`'s defaults. `Begin` is
    never called, so no saved port opens.
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
  `menucheck.sh` takes `WIDTH` and `HEIGHT` (1920 × 1080 unless given).
- **Runs (2026-11-15):** `m0-setup` and `m0-setup2` on `main` (exit 0; the second left the tree
  clean); EditMode tests 21 of 21; `m0-grid` on `m0-grid` (exit 0, 47 checks held, tree clean). M1: EditMode tests 69 of 69;
  `m1-tables` on `m1-tables` (exit 0, 51 checks held, tree clean). Before merging M1 (with the
  gel pads): EditMode tests 69 of 69. M2: EditMode tests 107 of 107; `m2-generator` on
  `m2-generator` (exit 0, 55 checks held, tree clean). M3: EditMode tests 128 of 128, then 131 of 131 after the CanvasRenderer fix; `m3` at
  1920 × 1080 and `m3-small` at 1280 × 720 on `m3-view` (exit 0 each, 94 checks held, tree clean). MIDI-1: EditMode tests 166 of 166 (the loopMIDI test
  opened "Greedo"); `midi1` on `midi-1-port` (exit 0, 99 checks held, 12 screenshots).
