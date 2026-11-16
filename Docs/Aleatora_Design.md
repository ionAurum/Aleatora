# Aleatora: design

What Aleatora is and what Jon has ruled. The plan and its milestones are in
`Aleatora_Markov_Plan.md`; how things are built is in `Aleatora_Tech.md`.

## Decided by Jon

1. **What Aleatora is** (2026-11-15). A MIDI and OSC controller and composition tool. It makes no
   sound of its own: no synthesiser, no audio output, no mixer. What it composes leaves as MIDI
   and OSC.
2. **Bring-up** (2026-11-15; the plan's decision 1, its default). The template's menus, theme,
   menu check and conventions, without its mixer, volume sliders or UI sounds.
3. **The grid's orientation** (2026-11-15; decision 2, its default). Flat-topped hexagons with the
   k axis running north–south. Pointy-topped, with the k axis horizontal, is built too and
   switchable on the tuning asset.
4. **The grid's starting dimensions** (2026-11-15; decision 3). 5 × 5 × 5: 61 cells, a regular
   hexagon. On the tuning asset.
5. **Where the Melodies view lives** (2026-11-15; decision 4, its default). A page opened through
   the menu stack, from a Melodies choice on the main menu and a button on the session menu.
6. **Which sets ship** (2026-11-15; decision 5). All 17.
7. **Which octave a degree is played in** (2026-11-15; decision 6, its default). Nearest to the
   note before, inside a range.
8. **The look is Jon's** (2026-11-15, the plan's *How we work*). Everything visual is on the
   tuning asset, read live; nothing visual is a constant in code.

9. **Serialized assets** (2026-11-15). Claude edits scenes, prefabs and other serialized assets
   as the work needs, without asking first. Packages and `ProjectSettings/` still wait for Jon.
10. **Our assets live under `Assets/Aurumworks/`** (2026-11-15). Jon moved `Materials`, `Meshes`,
    `Physics Materials`, `Prefabs` and `Textures` there.
11. **No AudioListener** (2026-11-15). Removed from the Main Camera.
12. **Cells are off the *Hexagons* layer** (2026-11-15). The layer was for picking, which no
    longer needs it.
13. **`Hexagon.prefab` is deleted** (2026-11-15).
14. **The cell prefab keeps its collider on `Mesh-Hexagon-Collider.asset`** (2026-11-15). Jon's
    reason: bodies collide with a simple mesh, cheaply, and don't catch in the visual gap between
    cells or bounce off their rounded edges. **The collider stays full-size while only the
    visual shrinks.** The idea behind it, from Jon's prototyping: the emitter cone spawns
    rigidbodies, and one striking a cell triggers a note or the like. Not in any milestone yet.
15. **Unity's audio is disabled** (2026-11-15). Project Settings > Audio > Disable Unity Audio.
16. **The *Hexagons* layer is removed** (2026-11-15). Layer 10 is unnamed again.
17. **The cells are gel pads** (2026-11-15). Soft, frosted silicone that glows from inside, after
    the Intuitive Instruments Exquis: the `Aurumworks/GelHexPad` shader, on
    `Rounded Hexagonal Prism.prefab` through `Gel Hex Pad.mat`. A cell's state colour is the colour
    it glows (Tech, *Cell look*).
18. **The *Melodies* page has a transition graph beside the transition grid** (2026-11-15). The
    order-1 table drawn a second way: a directed graph whose twelve nodes are the degrees, labelled
    with their names, on a chromatic circle (1 at the top, b2, 2, … 7 clockwise; in-mode nodes in
    accent, the rest muted). An arc runs from a degree to each degree that follows it, labelled
    with its weight, the row's share written as `.42`. Only arcs whose share is at least a minimum
    on the tuning asset are drawn (default 5%); a repeated note is a small loop on its node.
