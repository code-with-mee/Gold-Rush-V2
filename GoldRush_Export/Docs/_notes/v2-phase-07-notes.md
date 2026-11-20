# Phase 7 – Monsters (v2, UI later)

Guide: `phase-07.html` (15 steps). NavMeshSurface on `Static` + Bake, check monster FBX import, 3 controllers by duplicating Hero.controller
(clip table from Editor/CharacterSetup.cs), fill GoldRushAssets monsters, import EnemyAI + helpers, type MonsterSpawn + Badge, replace GameManager (phase 7),
build Monster_Slime/Ghost/MushroomKing prefabs (MonsterSpawn + EnemyAI + NavMeshAgent, no model child), replace the Slime markers.
Press Play: patrol/chase/search, badge, hits, lose after 2 hits. Old order reused: phase-07 tables (states, types, clips, agent values).

## Screenshot placeholders in this page (SPEC markup, all `figure.shot.todo`)
| data-shot | window | data-select | placeholder |
|---|---|---|---|
| `phase-07/step-03-a` | inspector | `Level 1 - Training Yard/Static` | Inspector of Static: NavMesh Surface (Agent Type Humanoid, Collect Objects Current Object Hierarchy, Use Geometry Physics Colliders) |
| `phase-07/step-03-b` | scene | `Level 1 - Training Yard/Static` | Scene view of Level1 from above with the blue NavMesh and holes around walls and blocks |
| `phase-07/step-04` | inspector | `Assets/GoldRush/Art/Characters/Monsters/Slime.fbx` | Slime.fbx Import Settings, Animation tab: the Walk clip with Loop Time and Loop Pose ticked |
| `phase-07/step-05-a` | project | `Assets/GoldRush/Animations/Slime.controller` | Project window: Animations with Hero, Slime, Ghost, MushroomKing controllers |
| `phase-07/step-05-b` | inspector | `Assets/GoldRush/Animations/Slime.controller` | Inspector of the Slime Locomotion blend tree: Idle 0, Walk 0.5, Walk 1 |
| `phase-07/step-06` | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | Inspector of GoldRushAssets: Slime, Ghost and Mushroom King open with Model, Controller and Height filled |
| `phase-07/step-07` | project | `Assets/GoldRush/Scripts/Gameplay/EnemyAI.cs` | Project window: Scripts/Gameplay with CharacterFactory.cs, EnemyAI.cs, GameManager.cs, OriginalMaterials.cs, PlayerMotor.cs, ThirdPersonCamera.cs |
| `phase-07/step-08` | project | `Assets/GoldRush/Scripts/Level/MonsterSpawn.cs` | Project window: Scripts/Level with CoinSpot, LevelScene, MonsterSpawn, PlayerSpawn |
| `phase-07/step-09` | project | `Assets/GoldRush/Scripts/Gameplay/Badge.cs` | Project window: Scripts/Gameplay with Badge.cs |
| `phase-07/step-10` | project | `Assets/GoldRush/Scripts/Gameplay/GameManager.cs` | Project window: Scripts/Gameplay with GameManager.cs (phase 7 version) |
| `phase-07/step-11` | inspector | `Assets/GoldRush/Prefabs/Gameplay/Monster_Slime.prefab` | Inspector of Monster_Slime: Monster Spawn (Type Patroller), Nav Mesh Agent (Radius 0.45, Height 0.95), Enemy AI |
| `phase-07/step-12` | inspector | `Assets/GoldRush/Prefabs/Gameplay/Monster_MushroomKing.prefab` | Inspector of Monster_MushroomKing: Monster Spawn (Type Guard, Home empty), Nav Mesh Agent (Radius 0.75, Height 2.6), Enemy AI |
| `phase-07/step-13-a` | hierarchy | `Level 1 - Training Yard/Gameplay/Monsters/Slime 1` | Hierarchy: Gameplay › Monsters › Slime 1, Slime 2 (blue prefab icons) |
| `phase-07/step-13-b` | scene | `Level 1 - Training Yard/Gameplay/Monsters` | Scene view: the two green MonsterSpawn boxes on the blue NavMesh |
| `phase-07/step-14` | game | `` | Game view: a Slime with its ITSTEP badge chases the hero |

