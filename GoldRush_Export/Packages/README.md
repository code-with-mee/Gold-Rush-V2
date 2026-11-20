# Gold Rush – course packages

Every phase of the course starts by importing its package. Each package is **complete**: all resources
(characters, sounds, icons, fonts, UI sprites, prefab kit, materials), **all game scripts** (the finished ones),
the game settings assets, and the scenes of that phase. All packages use the same script files, so they never clash.

How to use one: make a new empty Unity 6 project (Universal 3D) — or delete `Assets/GoldRush` — then
**Assets → Import Package → Custom Package…**, pick the package, click **Import**. Then add the scenes to
**File → Build Profiles → Scene List** (MainMenu first, then Level1 …).

| Package | After phase | Contains (on top of the starter resources) |
|---|---|---|
| GoldRush_Phase00_Project | 0 Create the project | starter resources only (no scripts) + Sandbox test scene |
| GoldRush_Phase01_MainMenu | 1 The main menu | MainMenu scene (3D stage + all menu screens) |
| GoldRush_Phase02_MenuScreens | 2 Level select, shop, settings | same scenes as phase 1 (the guide explores the other screens) |
| GoldRush_Phase03_Level | 3 Level design | + Level1: the level only (kit pieces, lights) and an overview camera |
| GoldRush_Phase04_Player | 4 Player and camera | + GameManager and PlayerSpawn (player + camera are made when you press Play) |
| GoldRush_Phase05_Coins | 5 Coins | + the coins (CoinSpots) |
| GoldRush_Phase06_Gate | 6 Exit gate, win and lose | same scenes as phase 5 (the guide explores the gate and the rules) |
| GoldRush_Phase07_Monsters | 7 Monsters | + monster spawn markers (monsters are made when you press Play) |
| GoldRush_Phase08_PowerUps | 8 Power-ups and effects | + power-ups |
| GoldRush_Phase09_GameUI | 9 Gameplay UI | + the game UI as scene objects (HUD, countdown, pause / win / lose dialogs) |
| GoldRush_Phase10_Final | 10 Polish and build | + Levels 2–4 and the Daily level = the complete game |
| GoldRush_Levels2to4 | (extra) | our Level 2–4 + Daily scenes only, with the Fix Script Links tool |
| GoldRush_GameUIDialogs | (extra) | the Pause / Win / Lose dialogs as prefabs |

Each package is tested by importing it into a brand-new empty project: it must compile with 0 errors and the
phase's "Press Play" result must work. The `.txt` file next to each package lists every file inside it.
