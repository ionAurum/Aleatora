# Aleatora: hand-off

Status (2026-11-16): **EM-6, the controls, is built on `em-6-controls`** (from `em-4-panel`; the
emitter plan, `Docs/Aleatora_Emitter_Plan.md`, *EM-6*; `Docs/Aleatora_Emitter_Controls.md`,
schemes A and B as written, rulings 77 and 78), **and revised after Jon's first try** (ruling 80:
View takes the emitter on the gamepad, the right trigger's pull sweeps the rate, the left trigger
and A set *Spread*, R/R3 and T/Y reset the tilt and the position). EditMode tests 393 of 393; the
menu check `em6b` exit 0, 338 checks held, 26 screenshots (Tech, *Emitter*, *As built (EM-6)*,
*Checks*). Committed. **Waiting for Jon to try the revised controls in the Editor**, then EM-7.

Finished on `main`: the Markov plan, MIDI out, MIDI in with clock and sync, the theory plan.
`em-1-board` to `em-6-controls` aren't merged; the plan merges at EM-7.
Jon's loopMIDI ports: "Aleatora to Reaper", "Reaper to Aleatora", "Aleatora Test" (ruling 45).
Reaper sends MIDI clock only while its transport runs (ruling 70).
Jon has an XInput gamepad plugged in: the menu check's gamepad checks run on a pad of their own
(Tech, *Checks*, as of EM-6).

A local change to `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF -
Fallback.asset` (the Editor's dynamic font atlas) is left uncommitted; it isn't ours.

## Open, for Jon

- **Try ruling 80's changes** (Editor; the controls doc's scheme B table has them all): View takes
  and lets go; held, the right trigger sprays from 0.5/s at a light pull to *Rate* at a full pull
  (the Space or panel *Spray* latch, if on, holds it at *Rate*); the left trigger widens *Spread*,
  A with it narrows; the right stick and D-pad only while held. R or R3 resets the tilt, T or Y
  the position. The feel is on `AleatoraTuning` (*Emitter: controls*, new *Spread Speed* 90).
- **EM-6's readings** (Tech, *As built (EM-6)*): the target shows a middle-sized sphere at
  *Speed*, so jitter and sizes scatter round it; the wheel and the left trigger write the tuning,
  as the panel does (in the Editor what they set stays on the asset after play).
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
