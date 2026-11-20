# V2 Phase 10 notes: Polish and build

Guide: `phase-10.html` (16 steps). No typed code. Day/night palette asset (12 rows) + DayNightTint on the Level1 root and on
`MenuDiorama` + NightLight on the 8 `LampLight`s (2.4) and the gate `LanternLight` (1.8); touch areas `LookArea (right half)` /
`MoveArea (left half)` built by hand in GameUI (values from our Level1 `GameUI`), GameUI → Joystick filled; Device Simulator
test; music check (AudioFX already real since phase 1); copy `Resources/Scripts/Final/*` over every script; import our
Levels 2–4 + Daily Rush as `GoldRush_Levels2to4.unitypackage` + **Gold Rush → Fix Script Links**; Scene List (MainMenu, Level1–4,
Game); final comparison with all 11 reference images; Build Profiles PC + Android.
Old order reused: `_old_order/phase-10.html` (day/night explanation, touch bullets, AudioFX table, builds, problems).

## Scripts in phase 10
| File | Change | Source |
|---|---|---|
| all of `Assets/GoldRush/Scripts/**` | overwrite with `Resources/Scripts/Final/**` (files only, keep `.meta`) | real files |
| `UI/GoldRushMenu.cs` | the only real change: Phase02 version → final (Cosmetics.Apply, shop confetti, BuildWorld/BuildUI fallbacks) | `Final/UI` |
| everything else | already identical to Final after phase 9 (checked: Helpers = Final = repo for all files) | – |

Compile check: all 44 files of `Resources/Scripts/Final` together → **0 errors** (Roslyn, Unity 6000.6.10f1 DLLs). Phase 9 set
(see v2-phase-09-notes) → 0 errors. Not run in Unity.

**Full list of phase versions / imported helpers in the course (my two phases):**
- Phase 9: no phase versions. Imported real: GameUI, VirtualJoystick, TouchLookArea, LevelBuilder, Props, DayNightTint,
  DayNightPalette, Coin, ExitGate (Helpers), PowerUp, GameManager (Final). Typed real: GameAction.
- Phase 10: Final copy (replaces GoldRushMenu phase 2 version; all others unchanged).

## Values used
- Palette = the 12 kit materials of our `Resources/DayNightPalette.asset` (M_Floor DBDEE6/212B52, M_Trim 6B7385/262E4C,
  M_BlockTop EDF0F7/7080AD, M_OuterGround 99A1B0/121730, M_Block CCD1DB/525C80, M_Orange FF9938/B86133, M_OrangeTop FFBD6B/D98047,
  M_Pad B2B8C7/171F40, M_Red ED4C4C/9E2B42, M_RedTop FF7870/C24254, M_Teal 33BDBD/1A6B85, M_TealTop 6BDED9/298FA1).
  Left out: M_TowerTop and the M_Floor_Tile* / M_OuterGround_Tile90* copies (not in the kit / starter package; our Level2–4 and
  menu scenes carry their own DayNightTint rows for them).
- NightLight: LampLight 2.4, gate LanternLight 1.8 (our Level1). LampPost prefab is NOT changed (the menu's LampLight instances
  already have a NightLight 2.2 from phase 1; a prefab NightLight would give them two).
- Touch: LookArea anchors (0.5,0)-(1,1) pivot 0,0, Image 000000·0 raycast; Finger 150, CanvasGroup 0 no raycast; Ring ring
  33C7E6·128; Glow soft 33C7E6·64 L/B/R/T −30. MoveArea (0,0)-(0.5,1); Stick 300, CanvasGroup 0; Bg circle 121A33·115; Ring ring
  33C7E6·153; Knob 130; Shadow circle 000000·64 L−4 B−10 R−4 T2; Face F2F7FF; Dot 33C7E6 inset 34. Radius 120.
- Levels table from `Levels.All` + the scenes (MonsterSpawn types counted: L2 2P 1C, L3 2P 2C 2G, L4 3P 3C 2G; coins 8/10/13;
  power-ups 2/3/4; arenaHalf 20/24/28; par 60/85/115).
- Android: Auto Rotation, Landscape Left/Right only (`Editor/GoldRushSetup.cs`), IL2CPP, ARM64. Scene list from
  `ProjectSettings/EditorBuildSettings.asset` (MainMenu, Level1–4, Game).

## Code / package changes needed
1. **`GoldRush_Export/Packages/GoldRush_Levels2to4.unitypackage`** (new): `Assets/GoldRush/Scenes/Level2.unity`, `Level3.unity`,
   `Level4.unity`, **`Game.unity`** (Daily Rush card → scene "Game"), plus every non-script dependency that is not in the starter
   package: `Materials/M_Floor_Tile24/28/32`, `M_Floor_Tile12(_B)`, `M_OuterGround_Tile90(_B)`, `M_TowerTop`, `M_GlowOrange*`,
   `M_GateBeam`, `M_Badge_*`, `M_TextWhite` (whatever "Select Dependencies" lists for the 4 scenes minus the starter package),
   and `Editor/FixScriptLinks.cs` (see v2-phase-09-notes, code change 3). **No `.cs` runtime scripts** in the package.
   The four scenes use only these GoldRush scripts: ButtonFX, CoinFlip, CoinSpot, DayNightButton, DayNightTint, ExitGate,
   GameAction, GameManager, GameUI, LevelScene, MonsterSpawn, NightLight, PlayerSpawn, PowerUpSpot, TouchLookArea,
   VirtualJoystick, WorldMarkers (no prefab references) → the Fix Script Links table must cover at least these 17.
2. **GameUI HideHud null-guard** (v2-phase-09-notes, change 1) must also be in `Resources/Scripts/Final/UI/GameUI.cs`, or the
   final copy re-introduces the crash for anyone who skips the joystick.
