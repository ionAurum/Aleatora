# Aleatora — the emitter's controls (EM-5)

The emitter plan's EM-5 (`Docs/Aleatora_Emitter_Plan.md`; ruling 68): the controls that make the
emitter an instrument, to replace the first controls of decision 8. Jon set the direction for the
mouse and keyboard himself (ruling 77, below). This doc works his scheme out to where it can be
built, names what it leaves open, and proposes gamepad (B) and MIDI (M) schemes in the same spirit. Each
decision has the default EM-6 builds if Jon doesn't say otherwise. Jon picks which schemes go into
EM-6; the choice is then written into the plan.

Written 2026-11-16 on `em-4-panel`, after EM-4 (ruling 76). **Ruled the same day** (rulings 78
and 79): A and B as written go into EM-6; M is a later milestone, for CC streams from Reaper.

---

## Jon's brief (2026-11-16, ruling 77)

Jon: "I want to give the user direct mouse control over the Emitter."

- The player selects and deselects the emitter for direct control.
- The mouse's X and Y movement tilts the cone, 0–180° on each of two axes.
- Left button down sprays; up stops.
- The wheel sets the rate.
- Right button held and the wheel set the spread, 0–180°; the yellow target on the board matches
  the spread.
- W A S D still move the emitter; Shift held locks the movement to the grid.
- Q and E move it up and down.
- Later, not now: more modifier keys, each giving the wheel another parameter while held; keys
  that change scales, keys (tonic), rhythm patterns and the like.

He will play with all four kinds of hardware (mouse and keyboard, gamepad, a MIDI control
surface, a MIDI keyboard or pads), with his attention on Aleatora alone: full screen, both hands
on it, Reaper only recording.

---

## Scheme A: mouse and keyboard (Jon's)

| Input | Emitter released | Emitter held |
|---|---|---|
| Mouse movement | The pointer, as now | Tilts the cone (A3); no pointer |
| Left button | Picks a cell, as now; on the cone, takes the emitter (A1) | Sprays while down (A8) |
| Wheel | Zooms the camera, as now | Rate (A6) |
| Right button + wheel | (right drag orbits the camera, as now) | Spread (A5, A7) |
| W A S D | Move the emitter over the board, relative to the camera | The same |
| Shift + W A S D | Move along the grid (A9) | The same |
| Q / E | Lower / raise it | The same |
| G | Takes the emitter | Releases it |
| Escape | The menu, as now | Releases it and opens the menu |
| Space | Spray on and off (A8) | The same |
| R / T | Reset the tilt (straight down) / the position (where it began, its height too); ruling 80 | The same |
| H, Tab | Hide Panel, Focus, as now | The same |

### Decisions

**A1. Taking and releasing the emitter.** **Default:** a left click on the cone takes it, and
**G** takes and releases it from anywhere ("grab"), so the cone needn't be found on screen.
Escape releases it as it opens the menu (the menu shell owns Escape; the pointer has to come back
for the menu anyway). Taking it doesn't move it or change its tilt. While it is held the cone
shows it (a highlight; its look on the tuning, Jon's). The cone has no collider (EM-2: the
spheres hit only the cells), so a click is tested against the cone's bounds, not by physics.

**A2. The pointer while the emitter is held.** **Default:** hidden and locked to the window's
middle (`Cursor.lockState`), so the mouse's movement is all tilt, can't run off the screen and
can't wander onto the panel. So the panel can't be clicked while the emitter is held: release it
first. Its values still follow (the wheel's rate shows on the panel's *Rate*).

**A3. The tilt.** Jon's "0–180° on each axis" read as: straight down is 90° on both; each axis
swings the cone from the horizon on one side (0°) through straight down to the horizon on the
other (180°). So the cone can point anywhere in the lower half of the sky and no higher.
- **Default:** the two axes are the camera's, flattened onto the board: moving the mouse right
  swings the spray to the right of the screen, moving it up swings the spray away from the
  camera, whichever way the camera looks. The cone's direction is kept in the world, so orbiting
  the camera (while released) doesn't swing the spray. The alternative is the board's own axes,
  which stay put but feel reversed when the camera is round the other side.
