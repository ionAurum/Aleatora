# Aleatora: hand-off

Status (2026-11-15): **IN-1 (MIDI in, the base layer) is done on branch `in-1-input`**, from
`main`. Jon accepted the MIDI in plan's decisions 2, 3, 4, 5, 8 and 9 as written and gave the
go-ahead (Design, rulings 38 to 43; 1, 6, 7 and 10 stand as defaults until IN-4). 241 EditMode
tests pass, the loopback through Jon's new loopMIDI port "Aleatora Test" included (a CC and a
tick back in about 1 ms). The menu check passes at both sizes (149 checks). On `main`: the Markov
plan and MIDI out (rulings to 36).

Open for Jon:
- **Renaming his loopMIDI ports** (he asked). As of the last test run, Greedo and Han were no
  longer listed (only SE49, MIDIIN2 (SE49) and Aleatora Test), so the saved output "Greedo" will
  read "(not found)" until he picks a new one. Recommended: "Aleatora to Reaper" (now Greedo),
  "Reaper to Aleatora" (now Han), and "Aleatora Test". loopMIDI can't rename a port: delete it
  and add the new one. Then pick the new output on the *MIDI* page (the old one reads "Greedo (not
  found)"), and enable the new ports in Reaper's *Preferences > MIDI Devices*. Once he has
  chosen the names, the plan's IN-4 steps and examples (Greedo, Han) are updated to match.
- Readings in IN-1 where the plan was silent (Tech, *As built (IN-1)*): `HeldNotes` treats CC 123
  as letting go of every key (the pedal still holds) and CC 120 as ending all; choosing an
  output that is an open input closes that input; note names use ♯ (`C♯4`), as the *Melodies*
  page does; `Describe` now reads "Note on E4 96" (logs put "ch 3 " in front).

## Next: IN-2, clock (branch `in-2-clock`, from `in-1-input`)

Start on Jon's go-ahead after he has reviewed IN-1.

Read:
- `Docs/Aleatora_MidiIn_Plan.md`: *Engineering rules*, *Decisions for Jon* 3 to 6, *Code
  layout*, *IN-2*.
- `Docs/Aleatora_Tech.md`: *MIDI*, *As built (MIDI-2)* and *As built (IN-1)*.
- The code: `Assets/Aurumworks/Scripts/Midi/NotePlayer.cs`, `Game/MidiPlayback.cs`, and
  `Editor/Tests/NotePlayerTests.cs` (every check must still hold after the move to beats).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
