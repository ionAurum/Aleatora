# Aleatora — the emitter: plan

The board becomes an instrument. The emitter (the cone above the board) sprays Rigidbody
spheres, with parameters like Unity's ParticleSystem's, changed live in play. The player moves
it and aims it like a garden hose nozzle. Every cell carries a note; a sphere striking a cell
sends that note out as MIDI, quantized to the clock. The physics makes the rhythm; where the
spray lands makes the pitch.

Jon's brief (2026-11-16; Design, rulings 59 to 64): cells take their degrees from the set's
unigram (*The set* on the *Melodies* page), not from the Markov orders; three layouts of degrees
on the board, chosen by the player; a hit's velocity from its impact speed, its length from the
sphere's size, its octave from the cell's distance from the centre cell; the same seed gives the
same board, and sessions needn't replay; notes quantized to the MIDI clock, start and length, to
a chosen note value (triplets too), with a strength.

**Not in this plan:** the Markov orders on the board (ruling 60); richer reactions than a note
(a cell that changes, chords, cells that pass a note on); OSC; recording or replaying a session;
the keyboard's notes (ruling 37); anything on the *Melodies* page.

Written 2026-11-16 from `main` after the theory plan (rulings to 64). Revised the same day
(rulings 65 to 71): the cells' colours, decisions 2, 6 and 7 and the axes layout decided, a step
to design the controls (EM-5), the physics step at 5 ms.

---

## Read first

- `Docs/Aleatora_Tech.md`, *Grid* (*As built (M0)*): `HexShape`, `HexLayout`, `HexGrid`,
  `HexCellView`, `GridSelection`.
- `Docs/Aleatora_Tech.md`, *MIDI*, *As built (MIDI-2)* and *As built (IN-2)*: the clock thread,
  `NotePlayer`'s lock order, `IBeatSource`, `InternalBeatSource`, `ClockFollower`.
- `Docs/Aleatora_Tech.md`, *Melody tables*: `MarkovTable.Unigram`, `ModeScale`.
- Design, rulings 14 (the colliders), 22 (`Gap` 0), 39 (Reaper leads).

## How it works

**The board's notes.** Each cell gets a degree when the board is built, the set changes, the
layout changes or the seed changes. The weights are the set's unigram, each degree outside the
mode multiplied by *Chromatic weight* (decision 3). Three layouts (ruling 61):

1. **Scattered.** The cells are shared out among the degrees in proportion to the weights, by
   largest remainder: on 61 cells `european_ionian` gives 1 ×13, 2 ×9, 3 ×11, 4 ×6, 5 ×12, 6 ×5,
   7 ×5. Then the cells are shuffled with the seed. The board holds the set's proportions exactly
   as near as whole cells allow, rather than drawing 61 times and hoping.
2. **Clustered.** The same counts, each degree one connected region, carved one region at a
   time in an order drawn from the seed (as built in EM-1; growing all the regions at once, as
   first written here, left a region in pieces on nearly every board). A region starts on the
   edge of the free cells and grows into the free neighbour with most of its own; a cell is
   taken only if the cells still free stay connected, so the last region is whole too. On 600
   test boards, 599 had every region whole; the counts always hold.
3. **Along the axes.** An isomorphic layout, like a harmonic table: the centre cell is 1, a step
   along +K moves the degree up *K interval* scale steps, a step along +N up *N interval*, and +M
   is their sum. The default, K a third and N a third, makes M a fifth, so every triad of the mode
   is a triangle of three touching cells. This layout uses the mode's degrees, not the weights.

**The octave** (ruling 62) comes from the cell's distance in cells from the centre cell (the
cell nearest the board's middle): the board's rings are shared evenly among *Octaves* octaves
upward from *Lowest octave*, the centre lowest (decision 4). On 5 × 5 × 5 (rings 0 to 4) at three
octaves: rings 0–1 the lowest, 2–3 the next, 4 the highest. The note is the tonic's pitch class in
that octave plus the degree's semitones.

