# Aleatora: hand-off

Status (2026-11-15): **MIDI-3 (the screens) is built on branch `midi-3-page` (from
`midi-2-player`). Hard stop: Jon plays into Reaper (MIDI-4).** The MIDI page (from Settings'
*MIDI* row and the Melodies page's *Output*), the transport on the Melodies page, the playhead on
the strip, and `MelodySource` (Endless steps the page's generator from the clock thread). 200
EditMode tests pass; the menu check passes with 144 checks at 1920 × 1080 and 1280 × 720.

## Next: MIDI-4, Jon's (`Docs/Aleatora_Midi_Plan.md`, *MIDI-4*)

Jon plays into Reaper through loopMIDI, then rules on what's open below; `midi-3-page` merges to
`main` (the three MIDI branches together). Rulings go into `Docs/Aleatora_Design.md`.

Open for Jon (Tech, *MIDI*, *As built (MIDI-2)* and *(MIDI-3)*):
- Decision 12, Panic as built: note offs on **all 16 channels** (2,048), and it **doesn't stop
  playback**. *Panic!* is a plain button (the danger colour is an option).
- *Pause* then *Play* resumes with the **next note at once**, not after the rest of the beat.
- A *BPM* change while playing: the next note comes **a beat at the new tempo after the last
  note went on** (at once if that's past).
- Choosing another port while playing **plays on**, on the new port.
- Decision 11, the transport row: its layout, the panels above made shorter for it, and
  *Output* showing **only the port's name** (about 170 px wide; a long name is cut with "…").
- Decision 9, the playhead: framed 3 px in the theme's **text** colour; the newest note keeps its
  thinner frame.
- The MIDI page **closes and opens again** after a choice or *Refresh*; a saved port that won't
  open reads "(not open)", one that's gone "(not found)".
- Loop, Endless, Channel and BPM last for the run (on `MidiPlayback`), **not saved**.

After MIDI-4, what comes next is open (the plan's *Later*, Design's *Open*): ask Jon.

## Not in any repository

`Unity/CLAUDE.md` (the folder of projects) lists `Aleatora/`; that folder is not a repository.
