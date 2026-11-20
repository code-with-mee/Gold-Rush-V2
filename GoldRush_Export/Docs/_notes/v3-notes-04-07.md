# VERSION 3 notes: phases 4–7 (package-driven)

Pages rewritten: `phase-04.html`, `phase-05.html`, `phase-06.html`, `phase-07.html`.
Format per SPEC "VERSION 3": What you get · Import · Check (3.x, one figure each) · How it works · ▶ Press Play · Try it · Common problems · pager.
Sidebar copied from `_notes/TEMPLATE.html` (only `aria-current` added).

## Facts checked in the finished game (Assets/GoldRush)
- `Scenes/Level1.unity` roots: `GameManager`, `Level 1 - Training Yard`, `GameUI`, `EventSystem`. No Player, no Main Camera, no Sun.
- Player / Main Camera are made at Play by `PlayerMotor.Create` / `ThirdPersonCamera.Create` (names `Player`, `Main Camera`).
- CoinSpots have **no Coin child** -> coins are made at Play (`Coin.Create`) under `Dynamic (runtime)` (name `Coin`).
- Monsters: `Gameplay/Monsters/Slime 1`, `Slime 2` (MonsterSpawn, type Patroller, home None) -> made at Play by `EnemyAI.Create`, name `Monster_Patroller`, under `Dynamic (runtime)`.
- `Static` has NavMeshSurface (Humanoid, Collect Objects = Children/Current Object Hierarchy, Use Geometry = Physics Colliders, NavMesh Data None); rebuilt at Play in `LevelFromScene`.
- Gate: `Level 1 - Training Yard/Static/ExitGate` (pos 0,0,16, not static, all serialized fields filled, greenStrip = M_GlowGreen). `Glow` inactive, `GlowLight` Light disabled. `Text` TextMesh = "EXIT".
- GateBlocker: `Level 1 - Training Yard/Static/Walls/PerimeterWall/GateBlocker` (pos 0,2.5,17.25, BoxCollider 7.5,5,1). The old page said 8,5,1 — fixed.
- Controllers are in `Art/Characters/Controllers/` (not `Animations/` as the v2 pages said).
- `Prefabs/Gameplay/Coin.prefab` and `ExitGate.prefab` have no scripts (visual only) -> not used in the checks.

## Screenshot placeholders (data-window / data-select)
"filled" = an existing image is already in the figure (class `shot`, not `todo`), with data attributes so it can be re-captured.

| data-shot | window | data-select | caption / note |
|---|---|---|---|
| phase-04/check-01 | hierarchy | `GameManager` | Hierarchy, no Player / Main Camera (GameUI + EventSystem visible in final scene, caption says so) |
| phase-04/check-02 | inspector | `GameManager` | Game Manager component, no fields |
| phase-04/check-03 | inspector | `Level 1 - Training Yard/Gameplay/PlayerSpawn` | pos 0,0,-9 |
| phase-04/check-04 | inspector | `Assets/GoldRush/Art/Characters/Hero/Hero.fbx` | **filled**: images/phase-04/step-02-a.png (Rig tab) |
| phase-04/check-05 | animator | `Assets/GoldRush/Art/Characters/Controllers/Hero.controller` | states + parameters |
| phase-04/check-06 | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | open the **Hero** foldout before capture |
| phase-04/check-07 | game | `Player` | PLAY MODE only |
| phase-04/check-08 | game | `Main Camera` | PLAY MODE only |
| phase-05/check-01 | inspector | `Level 1 - Training Yard` | Level Scene, Arena Half 16 |
| phase-05/check-02 | hierarchy | `Level 1 - Training Yard/Gameplay/Coins` | expand Coins |
| phase-05/check-03 | inspector | `Level 1 - Training Yard/Gameplay/Coins/CoinSpot 1` | |
| phase-05/check-04 | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | **filled**: images/phase-05/step-09.png |
| phase-05/check-05 | game | `Dynamic (runtime)/Coin` | PLAY MODE only (any of the 6 coins) |
| phase-05/check-06 | game | `GameUI/HUD/CoinPill` | PLAY MODE only, counter 0 / 6 |
| phase-06/check-01 | hierarchy | `Level 1 - Training Yard/Static/ExitGate` | expand ExitGate |
| phase-06/check-02 | inspector | `Level 1 - Training Yard/Static/ExitGate` | Exit Gate fields |
| phase-06/check-03 | inspector | `Level 1 - Training Yard/Static/ExitGate/Glow` | inactive checkbox |
| phase-06/check-04 | inspector | `Level 1 - Training Yard/Static/Walls/PerimeterWall/GateBlocker` | |
| phase-06/check-05 | inspector | `Assets/GoldRush/Scripts/Core/Data.cs` | script preview showing Levels.All |
| phase-06/check-06 | game | `GameUI/Modal/Win` | PLAY MODE only, after winning (or `DebugWin()`) |
| phase-07/check-01 | inspector | `Level 1 - Training Yard/Static` | **filled**: images/phase-07/step-03-a.png |
| phase-07/check-02 | hierarchy | `Level 1 - Training Yard/Gameplay/Monsters` | expand Monsters |
| phase-07/check-03 | inspector | `Level 1 - Training Yard/Gameplay/Monsters/Slime 1` | |
| phase-07/check-04 | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | open the **Slime** foldout before capture |
| phase-07/check-05 | animator | `Assets/GoldRush/Art/Characters/Controllers/Slime.controller` | |
| phase-07/check-06 | game | `Dynamic (runtime)/Monster_Patroller` | PLAY MODE only (two exist; any one) |
| phase-07/check-07 | game | `GameUI/HUD/Hearts` | PLAY MODE only, two hearts |

Note: `data-shot` ids use `phase-XX/check-NN` (the v3 pages have checks, not build steps).
Play-mode paths (`Player`, `Main Camera`, `Dynamic (runtime)/...`) do not exist in edit mode; capture them after pressing Play.

## Real images reused (not placeholders)
- phase-04: images/phase-04/camera-behind.jpg, images/phase-04/camera-wall.jpg (Press Play)
- phase-05: images/phase-05/coins-collect.jpg, images/phase-05/all-gold.jpg (Press Play)
- phase-06: images/phase-06/gate-open.jpg, images/07_win.jpg (Press Play)
- phase-07: images/08_level2.jpg, images/10_lose.jpg (Press Play)
- Dropped (no longer fit, show hand-built state): phase-03/hero-run.jpg on p4, phase-05/step-13-a.png (Coin inside CoinSpot), phase-06/step-04.png + step-05.png (gate prefab without/with fields), phase-07/step-13-a.png (monster prefabs).

## Points for the code owner
- Phase 4/5 package: the intro band reads `def.coins` from Data.cs, so in Phase 4 it already says "Collect all 6 gold" although there are no coins; Console shows the yellow warning "This level has no CoinSpot objects" (the page tells students this is normal).
- Phase 5 package already contains the working ExitGate (from Phase 3), so the gate opens after the last coin and you can win in Phase 5. Page says "the gate is Phase 6".
- Phase 6: you cannot lose yet (no monsters, no debug key in the final GameManager). The page explains the lose rules and says the monsters arrive in Phase 7.
- No code changes needed.
