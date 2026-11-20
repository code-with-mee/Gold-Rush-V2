# V2 Phase 1 notes: The main menu

Guide: `Docs/phase-01.html` (20 steps). Sources: `_old_order/phase-09.html` (menu), `_old_order/phase-01.html` (kit),
`Scripts/UI/GoldRushMenu.cs` (BuildWorld/BuildTitle), baked `Scenes/MainMenu.unity` (parsed: every RectTransform,
Image sprite/colour/type/PPU, Text font/size/alignment, effect, component in the guide tables is copied from it).

## Scripts in phase 1 (compile set)
| Path (Assets/GoldRush/Scripts/…) | Typed / imported | Source |
|---|---|---|
| Core/Tween.cs, Core/TweenRunner.cs | imported | Helpers/Core (real) |
| Core/Fader.cs | imported | Helpers/Core (real) |
| Core/GameAssets.cs | imported | **real file, must be ADDED to Helpers/Core** (see code changes) |
| Audio/AudioFX.cs | imported | **real file, must be ADDED to Helpers/Audio** |
| Audio/Synth.cs | imported | Helpers/Audio (real) |
| UI/UIKit.cs | imported | Helpers/UI (real) |
| UI/ButtonFX.cs | imported | Helpers/UI (real **+ code change 1**) |
| UI/LogoTitle.cs | imported | Helpers/UI (real) |
| UI/DayNightButton.cs | imported | Helpers/UI (real) |
| World/DayNight.cs, World/NightLight.cs, World/EnvironmentSetup.cs | imported | Helpers/World (real) |
| Core/Data.cs | typed | **PHASE 1 VERSION** (below) – replaced by the real file in phase 2 |
| UI/MenuAction.cs | typed | real file (25 lines) |
| UI/CoinFlip.cs | typed | real file |
| UI/Bob.cs | typed | real file **+ code change 2** (the guide shows the changed file) |
| UI/GoldRushMenu.cs | typed | **PHASE 1 VERSION** (157 lines, below) – replaced in phase 2 (import) and phase 10 (final) |

Dependency check done (grep of every class): UIKit→AudioFX,ButtonFX; ButtonFX→Tween,UIKit; AudioFX→Save,GameAssets,Synth;
GameAssets→EnemyType; LogoTitle→AudioFX,Tween,UIKit; DayNightButton→AudioFX,DayNight,Tween,UIKit; DayNight→Save,NightLight;
EnvironmentSetup→DayNight,GameAssets(+URP); Fader→UIKit; MenuAction→AudioFX,GoldRushMenu. Nothing else is needed.

### Phase versions
- `Core/Data.cs` phase 1 = real file minus `Scenes`, `LevelDef`, `Levels`, `GameSession`, `Rng`, `ShopItem`, `Catalog`;
  `Save.Fix()` only fixes `levels`/`owned` lists and the two free items. Same `SaveData` fields and same PlayerPrefs key,
  so saves carry over. Full text: in the guide step 4 (`p012/Data_p1.cs`).
- `UI/GoldRushMenu.cs` phase 1 = real file with: identical serialized fields (wiring survives the later replacements);
  Start without BuildWorld/BuildUI/Cosmetics/BindSettings/settingsRoot; RefreshTitle = gold only (null-guarded);
  Show null-guards levelScreen/shopScreen/topRight items and fades only the title; Run() only logs
  ("PLAY: you build the first level in phase 3." / "…this screen is built in phase 2."). No StartLevel. Full text in step 6.

## Code changes needed
1. **`UI/ButtonFX.cs`: find the glass parts by name when added by hand.** Hand-built buttons cannot easily set the
   private hover fields (halo, edge, disc, edgeGlow, sweep, content, chevron + their saved colours/offsets, group).
   Add to `Awake()` (after `button = GetComponent<Button>();`):
   ```csharp
   if (!face)   // added by hand in the editor: same part names as UIKit makes
   {
       Transform f = transform.Find("Face"); if (!f) f = transform.Find("Glass");
       if (f)
       {
           face = (RectTransform)f;
           SetupHover(transform.Find("Halo") ? transform.Find("Halo").GetComponent<Image>() : null,
               f.Find("Edge") ? f.Find("Edge").GetComponent<Image>() : null,
               f.Find("Sweep") as RectTransform,
               f.Find("IconDisc") ? f.Find("IconDisc").GetComponent<Image>() : null,
               transform.Find("EdgeGlow") ? transform.Find("EdgeGlow").GetComponent<Image>() : null,
               f.Find("Content") as RectTransform, f.Find("Chevron") as RectTransform);
       }
   }
   ```
   (Do not use `?.` on Unity objects.) Students set only **Flat** and **Base Color**. The guide is written as if this exists
   ("Button FX finds Halo, Edge, … by their names"). Note: on `Btn_Gear` (round chip) this also finds `Glass` + `Edge`;
   harmless (edge brightens on hover).
