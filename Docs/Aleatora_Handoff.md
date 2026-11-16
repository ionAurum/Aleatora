# Aleatora: hand-off

Status (2026-11-15): **IN-3 (the screens) is done on branch `in-3-page`**, from `in-2-clock`.
**Hard stop: IN-4 is Jon's** (Reaper and a keyboard). The *MIDI* page has *Inputs*, *Clock*,
*Send clock* and *Monitor*; the *Monitor* dialog shows the clock, the held notes and the last 16
messages; the *BPM* box shows the followed tempo, read-only. 268 EditMode tests pass; the menu
check passes at 1920 × 1080 and 1280 × 720 (185 checks, 17 screenshots, `6-MidiIn`, `6-Monitor`,
`6-Following` new). IN-1 is on `in-1-input`, IN-2 on `in-2-clock`; on `main`: the Markov plan
and MIDI out (rulings to 36). The branches merge together after IN-4, once Jon approves.

Open for Jon:
- **IN-4** (`Docs/Aleatora_MidiIn_Plan.md`, *IN-4*): follow Reaper through Han, lead it with
  Send clock through Greedo, play the keyboard into the *Monitor*.
- **Renaming the loopMIDI ports** (he asked). Greedo and Han weren't listed at the last runs
  (only SE49, MIDIIN2 (SE49) and Aleatora Test), so a saved output "Greedo" reads "(not found)".
  Recommended: "Aleatora to Reaper" (now Greedo), "Reaper to Aleatora" (now Han), "Aleatora
  Test". loopMIDI can't rename: delete and add. Then pick them on the *MIDI* page and enable them
  in Reaper's *Preferences > MIDI Devices*. Once chosen, the plan's IN-4 steps are updated to
  match.
- **Readings to accept or change at IN-4**, with decisions 1, 6, 7 and 10: IN-2's (Tech, *As
  built (IN-2)*, *Readings*; the one most likely to matter: while following, *Generate* keeps
  note *n* on beat *n*, so a new melody is heard from the note for the next beat, not its first)
  and IN-3's (Tech, *As built (IN-3)*, *Readings*: the monitor's beat counts from 1; its clock
  line's additions; the output's own port greyed by its label only).

## Next: IN-4 with Jon, then merge

After Jon's IN-4 run: record his rulings on decisions 1, 6, 7, 10 and the readings in
`Docs/Aleatora_Design.md` (*Decided by Jon*), fix what he finds, then merge `in-1-input`,
`in-2-clock` and `in-3-page` into `main` once he approves.

Read for it:
- `Docs/Aleatora_MidiIn_Plan.md`: *Decisions for Jon*, *IN-4*, *Risks*.
- `Docs/Aleatora_Tech.md`: *As built (IN-2)* and *As built (IN-3)*, their *Readings*.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