19. **The *Melodies* page as built in M3 is accepted** (2026-11-15). Jon: "it works as expected."
    The layout (the graph full height at the right, the bars and grid stacked at its left), the
    arcs bowing inward, the bars scaled to the tallest, a melody made on opening, the opaque
    backdrop, and `b3`/`#4` in the default font all stand.
20. **The voicing may leap up to an octave** (2026-11-15). Any leap up to and including an octave,
    up or down, is allowed; it replaces the plan's "no leap larger than a tritone". Ruling 7
    stands: nearest the note before, an octave inward at the range's edges (leaps of 7 to 11 there).
21. **The seed's default is 2112** (2026-11-15), a nod to Rush. On `MelodyTuning` and the tuning
    asset.
22. **`Gap` is never above 0, and the Inspector hides it** (2026-11-15). The cells touch, so the
    colliders tile as ruling 14 wants; the visual gap between pads is in the cell's mesh. This
    settles M0's open item on parting colliders.
23. **The default window is 1920 × 1080** (2026-11-15; Jon wrote "1920x180", read as 1080, and
    confirmed 1080 the same day). It applies when the player runs windowed; it starts in
    Fullscreen Window.
24. **Dithering is on for the Main Camera** (2026-11-15), set by Jon, for the gel glow's gradients.
25. **`m3-view` merges to `main`** (2026-11-15), ending M4.
26. **MIDI comes first of decision 7's plans** (2026-11-15). Its first proof: decide on and
    integrate a MIDI library, or build our own; a MIDI device selection screen (Jon uses loopMIDI
    between Aleatora and Reaper); a MIDI channel button, a BPM box and the usual playback
    controls, loop included, on the *Melodies* page; note on and off on that channel, driven by
    the generated melody, as quarter notes at the BPM. One direction only; MIDI in comes later.
    The plan: `Docs/Aleatora_Midi_Plan.md`.