2. **`UI/Bob.cs`: self-init when added by hand.** Real `Bob` returns in `Update` while `rt == null` (it is only set by
   `Setup()`), so a hand-added Bob never moves. Change (exact text shown in the guide):
   `Setup` calls a new `Remember()` (rt, basePos, baseRot) and `void Awake() { if (!rt) Remember(); }`.
3. **Add to Helpers**: `GoldRush_Export/Resources/Scripts/Helpers/Audio/AudioFX.cs` and `Helpers/Core/GameAssets.cs`
   (copies of the real files). They are imported in phase 1 (UIKit needs AudioFX, AudioFX needs GameAssets).
   (Old order typed them in phase 5: the new phase 5 writer must NOT ask to create them again.)
4. **`DayNight.Create` should fire `Changed`** (suggestion): `DayNightButton.Start` may run before
   `GoldRushMenu.Start` creates DayNight → the button shows the sun at night. Fix: at the end of `DayNight.Create`
   add `Changed?.Invoke(d.target > 0.5f);` (or let DayNightButton refresh in its first Update). The guide's Common
   problems says "click it twice".
5. Screenshot tool: shots with `data-manual="true"` (Animator window) cannot be captured from the 5 standard windows.
6. The stage is built from **kit prefab instances** (the baked menu builds it from primitives with `LevelBuilder`).
   Group names are the same (`MenuDiorama/Floor|Walls|Obstacles|Lamps|Decor|Scenery|Dynamic`), but the children are the
   kit prefabs, so stage screenshots must come from a student-built reference scene, not from our baked MainMenu.
   UI names/paths match the baked scene exactly (`GoldRushMenu/MenuUI/Title/PLAYBtn/Face/Content/Sub`, …).
   Differences on purpose: student scene keeps the URP template Main Camera, has no `Scenery` towers/rim blocks and
   no floor glow strips (the Floor_40m + FloorPad_Cyan look is close enough under the night fog).

## Values used (from the baked scene / code)
- Camera start (-1.3, 1.48, -5.56), rot (7,0,0), FOV 40, near 0.1, far 400 (script then drives it: look (0,0.8,0),
  title distance 6.2, sway 10°·sin(0.22t)).
- Lantern: Spot, (-1.5,5.5,-3.5), rot (52.7, 23.2, 0) (= LookAt(0,0.5,0)), FFD18C, 20, range 14, angle 55, soft shadows.
- MenuPlayer rot Y 180; Model = Hero.fbx at (0,0.068,0) scale 0.337, Apply Root Motion off, Always Animate.
- Kit layout = the positions of `LevelBuilder.BuildMenuDiorama()` mapped to the closest kit prefab (table in step 10).
  LampLight + NightLight 2.2 / 2.2 / 2.
- Optional monsters (positions, model scales, glow lights) from the baked scene; they are in T/rest pose until phase 7.
- UI: every value in steps 12–17 = baked RectTransforms/Images/Texts (colours as hex + alpha 0–255, e.g. 0.38 → 97).

## Decisions other phases rely on
- **Hero.controller is created in phase 1** (`Assets/GoldRush/Animations/Hero.controller`): trigger `Win`; states
  `Idle` (default, clip Idle) and `Win` (clip Wave); Any State→Win (no exit time, 0.1 s, can-transition-to-self off,
  condition Win); Win→Idle (exit time 3, 0.2 s). Hero.fbx: Rig Generic, Idle + Wave Loop Time/Loop Pose.
  **Phase 4** must extend this same file: add float `Speed`, triggers `Attack`, `Hit`, `Die`; replace the `Idle` state by
  the `Locomotion` blend tree (Idle 0 / Walk 0.5 / Run 1, default state); re-point Win→Locomotion; add Attack (Punch,
  exit 0.85), Hit (HitReact, 0.85), Die (Death, no way back) like `Editor/CharacterSetup.cs`; loop Walk/Run.
