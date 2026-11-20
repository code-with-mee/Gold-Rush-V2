# Gold Rush course: shared spec for the guide writers

Read this file first. Every phase guide must follow it, so the 11 guides read as ONE course.

## Goal
Students create an **empty new Unity project** and follow the guides step by step, from phase 0 to phase 10,
until they have the **complete Gold Rush game**. That game is the one in this repo:
`/Users/kingly/Desktop/M14/GoldRushDemo/Assets/GoldRush/`. It is the source of truth: read the real scripts.

Audience: beginner Unity students (they know the Unity editor a little, and basic C#). Plain, short English.
Short sentences. One action per step. No jargon without a one-line explanation.

## Output files (write ONLY your own files)
- `GoldRush_Export/Docs/phase-XX.html` (XX = 00..10), built from `_notes/TEMPLATE.html`.
- `GoldRush_Export/Docs/_notes/phase-XX-notes.md`: what you need from the code owner (see "Code changes" below)
  plus the list of screenshots you used.
- Do **NOT** edit anything under `Assets/`, `ProjectSettings/`, or other phases' files. Do not run Unity.
  Do not touch git.

## Phases
| # | File | Title | Students build | Ends with "Press Play: you should see…" |
|---|------|-------|----------------|-----------------------------------------|
| 0 | phase-00.html | Create the project | Unity Hub → new **Universal 3D** project "GoldRush" (Unity 6000.6.10f1 or newer). Check packages: **AI Navigation**, **Input System** (Player Settings → Active Input Handling = Both). Import the Resources folder. Create the folder layout. | the Project window has the folders and the hero model shows in a scene |
| 1 | phase-01.html | Materials and prefabs | URP Lit materials (floor grid, block, wall, accent colours, gold, gate). Block, wall, pillar, lamp prefabs (pivot at the bottom). Prefab vs instance, prefab mode. | a test scene with a few prefabs |
| 2 | phase-02.html | Level design | Level1 scene: floor, outer walls, blocks, lamps, directional light, sky, fog. Grid snapping. Empty "markers" for spawn, coins, monsters, gate. | fly around the level in the Scene view |
| 3 | phase-03.html | The player | Hero model import settings (Rig = Generic, loop clips). Build the **Animator Controller by hand** (blend tree Idle/Walk/Run on `Speed`, triggers Attack/Hit/Win/Die). Player prefab: CharacterController + PlayerMotor. Keyboard movement + sprint. | the hero runs around with the right animation |
| 4 | phase-04.html | The camera | ThirdPersonCamera: follow, mouse look, hidden/locked cursor (Esc frees it), smoothing, no clipping into walls. | smooth third-person view |
| 5 | phase-05.html | Coins and the HUD | Coin prefab (spin, trigger), CoinSpot markers, pickup sound, GameManager (coin count), HUD (coin counter). | collect coins, counter goes up |
| 6 | phase-06.html | Exit gate, win and lose | ExitGate opens when all gold is collected, timer, stars, win/lose dialogs, restart. | finish the level and see the win dialog |
| 7 | phase-07.html | Monsters | Bake NavMesh (NavMeshSurface), 3 monster prefabs (Slime = Patroller, Ghost = Chaser, MushroomKing = Guard) with their own Animator Controllers, EnemyAI state machine, MonsterSpawn markers, ITSTEP badge, hearts. | monsters patrol, chase, and can hit you |
| 8 | phase-08.html | Power-ups and effects | SPEED / GHOST / MAGNET power-ups, PowerUpSpot markers, particles (FX), world markers / arrow to the gate. | pick up a power-up and see it work |
| 9 | phase-09.html | Menu, levels, shop, save | MainMenu scene, Level Select (4 levels + Daily), Shop (colours, hats), save (gold, stars, unlocks), Levels 2–4, Build Profiles scene list. | the full game loop from the menu |
| 10 | phase-10.html | Polish and build | Real-time day/night, touch controls (left half = joystick, right half = look), music, final checks, build for PC/Android. | the complete game |

Match the phase boundaries to how the real code is split. If a script obviously belongs in another phase, say so in
your notes, but keep the table above as the plan.

## Folder layout in the student project (same as this repo)
```
Assets/GoldRush/
  Art/Characters/  Hero/, Monsters/, Licenses/   (from Resources/Characters)
  Art/Audio/       *.wav                         (from Resources/Audio)
  Art/UI/          Icons/, Fonts/, shapes *.png  (from Resources/UI)
  Art/Textures/    grid, badge                   (from Resources/Textures)
  Animations/      Hero.controller, Slime.controller, ...
  Materials/       M_Floor, M_Block, M_Wall, M_Gold, ...
  Prefabs/         Level/ (blocks), Gameplay/ (Player, Coin, Gate, monsters, power-ups)
  Scenes/          MainMenu, Level1..Level4
  Scripts/         Core/, Audio/, World/, Level/, Gameplay/, UI/   (same folders as the repo)
```

## Resources pack (what students get, GoldRush_Export/Resources/)
- `Characters/` Hero.fbx, Slime.fbx, Ghost.fbx, MushroomKing.fbx (Quaternius, CC0; animations inside the FBX), licences.
- `Audio/` coin, step, alert, hit, win, lose, power, click, gate, star, buy, error, count, go, whoosh, music (.wav).
  (These are exported from `Scripts/Audio/Synth.cs`. Assume the code will load them as AudioClips.)
- `UI/` Material Icons PNGs (the ones in `Assets/GoldRush/Resources/Icons/`), Poppins fonts, panel/button shapes.
- `Textures/` floor grid, ITSTEP badge.
- `Scripts/Helpers/` the BIG helper scripts students import instead of typing (see next section).

## Type it vs import it
- Gameplay scripts the student should understand: show the **full code** in a `<pre class="long"><code>` block
  (HTML-escaped!), copied from the real file, then 3–6 bullets explaining the key lines (like MillionQuiz).
- Rule of thumb: **paste if the file is about 250 lines or less**. Larger files (`UIKit.cs`, `Sprites.cs`,
  `GameUI.cs`, `Synth.cs`, `LevelBuilder.cs`, `Props.cs`, `GoldRushMenu.cs`, `FX.cs`, ...) are **imported** from
  `Resources/Scripts/Helpers/`. The guide explains what they do and the 2–3 functions the student calls.
- **Compile rule:** after every phase, the project must compile and play. If a real script uses a class
  from a LATER phase (for example PlayerMotor reading GameManager), give a short **phase version** of the script now,
  and in the later phase say "Replace the whole file with this updated version" (show the full new file).
  Make every phase version as close to the real file as possible. List each phase version in your notes.
- Do not include `Scripts/Dev/SmokeTester.cs` or the `Editor/` tools. Students do by hand what
  `Editor/GoldRushSetup.cs`, `CharacterSetup.cs` and `LevelBaker.cs` do automatically. Read those files to learn
  the exact settings (blend tree thresholds, clip names, material colours, heights, loop flags).

## Code changes
You must NOT change code. If the real code can't support a by-hand step (for example the GameAssets
ScriptableObject has no `[CreateAssetMenu]`, or a script builds something at runtime that students should place
in the scene), write it in `_notes/phase-XX-notes.md` under `## Code changes needed`: file, what to change, why.
The code owner will apply these. Write your guide as if the change is already done.

