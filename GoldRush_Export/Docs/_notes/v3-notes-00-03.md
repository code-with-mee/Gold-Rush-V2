# V3 notes: phases 0–3 (package-driven)

Pages rewritten: `phase-00.html`, `phase-01.html`, `phase-02.html`, `phase-03.html`.
Format (SPEC "VERSION 3"): What you get · Import · Check (one screenshot per check) · How it works · ▶ Press Play · Try it · Common problems · pager.
Phase 0 keeps its project-setup steps (Hub, template clean-up, AI Navigation, Input = Both) before Import / Check.
Sidebar = TEMPLATE.html, with `aria-current="page"` on the page's own link.

## Screenshot placeholders (to capture)
All `data-select` values were checked against `Scenes/MainMenu.unity`, `Scenes/Level1.unity` and the `Assets/` tree.
Markup: `<figure class="shot todo" data-shot=… data-window=… data-select=…>`.

| data-shot | window | data-select | What it must show |
|---|---|---|---|
| phase-00/check-01 | project | `Assets/GoldRush` | Folder tree: Art, Materials, Prefabs, Resources, Scenes, Scripts |
| phase-00/check-02 | inspector | `Assets/GoldRush/Art/Characters/Hero/Hero.fbx` | Animation tab, clip list (Idle, Run, Walk, Wave, …) |
| phase-00/check-03 | project | `Assets/GoldRush/Prefabs/Level` | The 15 kit prefabs |
| phase-00/check-04 | project | `Assets/GoldRush/Resources/UISprites` | The 15 UI shapes |
| phase-00/check-05 | project | `Assets/GoldRush/Art/Audio` | The 16 .wav files |
| phase-01/check-01 | hierarchy | `GoldRushMenu` | 9 root objects, MenuUI under GoldRushMenu |
| phase-01/check-02 | scene | `MenuDiorama` | The menu stage |
| phase-01/check-06 | hierarchy | `GoldRushMenu/MenuUI/Title` | Title children (BackdropTop … PLAYBtn, LEVELSBtn, SHOPBtn, Label) |
| phase-01/check-07 | inspector | `GoldRushMenu/MenuUI/Title/PLAYBtn` | Button, Button FX, Canvas Group, Menu Action = play |
| phase-01/check-08 | inspector | `GoldRushMenu` | Gold Rush Menu fields, all filled |
| phase-02/check-01 | hierarchy | `GoldRushMenu/MenuUI/Levels` | Dim, Header, SubHeader, BACKBtn, Cards (empty) |
| phase-02/check-02 | inspector | `GoldRushMenu/MenuUI/Levels/BACKBtn` | Menu Action = back |
| phase-02/check-03 | hierarchy | `GoldRushMenu/MenuUI/Shop/ShopPanel` | Glass → Badge, Title, Subtitle, Divider, Tabs, Grid |
| phase-02/check-04 | hierarchy | `GoldRushMenu/MenuUI/Settings/Panel` | Settings (inactive, grey) → Dim, Panel/Face → Ribbon, MUSIC/SOUND ONBtn, CLOSEBtn |
| phase-02/check-05 | inspector | `GoldRushMenu/MenuUI/Settings/Panel/Face/CLOSEBtn` | Menu Action = closeSettings |
| phase-02/check-07 | inspector | `Assets/GoldRush/Scripts/Core/Data.cs` | Code preview (Levels.All) |
| phase-03/check-01 | project | `Assets/GoldRush/Prefabs/Level` | The 15 kit prefabs (same view as phase-00/check-03 is fine) |
| phase-03/check-02 | hierarchy | `Level 1 - Training Yard` | Static (Floor, Walls, Structures, Obstacles, Lamps, ExitGate), Decor, Scenery |
| phase-03/check-03 | scene | `Level 1 - Training Yard/Static/Walls/PerimeterWall` | Top view of the wall square with the north gap |
| phase-03/check-04 | inspector | `Level 1 - Training Yard/Static/ExitGate` | Transform 0,0,16 + Exit Gate fields |
| phase-03/check-05 | inspector | `Level 1 - Training Yard/Static/Lamps/LampPost/LampLight` | Point light 2.4 / range 9 + Night Light 2.4 |
| phase-03/check-06 | scene | `Level 1 - Training Yard/Static/Obstacles` | Top view of the obstacles |
| phase-03/check-07 | scene | `Level 1 - Training Yard/Scenery` | Far view: yard, rim blocks, towers |
| phase-03/check-08 | buildprofiles | `Assets/GoldRush/Scenes/Level1.unity` | Scene List: 0 MainMenu, 1 Level1 |

