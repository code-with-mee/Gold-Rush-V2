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

## Phases (VERSION 2: UI first, user decision)
The course now starts with the menu UI, then gameplay and level design, then the gameplay UI.
The old-order guides are kept in `Docs/_old_order/` as source material (reuse their text, tables, values and code).

| # | File | Title | Students build | Ends with "Press Play: you should see…" |
|---|------|-------|----------------|-----------------------------------------|
| 0 | phase-00.html | Create the project | New Universal 3D project, packages (AI Navigation, Input System, Active Input Handling = Both), import the starter package, folder layout. | the kit and the hero model in a test scene |
| 1 | phase-01.html | The main menu | MainMenu scene: 3D stage (kit floor/walls/lamps, hero model with Idle, lantern, camera), Canvas (Canvas Scaler 1920×1080), title logo, tagline, PLAY / LEVELS / SHOP buttons, top-left chips (settings, day/night, gold, stars), MenuAction + GoldRushMenu (phase version if needed). Same look as our menu. | the title screen with a waving hero |
| 2 | phase-02.html | Level select, shop and settings | Levels screen (header, back, Cards box), Shop screen (panel, tabs, grid), Settings dialog (music/sound toggles, close), screen switching, Save data, gold/stars. | click LEVELS / SHOP / settings and back |
| 3 | phase-03.html | Level design | Tour of the prefab kit (thumbnails exist in images/kit/), then build Level1 from kit prefabs (old phase 1+2), markers, lights/sky/fog. PLAY in the menu now loads Level1. | fly around Level1; PLAY opens it |
| 4 | phase-04.html | Player and camera | Hero import settings + Animator Controller by hand, Player prefab (CharacterController, PlayerMotor), third-person camera with mouse look (old phase 3+4). | run around Level1 |
| 5 | phase-05.html | Coins | Coin prefab + script, CoinSpots, pickup sound, GameManager (counts coins; no HUD yet: Console / simple log). | collect coins, Console counts them |
| 6 | phase-06.html | Exit gate, win and lose | Gate opens after the last coin, win check, timer, stars, save, back to menu (no dialogs yet). | finish the level |
| 7 | phase-07.html | Monsters | NavMesh, 3 monster prefabs + controllers, EnemyAI, hearts/lives logic, ITSTEP badge. | monsters chase and hit you |
| 8 | phase-08.html | Power-ups and effects | SPEED / GHOST / MAGNET, particles (FX). | power-ups work |
| 9 | phase-09.html | Gameplay UI | HUD (coins, hearts, timer, day/night, pause), countdown intro, pause dialog, win/lose dialogs, toasts, world markers. Built as GameObjects like the menu (GameUI binds them). | the full game with HUD and dialogs |
| 10 | phase-10.html | Polish and build | Real-time day/night, touch controls, music, Levels 2–4 (our scenes as a package), final scripts, Build Profiles PC + Android. | the complete game = our version |

The final result must look and play like our version (the real game). Use the real values, names and layout.

## Step screenshots (user decision: an image for every step)
Every numbered step that creates an object, adds a component or changes values gets a placeholder that our
screenshot tool fills automatically from a real Unity editor. Use exactly this markup:
`<figure class="shot todo" data-shot="phase-01/step-03" data-window="inspector" data-select="Canvas/Title/PlayButton"><div class="ph">Inspector of PlayButton: Image, Button, MenuAction (action = play)</div><figcaption>Your PlayButton now has 3 components.</figcaption></figure>`
- `data-window` = hierarchy | inspector | scene | game | project | animator | lighting | buildprofiles | console (one per figure; use two figures for two windows).
- `data-select` = the Hierarchy path (or `Assets/...` path for an asset) to select before the picture.
- `data-shot` = unique id phase-XX/step-NN[-a|b].
Keep the caption short and say what the student should check in the picture.

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

## Provided prefabs (user decision)
Students do NOT build 3D objects or materials by hand. The starter package (Phase 0) gives them ready prefabs + materials:
level kit (blocks, walls, pillar, lamp, floor, accent blocks, barrier, terminal), and the VISUAL parts of gameplay objects
(coin model, exit gate model, power-up models). Guides only say "drag prefab X into the scene" and focus on
placing, scripts, components, wiring and Inspector values. Keep at most ONE short "try changing a colour" step.

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
GoldRush_Phase00_Project, Phase01_MainMenu, Phase02_MenuScreens, Phase03_Level, Phase04_Player, Phase05_Coins,
Phase06_Gate, Phase07_Monsters, Phase08_PowerUps, Phase09_GameUI, Phase10_Final (all `GoldRush_` prefix,
`.unitypackage`).