27. **Our own MIDI library** (2026-11-15; the MIDI plan's decision 1, its default). A small wrapper
    over Windows' `winmm.dll`, behind `IMidiOutput`. Jon: Aleatora is Windows-only.
28. **Microsoft GS Wavetable Synth is hidden from the port list** (2026-11-15; MIDI decision 3, its
    default). Jon: Aleatora is for serious composers and producers.
29. **The channel is a dropdown, 1 to 16, for now** (2026-11-15; MIDI decision 4). Jon hadn't
    specified the control clearly. Later something richer: checkboxes, perhaps purpose-specific
    ones (several channels at once, each for a purpose).
30. **Loop repeats the melody as it stands; and an *Endless* mode** (2026-11-15; MIDI decision 7).
    *Endless* keeps stepping the generator, playing each new note as it is made. Jon: "a cool
    idea".
31. **Run In Background is on** in the player (2026-11-15; MIDI decision 10, its default).
32. **A classic *Panic!* button** (2026-11-15): note off for all 128 MIDI notes.
33. **Virtual or mock MIDI ports are fine for Claude's tests** (2026-11-15): a loopMIDI port, or a
    recording output. No hardware port. Changed by ruling 42: the one test that sends and receives uses only
    the loopMIDI port "Aleatora Test".
34. **MIDI out is proved** (2026-11-15, MIDI-4). Jon played Aleatora into Reaper through a
    loopMIDI port ("Greedo"); Reaper received the note ons and offs.
35. **The MIDI work as built is accepted** (2026-11-15, MIDI-4). Jon: "I accept everything you
    suggested & built." So the MIDI plan's decisions 2, 5, 6, 8, 9 and 11 stand as their defaults,
    and decision 12 as built; and the readings flagged in MIDI-2 and MIDI-3 (Tech, *MIDI*) stand:
    - *Panic!* sends note off on all 16 channels (2,048), doesn't stop playback, and is a plain
      button, not the danger colour.
    - *Pause*, then *Play*: the next note at once. A *BPM* change while playing: the next note a
      beat at the new tempo after the last note went on (at once if that's past). Choosing another
      port while playing plays on, on the new port.
    - The transport row as laid out, the panels above made shorter for it, and *Output* showing
      only the port's name, cut with "…".
    - The playhead framed 3 px in the theme's text colour; the newest note keeps its thinner frame.
    - The *MIDI* page closes and opens again after a choice or *Refresh*; a saved port that won't
      open reads "(not open)", one that's gone "(not found)".
    - Loop, Endless, Channel and BPM last for the run, not saved.
36. **The MIDI branches merge to `main`** (2026-11-15): `midi-1-port`, `midi-2-player` and
    `midi-3-page`, through `midi-3-page`, ending the MIDI plan's MIDI-4.
37. **MIDI in, and clock and sync with Reaper, come next** (2026-11-15). The technical base
    layers first; what the incoming data means comes later. Jon plans a musical keyboard for
    entering chords and the like, for a use he hasn't settled. The plan:
    `Docs/Aleatora_MidiIn_Plan.md`.
38. **Several MIDI inputs open at once** (2026-11-15; MIDI in decision 2, as written). The
    keyboard and Reaper's clock come in on different ports. Aleatora doesn't offer its own
    output's loopMIDI port as an input, so it can't hear itself. Jon: "That's fine."
39. **Reaper leads by default** (2026-11-15; MIDI in decisions 3 and 4, as written). Reaper's
    Start, Stop and Continue drive Aleatora; Aleatora's note *n* lands on Reaper's quarter note
    *n* from the project's start (note 1 on bar 1, beat 1, through SPP); the *BPM* box shows
    Reaper's tempo and can't be edited while following. Jon: fine, since Aleatora will be taught
    Reaper's OSC soon.
40. **Aleatora can lead too: *Send clock*, off by default** (2026-11-15; MIDI in decision 5, as
    written). How tightly Reaper follows MIDI clock, and the Reaper menu path for its sync
    setting, are checked in IN-4. Jon: "exactly what we're after."
41. **A *Monitor* page** (2026-11-15; MIDI in decision 8, as written): the last 16 messages, the
    held notes by name, the clock's state. Jon: Aleatora is a sequencer, a looper and the like,
    not a MIDI recording tool; recording is Reaper's job.
42. **The loopback test port "Aleatora Test"** (2026-11-15; MIDI in decision 9, as written; this
    changes ruling 33). The one test that sends and receives does so only through a loopMIDI port
    named "Aleatora Test", which Jon makes; no test sends to his other loopMIDI ports. Without
    the port, the test is skipped.
43. **Go-ahead for the MIDI in plan's IN-1** (2026-11-15). Decisions 1, 6, 7 and 10, not ruled
    on, are built as their defaults; Jon rules on them in IN-4 with the rest.
44. **IN-1's readings where the plan was silent are accepted** (2026-11-15; Tech, *As built
    (IN-1)*): All Notes Off (CC 123) lets go of every key on its channel, the sustain pedal still
    holding them, and All Sound Off (CC 120) clears everything; choosing an output that is open as
    an input closes that input; note names use ♯ (`C♯4`); a message reads "Note on E4 96", with
    the channel in front in logs.
45. **MIDI in and clock are proved** (2026-11-15, IN-4). Jon followed Reaper's clock, led Reaper
    with *Send clock*, and played a keyboard into the *Monitor*: "it all seems to work as
    expected." Then the keyboard's port held by Reaper and Aleatora at once (MIDI in plan,
    *Risks*): "it works great". The loopMIDI ports are renamed (he asked): "Aleatora to Reaper" (was
    Greedo), "Reaper to Aleatora" (was Han) and "Aleatora Test".
