# Aleatora: hand-off

Status (2026-11-16): **the emitter plan is finished and merged into `main`**
(`Docs/Aleatora_Emitter_Plan.md`, EM-1 to EM-7; ruling 82). Spheres sprayed from the emitter
land on the board and play its notes as MIDI on Reaper's clock, with the live panel and the
controls of schemes A (mouse and keyboard) and B (gamepad). EditMode tests 393 of 393; the menu
check `em6b` exit 0, 338 checks held. **Nothing is in progress; the next plan is Jon's to pick.**

Since then, on **`octave-steps`** (from `main`, not merged): octave buttons and keys on the grid
and the Melodies page (ruling 83; Tech, *Octave steps*). EditMode tests 398 of 398; the menu check
`octave` exit 0, 347 checks held. Committed. **Waiting for Jon to try it in the Editor**, then
merge into `main`.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan, the
emitter plan.
GitHub: `ionAurum/Aleatora` (public), `origin`; `main` and `octave-steps` pushed.
Open source under MIT (ruling 84): `LICENSE`, `Docs/licences.md`, on `main` and merged into
`octave-steps`.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).
Jon has an XInput gamepad plugged in: the menu check's gamepad checks run on a pad of their own
(Tech, *Checks*, as of EM-6).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **Licences still open** (`Docs/licences.md`, *Open*), now the repository is public: the
  EmojiOne sprite sheet (committed, licence unclear; Attune leaves it out), where the two meshes
  came from, and the framework taken by path (the repository doesn't open alone; Attune takes the
  public `aurumworks-core` by git URL and tag, a change to `Packages/manifest.json`).
- **Try the octave steps** (Editor): Z and X, the panel's Octave row and the D-pad's left and
  right step the board's lowest octave; on the Melodies page Z and X, its Octave row and LB and RB
  move the melody an octave. In the Editor the board's octave stays on `AleatoraTuning` after play,
  as the panel's other controls do. Merge `octave-steps` once it's approved.

## Next: Jon picks the next plan

Each its own plan (Design, *Open*): what the grid means musically, how the generator drives the
grid, OSC out (ruling 39), the keyboard's notes (ruling 37), rhythm (every note is still a quarter
note). **Tabled until Jon asks** (ruling 82): the emitter's MIDI controls, scheme M (the controls
doc, *Scheme M*; ruling 79: Reaper's CC streams onto the emitter's parameters; it would read the
MIDI in thread, Tech, *MIDI*, *As built (IN-1)*: `FeedInput`, `HeldNotes`).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
