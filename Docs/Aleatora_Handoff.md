# Aleatora: hand-off

Status (2026-11-16): **the emitter plan is finished and merged into `main`**
(`Docs/Aleatora_Emitter_Plan.md`, EM-1 to EM-7; ruling 82). Spheres sprayed from the emitter
land on the board and play its notes as MIDI on Reaper's clock, with the live panel and the
controls of schemes A (mouse and keyboard) and B (gamepad). EditMode tests 393 of 393; the menu
check `em6b` exit 0, 338 checks held. **Nothing is in progress; the next plan is Jon's to pick.**

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan, the
emitter plan.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).
Jon has an XInput gamepad plugged in: the menu check's gamepad checks run on a pad of their own
(Tech, *Checks*, as of EM-6).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Next: Jon picks the next plan

Each its own plan (Design, *Open*): what the grid means musically, how the generator drives the
grid, OSC out (ruling 39), the keyboard's notes (ruling 37), rhythm (every note is still a quarter
note). **Tabled until Jon asks** (ruling 82): the emitter's MIDI controls, scheme M (the controls
doc, *Scheme M*; ruling 79: Reaper's CC streams onto the emitter's parameters; it would read the
MIDI in thread, Tech, *MIDI*, *As built (IN-1)*: `FeedInput`, `HeldNotes`).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
