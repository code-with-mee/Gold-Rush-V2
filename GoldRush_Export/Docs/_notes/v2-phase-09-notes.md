# V2 Phase 9 notes: Gameplay UI

Guide: `phase-09.html` (20 steps). Students copy the real GameUI + the files the real GameManager needs, type GameAction,
then build the whole `GameUI` canvas of `Level1` by hand (HUD, intro, countdown, PAUSE / CAUGHT / LEVEL COMPLETE) and wire
every GameUI field. Touch areas (LookArea / MoveArea) are left for phase 10 (GameUI → Joystick stays empty here).

Sources (read, not edited): `Scripts/UI/GameUI.cs` (fields, Bind, BuildAll/Build/BuildIntro/BuildHint/BuildPause/BuildWin/
BuildLose, Show*), `Scripts/UI/GameAction.cs`, `Scripts/Gameplay/GameManager.cs`, `Scripts/UI/UIKit.cs` (ChipButton, Glass,
Fit, colours), `Scripts/UI/DayNightButton.cs`, `Scripts/UI/WorldMarkers.cs`, and the baked `Scenes/Level1.unity` root
`GameUI` (parsed with a YAML script: every RectTransform, Image sprite/colour/type/PPU, Text font/size/alignment/best fit,
effect, CanvasGroup and component value in the tables comes from it; dump in scratchpad `p910/gameui_tree.txt`).
Reference images: `images/04_level1_start.jpg`, `09_pause.jpg`, `07_win.jpg`, `10_lose.jpg` (no `Temp/gr_smoke/` folder exists).
Old order reused: phase-05/06 HUD/dialog wording, phase-09/10 "Common problems" style.

## Scripts in phase 9
| Path (`Assets/GoldRush/Scripts/…`) | Typed / imported | Source |
|---|---|---|
| `UI/GameUI.cs` | imported (new) | `Resources/Scripts/Helpers/UI` (real) |
| `UI/GameAction.cs` | **typed** (25 lines, page text = real file, diffed) | real |
| `UI/VirtualJoystick.cs`, `UI/TouchLookArea.cs` | imported (replace the phase-4 stubs) | `Helpers/UI` (real). GameUI needs `Release()` / `Create()` |
| `World/LevelBuilder.cs`, `World/Props.cs`, `World/DayNightTint.cs`, `World/DayNightPalette.cs` | imported (new) | `Helpers/World` (real) |
| `World/Coin.cs`, `World/ExitGate.cs` | imported (replace phase versions) | `Helpers/World` (real) |
| `World/PowerUp.cs` | imported (replace phase version) | `Final/World` (real; not in Helpers) |
| `Gameplay/GameManager.cs` | imported (replace phase 8 version) | `Final/Gameplay` (real, 501 lines → import rule) |

**No phase versions are left after phase 9 except `UI/GoldRushMenu.cs` (phase 2 version, replaced in phase 10).**
Serialized field names of the replaced Coin / ExitGate / PowerUp are identical to the phase versions (checked), so the
students' prefab values survive.

Compile check: Roslyn against the Unity 6000.6.10f1 DLLs (scratchpad `p58/cc.py`): phase-1 helpers + Data + LogoTitle +
MenuAction + Phase02 GoldRushMenu + PlayerSpawn + the phase-8 page set (Badge, CoinSpot, LevelScene, MonsterSpawn, PowerUpSpot,
PlayerMotor and ThirdPersonCamera = real files) + phase-7/8 helpers + all files above → **0 errors**. Not run in Unity.
Between step 2 and step 3 there is one expected error (GameAction missing) – the page says so.

## Hierarchy built (same names as our scene unless listed)
`GameUI` (Canvas Overlay sort 10, Scaler 1920×1080 match 0.5, GameUI) → `WorldMarkers`, `HUD` → `CoinPill`, `Hearts`, `Stamina`,
`Timer`, `DayNight`, `Pause`, `Power0..2`, `Arrow`, `Toast`, `Hint`; `Flash`; `Modal` → `Intro`, `Countdown`, `Pause`, `Win`, `Lose`.
Phase 10 inserts `LookArea (right half)` and `MoveArea (left half)` as the first two children.
Glass chips reuse the phase-1 prefabs `GlassPill` / `GlassCircle` (PPU retyped = 128 ÷ height, Tint alpha 128 where ours is 0.5)
and the phase-2 `ChipButton` (toggles 76 high PPU 1.389, footer 92 high PPU 1.148). New student prefabs: `Prefabs/UI/CoinIcon`
(54 px coin) and `Prefabs/UI/StatCard` (Bg, Edge, Caption, Value – no icon; icon added per card).

