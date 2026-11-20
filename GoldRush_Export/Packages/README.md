# Gold Rush – course packages

Every package is a **complete checkpoint**: import it into an empty Unity 6 (URP) project and you get the
project exactly as it is at the END of that phase — all resources (characters, sounds, icons, fonts,
UI sprites, prefab kit, materials) + all scripts + all scenes built so far.
Use one when you are stuck: start a new project (or delete `Assets/GoldRush`), import the package of the
phase you finished last, and continue with the next guide.

| Package | After phase | Contains (on top of the starter resources) |
|---|---|---|
| GoldRush_Phase00_Project | 0 Create the project | starter resources only (no scripts) + Sandbox test scene |
| GoldRush_Phase01_MainMenu | 1 The main menu | MainMenu scene (3D stage + title screen), menu scripts |
| GoldRush_Phase02_MenuScreens | 2 Level select, shop, settings | all menu screens, save data |
| GoldRush_Phase03_Level | 3 Level design | Level1 scene built from the kit, markers, Build Profiles list |
| GoldRush_Phase04_Player | 4 Player and camera | Player prefab, Hero Animator Controller, third-person camera |
| GoldRush_Phase05_Coins | 5 Coins | Coin prefab + script, GameManager, GoldRushAssets (sounds) |
| GoldRush_Phase06_Gate | 6 Exit gate, win and lose | Exit gate, timer, stars, save |
| GoldRush_Phase07_Monsters | 7 Monsters | NavMesh, 3 monster prefabs + controllers, EnemyAI |
| GoldRush_Phase08_PowerUps | 8 Power-ups and effects | 3 power-ups, particles |
| GoldRush_Phase09_GameUI | 9 Gameplay UI | HUD, countdown, pause / win / lose dialogs (scene objects) |
| GoldRush_Phase10_Final | 10 Polish and build | the complete game (= the teacher's version) |

Each package is tested by importing it into a brand-new empty project: it must compile with 0 errors and the
phase's "Press Play" result must work. The `.txt` file next to each package lists every file inside it.
