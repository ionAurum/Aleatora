# Licences

Aleatora is open source under the MIT licence (`LICENSE`; Jon, 2026-11-17). This lists every
file in the repository that others made, or that comes from others' work, with its licence and
what that licence asks. List a third-party file here before it is committed. Checked 2026-11-17.

## In the repository

| What | From | Licence | It asks | Met by |
|---|---|---|---|---|
| Liberation Sans (`Assets/TextMesh Pro/Fonts/`) | TextMesh Pro's Essentials | SIL OFL 1.1 | The copyright notice and the licence with every copy | `LiberationSans - OFL.txt` sits beside it |
| TextMesh Pro's other essentials (shaders, settings, the font assets) | Unity | Unity Companion License | Use with Unity | Nothing more |
| TextMesh Pro's EmojiOne sprite sheet (`Assets/TextMesh Pro/Sprites/EmojiOne.png`, `Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset`) | TextMesh Pro's Essentials, art by EmojiOne | **Unclear**: see *Open* | | |
| `Leather035D_1K-PNG`, `Plastic010_1K-PNG` (`Assets/Aurumworks/Textures/`) | ambientCG, by their names | CC0 1.0 | Nothing | Nothing needed |
| The melody tables (`Tools/Markov/Source/*.json`, `*.csv`; `Assets/Aurumworks/Resources/Markov/` but `virtuoso_*`) | Counts of scale degrees taken from the folk melodies in music21's corpus: the Essen Folksong Collection, O'Neill's *Music of Ireland* (1850), Ryan's *Mammoth Collection* (1883) (`Tools/Markov/Source/README.md`) | Statistics, not the tunes: no melody is in the repository | Nothing, as far as is known | The source is named in `Tools/Markov/Source/README.md` |
| `com.aurumworks.core` | `../Framework/`, by path | Jon's own, MIT | The copyright notice and the licence with every copy | Not in the repository; its `LICENSE.md` comes with it |
| Unity's packages (URP, Input System, uGUI, Cinemachine) | Unity | Unity Companion License | Use with Unity | Not in the repository (`Packages/manifest.json` names them) |

Everything else (the code, the shader, the meshes, the tools, the docs and the theory tables
`virtuoso_*`) is Aleatora's own, under `LICENSE`.

## Open

- **EmojiOne.** Its attribution names EmojiOne without a version; EmojiOne 2.x was CC BY 4.0,
  later versions were not free. Aleatora shows no emoji. Attune and Cairn leave it out (Attune's
  `Docs/licences.md`); here it is committed, and in the history.
- **The meshes** (`Emitter Cone.fbx`, `Rounded Hexagonal Prism.fbx`) are listed as Aleatora's
  own, but where they came from isn't recorded (they were in the project as found, Tech, *As
  found*). If either came from elsewhere, list it above.
- **The framework is taken by path**, so the repository doesn't open on its own: it needs
  `../Framework/com.aurumworks.core` beside it. Attune takes it by git URL and tag from the
  public `github.com/ionAurum/aurumworks-core` instead.
- No lawyer has read this.