46. **The MIDI in work as built is accepted** (2026-11-15, IN-4). MIDI in decisions 1 (our winmm
    wrapper), 6 (the tempo over one beat), 7 (the keyboard's base layer) and 10 (nothing new on
    the *Melodies* page) stand as their defaults. The readings flagged in IN-2 and IN-3 (Tech,
    *As built (IN-2)* and *As built (IN-3)*, *Readings*) stand, among them: while following,
    *Generate* and *Step* keep note *n* on beat *n*, so a new melody is heard from the next
    beat's note; *Pause* while following is *Stop* that keeps the playhead; the monitor's beat
    counts from 1; the output's own port is greyed by its label only.
47. **The MIDI in branches merge to `main`** (2026-11-15): `in-1-input`, `in-2-clock` and
    `in-3-page`, through `in-3-page`, ending the MIDI in plan.
48. **Aleatora will be sold** (2026-11-15). No data under a non-commercial licence goes into it,
    so the CantusCorpus and SymbTr (both CC BY-NC-SA) are out.
49. **The Hindu pentatonic is 1 3 4 5 ♭7** (2026-11-15).
50. **New melody tables from theory, in the style of rock guitar virtuosos** (2026-11-15). Lydian,
    Phrygian, Byzantine (double harmonic) and the Hindu pentatonic, inspired by Joe Satriani, Pete
    Thorn, Kiko Loureiro's solo work, Andy Timmons and John Petrucci. No fetch, analyse and learn
    pipeline: Jon: "generate the 1st through 4th order Markov chains based on standard Western
    music theory". The plan: `Docs/Aleatora_Theory_Plan.md`.
51. **The theory plan's decisions stand as their defaults; go-ahead for TH-1** (2026-11-15). Jon:
    "defaults are fine." So: his four modes only (`lydian`, `phrygian`, `double_harmonic`,
    `hindu_pentatonic`); the corpus `virtuoso`, the sets `virtuoso_<mode>`, 21 in all; the figures'
    weights as written (runs 30%, sequences 25%, arpeggios 15%, pedals 15%, turns 10%, leaps 5%;
    the colour note's pull followed 70%) in `Tools/Markov/Theory/virtuoso.json`; chromatic passing
    notes in about 3% of notes, inside runs only; 20,000 phrases a mode; Python's seeded
    `random.Random` in the offline script.
52. **TH-1 merges to `main` before TH-2's listening** (2026-11-15). Jon: "run the tests, commit,
    then merge." The tests (291) and the menu check (186) passed first. TH-2's tuning of
    `virtuoso.json` happens after the merge.
53. **Every Virtuoso phrase starts on the root** (2026-11-16, TH-2). Jon: "change the generator
    such that all melodies start on the scale root." In `virtuoso.py`, not in the game's
    `MelodyGenerator`: the folk sets keep their own first notes, and with *Smoothing* above 0 a
    Virtuoso melody's first note can still be other than 1 (Jon chose this over making the game's
    first note always 1).
54. **The double harmonic is shown as "Byzantine"** (2026-11-16, TH-2): "Virtuoso · Byzantine" on
    the *Melodies* page. The set keeps its name on disk, `virtuoso_double_harmonic`.
55. **`th-2-tuning` merges to `main`** (2026-11-16). Jon: "Merge `th-2-tuning`." On the EditMode
    tests (291 of 291); the menu check was not run for it.
56. **The Virtuoso sets work as expected and sound good** (2026-11-16, TH-2). Jon: "The Virtuoso
    sets work as expected and sound good."
57. **A fifth theory set: the phrygian dominant, 1 ♭2 3 4 5 ♭6 ♭7** (2026-11-16). Jon: "I did
    forget one scale I wanted, the phrygian dominant." Its pulls are ♭2 → 1 and ♭6 → 5, as the
    double harmonic's without the 7 (Jon chose them). `virtuoso_phrygian_dominant`, shown as
    "Virtuoso · phrygian dominant"; 22 sets in all.
58. **The phrygian dominant is approved; `th-2-phrygian-dominant` merges to `main`** (2026-11-16).
    Jon: "That's right on point; merge." This finishes TH-2 and the theory plan.
