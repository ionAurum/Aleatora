# Aleatora: hand-off

Status (2026-11-16): **EM-3, notes out, is built on `em-3-notes`** (from `em-2-emitter`; the
emitter plan, `Docs/Aleatora_Emitter_Plan.md`). Jon took EM-2 as good enough for now and is
thinking about the user interaction (Design, ruling 74). EditMode tests 363 of 363; the menu
check `em3` exit 0, 251 checks held, 21 screenshots (Tech, *Emitter*, *As built (EM-3)*,
*Checks*). Committed; waiting for Jon to try it in the Editor, into Reaper, before EM-4.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board`, `em-2-emitter` and `em-3-notes` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **From EM-2, waiting for EM-5** (ruling 74): moving the emitter carries the aim point; no raise
  or lower on the gamepad; *Spray* is two actions (Space toggles, the right trigger's pressure).
  The starting looks on `AleatoraTuning` (Emitter: look) are Jon's to change.
- **From EM-3** (Tech, *As built (EM-3)*): the grid sixteenth and strength 1 are C2's starting
  values (the plan gives none), on the tuning (Emitter: notes). A hit is placed on the beat line
  when Unity reports it, so at strength 0 the hits of one frame go out together. When the line
  changes (Reaper starts, stops, jumps) the notes waiting go to its next grid point. Merged notes
  take the louder velocity and the longer length.
- **To hear it:** the MIDI page's output to "Aleatora to Reaper", a record-armed, monitored track
  on channel 1; Space sprays. Following Reaper's clock, the notes land on Reaper's sixteenths
  while it plays, on Aleatora's own line at the last tempo while it is stopped.

## Next: EM-4, the panel (branch `em-4-panel`, from `em-3-notes`)

`View/EmitterPanel`: the parameters of decisions 1, 5, 6, 10 and 11 live (set, tonic, layout,
seed, rate, spread, speed, sizes, bounciness, lifetime, grid, strength, channel, show degrees);
*Spray*; **H** hides it (the *Hide Panel* action, not yet in the `Player` map); clicks on the panel
don't reach the board; not a menu (doesn't count for `MenuFocus`). Then the menu check's panel
checks.

Read for it: the emitter plan, *Decisions* 1, 5, 6, 9, 10, 11 and *EM-4*; Tech, *Emitter* (both
As builts), *Melodies page* (how a page is built with the kit); `../Framework/README.md`, *UI kit*;
`Game/EmitterTuning.cs`, `View/MidiPage.cs` (a page built with the kit), `Grid/GridSelection.cs`
(the board's clicks).

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