Old screenshots in `images/phase-05`, `images/phase-06` were **not** used: they show the HUD, which now comes in phase 9.

## Shared for phases 5–8 (same text in all four v2 notes)

### Baseline at the end of phase 4 (checked against the new phase-01/04 pages and v2-phase-03/04 notes)
Scripts (in `Assets/GoldRush/Scripts/`):
- Phase 1 helpers: `Core/` Tween, TweenRunner, Fader, GameAssets; `Audio/` AudioFX, Synth; `UI/` UIKit, ButtonFX, LogoTitle, DayNightButton;
  `World/` DayNight, NightLight, EnvironmentSetup. Phase 1/2 typed: Data.cs (full in phase 2), GoldRushMenu (phase version), MenuAction.
- Phase 4: `UI/VirtualJoystick.cs` + `UI/TouchLookArea.cs` **stubs**, `Level/PlayerSpawn.cs` (real), `Gameplay/PlayerMotor.cs` (phase 4 version),
  `Gameplay/ThirdPersonCamera.cs` (phase 4 version: no `PlayIntro`, `PHASE 4 ONLY` cursor block).
- Phase 5 replaces **both** PlayerMotor (my phase 5 version) and ThirdPersonCamera (**real file**, as v2-phase-04-notes point 2 asks).
Assets / scene:
- `Resources/GoldRushAssets.asset` (phase 1): Hero model + `Animations/Hero.controller`, GR_ materials, GR_SunsetSky, 16 sounds.
  Phase 5 step 9 adds Hero Hat Scale 1.9 / Hat Height 0.74 / Walk 0.59 / Run 0.84 (repo values; = phase 4 `clipWalkSpeed/clipRunSpeed`).
- `Level1` (phase 3): root `Level 1 - Training Yard` › `Static` (Batching Static; Floor, Walls incl. gap at z = 16 + `Walls/GateBlocker` 0,2.5,17.25 box **8×5×1**,
  Lamps, Obstacles), `Gameplay` › `PlayerSpawn` (0,0,-9), `Coins/CoinSpot 1..6`, `ExitGateSpot` (0,0,16), `Monsters/Slime 1` (5.5,0,10.5), `Slime 2` (14,0,-9.5),
  `PowerUps/PowerUp Magnet` (-14.5,0,-8.5) – plain empties. Player prefab instance at the scene root; Main Camera with ThirdPersonCamera (Target = Player).
  `MainMenu` (0) and `Level1` (1) in Build Profiles → Scene List.
Compile check: Roslyn against the Unity 6000.6.10f1 DLLs (scratchpad `p58/cc.py`; `p58/pagecheck.py` extracts every full-file code block from
the HTML pages of phases 5–8 and compiles the cumulative set on top of the phase 1 helpers + the phase 4 stubs/versions taken from `phase-04.html`).
**0 errors in phases 5, 6, 7 and 8.** Not run in Unity. `_notes/checkpoint-log.md` does not exist, so I could not reuse a tester log.

### Design decisions (all four phases)
- **No GameUI in 5–8.** GameManager phase versions never touch `UI`. Progress is `Debug.Log("[GoldRush] …")`.
  Win → log the RunResult → `BackToMenu()` after 4 s. Lose → slow motion → log → `Restart()` after 2 s. Pause = Esc/P, `timeScale 0`, log.
- **No LevelBuilder / Props / LevelLayout in 5–8.** Phase versions keep `spawn`, `spawnYaw`, `arenaHalf` fields instead of `LevelLayout`,
  and only use objects placed by hand (Coin in CoinSpot, PowerUp in PowerUpSpot, monster prefabs). Empty spots log a warning.
  So `Coin.cs`, `ExitGate.cs`, `PowerUp.cs` are phase versions **without `Create()`** (and ExitGate without `Build()`).
  **Phase 9/10 must import** `World/LevelBuilder.cs`, `World/Props.cs`, `World/DayNightTint.cs`, `World/DayNightPalette.cs` and replace Coin/ExitGate/PowerUp
  with the real files (`Resources/Scripts/Helpers/World/Coin.cs`, `ExitGate.cs`; real `PowerUp.cs` after code change 1) when the real GameManager comes back.
  (I compiled the real GameManager + GameUI + those files on top of my phase 8 set: only `GameAction.cs` (new UI file) was missing.)
