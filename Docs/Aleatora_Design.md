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

## Open, each its own plan later (the plan's decision 7)

- What the grid means musically (the three axes are ready to carry, say, an interval each).
- How the generator drives the grid.
- How notes leave Aleatora as MIDI and OSC.

## Open, from M0's report (2026-11-15)

- The Main Camera in the scene has an AudioListener. It plays nothing and was left; Jon to say
  whether it goes.
- The default window size is 1024 × 768 (the template's is 1920 × 1080); left as found.
- Cells are no longer on the *Hexagons* layer (picking needs no layer); the layer itself is still
  in the project.
- `Assets/Prefabs/Hexagon.prefab`, `Assets/Meshes/Mesh-Hexagon-Collider.asset` and the collider on
  the cell prefab are unused by the new grid and stay until Jon says they can go.