- *Tilt Sensitivity* on the tuning: degrees a mouse count; starting value 0.15° (about 1,200
  counts, a long sweep of a desk mouse, from horizon to horizon). Unity's mouse delta is in
  counts, not pixels, so it doesn't change with the window's size.
- At a limit the tilt stops; pushing further does nothing, and moving back comes away at once.
- **The aim point goes**: today the cone is turned to face a point on the board (the aim point),
  and moving the emitter carries that point (EM-2's reading). From here the tilt is what the
  player sets, and moving the emitter keeps its tilt, so the spray moves with it. Shift's
  pointer-aim and the right stick's aim point go (A9 takes Shift; scheme B the stick).

**A4. The target matches the spray.** Today the yellow marker is a disc of fixed size where the
cone's axis meets the board, which ignores gravity: tilted far, the spheres fall well short of it,
and at the horizon the axis never meets the board at all. **Default: the target is where the
spray lands**: worked out in closed form from the nozzle, the tilt, *Speed* and gravity, for the
cone's axis (the centre) and for a ring of 32 directions on the spread's edge (the outline). It
always lands, at any tilt, and widens and narrows with the spread. *Speed jitter* and the bounces
blur the real spray around it; it shows where the first fall goes. It is drawn as the centre
disc as now plus the outline (a line on the board), in *Aim Color*; the line's width on the
tuning. Spread 180° with the cone tilted to the horizon is a fan up to and over the far side of
the board; the outline is clipped to the board's plane and drawn wherever it lands, on or off the
board.
- The alternative is the straight-line cone's footprint (an ellipse where the cone meets the
  board, ignoring gravity): cheaper, but wrong whenever the spray is tilted and undefined once the
  cone's edge reaches the horizon.

**A5. Spread 0–180°.** Today *Spread* is the half-angle (axis to edge), 0–60°. Jon's 0–180° reads
as the whole angle, edge to edge: 180° is the whole lower half, 0° a single line. **Default:**
*Spread* becomes the whole angle, 0–180°, on the tuning, the panel and the wheel; the asset's 10
becomes 20 so the spray today is unchanged. The draws stay even over the cone's cap (EM-2). The
field keeps its name, so its Inspector value is kept and set by hand.

**A6. The wheel's steps.** **Default:** each notch multiplies or divides the rate by *Wheel
Step* (1.25 to start, on the tuning), between 0.5 and 50 a second: 21 notches from end to end, and
each notch the same felt change whether the spray is sparse or thick. Right button + wheel steps
*Spread* by *Spread Step* (5° to start), 0° to 180°. A notch's step is the same however fast the
wheel turns. Later modifiers each give the wheel another parameter: built as a short table of
modifier → parameter, step and range, so each later one is a line in it (Jon: "eventually").

**A7. The right button and the camera.** Today a right drag orbits the camera and the wheel zooms
it. **Default:** while the emitter is held the camera neither orbits nor zooms; the right button
is only the wheel's modifier, and the mouse goes on tilting the cone while it is down. Released,
the camera is as now. So to move the camera while playing: release (G), move it, take it again.
The alternative: a middle drag orbits the camera while the emitter is held.

**A8. Spraying.** **Default:** while the emitter is held, the left button sprays while it is down,
at *Rate*. **Space and the panel's *Spray* stay** as a latch that sprays held or released, so the
spray can run while the hands do something else; the left button sprays as well while the latch
is off. The alternative is to drop Space and the panel's *Spray*, leaving the left button the only
way. A left click held doesn't pick cells.