- **GoldRushAssets.asset is created in phase 1** in `Assets/GoldRush/Resources/` (hero model + controller, lit/emissive/
  transparent/particle materials, skybox GR_SunsetSky, 16 sounds). Phase 4/7 only fill the remaining fields
  (hero walk/run speeds if used, slime/ghost/mushroomKing).
- UI prefabs in `Assets/GoldRush/Prefabs/UI/`: `GlassCircle` (76, circle), `GlassPill` (200×76, PPU 1.684),
  `GlassButton` (470×90 blue LEVELS, PPU 1.173, with Button/ButtonFX/MenuAction/CanvasGroup). Phase 2 adds `ChipButton`.
  Phase 9 (HUD, dialogs) can reuse them (HUD chips = GlassPill/GlassCircle; dialog footer buttons = GlassButton).
- **Phase 7** should add the three menu monsters (MenuPatroller/MenuChaser/MenuGuard + Model + Glow + Badge) if the
  student skipped the optional step, and put the Glow lights in GoldRushMenu → Menu Glows.
- The UI timing shows: PLAY etc. only log in phase 1.

## Screenshots (data-shot ids)
| id | window | select |
|---|---|---|
| phase-01/step-03 | project | Assets/GoldRush/Scripts/UI |
| phase-01/step-06 | project | Assets/GoldRush/Scripts/UI/GoldRushMenu.cs |
| phase-01/step-07 | inspector | Assets/GoldRush/Resources/GoldRushAssets.asset |
| phase-01/step-08-a | inspector | Assets/GoldRush/Art/Characters/Hero/Hero.fbx (Animation tab) |
| phase-01/step-08-b | inspector (**manual**: Animator window) | Assets/GoldRush/Animations/Hero.controller |
| phase-01/step-08-c | inspector | Assets/GoldRush/Resources/GoldRushAssets.asset |
| phase-01/step-09-a | inspector | Main Camera |
| phase-01/step-09-b | inspector | GoldRushMenu |
| phase-01/step-10-a | hierarchy | MenuDiorama |
| phase-01/step-10-b | inspector | MenuDiorama/Lamps/LampPost/LampLight |
| phase-01/step-10-c | scene | MenuDiorama |
| phase-01/step-11-a | inspector | MenuPlayer/Model |
| phase-01/step-11-b | inspector | Lantern |
| phase-01/step-12-a | inspector | GoldRushMenu/MenuUI |
| phase-01/step-12-b | inspector | GoldRushMenu/MenuUI/Title/BackdropTop |
| phase-01/step-13-a | hierarchy | GoldRushMenu/MenuUI/Title/Btn_Gear |
| phase-01/step-13-b | inspector | GoldRushMenu/MenuUI/Title/Btn_Gear |
| phase-01/step-13-c | inspector | GoldRushMenu/MenuUI/Title/DayNight |
| phase-01/step-14-a | hierarchy | GoldRushMenu/MenuUI/Title/Gold |
| phase-01/step-14-b | inspector | GoldRushMenu/MenuUI/Title/Gold/Label |
| phase-01/step-14-c | game | GoldRushMenu/MenuUI/Title |
| phase-01/step-15 | inspector | GoldRushMenu/MenuUI/Title/LogoGlow |
| phase-01/step-16-a | hierarchy | GoldRushMenu/MenuUI/Title/LEVELSBtn |
| phase-01/step-16-b | inspector | GoldRushMenu/MenuUI/Title/LEVELSBtn |
| phase-01/step-16-c | inspector | GoldRushMenu/MenuUI/Title/LEVELSBtn/Face |
| phase-01/step-17-a | hierarchy | GoldRushMenu/MenuUI/Title |
| phase-01/step-17-b | inspector | GoldRushMenu/MenuUI/Title/PLAYBtn |
| phase-01/step-17-c | scene | GoldRushMenu/MenuUI/Title |
| phase-01/step-18 | inspector | GoldRushMenu |
| phase-01/step-19 | game | GoldRushMenu (play mode) |
Reference image (real): `images/01_title.jpg` (step 1).