3. Same `.meta` warning everywhere: every "replace the script" step in phases 1–8 that says "drag the file into Unity, click
   Replace" should be checked – Unity may import a same-named file as `X 1.cs`. My pages copy `.cs` files in Finder/Explorer
   (files, never folders, because a Mac folder replace deletes the `.meta` files).
4. Screenshot tool: steps 7, 12 and 14-b are `data-manual="true"` (Device Simulator, Project Settings → Player). Build Profiles
   shots use `data-window="buildprofiles"` with `data-select="ProjectSettings/BuildProfiles/Windows|Android"` (= platform to
   select in the window) or a scene asset for the Scene List view.
5. Checkpoint `GoldRush_Phase10_Final.unitypackage` = this repo minus `Scripts/Dev`, `Editor/` dev tools, plus
   `Editor/FixScriptLinks.cs` (harmless) – the students' Level1/MainMenu are hand-built, ours are baked; both are fine.

## Promises kept for other phases
- Phase 3/9: MENU in PAUSE goes back to the menu; NEXT works from step 11 on.
- Phase 4/5–8 notes: VirtualJoystick / TouchLookArea stubs replaced in phase 9 (real files), objects placed in phase 10.
- Phase 2 notes point 3: the final GoldRushMenu runs on the hand-built MainMenu unchanged (`if (!cam) BuildWorld(); if (!titleScreen) BuildUI();` is in the repo).
- Phase 1/2 notes: MenuDiorama gets a DayNightTint here (palette applies to the menu kit too).

## Screenshots
Existing images used (step 12 `.shots`): 01_title, 01b_title_day, 02_levels, 03_shop, 04_level1_start, 05b_level1_night,
06_gate_open, 07_win, 08_level2, 11_level4, 12_daily.
Placeholders (`figure.shot.todo`, SPEC markup):

| data-shot | window | data-select | shows |
|---|---|---|---|
| `phase-10/step-03` | inspector | `Assets/GoldRush/Resources/DayNightPalette.asset` | Inspector of Resources/DayNightPalette: Entries with 12 rows (material, day colour, night colour) |
| `phase-10/step-04-a` | inspector | `Level 1 - Training Yard` | Inspector of Level 1 - Training Yard: Level Scene (Arena Half 16) and Day Night Tint (Entries 0) |
| `phase-10/step-04-b` | inspector | `Level 1 - Training Yard/Static/Lamps/LampPost/LampLight` | Inspector of a LampLight: Light (Point) and Night Light, Night Intensity 2.4 |
| `phase-10/step-04-c` | game | `Level 1 - Training Yard` | Game view in Play mode after pressing N: Level 1 at night, dark blue floor, lamps glowing, lantern light at the gate |
| `phase-10/step-05` | inspector | `GameUI/LookArea (right half)` | Inspector of LookArea (right half): anchors 0.5/0 – 1/1, Image alpha 0 with Raycast Target on, Touch Look Area (Area, Marker = Finger, Marker Group = Finger) |
| `phase-10/step-06-a` | hierarchy | `GameUI/MoveArea (left half)` | Hierarchy: GameUI → LookArea (right half) → Finger (Ring, Glow), MoveArea (left half) → Stick (Bg, Ring, Knob → Shadow, Face, Dot), WorldMarkers, HUD, Flash, Modal |
| `phase-10/step-06-b` | inspector | `GameUI/MoveArea (left half)` | Inspector of MoveArea (left half): anchors 0/0 – 0.5/1, Image alpha 0 raycast, Virtual Joystick (Area, Stick Base = Stick, Knob, Radius 120, Group = Stick, Edge = Ring) |
| `phase-10/step-07` | game (**manual**) | `GameUI/MoveArea (left half)` | Device Simulator (landscape phone) in Play mode: the joystick visible on the left half, the touch hint at the bottom |
| `phase-10/step-08` | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | Inspector of GoldRushAssets: the Sounds list with 16 entries, the last one music = music.wav |
| `phase-10/step-09` | console | `Assets/GoldRush/Scripts/UI/GoldRushMenu.cs` | Console window after the copy: no red errors |
| `phase-10/step-10-a` | project | `Assets/GoldRush/Scenes/Level2.unity` | Project window: Scenes with Game, Level1, Level2, Level3, Level4, MainMenu |
| `phase-10/step-10-b` | scene | `Level 2 - Ghost Alley` | Scene view of Level2 from above: the walls, blocks and markers of Ghost Alley |
| `phase-10/step-11` | buildprofiles | `Assets/GoldRush/Scenes/MainMenu.unity` | Build Profiles window, Scene List: MainMenu 0, Level1 1, Level2 2, Level3 3, Level4 4, Game 5, all ticked |
| `phase-10/step-12` | inspector (**manual**) | `ProjectSettings/Player` | Project Settings → Player: Company Name, Product Name Gold Rush, Version 1.0, Default Icon |
| `phase-10/step-13` | buildprofiles | `ProjectSettings/BuildProfiles/Windows` | Build Profiles window with Windows selected (active), the Scene List and the Build button |
| `phase-10/step-14-a` | buildprofiles | `ProjectSettings/BuildProfiles/Android` | Build Profiles window with Android selected (active) and the Build / Build And Run buttons |
| `phase-10/step-14-b` | inspector (**manual**) | `ProjectSettings/Player` | Player Settings for Android, Resolution and Presentation: Auto Rotation, only Landscape Left and Landscape Right ticked |
| `phase-10/step-15` | game | `GameUI/HUD` | Game view in Play mode: Level 2 – Ghost Alley at night with the HUD (0 / 8, two hearts, timer PAR 1:00, moon button) |