59. **The emitter is the next plan** (2026-11-16). Jon's original idea (ruling 14): the emitter
    cone sprays Rigidbody spheres, with parameters like Unity's ParticleSystem's but controlled
    live in play; the player moves and aims it, vaguely like a garden hose nozzle; a sphere
    striking a cell makes the cell react musically, at the simplest by playing a note of the
    chosen set; the cells' notes come from the set's distribution (*The set* on the *Melodies*
    page). Jon re-enabled the cone in `SampleScene`. The plan: `Docs/Aleatora_Emitter_Plan.md`.
60. **The board's notes come from the unigram only, for now** (2026-11-16), not the Markov
    orders.
61. **Three layouts of degrees on the board, chosen by the player** (2026-11-16): scattered
    (seeded, from the distribution), clustered (regions with character), and along the k × m × n
    axes (intervals). Jon: "perfect for the first iteration."
62. **What a hit carries** (2026-11-16): velocity from the impact speed, length from the sphere's
    size, the octave from the cell's radial distance from the centre cell.
63. **The same seed gives the same board; sessions needn't replay** (2026-11-16). Jon: this tool
    is "about generating aleatoric/stochastic music." The emitter's own draws still come from
    `Music/Rng`; the physics and the player's input aren't reproduced.
64. **Hits are quantized to the MIDI clock** (2026-11-16): the note's start and its length, to the
    nearest chosen note value (quarter, eighth, eighth triplet and so on), as a DAW's or a
    sequencer's quantize, with a strength (the "humanizer" knob).
65. **The cells are coloured by their degree** (2026-11-16): the root red; the others between
    white and blue by their probability, full white at 0 and full blue at 1. Changed by ruling 71:
    scaled to the most likely.
66. **Quantizing moves a note forward, to the next grid point** (2026-11-16; the emitter plan's
    decision 6, its default). Jon understands the latency "nearest" would cost.
67. **The scattered layout shares the cells out, and the axes layout is as written**
    (2026-11-16; the emitter plan's decision 2 and layout 3, their defaults): largest remainder,
    then a seeded shuffle; +K and +N a third each, +M a fifth.
68. **A step to design the emitter's real-time controls, between the panel and Jon's playing**
    (2026-11-16). Jon: "I'm looking for richer, more intuitive user interaction than what you've
    suggested." The emitter plan's EM-5 (design) and EM-6 (build); Jon plays it in EM-7.
69. **The fixed timestep is 0.005 s** (2026-11-16), set by Jon in Project Settings > Time, for the
    emitter's collisions (200 Hz; Unity's default was 0.02).
70. **While a followed clock is stopped, the emitter plays on Aleatora's own beat line at the
    last tempo measured** (2026-11-16; the emitter plan's decision 7, its revised default). So the
    board can be played into a record-armed, monitored track while Reaper is stopped; when Reaper
    plays, the notes move to its grid. Jon checked: Reaper sends clock only while its transport
    runs. Jon: "addressed it the way I wanted."
71. **The cells' blue is scaled to the most likely degree** (2026-11-16; changes ruling 65): the
    root red, the likeliest other degree full blue, the rest p / p<sub>max</sub> of the way from
    white. Jon: "lock that in."
72. **The emitter plan's other decisions stand as their defaults; go-ahead for EM-1**
    (2026-11-16). Jon: "Defaults are fine." So decisions 1, 3, 4, 5, 8, 9, 10 and 11 as written.
73. **The cells' labels are near-black; go-ahead for EM-2** (2026-11-16; changes decision 10's
    label colour). Jon looked at EM-1: the white labels are too washed out on the pale cells, so
    *Label Color From Theme* is off and the labels take *Label Color* (near-black, 0.1 grey).
74. **EM-2 works well enough for now; go-ahead for EM-3** (2026-11-16). Jon: "It works well
    enough for now. I need to think on the user interaction." EM-2's open readings (moving carries
    the aim point, no raise on the gamepad, *Spray* as two actions) wait for EM-5.
