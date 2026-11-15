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

## Open, each its own plan later (the plan's decision 7)

- What the grid means musically (the three axes are ready to carry, say, an interval each).
- How the generator drives the grid.
- How notes leave Aleatora as MIDI and OSC.

## Open, from M0's report (2026-11-15)

- The default window size is 1024 × 768 (the template's is 1920 × 1080); left as found.
- **Not yet as ruling 14 says:** the collider scales with the cell and `Gap` moves cells apart,
  so a `Gap` above 0 opens real gaps between colliders. To be built when the emitter and its
  bodies get a plan: the colliders keep tiling at every `Gap` and only the visual shrinks.

## Open, from M2's report (2026-11-15)

- The seed's default is 1; the plan names none.
