# Aleatora

A MIDI and OSC controller and composition tool (Jon, 2026-11-15). **It makes no sound of its
own:** no synthesiser, no audio output, no mixer. What it composes leaves as MIDI and OSC. Its
board is a hexagonal grid on three dimensions (k × m × n); its melodies come from Markov tables
of folk melodies (`Tools/Markov/Source/`). Unity 6000.0.85f2, URP 17.0.4, Input System 1.20.1,
Cinemachine 3.1.7, uGUI with TextMeshPro. Brought up to the Aurumworks framework on 2026-11-15.

**Read `Docs/Aleatora_Handoff.md` first.** It is the hand-off doc: the state, what comes next and
what to read for it. Jon's rulings are in `Docs/Aleatora_Design.md`; how each system is built is
in `Docs/Aleatora_Tech.md`; the plans with their milestones are `Docs/Aleatora_Markov_Plan.md` (finished) and
`Docs/Aleatora_Midi_Plan.md` (MIDI out, in hand: MIDI-1 built). Read
the sections the hand-off names, not all of them.

## Layout
- Everything of ours is under `Assets/Aurumworks/`: `Materials/`, `Meshes/`, `Physics Materials/`,
  `Prefabs/`, `Textures/` (moved there by Jon, 2026-11-15), `Resources/`, `ScriptableObjects/`,
  `Shaders/` (the cells' gel pad shader, `GelHexPad.shader`: `Docs/Aleatora_Tech.md`, *Cell look*).
- `Assets/Aurumworks/Scripts/`: our code, in Assembly-CSharp, namespace `Aurumworks.Aleatora`.
  The folders under it are in `Docs/Aleatora_Tech.md` (*Code layout*). The grid's coordinates,
  shapes and layout, and everything under `Music/` and `Midi/`, are plain C# with no `UnityEngine` in them, so
  EditMode tests run them whole.
- Code shared with the other Aurumworks projects is in `com.aurumworks.core`, taken by path from
  `../Framework/` (its own repository; namespace `Aurumworks`): settings, the UI kit and its theme,
  the menu shell, the self-test harness. A change there reaches every project: follow
  `../Framework/README.md` and commit there first. Nothing goes into the framework until a second
  game needs it, so the grid and the Markov code stay here.
- **Everything is built in code:** the scene (`Assets/Scenes/SampleScene.unity`) holds the camera,
  the Cinemachine camera, the light and little else. The board and the pages are made as the game
  runs. Tuning lives on `Assets/Aurumworks/Resources/AleatoraTuning.asset` for Jon to tune in the
  Inspector. **The look is Jon's:** expose it on the tuning asset; don't choose it.
- The menus (`MainMenu`, `SessionMenu`, `SettingsMenu`) are built in code with the framework's UI
  kit, on `Assets/Aurumworks/Resources/Menus.prefab`, which is made as the game starts. Their look
  is `Assets/Aurumworks/ScriptableObjects/UI Theme.asset`. No new colours beyond the theme's roles
  unless Jon asks.
- `Tools/markov.py` turns the analysis's JSON into the text tables the game reads.

## Never edit
- `.meta` files. Unity owns them; a changed or lost GUID breaks every reference to the asset.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `.vs/`, and the generated `*.csproj` / `*.slnx`.
- `Assets/InputSystem_Actions.cs`: Unity generates it from the `.inputactions` asset.
- Without explicit approval: `Packages/manifest.json`, `packages-lock.json` and
  `ProjectSettings/`. Read them freely; to change them, describe the change for Jon to make, or
  ask to make it. The plan's own steps are approved by Jon's go-ahead on the plan.
- Serialized assets (`.unity`, `.prefab`, `.asset`, `.mat`, `.inputactions`, model import
  settings) are edited as the work needs, without asking (Jon, 2026-11-15). Say in the report
  what changed in them. An open Editor asks Jon to reload a scene changed on disk.
- No package upgrades or downgrades. Input System stays at 1.20.1.

## Unity rules
- **No sound.** No AudioSource, AudioMixer, `OnAudioFilterRead`, synthesiser or sound cue
  anywhere, and no hover or press cues in the UI theme. The menu check fails if an AudioSource
  exists.
- One MonoBehaviour or ScriptableObject per file, and the file name must match the class name.
- Never rename or move assets (scripts included) from the file system: the `.meta` must move with
  them. Ask Jon to do it in the Editor.
- New files: create only the file; Unity generates its `.meta` on import (a headless run does,
  when no Editor is open).
- Renaming a serialized field loses its Inspector values everywhere.
- Inspector values override code defaults. Tuned values live on the tuning asset.
- Statics and static events are reset in a
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method.
- Whatever reads the player's input checks `MenuFocus.IsAnyOpen` and does nothing while a menu is
  open.
- **Deterministic.** Every random draw comes from the project's own seeded generator
  (`Music/Rng`, M2), never `UnityEngine.Random` or `System.Random`.
- **No material instances at run time.** Per-cell colour goes through a `MaterialPropertyBlock`,
  never `renderer.material`. `HexCellView` owns the cells' block: `_BaseColor`, and on the gel
  material `_Glow` and `_GlowColor`. The gel's look is on `Gel Hex Pad.mat`; its glow reads object
  space, so keep the cells out of static batching and keep `_CapBase`, `_CapHeight` and
  `_CapApothem` matched to the cell's mesh.

## Code style
`../Framework/README.md` (*Code style*): braces on their own lines, `_camelCase` private fields,
`[SerializeField] private` Inspector fields, `<summary>` on public members.

## Menu check
`bash Tools/menucheck.sh <name>` builds the Windows player and runs it with `-aleatoraMenuCheck`:
it walks the menus, checks what the menu shell promises, that Settings lists no volumes and that
nothing has an AudioSource, then the board (its cells, picks, shapes, both orientations), and
exits 0 only if every check held and nothing logged an error
(`Assets/Aurumworks/Scripts/MenuCheck.cs`). Screenshots and the log land in
`%TEMP%\aleatora-runs\<name>`. It refuses while an Editor has the project open
(`Temp/UnityLockfile`; `NOBUILD=1` reuses the last build), needs a window, and plays on Jon's
screen. A build leaves the working tree clean. **The check grows with each milestone**
(`Docs/Aleatora_Tech.md`, *Checks*).

## EditMode tests
`Assets/Aurumworks/Scripts/Editor/Tests/`, namespace `Aurumworks.Aleatora.Tests`. They run Unity
headless, so only while no Editor has the project open:
`U="/c/Program Files/Unity/Hub/Editor/6000.0.85f2/Editor/Unity.exe"`, then
`"$U" -batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode
-testResults <xml> -logFile <log>`; exit 0 when all pass. Logs in `$TEMP/aleatora-runs`. NUnit's
`Assert.That` model only.

## After every change: editor checklist
Claude can't run the Unity Editor. End every change with what Jon should verify:
1. It compiles (Console free of errors).
2. Inspector fields to assign or re-check, and assets or `.meta` files Unity will create.
3. What to look at in play.
State plainly what couldn't be verified.

## Workflow
- Git with LFS for binaries, branch `main`, no remote until Jon asks. Each milestone on its own
  branch (`m0-grid`, `m1-tables`, …), small commits, merged once Jon approves. Framework changes
  are committed in `../Framework/` first.
- One milestone at a time; stop at the end of each and report. Don't invent design: where the
  plan is silent, ask.
- Ask before deleting files or changing settings.
- `Docs/Aleatora_Handoff.md` is the hand-off doc. Update it at the end of every turn that changes
  state. A ruling of Jon's goes into `Docs/Aleatora_Design.md` (*Decided by Jon*) with its date.
  What was built goes under its section as "As built" in `Docs/Aleatora_Tech.md`.