**A9. Shift: moving along the grid.** "Locks the movement to match the grid", read as: while
Shift is held the emitter moves along the grid's lines and stops over cells. **Default:** the
direction W A S D give (relative to the camera, as now) is turned to the nearest of the grid's
six directions; the emitter glides along it, over the middles of the cells, at *Move Speed*;
when the keys are let go it carries on to the nearest cell's middle and stops there. Shift
pressed while moving first brings the emitter onto the nearest line. W with D is the diagonal
direction between W's and D's, so all six are within reach. The alternatives: one cell a press
(a step, repeating while held), or only the direction locked, not the stopping place.

**A10. With the emitter released.** **Default:** W A S D, Shift, Q, E and Space work held or
released, as Jon's "still" suggests; only the mouse changes with taking the emitter.

**A11. What the hand feels** (the plan's *nozzle in the hand*). Optional within A: *Swing*, a
time on the tuning by which the cone follows the tilt the mouse sets, so a flick whips the spray
and the cone settles after it; 0 (no lag) to start, so Jon's scheme is as he wrote it until he
tries more. Cheap; worth having to try. **Default:** in, at 0.

### What A costs

The size of EM-2. In `Player`: *Grab Emitter* (G), *Tilt* (the mouse's delta), *Wheel*, *Wheel
Modifier* (the right button), *Grid Lock* (Shift, where *Aim* is now), *Spray Hold* (the left
button); *Aim* and *Aim Point* go. `Emitter`'s controls rewritten round the tilt (the aim point
goes, A3); the target's landing worked out in a new plain C# `Game/SprayLanding`, tested in
EditMode against the physics' own falls; the outline drawn as one `LineRenderer`; `GridSelection`
gives the camera and the left click up while the emitter is held; the pointer locked and freed;
the move along the grid's lines from the layout (both orientations). *Spread*'s meaning and the
asset's value (A5). The menu check grows: take and release (click, G, Escape), tilt by queued
mouse movement to both limits, the wheel's steps and limits, the target against where spheres
land, Shift's moves ending on a cell's middle in both orientations, the camera still while held.

---

## Scheme B: gamepad

It mirrors A where it can. **Revised by ruling 80** (Jon, after trying EM-6): as first written the
gamepad always held the emitter, with no taking or releasing, and the left trigger was the D-pad's
modifier for *Spread*. Now View takes and lets go as G does, and what the mouse does only while
held, the gamepad's tilt, triggers and D-pad do only while held.

| Input | Does |
|---|---|
| View | Takes and lets go of the emitter, as G (ruling 80) |
| Left stick | Moves the emitter, relative to the camera (held or not) |
| Left stick pressed (held) | Along the grid, as Shift (A9) |
| Right stick (emitter held) | Tilts the cone (B1) |
| Right stick pressed | Resets the tilt, straight down, as R |
| Right trigger (emitter held) | Sprays; the pull sweeps the rate from 0.5 a second to *Rate*, on the wheel's geometric curve (`Game/TriggerRate`; ruling 80) |
| Left trigger (emitter held) | Widens *Spread* by how far it is pulled (*Spread Speed*, 90° a second at a full pull) |
| A + left trigger (emitter held) | Narrows *Spread* |
| D-pad up / down (emitter held) | Rate up / down a step |
| Y | Resets the position, where it began, as T |
| LB / RB | Lower / raise the emitter (B2; held or not) |
| Start | The menu (the menu shell's) |

**B1. The right stick tilts.** **Default:** the stick's push is the tilt's speed (*Stick Tilt
Speed*, 120° a second at full push to start), so the cone stays where it is left, as with the
mouse. The alternative ("spring"): the stick's position is the tilt, so letting go brings the
cone back to straight down; quicker to play, but the cone can't be left tilted.

**B2. Raising and lowering.** Decision 8 gave the gamepad none (EM-2's reading, waiting for
EM-5). **Default:** LB lowers, RB raises.

**B3. The camera on the gamepad.** None today (the camera's input is the mouse's). **Default:**
none in EM-6 either; the gamepad plays, the mouse places the camera. The alternative: the
left trigger with the right stick orbits the camera instead of the spread on the D-pad.

### What B costs

Small once A is in: bindings on A's actions, the stick's tilt speed, the trigger as a modifier,
the D-pad's steps. Menu check: the same moves through queued gamepad input.

---

## Scheme M: MIDI control changes (later; ruling 79; tabled until Jon asks, ruling 82)

Not in EM-6. What drives the emitter here is Reaper, not knobs (Jon, M5): track automation, a
plugin's LFO, anything that sends control changes, into Aleatora over loopMIDI, onto the
emitter's parameters. The MIDI inputs Aleatora already opens carry them (rulings 37 and 38; the
*MIDI* page): the "Reaper to Aleatora" port already brings the clock. Today their notes go to
`HeldNotes` and nothing reads their control changes. Its decisions, revised for ruling 79, are
settled when M is planned:

**M1. Learning a CC.** A *Learn* switch at the top of the panel: with it on, click a control on
the panel, then play the automation (or move the plugin's parameter) in Reaper, and the control
follows that CC from then on, its whole range across 0–127. An LFO already running would be
learned first, so learn takes the first CC that wasn't arriving when the control was clicked; a
list of the CCs arriving, to pick from, is the alternative. *Forget* unlearns; the learned CCs
show beside the controls' names. A switch on the panel (*Show degrees*, *Spray*) follows a CC
above or below 64.
**M2. Where the map is kept.** On the tuning asset (*MIDI Map*: the input port's name, channel,
CC, parameter), so it is in the Inspector, in git and in the build; or the framework's settings
file, per machine.
**M3. Following.** A stream is the truth: the control takes each value as it comes, with no
pick-up (pick-up is for knobs, which Jon hasn't). While a CC drives a control, the panel shows it
moving and the mouse or wheel can still set it, until the next value arrives.
**M4. Dense streams.** An LFO sends a CC many times a second; the values are kept per CC and
applied once a frame, the latest winning. 7-bit values: *Spread* moves in steps of 1.4°, *Rate*
of 0.4 a second; 14-bit pairs (CC n with n + 32) if the steps show.
**M5. Ruled** (79): Reaper's CC streams, not hardware knobs.

### What M costs

The size of EM-4. A control-change queue from the input thread to the main thread (as
`HeldNotes` does for notes), the map and learn in plain C# with EditMode tests through
`FeedInput`, the panel's *Learn*, *Forget* and the CC labels, the map on the tuning. Menu check:
learn a fed CC while another streams, follow it, a switch above and below 64.

---

## The plan's other starting points

- **The nozzle in the hand:** A's tilt is the nozzle in the hand without dragging the cone;
  *Swing* (A11) is its inertia.
- **Squeeze:** the right trigger's pressure (B) already is it; on the
  mouse, the wheel's rate stands in.
- **The pattern dial:** a later modifier of the wheel (A6's table), each notch a preset of spread,
  speed, size and rate. Not in EM-6.
- **Gestures on the board** (a drawn path looped to the bars, an orbit round a pinned cell): later,
  their own step.
- **What the player sees:** A4's landing target, before spraying as during it. A trajectory arc
  (the axis's fall drawn in the air) costs little once A4's landing is worked out; not in EM-6
  unless Jon asks.

## Prototypes

None proposed before EM-6. Jon's scheme says what each hand does; what is left to feel
(*Tilt Sensitivity*, the wheel's steps, *Swing*, the stick's speed) is tuning on the asset, which
Jon tries in EM-6 itself. If Jon would rather see A4's two targets side by side before choosing,
that is a small prototype on its own branch.

## Ruled (2026-11-16)

Rulings 78 and 79: A1 to A11 and B1 to B3 as written; EM-6 builds A and B; M is a later
milestone, for Reaper's CC streams (above). The pattern dial, gestures and the trajectory arc:
later.
