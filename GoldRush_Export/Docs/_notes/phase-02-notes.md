# Phase 2 notes: Level design

## Code changes needed
None required for phase 2 (no scripts). Points for the code owner and later phase writers:

1. **Marker scripts are added later** (phase 2 uses plain named empty GameObjects with Inspector icons).
   `GameManager.LevelFromScene` finds them with `GetComponentInChildren` under the `LevelScene` root, so they must
   stay children of `Level 1 - Training Yard` (they are: `Gameplay/...`). Add in the phase that uses them:
   - `LevelScene` on `Level 1 - Training Yard`, `arenaHalf = 16` → phase where GameManager first runs (3 or 5).
     Without `LevelScene` GameManager generates a level from the seed instead of using the scene.
   - `PlayerSpawn` on `Gameplay/PlayerSpawn` → phase 3 (or whenever GameManager spawns the player).
   - `CoinSpot` on `Gameplay/Coins/CoinSpot 1..6` → phase 5.
   - ExitGate prefab placed at `Gameplay/ExitGateSpot` (0,0,16) → phase 6. `ExitGateSpot` is a guide-only marker
     (repo has the ExitGate itself under `Static` at (0,0,16)). The gate must NOT be Batching Static (door moves):
     in the repo `LevelBaker.MarkStatic` skips the gate. Suggest phase 6: drag the gate into `Gameplay` and delete
     `ExitGateSpot`, or put the gate under `Static` and untick static on it.
   - `MonsterSpawn` (type Patroller) on `Gameplay/Monsters/Slime 1`, `Slime 2` → phase 7.
   - `PowerUpSpot` (type Magnet) on `Gameplay/PowerUps/PowerUp Magnet` → phase 8.
   - `GameManager` GameObject at scene root (repo Level1 has one) → phase 5 (or 3).
   - `NavMeshSurface` on `Static` (repo has it there; GameManager rebuilds it at start) → phase 7.
     `OuterGround` is in `Scenery` (outside `Static`) so it is not in the NavMesh.
   - `DayNightTint` on the level root (repo) → phase 10.
2. **Camera.** The guide keeps the template Main Camera as an overview camera at (0,22,-30) rot (35,0,0).
   `ThirdPersonCamera.Create` (called by GameManager) makes its own "Main Camera" + AudioListener, so **phase 4 (or
   the phase that adds GameManager) must tell students to delete the scene's Main Camera** (the repo Level1 has no
   camera and no light).
3. **Lighting at runtime.** `EnvironmentSetup.Apply` (called by GameManager) re-sets skybox (from
   `GameAssets.skybox`), trilight ambient, linear fog 35–120 and finds the existing directional light as the sun
   (it only creates and colours one if none exists). The by-hand values in phase 2 equal the runtime values, so the
   look does not jump. The hand-made `Sun` keeps the values from step 13 (colour FFDEAD, 1.5, rot 30/35/0, soft,
   strength 0.75, bias 0.05/0.4) = what EnvironmentSetup would create. Global Volume from the URP template is deleted
   (EnvironmentSetup adds a "PostFX" volume).

## Layout: real Level1.unity vs. guide
Real (read from `Scenes/Level1.unity`): arena size 32 (half 16), walls 2.3 m × 1 m with inner face at ±16
(`Props.PerimeterWall`), corner blocks 1.7×3.2×1.7, pilasters every ~10 m, gate gap 5.5 m at +Z, GateBlocker
(0,2.5,17.25) size 7.5×5×1, floor = Quad 40×40 + plinth (trim) + GroundCollider, OuterGround Quad 480 m at
y -0.6 (tiling 120), 8 lamps at (±14.4,±14.4), (±14.4,0), (±4.5,-14.4), floor strips/pads (decor), 32 towers +
18 rim blocks (scenery), structures/obstacles below. Spawn (0,0,-9) yaw 0.

Guide simplifications:
- User decision: everything is placed from the provided kit (no primitives, no materials). Only empties and one
  Box Collider (GateBlocker) are created by hand.
- Floor = kit prefab `Floor_40m` at (0,0,0) under `Static/Floor`; `OuterGround` kit prefab at (0,0,0) under
  `Scenery` (its quad sits at local y -0.6). See phase-01-notes "Starter package contents".