Differences from our baked scene (on purpose, visually identical or invisible):
- Name clashes renamed so screenshot paths are unique: `Timer/ParLabel` (ours: second `Label`), `Toast/DropShadow`, `Toast/Title`,
  `Toast/Sub` (ours: `Shadow`, `Label`, `Label`).
- 40 px coins (Arrow, stat cards) = the 54 px `CoinIcon` prefab at Scale 0.74 (ours: 40 px with scaled insets).
- StatCard `Caption` / `Value` use top-/bottom-stretch anchors (Left 68 Right 12 / Left 20 Right 10) instead of fixed widths
  (w − 80 / w − 30): same result, and one prefab fits both card widths (222.667 and 249.333).
- HUD child order: CoinPill, Hearts, Stamina, Timer, DayNight, Pause, Power0-2, Arrow, Toast, Hint (ours: Timer, Pause, DayNight,
  Power*, Stamina…). No overlaps, so no visual change.
- HUD `Coin` is a `CoinIcon` prefab instance with an added CoinFlip.
- Toast glass layers come from an unpacked GlassPill (same values).

## Code changes needed
1. **`Scripts/UI/GameUI.cs` → `HideHud()` must accept an empty Joystick field (BLOCKER for phase 9).** In phase 9 the
   joystick is not placed yet (phase 10), so `Joystick.Release()` throws a NullReferenceException at the start of `ShowWin` /
   `ShowLose` and the dialogs never appear. Change the last two lines of `HideHud()` to:
   ```csharp
   if (joystick) { joystick.Release(); joystick.gameObject.SetActive(false); }
   ```
   (Same file in `Helpers/UI/` and `Final/UI/`.) Everything else in GameUI is null-safe for this setup, or the field is filled.
2. **New package `GoldRush_Export/Packages/GoldRush_GameUIDialogs.unitypackage`** (optional shortcut, step 13 tip): export from
   our Level1 `GameUI/Modal/Pause`, `Win`, `Lose` as prefabs `Assets/GoldRush/Prefabs/UI/Dialog_Pause.prefab`,
   `Dialog_Win.prefab`, `Dialog_Lose.prefab` (inactive roots, same children as the scene) + the Fix Script Links editor tool
   below. Rename the duplicate-name children the same way as the guide if you can (not required).
3. **New editor tool `Assets/GoldRush/Editor/FixScriptLinks.cs`** (menu **Gold Rush → Fix Script Links**), shipped inside the
   two "our assets" packages (`GoldRush_GameUIDialogs`, `GoldRush_Levels2to4`). Why: students create or drag in every script
   themselves, so their `.cs.meta` GUIDs differ from the repo GUIDs; our prefabs/scenes reference the repo GUIDs → every
   GoldRush component shows "Missing (Mono Script)" after import. The tool must:
   - contain a table *repo GUID → class name* for all runtime scripts in `Assets/GoldRush/Scripts` (generate it from the metas);
   - for every `.unity` / `.prefab` under `Assets/GoldRush` (text YAML), replace `m_Script: {fileID: 11500000, guid: <repoGuid>, type: 3}`
     where `AssetDatabase.GUIDToAssetPath(repoGuid)` is empty by the GUID of the student's `MonoScript` with the same class
     (`AssetDatabase.FindAssets("t:MonoScript")` → `GetClass()` with namespace `GoldRush`), write the file, `AssetDatabase.Refresh()`,
     log "Fixed N script links in M files". No-op for students who started from a checkpoint package (GUIDs already match).
   - Packages must NOT contain the `.cs` files themselves (they would import as `GameManager 1.cs` → duplicate classes).
4. Checkpoint `GoldRush_Phase09_GameUI.unitypackage`: Level1 with the hand-built GameUI (names as in this guide), the two new
   prefabs, all real scripts above, Phase02 GoldRushMenu still in place.

## Notes for other writers / the code owner
- Promise from phase 3 kept: PAUSE → MENU = `GameManager.BackToMenu()` → `Fader.Load(Scenes.Menu)`.
- NEXT after Level 1 loads `Level2`, which is not in the build until phase 10 → the page warns "don't click NEXT yet".
- The HUD day/night button makes **N** work in levels from phase 9 (DayNightButton.Update). Sky/fog/sun change; material tint +
  lamp NightLights come in phase 10.
- `GameManager.LevelFromScene` already supports hand-placed PowerUp prefabs (phase 5–8 code change 2 is in the repo), and
  `PowerUp.cs` already has `[SerializeField] PowerType type` + Start() part lookup (code change 1 is in the repo).
- TEMPLATE sidebar: already V2 titles; used as is.

