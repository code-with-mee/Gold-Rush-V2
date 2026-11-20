# Phase 9 notes: Menu, levels, shop, save

Guide: `phase-09.html` (19 steps). It matches the finished **scene-based menu** (GoldRushMenu.cs
with serialized screens + MenuAction). Students build the Canvas and the four screens by hand; level cards, shop tabs and
shop items are generated at runtime. In the hand-built scene the 3D stage is left to `BuildWorld()` (see change 1).

## Code changes needed

The refactored `GoldRushMenu.cs` / `MenuAction.cs` are now used as-is (fields, actions `play`, `levels`, `shop`,
`back`, `settings`, `closeSettings`, tabs built in `shopTabs`, toggles bound with `GameUI.BindToggle`,
sprites from `Resources/UISprites/`). The earlier requests about field names and extra actions are dropped.

### 1. `Scripts/UI/GoldRushMenu.cs`: support a HAND-BUILT scene (blocker)
`Start()` runs `if (!baked) { BuildWorld(); BuildUI(); }`, and `baked` is `[HideInInspector]` and only set by
`BuildForEditor()` (MenuBaker). A student's hand-built scene has `baked == false`, so the code builds a second
Canvas on top of theirs and overwrites every field. The guide (step 14) tells students to fill the Screens / runtime
fields and **leave the Stage fields empty**. Please change it to build each part only when it is missing:
```csharp
if (!cam) BuildWorld();        // stage: diorama, hero, monsters, camera, lantern (students leave these empty)
if (!titleScreen) BuildUI();   // screens: students build these by hand
```
(or equivalent). The baked scene still works because both are set there.

### 2. `Scripts/UI/LogoTitle.cs`: build itself when placed in a scene
Students add **LogoTitle** to an empty `Logo` RectTransform (1240×200) and leave it empty. Please add an
`Awake()` (or a serialized `fontSize = 176`) that runs the existing build code when `layers` is null/empty,
so `LogoTitle.Build(...)` is not required from code. (Also guard `Update` if `layers` is null.)
If the baked scene already saves the layers, this only matters for the hand-built scene.

### 3. Resources pack
- `GoldRush_Export/Resources/Scripts/Helpers/` must contain `GoldRushMenu.cs`, `LogoTitle.cs`, `CoinFlip.cs`, `Bob.cs`
  (final versions). ButtonFX is assumed to come with UIKit in phase 5.
- Students need `Assets/GoldRush/Resources/UISprites/*.png` (pill, rr, circle, pillo, rro2, ring2, soft, fadeD/R/U,
  star, heart) with their import settings (Sprite, borders). Please ship them in the phase 0 Resources import or
  the phase 5/9 package.
- **Optional shortcut in step 15:** `GoldRush_Export/Resources/Scenes/GoldRush_Levels2to4.unitypackage`
  with `Level2.unity`, `Level3.unity`, `Level4.unity` from this repo (and their `Materials/Level` dependencies).

### Hand-built scene: what the guide uses
- Required fields: canvasRt, titleScreen, levelScreen, shopScreen, settingsRoot, goldPill, goldText, titleGoldText,
  starsText, starsBar, levelCards, shopTabs, shopGrid (null → NullReferenceException in Start/RefreshTitle/Show).
- Recommended: settingsPanel, topRight = {GoldPill, TopGear} (+ DayNight in phase 10), entrance = {Play, Levels, Shop},
  playSubtitle, musicToggle, soundToggle.
- Title, Levels, Shop and Settings each have a CanvasGroup (Show/ShowSettings read it). Settings is the last child.
- The Title gold/stars chips are simplified (pill Image + texts; no Glass/Edge layers); coin icon = `circle.png` gold.

## Assumptions about other phases (please check when merging)
- `Data.cs` already exists from an earlier phase (GameManager/AudioFX use `Save`, `Levels`, `GameSession`).
  Step 3 says "replace the whole file" with the full real file (228 lines, pasted).
- `Fader.cs` may already exist from phase 6 (Restart). Step 4 says "check it matches, or create it".
- Phase 1 material names `M_Floor`, `M_Wall`, `M_Block` (used in phase 10 palette table).
- Phase 2 Level1 root has `LevelScene`, `NavMeshSurface`, markers as in the repo (`CoinSpot`, `MonsterSpawn`, `PowerUpSpot`, `PlayerSpawn`, `ExitGate`).

## Values used (from the real code, GoldRushMenu.BuildUI)
- Canvas: Overlay, sort order 10, Scale With Screen Size 1920×1080, match 0.5 (`UIKit.Canvas`).
- Title buttons: column x=40, width 470, PLAY 140 high at y=272, LEVELS 90 at y=166, SHOP 90 at y=60 (bottom-left).
- Title cluster (top-left, y=-30, 76 high): gear (32), day/night in phase 10 (122), gold chip 200 wide (212), stars chip 200 wide (426).
- Top-right bar: GoldPill (-40,-34) 300×100, day/night (-362,-34) 100, gear (-484,-34) 100.
- Logo (0,-228) 1240×200 size 176; tagline (0,-350) 480×46 size 24; backdrops fadeD 420 high / fadeR 820 wide, navy α0.6.
- Levels: header (0,-40) 900×80 size 60; subheader (0,-118); back (32,-34) 200×80; Cards (0,-50) 5×300+4×28 = 1612 × 440.
- Shop: panel right-middle (-150,-40) 900×880 (rr, ppu 0.8); Tabs box (0,-150) 560×76; Grid (0,-250) 820×610.
- Settings: panel 600×440; header (0,36) 380×96; music y=285, sound y=180 (260×76); close y=66 (240×86, closeSettings).
- Level table (step 15) from `Levels.All` + the real scenes: arenaHalf 20/24/28, coins 8/10/13, monsters and power-ups as in Level2–4.unity.
- Build scene order from `EditorBuildSettings.asset`: MainMenu, Level1–4, Game.

## Screenshots (placeholders)
1. Hierarchy of MainMenu with GoldRushMenu → MenuUI → Title, Levels, Shop, Settings, GoldPill, TopGear + EventSystem
2. Inspector of the Play button with Button, Menu Action (action = play) and Button FX (Flat ticked)
3. Inspector of GoldRushMenu with the Screens and Filled in at runtime fields filled, Stage fields empty
4. Scene view of Level2 from above, with the yellow arena box, coin spots and monster markers
5. Build Profiles → Scene List with MainMenu, Level1–4, Game ticked