75. **EM-3 works; go-ahead for EM-4** (2026-11-16). Jon: "EM-3 work. Go ahead with EM-4." The
    hits' notes reach Reaper; EM-3's readings stand.
76. **EM-4 works well; go-ahead for EM-5** (2026-11-16). Jon: "I've tried EM-4 and it works
    well. Start EM-5." EM-4's readings stand (the panel writes the tuning; hidden under a menu; H;
    no orbit or zoom over it; the extra controls on it).
77. **The emitter's controls: direct mouse control** (2026-11-16; EM-5's brief). Jon: "I want to
    give the user direct mouse control over the Emitter." The player selects and deselects the
    emitter; the mouse's X and Y tilt the cone 0–180° on each of two axes; left button down sprays,
    up stops; the wheel sets the rate; right button with the wheel sets the spread, 0–180°, and the
    yellow target matches it; W A S D still move it, Shift held locks the movement to the grid; Q
    and E move it up and down. Later, not now: more modifiers for the wheel, and keys for scales,
    keys, rhythm patterns and the like. He will play with mouse and keyboard, gamepad, a MIDI
    control surface and a MIDI keyboard or pads, with his attention on Aleatora alone. Worked out
    in `Docs/Aleatora_Emitter_Controls.md`, its open points with defaults.
78. **The controls: schemes A and B in EM-6, as written; M later** (2026-11-16). Jon: "Your
    planning decision for building A plus B now then M later down the road is accepted. I also
    accept your A 1-9 defaults", and A10, A11 and B1 to B3 "all as written". So EM-6 builds the
    mouse and keyboard (A) and the gamepad (B) as `Docs/Aleatora_Emitter_Controls.md` has them;
    the MIDI controls (M) are a later milestone.
79. **The MIDI controls are for CC streams from Reaper** (2026-11-16; the controls doc's M5). Jon:
    "I don't have many hardware MIDI controllers with knobs, really. I wanted to allow Reaper stuff
    (track automation streams, plugins' LFOs, and such) that produce CC messages to be mappable to
    Aleatora Emitter parameters." Scheme M is written for that (the controls doc, *Scheme M*).
80. **EM-6 tried: the gamepad takes the emitter, its triggers are analog, both schemes reset**
    (2026-11-16). Jon, after trying EM-6: the gamepad needs an equivalent of G; a spray-angle
    control, "left trigger to widen and A + left trigger to narrow"; the right trigger analog, "the
    more trigger pull distance controls spray rate"; both schemes need "reset rotation" and "reset
    position". Picked from C2's options the same day: **View** takes and lets go, and the gamepad's
    tilt, triggers and D-pad work only while the emitter is held (the left stick, LB and RB either
    way, as W A S D and Q/E); the right trigger sweeps from 0.5 a second at a light pull to *Rate*
    at a full pull, on the wheel's geometric curve (the D-pad still sets *Rate*, the ceiling);
    **R** and **R3** point the cone straight down, **T** and **Y** put it back where it began,
    its height too. Changes B and B3's table in the controls doc (the left trigger no longer the
    D-pad's modifier).

## Open, each its own plan later (the plan's decision 7)

- What the grid means musically (the three axes are ready to carry, say, an interval each).
  Begun by the emitter plan (rulings 59 to 64).
- How the generator drives the grid.
- How notes leave Aleatora as OSC. MIDI out is built (rulings 26 to 36), and MIDI in with clock
  and sync (rulings 37 to 47). Jon means to teach Aleatora Reaper's OSC (ruling 39).
- What the keyboard's notes do (ruling 37: chords and the like, later).
- Rhythm: every note is still a quarter note (the MIDI plan's *Later*).
- More modes for the Markov tables: done, five from theory (rulings 48 to 58,
  `Docs/Aleatora_Theory_Plan.md`, finished).