- Monster prefabs have **no model child**: `EnemyAI.SetupPlaced()` builds the body from GoldRushAssets (`CharacterFactory.BuildModel`, correct height).
  A Model child would not be scaled by the real `SetupPlaced()` (see optional code change 3).
- Player layer: the real `PlayerMotor.SetupPlaced()` sets Ignore Raycast at runtime, so no manual layer step.

### Phase versions (whole files shown in the guide; "replace the whole file")
| File | P5 | P6 | P7 | P8 | Later |
|---|---|---|---|---|---|
| `Gameplay/GameManager.cs` | v5: spawn, camera, EnvironmentSetup, intro swoop (`Cam.PlayIntro`, 2.8 s wait instead of `UI.Intro`), coins, pause | v6: + `RunResult`, gate, win check, Win/Lose (no FX), save, `Restart`/`BackToMenu`, TEST key L | v7: + NavMesh rebuild, placed monsters (`SetupPlaced(…, null)`), lives/hits, `OnPlayerCaught`, `Enrage`, tension; `GhostActive => false`; no L key | v8: real minus UI and minus LevelBuilder/LevelLayout/`Create` paths, minus `NoDamage`, `NextLevel`, `ArrowTarget`, debug hooks | phase 9: real file |
| `Gameplay/ThirdPersonCamera.cs` | **real file** (replaces the phase 4 version) | = | = | = | – |
| `Gameplay/PlayerMotor.cs` | v5: real minus `Create`, `Cosmetics.Apply`, FX (dust/boost/Puff), ghost look (`SetGhost`, Mats), `SpeedMultiplier`, `AddLantern` | = | = | **real file** (final) | – |
| `World/Coin.cs` | v5: real minus `Create()` | = | = | = | real (helper) with LevelBuilder |
| `World/ExitGate.cs` | – | v6: real minus `Create()`/`Build()`, `Opening`, `Red`, `Green` | = | = | real (helper) with LevelBuilder |
| `World/PowerUp.cs` | – | – | – | v8: real **after code change 1**, minus `Create()`/`Build()` | real (after change 1) with Props |
| `Level/CoinSpot.cs`, `Level/LevelScene.cs` | real (typed) | | | | |
| `Level/MonsterSpawn.cs`, `Gameplay/Badge.cs` | | | real (typed) | | |
| `Level/PowerUpSpot.cs` | | | | real (typed) | |
The exact phase-version sources are the code blocks in the HTML pages (and in scratchpad `p58/p5..p8/`).

### Imported helpers per phase (from `Resources/Scripts/Helpers/`, unchanged)
- Phase 5: none (everything needed comes from phases 1–4; Tween for `PlayIntro` is from phase 1).
- Phase 6: none.
- Phase 7: `Gameplay/EnemyAI.cs`, `Gameplay/CharacterFactory.cs`, `Gameplay/OriginalMaterials.cs`, `Core/Mats.cs`, `UI/WorldMarkers.cs`
  (WorldMarkers only so EnemyAI compiles; GameManager passes `null` markers until phase 9).
- Phase 8: `Gameplay/FX.cs`.

### GoldRushAssets fields filled by students
- Phase 5: check Hero model/controller + 16 sounds (phase 1); type Height 1.35, Hat Scale 1.9, Hat Height 0.74, Walk 0.59, Run 0.84 (repo values).
- Phase 7: Slime (Slime.fbx, Slime.controller, 0.95), Ghost (Ghost.fbx, Ghost.controller, 1.3), Mushroom King (MushroomKing.fbx, MushroomKing.controller, 2.6, Walk 1.48, Run 2.11); check Lit/LitEmissive/LitTransparent/Particle.
- Phase 8: check Particle = GR_Particle.

