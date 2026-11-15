# Aleatora: hand-off

Status (2026-11-15): **the MIDI plan is finished and merged to `main`.** Jon played Aleatora
into Reaper through loopMIDI and accepted everything as built (Design, rulings 34 to 36). On
`main`: the Markov plan (M0–M4) and MIDI out (the `winmm` port, the clock-thread player, the
*MIDI* page, the transport and playhead on the *Melodies* page, *Endless*, *Panic!*). 200
EditMode tests pass; the menu check passes with 144 checks at 1920 × 1080 and 1280 × 720.

## Next: open, ask Jon

No plan is in hand. The candidates:
- The MIDI plan's *Later* (`Docs/Aleatora_Midi_Plan.md`, *Later*): MIDI in; MIDI clock and sync
  with Reaper; a richer channel choice (ruling 29); rhythm beyond quarter notes; MIDI files.
- Design's *Open* (`Docs/Aleatora_Design.md`, *Open*): what the grid means musically, how the
  generator drives the grid, OSC out.

Whichever Jon picks gets its own plan doc first (as `Aleatora_Midi_Plan.md` was), with
decisions for Jon and their defaults, before any code. To write it, read: `CLAUDE.md`; the
candidate's section above; Tech's *Code layout* and the *As built* sections of the systems it
touches (*MIDI* for anything MIDI; *Grid* and *Melodies page* for the grid).

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
