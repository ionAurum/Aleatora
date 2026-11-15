# Aleatora: how it is built

One section per system, each with what was built under "As built". The plan is
`Aleatora_Markov_Plan.md`; Jon's rulings are in `Aleatora_Design.md`.

## Contents

- *Code layout*
- *Building in code*
- *Differences from the template*
- *Grid* (as found, the review, as built)
- *Checks*

## Code layout

All in Assembly-CSharp (no asmdefs), namespace `Aurumworks.Aleatora`, under
`Assets/Aurumworks/Scripts/`. Tests are in `Editor/Tests/`, namespace `Aurumworks.Aleatora.Tests`.

| Folder | Holds | Unity in it? |
|---|---|---|
| `Grid/` | `HexCoord`, `HexDirection` (with `HexDirections`), `HexShape`, `HexLayout` (with `HexOrientation`, `HexPoint`): plain C#. `HexGrid`, `HexCellView`, `GridSelection` | The last three only |
| `Music/` (M1, M2) | `Degree`, `ModeScale`, `MarkovTable`, `MarkovTableReader`, `Rng`, `GeneratorSettings`, `MelodyGenerator`, `MelodyStep`, `Voicing` | No |
| `Game/` | The bootstrap `Aleatora`, `AleatoraTuning`, `GridTuning`; later `MelodyTuning`, `MarkovLibrary` | Yes |
| `View/` (M3) | `MelodiesPage`, `DegreeBars`, `TransitionGrid`, `MelodyStrip` | Yes |
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
- Player settings: only Resizable Window changed (off to on); the rest already matched.
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
  (`_BaseColor`) on the cell's renderers. States: at rest, selected.
- **`GridSelection`**: a press of `Player/Left Click` casts the pointer's ray onto the plane at
  `TopHeight` and asks `Layout.Pick`; a point off the board deselects. `Player/Focus` has the
  Cinemachine camera follow the selected cell, or `Centre` with none. `PickAt(screenPoint)`,
  `Select`, `Deselect`, `Focus` are public (the menu check uses them). While
  `MenuFocus.IsAnyOpen` it does nothing and switches the scene's `CinemachineInputAxisController`
  off, so orbit and zoom wait for menus too. After a rebuild the selection stays if its cell is
  still on the board.
- **`GridTuning`** (a section of `AleatoraTuning`): `K`, `M`, `N` (1 to 40; default 5, 5, 5),
  `Orientation` (Flat), `Size` (1), `Gap` (0), `CellPrefab` (the found prefab), `Height` (1),
  `RestColor` (white), `SelectedColor` (the found green). These reproduce the found look but for
  the board's shape.
- **Not carried over:** cells are on the prefab's layer, not *Hexagons* (Jon agreed); the old
  grid's half-cell centring error is gone.
- **The cell's collider** stays on the prefab (Design, ruling 14): a non-convex MeshCollider on
  `Mesh-Hexagon-Collider.asset`, a full hexagon one unit to a corner, so at `Gap` 0 the colliders
  tile without the visual gap. Picking doesn't use it. It scales with the cell.

## Checks

- **EditMode tests** (`Editor/Tests/`), 21 as of M0:
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
  - The tests draw no random numbers.
- **The menu check** (`MenuCheck.cs`), as of M0 (10 screenshots): the template's walk (main menu,
  Settings, Play, session menu and its Settings, Quit to Menu), with no volumes and no
  AudioSource in place of the mixer check; then `CheckGrid`: the bootstrap made the grid; the
  tuning is 5 × 5 × 5 flat; 61 cells; picks at the screen points of (0, 0), (0, 4) and (4, 4)
  select each; a pick off the board selects nothing; the board rebuilds as 7 × 4 × 1 (28),
  3 × 5 × 7 (57) and 5 × 5 × 5 pointy (61), set through the tuning (`3-Grid`,
  `3-Grid-Parallelogram`, `3-Grid-Uneven`, `3-Grid-Pointy`); and under the session menu a pick
  selects nothing.
- **Runs (2026-11-15):** `m0-setup` and `m0-setup2` on `main` (exit 0; the second left the tree
  clean); EditMode tests 21 of 21; `m0-grid` on `m0-grid` (exit 0, 47 checks held, tree clean).