## Screenshots
Existing images used: `images/04_level1_start.jpg`, `09_pause.jpg`, `07_win.jpg`, `10_lose.jpg` (step 1 `.shots`).
Placeholders (`figure.shot.todo`, SPEC markup). New window type used: `console` (step 3). "edit mode, X ticked" = tick that
inactive dialog only for the picture.

| data-shot | window | data-select | shows |
|---|---|---|---|
| `phase-09/step-02` | project | `Assets/GoldRush/Scripts/UI/GameUI.cs` | Project window: Scripts/UI with GameUI, TouchLookArea, VirtualJoystick (and the menu scripts) |
| `phase-09/step-03` | console | `Assets/GoldRush/Scripts/UI/GameAction.cs` | Console window: empty, no red errors after typing GameAction |
| `phase-09/step-05-a` | hierarchy | `GameUI` | Hierarchy: GameUI → WorldMarkers, HUD, Flash, Modal; EventSystem at the root |
| `phase-09/step-05-b` | inspector | `GameUI` | Inspector of GameUI: Canvas (Overlay, Sort Order 10), Canvas Scaler (1920 x 1080, Match 0.5), Graphic Raycaster, Game UI (fields empty) |
| `phase-09/step-05-c` | inspector | `GameUI/WorldMarkers` | Inspector of WorldMarkers: stretch 0, World Markers (Root = WorldMarkers) |
| `phase-09/step-06-a` | hierarchy | `GameUI/HUD/CoinPill` | Hierarchy: CoinPill (blue) → Shadow, Tint, Glass → Edge, Coin (blue) → Rim, Body, Inner, Shine, Label |
| `phase-09/step-06-b` | inspector | `GameUI/HUD/CoinPill/Label` | Inspector of CoinPill/Label: stretch L76 R8, Text "0 / 0", Poppins-Bold 38, Middle Left, Rich Text, Outline |
| `phase-09/step-06-c` | game | `GameUI/HUD/CoinPill` | Game view (edit mode), top-left corner: dark glass pill with the gold coin and "0 / 0" |
| `phase-09/step-07-a` | inspector | `GameUI/HUD/Hearts/Heart1` | Inspector of Heart1: middle left, pivot 0/0.5, Pos 48, 0, 40 x 40, Image heart red |
| `phase-09/step-07-b` | inspector | `GameUI/HUD/Stamina` | Inspector of Stamina: top left, 40, -158, 150 x 24, Canvas Group (Alpha 0) |
| `phase-09/step-08-a` | inspector | `GameUI/HUD/Timer` | Inspector of Timer: top right, pivot 1/1, Pos -200, -28, 188 x 70 |
| `phase-09/step-08-b` | inspector | `GameUI/HUD/Timer/ParLabel` | Inspector of Timer/ParLabel: bottom centre, pivot 0/0.5, Pos -32, 16, 120 x 22, Text PAR 0:40 SemiBold 18 gold |
| `phase-09/step-09-a` | inspector | `GameUI/HUD/DayNight` | Inspector of DayNight: top right, -116, -28, 70 x 70, Button (Target Graphic Glass), Day Night Button (Sun = Icon_Sun, Moon = Icon_Moon) |
| `phase-09/step-09-b` | hierarchy | `GameUI/HUD/Pause` | Hierarchy: Pause (blue) → Shadow, Tint, Glass → Edge, Icon_Pause → Glyph |
| `phase-09/step-09-c` | game | `GameUI/HUD` | Game view (edit mode), top-right corner: timer pill 0:00.0 with ★ PAR 0:40, sun button, pause button |
| `phase-09/step-10-a` | inspector | `GameUI/HUD/Power0/Fill` | Inspector of Power0/Fill: stretch 5/5/5/5, pivot 0/0.5, Image pill white, Sliced, PPU 4.267 |
| `phase-09/step-10-b` | hierarchy | `GameUI/HUD/Power2` | Hierarchy: HUD → CoinPill, Hearts, Stamina, Timer, DayNight, Pause, Power0, Power1, Power2 (greyed out) |
| `phase-09/step-11-a` | hierarchy | `GameUI/HUD/Toast` | Hierarchy: Toast → DropShadow, Shadow, Tint, Glass → Edge, Title, Sub; Arrow → Tri → Shape, Bg, CoinIcon; Hint (greyed out) → layers + Label |
| `phase-09/step-11-b` | inspector | `GameUI/HUD/Toast` | Inspector of Toast: anchors 0.5/0.78, 640 x 96, Canvas Group (Alpha 0, Blocks Raycasts off) |
| `phase-09/step-11-c` | inspector | `GameUI/HUD/Hint/Label` | Inspector of Hint/Label: stretch 0, the WASD text with colour tags, Poppins-SemiBold 24, Middle Center, Rich Text |
| `phase-09/step-12-a` | game | `GameUI/Modal/Intro` | Game view (edit mode, Intro ticked for the picture): dark band across the screen with LEVEL 1, TRAINING YARD, Collect all gold… |
| `phase-09/step-12-b` | inspector | `GameUI/Modal/Countdown` | Inspector of Countdown: middle centre, 800 x 260, Text "3", Poppins-ExtraBold 220, gold, Outline (5, -5) |
| `phase-09/step-13-a` | hierarchy | `GameUI/Modal/Pause/Panel/Glass` | Hierarchy: Modal → Intro, Countdown, Pause → Dim, Panel → Shadow, Tint, Glass → Edge, Badge (Ring, Icon_Pause/Glyph), Title, Subtitle, Divider, Divider |
| `phase-09/step-13-b` | inspector | `GameUI/Modal/Pause/Dim` | Inspector of Pause/Dim: stretch 0, Image without sprite, navy alpha 166, Raycast Target ON |
| `phase-09/step-13-c` | inspector | `GameUI/Modal/Pause/Panel/Glass/Title` | Inspector of Title: top left, 148, -18, 592 x 60, Text PAUSED, Poppins-Bold 50, Lower Left, Shadow (0, -2.083) |
| `phase-09/step-14-a` | inspector | `GameUI/Modal/Pause/Panel/Glass/Stat_GOLD/Value` | Inspector of Stat_GOLD/Value: bottom stretch, pivot 0/0, Left 20, Right 10, Pos Y 12, Height 56, Poppins-Bold 40 gold, Best Fit 20–40, Shadow |
| `phase-09/step-14-b` | game | `GameUI/Modal/Pause` | Game view (edit mode, Pause ticked): the PAUSED header and three cards GOLD 0 / 0, TIME 0:00.0, LIVES 2 / 2 |
| `phase-09/step-15-a` | inspector | `GameUI/Modal/Pause/Panel/Glass/RESUME` | Inspector of RESUME: top left, 476.849, -408, 263.152 x 92, Button, Button FX (Flat, green), Canvas Group, Game Action (resume) |
| `phase-09/step-15-b` | inspector | `GameUI/Modal/Pause/Panel/Glass/MUSIC  ONBtn` | Inspector of MUSIC ONBtn: top centre, pivot 0.5/1, -150, -286, 260 x 76, Button FX green, no Game Action |
| `phase-09/step-15-c` | game | `GameUI/Modal/Pause` | Game view (edit mode, Pause ticked): the full PAUSED dialog like the real one: cards, MUSIC ON / SOUND ON, MENU, RETRY, RESUME |
| `phase-09/step-16-a` | hierarchy | `GameUI/Modal/Lose/Panel/Glass` | Hierarchy: Lose → Dim, Panel → Shadow, Tint, Glass → Edge, Badge, Title, Subtitle, Divider ×2, Stat_COLLECTED, Stat_TIME, Stat_BANKED, MENU, RETRY, Tip |
| `phase-09/step-16-b` | game | `GameUI/Modal/Lose` | Game view (edit mode, Lose ticked): CAUGHT! with red badge, COLLECTED / BANKED / TIME cards, cyan tip box, MENU and a wide green RETRY |
| `phase-09/step-17-a` | inspector | `GameUI/Modal/Win/Panel/Glass/Stars/Back0` | Inspector of Stars/Back0: middle centre, -160, -6, 118 x 118, Rotation Z 12, Image star white alpha 26 |
| `phase-09/step-17-b` | game | `GameUI/Modal/Win` | Game view (edit mode, Win ticked): LEVEL COMPLETE! with green badge, three gold stars, three rules, TIME / BEST / GOLD EARNED cards, MENU, RETRY, NEXT |
| `phase-09/step-17-c` | hierarchy | `GameUI/Modal` | Hierarchy: Modal → Intro, Countdown, Pause, Win, Lose (all greyed out) |
| `phase-09/step-18-a` | inspector | `GameUI` | Inspector of GameUI (locked): Layers and HUD fields all filled, Joystick empty, Power Rows with 3 elements |
| `phase-09/step-18-b` | inspector | `GameUI` | Inspector of GameUI, Win Dialog opened: Root, Panel, Badge, Title, Subtitle, Values 3, Captions 3, Stars 3, Crit Stars 3, Crit Texts 3, Buttons 3, Weights 1/1/1.3, Width 860, Height 740 |
| `phase-09/step-19-a` | game | `GameUI/HUD` | Game view in Play mode: Level 1 with the full HUD, hint at the bottom, arrow to a coin |
| `phase-09/step-19-b` | game | `GameUI/Modal/Pause` | Game view in Play mode after Esc: the PAUSED dialog over the dimmed level, values filled in (TRAINING YARD, 2 / 6, time, 2 / 2) |
