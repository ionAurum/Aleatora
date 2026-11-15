# Aleatora: hand-off

Status (2026-11-15): **the MIDI in, clock and sync plan is written
(`Docs/Aleatora_MidiIn_Plan.md`), waiting for Jon's go-ahead and his answers to its *Decisions
for Jon*.** The brief is Design ruling 37: the technical base for MIDI in and for clock and sync
with Reaper. What the keyboard's notes mean comes later. On `main`: the Markov plan and MIDI out
(rulings to 36). 200 EditMode tests pass; the menu check passes (144 checks, both sizes).

Open for Jon: the plan's ten decisions, each with a default. Most worth a look:
- 2: any number of inputs, because the keyboard and Reaper's clock arrive on different ports.
- 3 and 4: following Reaper as the main case, with note *n* on Reaper's quarter note *n* from
  the project's start, through SPP.
- 5: *Send clock* (leading Reaper), off by default.
- 8: the *Monitor* page.
- 9: a loopMIDI port named "Aleatora Test" for the one loopback test. This changes ruling 33.
  Jon makes the port in loopMIDI.

## Next: IN-1, input (branch `in-1-input`, from `main`)

Start on Jon's go-ahead, with his rulings written into `Docs/Aleatora_Design.md` and the plan.

Read:
- `Docs/Aleatora_MidiIn_Plan.md`: all of it.
- `Docs/Aleatora_Midi_Plan.md`: *Engineering rules*.
- `Docs/Aleatora_Tech.md`: *MIDI*, all three *As built* sections.
- The code: `Assets/Aurumworks/Scripts/Midi/WinMm.cs`, `WinMmOutput.cs`, `MidiPorts.cs`,
  `MidiMessage.cs`, and `Game/MidiPlayback.cs`.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