### Code changes needed (real code; I wrote the guides as if they are done)
1. **`Scripts/World/PowerUp.cs`** – a power-up placed by hand (the provided PowerUp_* visual prefabs + the script) must work:
   ```csharp
   [SerializeField] PowerType type = PowerType.Speed;
   public PowerType Type => type;                 // was: public PowerType Type { get; private set; }
   public bool Collected { get; private set; }
   [SerializeField] Transform visual, inner, ring; // was: Transform visual, inner, ring;
   float phase;

   void Start()
   {
       phase = Random.Range(0f, Mathf.PI * 2f);
       // a power-up placed by hand: find its parts by name if the fields are empty
       if (!visual) visual = transform.Find("Visual");
       if (!inner && visual) inner = visual.Find("Inner");
       if (!ring) ring = transform.Find("Ring");
   }
   ```
   and in `Create()`: `p.type = type;` instead of `p.Type = type;` (you may also drop `p.phase = …` there, Start sets it).
   Why: today `Type`/`visual`/`inner`/`ring` exist only for code-built orbs; a prefab with the script would be a Speed orb that never moves.
   Then copy the changed file to `Resources/Scripts/Final/World/PowerUp.cs`.
2. **`Scripts/Gameplay/GameManager.cs`** (real file) – use hand-placed power-ups like hand-placed coins:
   in `LevelFromScene()`:
   ```csharp
   foreach (var pu in scene.GetComponentsInChildren<PowerUpSpot>())
   {
       var placed = pu.GetComponentInChildren<PowerUp>();
       if (placed) { powerUps.Add(placed); continue; }   // a PowerUp prefab placed by hand
       l.powerUpPoints.Add(pu.transform.position);       // an empty marker: Build() makes an orb here
       powerTypes.Add(pu.type);
   }
   ```
   and in `Build()` give the placed ones a label too: move `UI.AddPowerUpLabel(pu)` out of the `PowerUp.Create` loop into
   `foreach (var pu in powerUps) UI.AddPowerUpLabel(pu);` after it.
3. (Optional, not needed by my guide) `EnemyAI.SetupPlaced()`: if a placed prefab has a Model child, scale it to `def.height` and put the feet
   on the floor (same code as `CharacterFactory.BuildModel`). Then a monster prefab could show its model in the editor.
4. **Starter package** must contain `Prefabs/Gameplay/Coin.prefab`, `ExitGate.prefab`, `PowerUp_Speed/Ghost/Magnet.prefab` (visual only – they are,
   in the repo), materials `M_GlowGreen`, `M_GlowRed`, `GR_*`, the 3 monster FBX with the loop flags of `CharacterImportRules`.
   ExitGate prefab: `Glow` inactive, `GlowLight` Light disabled (they are). Note: `LanternLight` in the prefab has no `NightLight`, so it also shines by day.
5. (Optional) Put my phase-version files in `Resources/Scripts/Phases/phase-0X/` so students can copy them instead of selecting text from the page.

### For other phases
- Phase 4: done as you asked – phase 5 replaces ThirdPersonCamera with the real file (cursor → GameManager) and PlayerMotor with a phase 5 version.
  Phase 7 duplicates `Assets/GoldRush/Animations/Hero.controller`. The VirtualJoystick/TouchLookArea stubs stay until phase 9/10 (my files only use `Value`, `Held`, `Consume()`).
- Phase 3: promise kept – phase 6 goes back to the menu after a win (and reloads after a loss). Phase 6 puts the gate under `Static` at 0,0,16 (like the repo, so the
  NavMesh in phase 7 includes the posts) and deletes `ExitGateSpot`; it tells students to keep the gate **not** static.
- Phase 9: real GameManager + GameUI; also import LevelBuilder/Props/DayNightTint/DayNightPalette + real Coin/ExitGate/PowerUp (see above),
  and GameAction.cs. The "!" bubbles, hearts, power-up bars/labels, arrow, toasts, countdown and dialogs appear there. Remove nothing else:
  my phase 8 PlayerMotor is already final.
- Phase 10: Levels 2–4 can use Monster_Ghost / Monster_MushroomKing prefabs (Home field for guards).
