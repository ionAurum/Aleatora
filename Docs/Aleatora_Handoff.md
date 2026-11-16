# Aleatora: hand-off

Status (2026-11-15): **IN-2 (the clock) is done on branch `in-2-clock`**, from `in-1-input`.
`NotePlayer` runs in beats, through its own beats or a `ClockFollower`; `ClockSender` sends
Aleatora's clock; `MidiPlayback` has the clock choice and *Send clock* (saved, not yet on any
screen). 265 EditMode tests pass, MIDI-2's unchanged; following a fed clock at 300 BPM, note ons
come at worst 1.06 ms after their ticks. The menu check passes (149 checks, 1920 × 1080). IN-1 is
on `in-1-input`; on `main`: the Markov plan and MIDI out (rulings to 36). The branches merge
together after IN-4.

Open for Jon:
- **IN-2's readings where the plan was silent** (Tech, *As built (IN-2)*, *Readings*), to accept
  or change at IN-4 with decisions 1, 6, 7 and 10. The one most likely to matter: while
  following, *Generate* keeps note *n* on beat *n*, so a new melody is heard from the note for the
  next beat, not from its first.
- **Renaming his loopMIDI ports** (he asked). Greedo and Han weren't listed at the last runs
  (only SE49, MIDIIN2 (SE49) and Aleatora Test), so the saved output "Greedo" reads "(not
  found)". Recommended: "Aleatora to Reaper" (now Greedo), "Reaper to Aleatora" (now Han),
  "Aleatora Test". loopMIDI can't rename: delete and add. Then pick the new output on the *MIDI*
  page and enable the ports in Reaper's *Preferences > MIDI Devices*. Once chosen, the plan's IN-4
  steps (Greedo, Han) are updated to match.

## Next: IN-3, the screens (branch `in-3-page`, from `in-2-clock`)

Hard stop after IN-3: Jon plays with Reaper and a keyboard (IN-4).

Read:
- `Docs/Aleatora_MidiIn_Plan.md`: *Decisions for Jon* 2, 3, 4, 5, 8, 10; *Code layout*
  (`Scripts/View/`); *IN-3*.
- `Docs/Aleatora_Tech.md`: *As built (MIDI-3)* (the *MIDI* page and the transport), *As built
  (IN-2)* (`MidiPlayback`'s clock API), *Checks* (the menu check, `CheckMidi`).
- The code: `View/MidiPage.cs`, `View/MelodiesPage.cs` (the transport row, the *BPM* box),
  `MenuCheck.cs` (`CheckMidi` and the transport checks), `Game/MidiPlayback.cs` (the clock members
  near the top).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