## Page format
- Start from `_notes/TEMPLATE.html`. Keep the header and sidebar exactly as they are (all 11 phases listed).
  Set `aria-current="page"` on your own phase link. Fill in the eyebrow, h1, lead, meta, toc and steps.
- Use the classes from `assets/style.css` (copied from the MillionQuiz course): `.step`, `.lead`, `.meta`, `.toc`,
  `.note`, `.warn`, `.tip`, `.ok`, `table`, `pre.long`, `figure`, `.shots`. Open `assets/style.css` to see the
  callout class names that actually exist and use only those.
- Each phase: **8–20 numbered steps** (`<h2 id="step-N"><span class="step">N</span>Title</h2>`).
  Inside a step, use `<ol>` for the clicks. Bold the exact Unity menu names: <b>GameObject → 3D Object → Cube</b>.
- Always include, in this order: "What you will build" (lead), "What you need" (previous phase + package name),
  the steps, a **"▶ Press Play"** check step, a **"Common problems"** table (Problem | Fix), and a
  "Checkpoint package" line: `GoldRush_PhaseXX_<Name>.unitypackage` (names below).
- Screenshots: we will capture them later. Put a placeholder where one helps:
  `<figure class="shot todo"><div class="ph">Screenshot: Inspector of Player with CharacterController</div><figcaption>…</figcaption></figure>`
  and list every placeholder in your notes.
- Values (positions, speeds, colours) must be the REAL values from the code, so the result matches the game.
- Footer: "Previous phase" / "Next phase" links.

## Checkpoint package names
GoldRush_Phase00_Project, Phase01_Prefabs, Phase02_Level, Phase03_Player, Phase04_Camera, Phase05_Coins,
Phase06_Gate, Phase07_Monsters, Phase08_PowerUps, Phase09_Menu, Phase10_Final (all `GoldRush_` prefix,
`.unitypackage`).

## Reference course (style only)
`/Users/kingly/Desktop/M14/MillionQuiz_Export/Docs/part-2-guide.html`. Copy its tone and structure, not its content.
