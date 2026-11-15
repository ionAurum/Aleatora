# Aleatora

A MIDI and OSC controller and composition tool, built in Unity (6000.0.85f2, URP). It makes no
sound of its own: what it composes leaves as MIDI and OSC. Its board is a hexagonal grid on three
dimensions, and its melodies are drawn from Markov tables of about 10,000 folk melodies.

- `CLAUDE.md`: how the project is laid out and worked on.
- `Docs/Aleatora_Handoff.md`: where things stand and what comes next.
- `Docs/Aleatora_Design.md`: what Aleatora is, and Jon's rulings.
- `Docs/Aleatora_Tech.md`: how each system is built.
- `Docs/Aleatora_Markov_Plan.md`: the plan and its milestones.

It takes `com.aurumworks.core` by path from `../Framework/`, so it needs that folder beside it.
`bash Tools/menucheck.sh <name>` builds the Windows player and runs its self-test.
