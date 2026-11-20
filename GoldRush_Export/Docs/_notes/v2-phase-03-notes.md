# V2 Phase 3 notes: Level design (old phases 1 + 2)

Guide: `phase-03.html` (19 steps). Built from `_old_order/phase-01.html` (kit tour, shortened to one step) and
`_old_order/phase-02.html` (Level1 build, unchanged values). Real sources checked: `Scripts/Core/Data.cs`
(`Levels.All[0].scene = "Level1"`, `GameSession.SceneName`), `Scripts/UI/GoldRushMenu.cs` (`Run("play")` →
`StartLevel` → `Fader.Load(GameSession.SceneName)`), `Scripts/Core/Fader.cs` (persistent, fades in by itself after
`LoadSceneAsync`, so Level1 needs nothing for the fade), tester checkpoint `GoldRush_Phase02_Level.unitypackage`
(its `Level1.unity` hierarchy matches the guide 1:1).

Note: `_notes/checkpoint-log.md` does not exist. I used the tester's checkpoint packages in
`GoldRush_Export/Packages/` (Phase02–04) as the record of what was built.

## What changed vs. the old order
- Kit tour = one step (table with the `images/kit/*.png` thumbnails; accent blocks and walls merged into one row
  each), plus a pivot note and an "do not Apply All" warning. Prefab Mode, colour change and override/revert
  steps were dropped (too long; not needed to build the level).
- Grid snapping step dropped (a tip says "type the numbers"; Ctrl+D tip kept).
- Marker table condensed (CoinSpot 1–6 in one row). PlayerSpawn now says "used in Phase 4".
- Fly-around controls moved into the Press Play step.
- NEW step 17: add `MainMenu` (index 0) and `Level1` (index 1) to **File → Build Profiles → Scene List**;
  untick Sandbox/SampleScene.
- NEW Press Play: start in MainMenu, click PLAY → fade → Level1 (overview camera).
- "Back to the menu" note: phases 3–5 = stop Play mode (or test by pressing Play in Level1 directly);
  phase 6 = automatic return after win/lose; phase 9 = pause dialog (Esc) Home button.
  **Phase 6 and 9 writers: please keep these promises** (`GameManager.BackToMenu()` → `Fader.Load(Scenes.Menu)`).

## Assumptions about phases 0–2 (other writer)
- Phase 2 checkpoint is `GoldRush_Phase02_MenuScreens.unitypackage` (SPEC name).
- The phase-1/2 version of `GoldRushMenu` keeps `Run("play")` → `StartLevel(...)` → `Fader.Load(GameSession.SceneName)`
  and `Data.cs` is the real file (Level 1 scene name `"Level1"`, `Save.Data.unlocked` default 1).
- `MainMenu` may or may not be in the Scene List already; step 17 handles both cases.
- LEVELS cards for levels 2–4 would fail to load (scenes not in the build): covered in Common problems
  (they are locked in a fresh save anyway).

## Phase versions of scripts
None. No scripts typed or imported in phase 3.

## Imported helpers
None.

## Code changes needed
None for phase 3. Notes for the code owner / other writers:
1. **Checkpoint packages do not carry the Scene List** (`ProjectSettings/EditorBuildSettings.asset` is not in a
   .unitypackage). Phase 4 "What you need" tells students who import `GoldRush_Phase03_Level` to redo step 17.
   Same issue for every later checkpoint.
2. Markers are still plain empties (no scripts). Later phases add: `PlayerSpawn` (phase 4), `LevelScene` on
   `Level 1 - Training Yard` with `arenaHalf = 16` + `CoinSpot` (phase 5), ExitGate prefab at
   `Gameplay/ExitGateSpot` (0,0,16), not Batching Static (phase 6), `MonsterSpawn` + `NavMeshSurface` on `Static`
   (phase 7), `PowerUpSpot` (phase 8), `DayNightTint` (phase 10). Unchanged from `phase-02-notes.md`.
3. Main Camera stays in the scene as the overview camera; phase 4 turns it into the ThirdPersonCamera
   (`GameManager.Build` already uses a scene `ThirdPersonCamera` via `FindAnyObjectByType` + `Init`).
4. `TEMPLATE.html` still lists the OLD phase titles in the sidebar. I used the V2 titles (Create the project, The
   main menu, Level select, shop and settings, Level design, Player and camera, Coins, Exit gate, win and lose,
   Monsters, Power-ups and effects, Gameplay UI, Polish and build). Please update the template so all pages match.
5. Screenshot tool: the Lighting window (step 14) and Build Profiles window (step 17) are not in the allowed
   `data-window` list. I used `scene` (result of the lighting) and `project` (the Scenes folder) instead.
   If the tool can capture other editor windows, add `lighting` / `buildprofiles`.
6. `.sw` colour swatch spans from the old guide are not in `assets/style.css`; I removed them (hex codes only).

## Screenshots
Existing images used: `images/kit/*.png` (13 thumbnails in step 2), `images/phase-02/level1-top.jpg` (step 3),
`images/phase-02/level1-overview.jpg` (step 18).

Placeholders (`figure.shot.todo`):
| data-shot | window | select | shows |
|---|---|---|---|
| phase-03/step-02 | project | Assets/GoldRush/Prefabs/Level | kit thumbnails |
| phase-03/step-04 | hierarchy | Main Camera | new scene: Main Camera + Directional Light |
| phase-03/step-05 | hierarchy | Level 1 - Training Yard | group tree |
| phase-03/step-06 | scene | Level 1 - Training Yard/Static/Floor/Floor_40m | floor on outer ground |
| phase-03/step-07 | scene | Level 1 - Training Yard/Static/Walls/WallSouth | south wall |
| phase-03/step-08-a | scene | Level 1 - Training Yard/Static/Walls/WallNorth | north wall with opening |
| phase-03/step-08-b | inspector | Level 1 - Training Yard/Static/Walls/GateBlocker | Box Collider 8,5,1 |
| phase-03/step-09 | scene | Level 1 - Training Yard/Static/Walls | all walls + corner pillars |
| phase-03/step-10 | scene | Level 1 - Training Yard/Static/Lamps | 8 lamps |
| phase-03/step-11 | scene | Level 1 - Training Yard/Static/Obstacles | 17 obstacles |
| phase-03/step-12-a | hierarchy | Level 1 - Training Yard/Gameplay | 11 markers |
| phase-03/step-12-b | scene | Level 1 - Training Yard/Gameplay/PlayerSpawn | coloured marker icons |
| phase-03/step-13 | inspector | Sun | light values |
| phase-03/step-14 | scene | Sun | sunset sky + fog |
| phase-03/step-15 | inspector | Level 1 - Training Yard/Static | Batching Static |
| phase-03/step-16-a | inspector | Main Camera | 0,22,-30 / 35,0,0 |
| phase-03/step-16-b | game | Main Camera | overview of the yard |
| phase-03/step-17 | project | Assets/GoldRush/Scenes/Level1.unity | MainMenu + Level1 scenes |
| phase-03/step-18 | game | Main Camera | Play mode after PLAY in the menu |