## Reference course (style only)
`/Users/kingly/Desktop/M14/MillionQuiz_Export/Docs/part-2-guide.html`. Copy its tone and structure, not its content.

## VERSION 3 (current, user decision): package-driven phases + hands-on practicals
Phases 0–10 keep their titles and order (UI first). Students no longer build or type each phase by hand:
each phase starts by IMPORTING its checkpoint package; the guide then tells them what to CHECK and UNDERSTAND.
The hands-on building happens in the Practicals (P1, P2 fully step by step with screenshots; P3, P4 challenges).

All packages are built from the finished game with the SAME script files (same GUIDs), so they never clash.
Every package contains: the starter resources (characters, sounds, icons, fonts, UI sprites, kit prefabs, materials)
+ ALL final scripts (Assets/GoldRush/Scripts, no Dev) + Resources assets (GoldRushAssets, DayNightPalette) +
Animator Controllers + the scenes listed below. No editor tools. Students import into a NEW empty project each time
(or delete Assets/GoldRush first), then add the scenes to File → Build Profiles → Scene List (MainMenu first).

| # | Package | Scenes / what is new | Notes for the guide |
|---|---|---|---|
| 0 | Phase00_Project | starter resources only, Sandbox scene | create project, packages, input = Both, import |
| 1 | Phase01_MainMenu | MainMenu (full menu: stage + Title/Levels/Shop/Settings) | focus: stage, Canvas, title screen, LogoTitle, buttons + MenuAction, GoldRushMenu fields |
| 2 | Phase02_MenuScreens | same scenes as phase 1 | focus: Levels / Shop / Settings screens, Save data (Data.cs), shop items, gold |
| 3 | Phase03_Level | + Level1 with ONLY the level (Static, Decor, Scenery, gate) + an overview Main Camera; no GameManager / gameplay | focus: kit prefabs, layout groups, lights/sky/fog, Build Profiles; PLAY from menu opens Level1 |
| 4 | Phase04_Player | Level1 + GameManager + Gameplay/PlayerSpawn (player + camera are made by code at Play) | focus: PlayerSpawn, PlayerMotor, CharacterController, Hero controller, ThirdPersonCamera; HUD appears (made by code; scene UI comes in phase 9) |
| 5 | Phase05_Coins | + Gameplay/Coins (CoinSpots with Coin) | focus: Coin, CoinSpot, GameManager pickup, GoldRushAssets sounds |
| 6 | Phase06_Gate | same as phase 5 | focus: ExitGate script/fields, win/lose rules, stars, save |
| 7 | Phase07_Monsters | + Gameplay/Monsters (MonsterSpawn markers), NavMeshSurface on Static | focus: NavMesh, EnemyAI states, monster controllers, ITSTEP badge, hearts |
| 8 | Phase08_PowerUps | + Gameplay/PowerUps | focus: PowerUp, PowerUpSpot, FX |
| 9 | Phase09_GameUI | + GameUI scene object (HUD, intro, pause/win/lose dialogs) + EventSystem | focus: GameUI fields, GameAction buttons, dialogs |
| 10 | Phase10_Final | + Level2–4 + Game (daily) = the complete game | focus: day/night, touch controls, music, build PC/Android |

Phase page format (short!): 1 What you get (lead + package line) · 2 Import (numbered) · 3 Check (4–8 numbered checks,
each with ONE screenshot placeholder in the SPEC markup, data-select = an object that EXISTS in that package's scene,
e.g. "Level 1 - Training Yard/Gameplay/Coins/CoinSpot 1", "GoldRushMenu/MenuUI/Title", "GameUI", or an Assets/ path) ·
4 How it works (short read-only code tour: the 5–15 key lines of the main script in a code block, bullets) ·
5 ▶ Press Play (what you should see; reuse a game image from images/ if one fits) · 6 Try it (1–3 small edits with the
exact field/line to change and what happens) · Common problems (3–6 rows) · pager.
Screenshots: the code owner captures them from the finished game with the tool, using data-window + data-select.