**The cells' colours** (rulings 65 and 71). A cell's colour at rest shows its degree: the root
red; every other degree between white and blue by its probability scaled to the most likely,
p / p<sub>max</sub>, so the likeliest degree after the root is full blue and a degree never
played white (as the *Melodies* page's bars are scaled to the tallest). The probability is the
degree's share of the board's weights (the unigram, chromatics multiplied by *Chromatic
weight*), p<sub>max</sub> the largest of them but the root's. The three colours are on the tuning (*Root colour*, *Unlikely colour*,
*Likely colour*). Selected and hit colours go over it as now.

**The emitter.** The scene's `Emitter` object (the cone) gets an `Emitter` component from the
bootstrap, as the grid does. While spraying it launches spheres from the cone's tip, along its
axis, spread inside a cone of *Spread* degrees, at *Rate* a second. Each sphere's size, speed and
direction are drawn from the emitter's own `Rng` (ruling 63): the same seed gives the same draws,
though not the same collisions. Spheres come from a pool, live *Lifetime* seconds or until they
fall below the board, and are never more than *Max spheres*. They collide with the cells, the
other spheres and nothing else.

**A hit.** When a sphere enters contact with a cell at an impact speed (the relative velocity
along the contact's normal) of at least *Min speed*, the cell glows briefly and a note is
scheduled:
- **Velocity** from impact speed: *Min speed* to *Max speed* mapped to *Min velocity* to *Max
  velocity* (1–127), with a *Velocity curve* exponent.
- **Length** from the sphere's size: *Min size* to *Max size* mapped to *Shortest* to *Longest*
  (in beats).
- **Start**, quantized (ruling 64). On the clock's beat line, a hit lands at beat *b*. With
  *Grid* g (a quarter note is 1 beat; ⅛ ½; 1/16 ¼; 1/32 ⅛; ⅛ triplet ⅓; 1/16 triplet ⅙; or
  *Off*) and *Strength* s (0 to 1), the note goes on at b + s·(next grid point − b). A note can't
  go back in time, so "nearest" isn't possible without delaying everything (decision 6).
- **Length**, quantized too, to the nearest multiple of g, at least one: L + s·(Lq − L).
- The same pitch going on at the same moment twice (two spheres, one grid point) is one note,
  at the louder velocity. A pitch going on while it still sounds ends first, then goes on again.
  No more than *Max voices* sound at once; a note past that is dropped.

**The clock.** Notes go out on the clock thread that `MidiPlayback` already runs, through the
same output, on the emitter's *Channel*. The beat line is Reaper's while following (ruling 39)
and its clock runs; while the followed clock is stopped, Aleatora's own line at the last tempo
measured (decision 7). On Aleatora's own clock it is a free-running line at the *BPM*, from when
the board began.

**The first controls** (decision 8), enough to build and check EM-2 to EM-4 by; the real ones
are designed in EM-5 (ruling 68) and built in EM-6. Keyboard and mouse: **Space** turns spraying on and off; **W A S D**
move the emitter over the board, relative to the camera; **Q** and **E** lower and raise it;
holding **Left Shift** makes the aim point (where the cone points, marked on the board) follow the
pointer. Gamepad: the left stick moves, the right stick moves the aim point, the right trigger
sprays, its pressure scaling *Rate*. The camera's controls (right drag, scroll, Tab) and the left
click stay as they are. The parameters are on a panel at the side of the board (decision 9).

## Decisions for Jon

Each has the default this plan builds if Jon doesn't say otherwise. All decided (ruling 72: the rest as their defaults): 2, 6 and 7, the
axes layout as written, and the cells' colours (rulings 65 to 67, 70 and 71).

1. **The board's set.** **Default:** its own *Set* on the emitter's panel, starting from the
   tuning's (`european_ionian`), with its own *Tonic*. The *Melodies* page keeps its own.
2. **Scattered: shared out or drawn.** **Decided** (ruling 67): shared out by largest
   remainder, then shuffled (as above).
3. **Chromatic degrees on the board.** **Default:** *Chromatic weight* 0, so only the mode's
   degrees. Above 0, the rare degrees get cells in proportion (at 1, as the data has it, the
   commonest chromatic in `european_ionian`, ♯4, is 1 note in 330: no cell on any board Aleatora
   builds today).
4. **Which way the octaves run.** **Default:** the centre lowest, rising outward (a drum's centre
   is its deepest note); *Outward lower* turns it round. *Lowest octave* 3 (C3 = 48 with tonic C),
   *Octaves* 3.
5. **The emitter's parameters for this plan.** **Default:** *Rate* (0–50 a second, 4), *Spread*
   (0–60°, 10), *Speed* (2–30 units a second, 8) with *Speed jitter* (0–50%, 10), *Min size* and
   *Max size* (0.1–1 unit, 0.2 and 0.5), *Bounciness* (0–1, 0.6), *Lifetime* (1–30 s, 8), *Max
   spheres* (300). Bursts, gravity, wind and a rate in beats rather than seconds are later.
6. **Quantizing forward or to the nearest.** **Decided** (ruling 66): forward, to the next grid
   point. One grid and one strength for start and length, *Quantize length* on.
7. **Following a stopped clock.** **Decided** (ruling 70): play on Aleatora's own line at the
   last tempo measured, so the board can be played into a record-armed, monitored track in Reaper
   while Reaper is stopped; when Reaper starts, the notes move to its grid.
8. **The first controls.** **Default:** as above, until EM-5. Spraying pauses while a menu is open (spheres in the
   air land and play), so the *Melodies* page and the board never play over each other.
9. **Where the parameters live.** **Default:** a panel at the right of the board, built with the
   framework's UI kit but not a menu (it doesn't count for `MenuFocus`), operated by the mouse; **H**
   hides it. Its look is the theme's. Gamepad navigation of the panel is later.