### Checks filled with an existing real screenshot (recapture optional)
These figures have `class="shot"` (no `todo`) and an `<img>`, but keep `data-shot` / `data-window` / `data-select`, so they can be recaptured.

| data-shot | image reused | why it fits |
|---|---|---|
| phase-00/check-06 | `images/phase-00/step-10-b.jpg` | Sandbox scene view (Sandbox is not in the repo, so no data-select) |
| phase-01/check-03 | `images/phase-01/step-11-a.png` | MenuPlayer/Model: 0,0.068,0, scale 0.337, Controller Hero, root motion off = real scene |
| phase-01/check-04 | `images/phase-01/step-11-b.png` | Lantern: Spot, FFD18C, 20, 14, 55 = real scene |
| phase-01/check-05 | `images/phase-01/step-12-a.png` | MenuUI: Overlay, Sort Order 10, 1920×1080, Match 0.5 = real scene |
| phase-02/check-06 | `images/phase-02/step-06-a.png` | Real MainMenu hierarchy, GoldPill selected |

Other images reused (not checks): `phase-00/step-05.png` (AI Navigation), `phase-00/step-11.jpg` (Sandbox Play),
`01_title.jpg`, `02_levels.jpg`, `03_shop.jpg`, `images/kit/*.png`, `phase-02/level1-top.jpg`, `phase-02/level1-overview.jpg`
(older picture without the gate and towers; the caption says so; replace with a real capture of the phase-3 overview camera if you can).
Old hand-build shots no longer used: phase-00 step-08/09/10-a; phase-01 step-03, 06, 07, 08-*, 09-*, 10-*, 12-b, 13-*, 14-*, 15, 16-*, 17-*, 18, 19; phase-02 step-05; phase-03 step-04, step-08-b.

## Capture tips
- `MenuDiorama` and `MenuPlayer` are **root** objects in MainMenu (not children of `GoldRushMenu`).
- `Settings` is inactive in Edit mode: phase-02/check-04 must show it grey; for check-05 the tool must be able to select an object under an inactive parent.
- `Levels/Cards`, `Shop/ShopPanel/Glass/Tabs` and `Grid` are empty in Edit mode (filled at runtime). The pages say so.
- Level1 has 8 objects called `LampPost` (and several `Block`, `Pillar`): `…/Lamps/LampPost/LampLight` should pick the first one.
- The finished Level1 also has `GameManager`, `GameUI`, `EventSystem`, `Gameplay` and a NavMeshSurface on `Static`. For phase 3 shots, capture from the phase-3 package project if possible, or crop/collapse those, so students do not see later-phase parts. Same for the menu hierarchy in phase 1/2 (fine: the menu scene is the same in all packages).
- The Settings toggles are named `MUSIC  ONBtn` / `SOUND  ONBtn` (two spaces); the pages avoid them as data-select.

## Notes for the code owner / package builder
1. **Phase 0 package** must contain `Scenes/Sandbox.unity` (Floor_40m, Hero.fbx at scale 0.337, Block_2x2x2 at -3,0,3, Pillar at 3,0,3, LampPost at 0,0,5, camera 0,1.5,-4 rot 10,0,0). It is not in the repo. Per the V3 rule the package also has all scripts; the folder table says so.
2. **Phase 3 package / Level1**: Level1 has no directional light, no camera and default RenderSettings; sky, sun and fog only come from `EnvironmentSetup.Apply()` via GameManager (phase 4). The page says the level looks plainer in phase 3. If you want the sunset look already in phase 3, add a Sun + lighting to the phase-3 scene (or a tiny scene component that calls `EnvironmentSetup.Apply(Camera.main, false)`), and update the note in phase-03 step 5 and the "dark and grey" row in Common problems.
3. The phase-3 overview camera position is not stated on the page (we do not know your value); the Press Play figure is the old overview picture.
4. No code changes needed.
