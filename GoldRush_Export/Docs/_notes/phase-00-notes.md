# Phase 0 notes: Create the project

## Code changes needed
None for phase 0 itself. Things the code owner / pack builder must know:

1. **SPEC folder layout vs. the code (icons and fonts).** SPEC puts icons and fonts in `Art/UI/`, but
   `Scripts/UI/UIKit.cs` loads them with `Resources.Load<Sprite>("Icons/" + file)` (line ~351) and
   `Resources.Load<Font>("Fonts/" + name)` (line ~164), exactly like the repo (`Assets/GoldRush/Resources/Icons`,
   `Assets/GoldRush/Resources/Fonts`). The phase 0 guide therefore imports them into
   `Assets/GoldRush/Resources/Icons` and `Assets/GoldRush/Resources/Fonts` (shape PNGs still go to `Art/UI`).
   **No code change if the SPEC folder tree is updated** to include `Resources/Icons` and `Resources/Fonts`.
   (Alternative: change UIKit to take icon/font references from GameAssets. Not recommended: more work for students.)
   `Resources/` will also hold `GoldRushAssets.asset` (GameAssets.I loads `Resources.Load<GameAssets>("GoldRushAssets")`)
   and `DayNightPalette.asset` (phase 10).
2. **Starter package.** (User decision.) Phase 0 now imports `GoldRush_Phase00_Project.unitypackage` (Assets →
   Import Package → Custom Package) instead of drag-and-dropping the Resources folders. It must contain everything
   listed in `phase-01-notes.md` ("Starter package contents"): Art, Resources/Icons + Fonts (icons already imported
   as Sprite, with their .meta files), Materials (M_*, GR_*), Prefabs/Level/*, Prefabs/Gameplay/* (visuals only,
   no scripts). Students create only Animations/, Scenes/ and Scripts/*. The Sandbox scene uses `Floor_40m`.
   The final checkpoint of phase 0 has the same name as the starter package (starter + Sandbox scene).
   Content the guide assumes:
   - `Characters/Monsters/Atlas_Monsters.png` must be in the pack (the monster FBX files use it). The guide says
     "the monster texture"; folder structure `Characters/Hero/`, `Characters/Monsters/`, `Characters/Licenses/`
     (same as `Assets/GoldRush/Art/Characters`).
   - `Textures/grid.png`: an export of `Mats.ProcTex.Grid` (`Core/Mats.cs`, 256 px, one tile per repeat). The
     guides no longer reference the file by name (M_Floor already uses it).
   - `UI/Icons/*.png` (+ licence), `UI/Fonts/Poppins-*.ttf` (+ OFL), shape PNGs at `UI/` top level.
3. The editor script `UIImportRules.cs` is not shipped: the icon import settings (Sprite (2D and UI), Single,
   Alpha Is Transparency, mipmaps, Uncompressed, Bilinear) must be baked into the icon .meta files in the package.
   Prefabs in the package must not reference any script (the package is imported before any script exists),
   so: no NightLight, DayNightTint, ExitGate, Coin, PowerUp components.
4. `GoldRushSetup` also sets landscape-only auto-rotation in Player Settings. Not in phase 0: **phase 10 (build)**
   should do it (Default Orientation = Auto Rotation, only Landscape Left/Right allowed).
5. Active Input Handling = **Both** is required: PlayerMotor, GameManager, ThirdPersonCamera, GoldRushMenu,
   DayNightButton and LogoTitle use `UnityEngine.Input` (old API); the UI uses the Input System package.

## Phase versions of scripts
None (no scripts in phase 0).

## Decisions other phases rely on
- Test scene: `Assets/GoldRush/Scenes/Sandbox.unity` (Main Camera, Directional Light, Floor_40m, Hero.fbx instance).
  Phase 1 reuses it. It is not in the real game; phase 9/10 can tell students to delete it or leave it out of the
  build list.
- The `Assets/Scenes/SampleScene` and `TutorialInfo`/`Readme` from the template are deleted. `Assets/Settings`
  (URP assets) is kept.
- Hero model import settings are NOT touched in phase 0 (phase 3 does Rig = Generic, loops, etc.).
- Folders from the package: Art/{Characters,Audio,UI,Textures}, Materials, Prefabs/{Level,Gameplay},
  Resources/{Icons,Fonts}. Created by students (step 8): Animations, Scenes, Scripts/{Core,Audio,World,Level,Gameplay,UI}.

## Screenshot placeholders
1. Unity Hub, New project, Universal 3D template selected, name GoldRush
2. Package Manager, Unity Registry, AI Navigation selected, Install button
3. Project Settings → Player → Other Settings, Active Input Handling = Both
4. Project window with the full Assets/GoldRush folder tree open
5. Scene view with the hero model standing on the grid floor