10. **How a cell shows its note.** Its colour is ruled (65 and 71). **Default** for the rest: its
    degree's name on its top ("♭3"), in the theme's text colour, *Show degrees* on the panel; a hit
    glows the cell, *Hit glow* 1 fading over *Hit fade* 0.25 s, in *Hit colour*. All on the tuning
    (the look is Jon's).
11. **Velocity and length.** **Default:** *Min speed* 1, *Max speed* 15, *Min velocity* 30,
    *Max velocity* 127, *Velocity curve* 1; *Shortest* ¼ beat, *Longest* 2 beats. *Max voices*
    32. *Channel* 1 to start, its own dropdown on the panel.

## Code layout

- `Music/CellNotes` (new, plain C#): the three layouts, the share-out and the octaves. Given a
  `HexShape`, the weights, the mode's mask, the layout's values and a seed: a degree and an octave
  per cell index.
- `Music/Quantize` (new, plain C#): the grid values and the start and length formulas.
- `Midi/HitScheduler` (new, plain C#): the notes waiting and sounding, in beats on an
  `IBeatSource`; `Add` from the main thread, `Advance(now, output)` from the clock thread; its own
  lock, after the player's in the lock order; `Panic` and `Release`.
- `Game/Emitter` (new), `Game/EmitterSphere` (new; the pooled sphere's collision callback),
  `Game/EmitterTuning` (new, a section of `AleatoraTuning`), `Game/BoardNotes` (new; gives the
  grid's cells their notes and shows them).
- `View/EmitterPanel` (new): the panel.
- `Game/MidiPlayback`: the scheduler, the free-running beat line, and the thread kept running
  while the emitter has notes waiting or sounding. `Game/Aleatora`: makes the emitter and
  `BoardNotes`.
- `Grid/HexCellView`: a hit's glow, fading, through its existing block.
- `Assets/InputSystem_Actions.inputactions`: *Spray*, *Move Emitter*, *Raise Emitter*, *Aim*,
  *Aim Point*, *Hide Panel* in the `Player` map (Unity regenerates `InputSystem_Actions.cs`).

## EM-1 — The board's notes (branch `em-1-board`, from `main`)

1. `Music/CellNotes`: the share-out, the three layouts, the centre cell and the octaves.
2. `Game/EmitterTuning` on `AleatoraTuning` (added to the asset) with the board's values: set,
   tonic, layout, seed, chromatic weight, K and N intervals, lowest octave, octaves, outward
   lower, show degrees, the label's size and height, the three cell colours.
3. `Game/BoardNotes`: gives each cell its note and its colour on build and rebuild, and when a
   value changes in the Inspector in play; the labels. `HexCellView` takes a rest colour per cell.
4. EditMode tests: the share-out sums to the cells and matches largest remainder; no cells for a
   weight of 0; the same seed the same board, another seed another; clustered regions hold their
   counts and are connected on 5 × 5 × 5 for many seeds; the axes layout at the centre and its
   neighbours; the octaves by ring on 5 × 5 × 5, 3 × 5 × 7 and 1 × 1 × 1, both ways.
   The colours: the root's cells red, the likeliest other degree full blue, a degree of
   probability p at white + (p / p<sub>max</sub>)·(blue − white).
5. Menu check, `CheckBoard`: every cell has a note in the mode and its octave by its ring, and
   its colour; the three layouts, a screenshot each; a rebuild to 3 × 5 × 7 keeps every cell
   noted.

## EM-2 — The emitter (branch `em-2-emitter`, from `em-1-board`)

1. `Game/Emitter` and `Game/EmitterSphere`: the pool, the draws from its `Rng`, spraying, the hits
   (cell, impact speed, sphere size, time), the hit glow. Its parameters on `EmitterTuning`, live.
2. The input actions and the controls of decision 8; nothing while a menu is open.
3. EditMode tests: the draws from a seed (sizes, speeds, directions inside the spread).
4. Menu check, `CheckEmitter`: the bootstrap gave the scene's `Emitter` its component; aimed at
   the centre cell and spraying for two seconds, spheres hit cells and those cells glowed; no more
   than *Max spheres*; under the session menu it stops spraying. No MIDI yet: hits are logged.

## EM-3 — Notes out (branch `em-3-notes`, from `em-2-emitter`)

1. `Music/Quantize` and `Midi/HitScheduler`; velocity and length from the hit.
2. `MidiPlayback`: the scheduler on the clock thread, the beat line (own or followed), *Panic!*
   clears it too.
3. EditMode tests: each grid value (triplets too); strength 0, ½, 1; never before the hit; on a
   grid point it stays; length at least one step; the scheduler with a `RecordingOutput` and
   made-up times: on and off at their beats, the same pitch merged, overlap ended first, the voice
   cap, a stopped followed clock silent, `Advance` allocating nothing.
4. Menu check, `CheckEmitter` goes on with a `RecordingOutput` at 120 BPM, sixteenths, strength 1:
   note ons arrive, each within 5 ms of a sixteenth; every pitch its cell's; velocities in range;
   *Panic!* leaves nothing sounding.

## EM-4 — The panel (branch `em-4-panel`, from `em-3-notes`)

1. `View/EmitterPanel`: the parameters of decisions 1, 5, 6, 10 and 11 (set, tonic, layout, seed,
   rate, spread, speed, sizes, bounciness, lifetime, grid, strength, channel, show degrees), each
   live; *Spray*; **H** hides it. Clicks on the panel don't reach the board.
2. Menu check: the panel's controls each change what they name (the layout a board, the grid the
   note times); a click on the panel selects no cell; a screenshot with the panel and without.

## EM-5 — Designing the controls (ruling 68)

The controls of decision 8 and the panel of EM-4 are scaffolding: they set every parameter but
don't make the emitter feel like an instrument. Jon wants richer, more intuitive real-time
control. With EM-4 in hand to try things on:

1. C2 writes `Docs/Aleatora_Emitter_Controls.md`: what the player does with each hand, on
   mouse and keyboard, gamepad and a MIDI controller, as several whole schemes to choose between,
   each with what it costs to build. Starting points, none chosen:
   - **The nozzle in the hand.** Grab the cone and drag it through space; it lags and swings
     after the pointer with a little inertia, so a flick whips the spray across the board.
   - **Squeeze.** How long or hard a button is held opens the valve: rate and speed rise with
     it (the trigger's pressure on a gamepad, as a real hose's lever).
   - **The pattern dial.** A garden nozzle's ring of patterns (jet, cone, shower, mist, flat
     fan), each a preset of spread, speed, size and rate, turned through with the scroll wheel or
     a shoulder button, with morphing between neighbours.
   - **Knobs and faders.** MIDI learn on the inputs Aleatora already reads (rulings 37 and 38):
     any parameter on any CC, so a control surface plays the emitter with both hands.
   - **Gestures on the board.** Draw a path on the board and the aim sweeps along it, looped to
     the clock's bars; or pin the aim to a cell and let the nozzle orbit it.
   - **What the player sees.** The aim's footprint on the board before spraying, a trajectory
     arc, the parameters drawn at the nozzle rather than on a side panel.
2. Small prototypes where reading isn't enough (a scheme's feel), on a branch of their own, each
   shown to Jon.
3. Jon picks and rules; the chosen scheme is written into this plan as EM-6.

As written (2026-11-16): Jon gave the mouse scheme himself (ruling 77);
`Docs/Aleatora_Emitter_Controls.md` works it out (scheme A) and proposes the gamepad (B) and MIDI
controllers (M), with decisions and costs, no prototypes before EM-6. Ruled (78, 79): A and B
as written in EM-6; M later, for CC streams from Reaper.

## EM-6 — The controls (branch `em-6-controls`, from `em-4-panel`)

Schemes A (mouse and keyboard) and B (gamepad) of `Docs/Aleatora_Emitter_Controls.md`, as
written (rulings 77 and 78). They replace decision 8's controls. Revised after Jon's try (ruling
80): View takes the emitter on the gamepad, whose tilt, triggers and D-pad then work only while
held; the right trigger's pull sweeps the rate; the left trigger (with A) sets *Spread*; R/R3 and
T/Y reset the tilt and the position (Tech, *As built (EM-6)*). Jon tried the revision: it works
well (ruling 81).

1. **Input** (`Player`): *Grab Emitter* (G), *Tilt* (the mouse's movement), *Stick Tilt* (the
   right stick, in place of *Aim Point*), *Wheel* (the mouse's wheel), *Wheel Step* (the D-pad's
   up and down), *Wheel Modifier* (the right button, the left trigger), *Grid Lock* (Shift, the
   left stick pressed, in place of *Aim*); *Raise Emitter* gains LB and RB. The left button is
   *Left Click*, as the cells' pick reads it.
2. **`Game/ConeTilt`** (plain C#): the tilt as two angles, each −90° to 90° (0–180° from the
   horizon, A3), the direction from them, a movement on the board's plane turned into them.
   **`Game/SprayLanding`** (plain C#): where a sphere from the nozzle lands on the board's plane
   under gravity; the axis's landing and a ring of 32 on the spread's edge (A4).
3. **`Game/Emitter`**: taken and released (a click on the cone, G; a menu or the window losing
   focus releases it), the pointer locked while held; the tilt from the mouse while held and
   from the right stick, *Swing*; spraying by the left button while held, Space's latch, the
   trigger; the wheel's steps of *Rate* and, with the modifier, *Spread* (a table of modifier
   and parameter, A6), the D-pad the same; moving along the grid with *Grid Lock* (A9); the
   target: the axis's landing and the outline; the cone tinted *Held Color* while held.
   `AimAt` turns the cone's axis toward a point.
4. **`Grid/GridSelection`**: no pick and no camera orbit or zoom while the emitter is held, and no
   pick from the click that takes it (A1, A7).
5. **`EmitterTuning`**: *Spread* the whole angle, 0–180° (the asset's 10 becomes 20, A5);
   *Tilt Sensitivity* 0.15° a count, *Swing* 0, *Stick Tilt Speed* 120° a second, *Wheel Step*
   1.25, *Spread Step* 5°, *Aim Line Width*, *Held Color*; *Aim Speed* goes. The panel's *Spread*
   0–180°.
6. **Checks.** EditMode: `ConeTilt` (the limits, straight down, the camera's axes),
   `SprayLanding` (straight down, tilted, past the horizon, against small steps under gravity).
   Menu check `em6`: take by a click on the cone and by G, release by G and by Escape (the menu
   opens); no pick and a still camera while held; the pointer locked and freed; tilt by queued
   mouse movement to both limits; the wheel's steps and limits of *Rate*, with the right button
   of *Spread*; the left button sprays while down; the target's centre where spheres land; Grid
   Lock's moves ending on a cell's middle in both orientations; the gamepad's stick, trigger,
   D-pad and shoulders.

## EM-7 — Jon plays it

Jon sprays into Reaper, following its clock: the layouts, the grids and strengths, the
parameters, the controls. He rules on the decisions and the readings, and says what to change.
Merge once he approves.

**Approved and merged into `main`** (ruling 82, 2026-11-16): the readings stand as built; the
plan is finished. Scheme M is tabled until Jon asks.

## Milestone summary

| # | Branch | Lands | Checked by |
|---|---|---|---|
| EM-1 | `em-1-board` | Each cell's degree, octave and colour, three layouts, labels | EditMode tests, menu check |
| EM-2 | `em-2-emitter` | The emitter, spheres, first aim and move, hits that glow | EditMode tests, menu check |
| EM-3 | `em-3-notes` | Hits as quantized MIDI notes on the clock | EditMode tests, menu check |
| EM-4 | `em-4-panel` | The live panel | Menu check |
| EM-5 | (doc, prototypes) | The controls designed, Jon's choice | **Jon** |
| EM-6 | `em-6-controls` | The chosen controls | As EM-5 writes |
| EM-7 | merge | Jon's playing, rulings | **Jon** |

## Risks

- **Too many notes.** A wide spray at a high rate on bouncy spheres is a hail of notes; each
  bounce above *Min speed* is one. *Min speed*, *Max voices*, the merge at a grid point and
  *Bounciness* are the levers; Jon tunes them in EM-7.
- **Physics cost.** Hundreds of spheres on 61 non-convex mesh colliders is light for PhysX, but a
  40 × 40 × 40 board is thousands of colliders, and the 5 ms step (ruling 69) runs the physics
  four times as often as Unity's default. *Max spheres* caps it; measured in EM-2.
- **Fast spheres.** A sphere fast enough to pass through a cell between physics steps makes no
  note; spheres use continuous collision detection.
- **The time a hit lands.** Unity reports a collision in the physics step after it, up to one
  fixed step late: 5 ms, with Jon's fixed step of 0.005 s (ruling 69; Unity's default was 20 ms).
  Below what a player feels at any strength.
- **Two beat lines.** On Aleatora's own clock the emitter's line and the *Melodies* page's player
  aren't lined up; decision 8's pause keeps them from playing together. Following Reaper, both use
  Reaper's.
- **Recording into Reaper** needs Aleatora following Reaper's clock (ruling 39, the default):
  *Send clock* goes with the *Melodies* page's player, not the emitter. Notes played while Reaper
  is stopped (decision 7) are heard through a record-armed, monitored track but not recorded, and
  aren't on Reaper's grid.
- **Reaper sends clock only while its transport runs** (Jon checked, 2026-11-16). So while it is
  stopped the last tempo measured is the one from its last playing; a tempo changed in Reaper
  while stopped reaches Aleatora only when Reaper plays.
