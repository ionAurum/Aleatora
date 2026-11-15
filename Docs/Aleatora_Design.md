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
    recording output. No hardware port.

## Open, each its own plan later (the plan's decision 7)

- What the grid means musically (the three axes are ready to carry, say, an interval each).
- How the generator drives the grid.
- How notes leave Aleatora as MIDI (planned first, ruling 26; being built) and OSC.

