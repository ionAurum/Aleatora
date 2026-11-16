# Aleatora: hand-off

Status (2026-11-16): **EM-6, the controls, is built on `em-6-controls`** (from `em-4-panel`; the
emitter plan, `Docs/Aleatora_Emitter_Plan.md`, *EM-6*; `Docs/Aleatora_Emitter_Controls.md`,
schemes A and B as written, rulings 77 and 78). EditMode tests 383 of 383; the menu check `em6`
exit 0, 329 checks held, 26 screenshots (Tech, *Emitter*, *As built (EM-6)*, *Checks*).
Committed. **Waiting for Jon to try the controls in the Editor**, then EM-7.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board` to `em-6-controls` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).
Jon has an XInput gamepad plugged in: the menu check's gamepad checks run on a pad of their own
(Tech, *Checks*, as of EM-6).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **Try EM-6** (Editor): G or a click on the cone takes the emitter; the mouse tilts it; the left
  button sprays; the wheel sets *Rate*, the right button and the wheel *Spread*; Shift with W A S D
  glides along the grid; G or Escape lets go. The gamepad: right stick, right trigger, D-pad (with
  the left trigger for *Spread*), LB and RB, the left stick pressed for Grid Lock. The feel is on
  `AleatoraTuning` (*Emitter: controls*: *Tilt Sensitivity*, *Swing*, *Stick Tilt Speed*, *Wheel
  Step*, *Spread Step*; *Aim Line Width*, *Held Color* under the look).
- **EM-6's readings** (Tech, *As built (EM-6)*): the target shows a middle-sized sphere at
  *Speed*, so jitter and sizes scatter round it; the wheel writes the tuning, as the panel does
  (in the Editor it stays on the asset after play); the gamepad's stick and D-pad work held or not.
- **From EM-3** (Tech, *As built (EM-3)*): the grid sixteenth and strength 1 are C2's starting
  values; readings for EM-7.

## Next: EM-7, Jon plays it (once he has tried EM-6)

Jon sprays into Reaper and rules on the readings; merge the emitter branches into `main` once he
approves (the plan, *EM-7*). Then the MIDI controls (scheme M, ruling 79: Reaper's CC streams onto
the emitter's parameters) as its own plan or milestone. Read for it: the controls doc, *Scheme M*;
the MIDI in plan's input thread (Tech, *MIDI*, *As built (IN-1)*: `FeedInput`, `HeldNotes`).

Later, each its own plan (Design, *Open*): how the generator drives the grid, OSC out (ruling
39), the keyboard's notes (ruling 37), rhythm for the *Melodies* page.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