- Walls: groups `WallSouth/North/East/West` of Wall_8m/Wall_4m prefabs at local x ±4, ±12 (north: Wall_8m ±12,
  Wall_4m ±6). **Gate opening is 8 m (x -4..4) instead of 5.5 m**; the ExitGate frame (posts at ±3.4, 1.3 wide,
  covering 2.75..4.05) fills the extra width. GateBlocker 8×5×1 at (0,2.5,17.25).
- Corners: Pillar prefab (1×3×1) instead of 1.7×3.2×1.7 blocks. No pilasters.
- Lamps rounded to ±14.5.
- Obstacles mapped to the phase 1 prefabs (17 objects, all kit prefabs):

| Real object (pos, yaw) | Guide |
|---|---|
| LCover (4,0,4) L-shaped room (arms ~7 m, h 1.3) | AccentBlock_Teal (4,0,4.5) + Block_4x1x2 (7,0,4.5), (11,0,4.5) + Block_4x1x2 rot 90 (4,0,7.5) |
| Pillar (-12.5,0,4.5), (-11.5,0,-5), (-5,0,12.5) | Pillar, same positions |
| Stack (-11,0,10.5) 2×1.4×2 + small | Block_2x2x2 (-11,0,10.5) |
| Stack (-6.5,0,1.5) | Block_2x2x2 (-6.5,0,1.5) + Block_1x1x1 on top (-6.5,2,1.5) |
| Block (6,0,-6) yaw 90, 2×1×2 | Block_4x1x2 rot 90 (6,0,-6) |
| Block (-6.5,0,-9.5) 1×1×1 | Block_1x1x1 same |
| Barrier (-2.5,0,-5) length 4 | Barrier_Orange (3 m) same pos |
| Block (10.5,0,-10) 1.5×2.5×3 | AccentBlock_Red (10.5,0,-10) |
| Block (12,0,-3.5) 1.47 | AccentBlock_Teal (12,0,-3.5) |
| Terminal (10.5,0,12) yaw 90 | Terminal (10.5,0,12) rot Y 90 |
| Block (-7.5,0,7) 1.27 | AccentBlock_Red (-7.5,0,7) |
| FloorStrips, FloorPads, Towers, Rim blocks | not placed (FloorPad_Cyan is in the kit; optional) |

- Markers (real → guide, rounded to 0.5 m, y 0.08 → 0): CoinSpot 1 (5.12,11.32)→(5,11.5); 2 (-6.49,-12.5)→
  (-6.5,-12.5); 3 (14.27,-7.36)→(14.5,-7.5); 4 (-7.35,0)→(-7.5,0); 5 (-14.39,12.74)→(-14.5,12.5);
  6 (4.89,-1.91)→(5,-2); Slime 1 (5.23,10.76)→(5.5,10.5); Slime 2 (14.28,-9.35)→(14,-9.5);
  PowerUp Magnet (-14.35,-8.59)→(-14.5,-8.5); PlayerSpawn (0,0,-9) yaw 0; ExitGateSpot (0,0,16).
  (MonsterSpawn positions are snapped to the NavMesh at runtime, `SamplePosition` 3 m.)
- Static: guide ticks only **Batching Static** on `Static` + children (repo: Batching | Occluder | Occludee static on
  renderers). LampLight objects become static too; harmless.
- Grid snapping: Grid Size 0.5, Move increment 0.5, Rotate 90.

## Phase versions of scripts
None (no scripts in phase 2).

## Screenshot placeholders
1. Top view of the finished Level1 (Scene view, Top, iso), with labels N / S, spawn, gate
2. Grid and Snap Settings window (Grid Size 0.5) and the Grid Snapping button in the Scene toolbar
3. Scene view, the floor with all four walls, corner pillars and the north opening
4. Scene view of the yard with lamps and all 17 obstacles
5. Hierarchy with Gameplay open (PlayerSpawn, Coins, Monsters, PowerUps, ExitGateSpot) and the coloured icons
6. Lighting window, Environment tab: GR_SunsetSky, Sun, Gradient colours, Fog Linear 35–120
7. Scene view at hero height from PlayerSpawn looking north to the gate opening, sunset sky and fog
