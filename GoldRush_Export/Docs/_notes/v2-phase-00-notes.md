# V2 Phase 0 notes: Create the project

Guide: `Docs/phase-00.html` (rewritten from `_old_order/phase-00.html`).

## What changed vs. the old phase 0
- Sidebar uses the **V2 phase titles** (0 Create the project, 1 The main menu, 2 Level select, shop and settings,
  3 Level design, 4 Player and camera, 5 Coins, 6 Exit gate, win and lose, 7 Monsters, 8 Power-ups and effects,
  9 Gameplay UI, 10 Polish and build). `_notes/TEMPLATE.html` still has the OLD titles: please update the template
  (or every V2 writer must use these labels) so all 11 pages have the same sidebar.
- Starter package table now lists `Resources/UISprites` (needed by phase 1).
- New folder `Prefabs/UI/` (student-made glass UI prefabs, phases 1–2, reused in phase 9).
- Test scene `Sandbox` = Floor_40m + Hero.fbx (scale **0.337**, the real menu/model value 1.35 m; old guide said 0.36)
  + Block_2x2x2 (-3,0,3), Pillar (3,0,3), LampPost (0,0,5). Camera (0,1.5,-4) rot (10,0,0).

## Code changes needed
None for phase 0. Package / pack requirements:
1. **Starter package** `GoldRush_Phase00_Project.unitypackage` must contain (with .meta files):
   - `Resources/UISprites/*.png` (pill, pillo, rr, rro2, circle, ring, ring2, soft, fadeD, fadeR, fadeU, star, heart,
     tri, square) **with the sprite borders of the repo metas** (pill/pillo 62, rr/rro2 34, others 0) and
     Texture Type = Sprite. Phase 1 uses them by hand (Image Type Sliced + PPU).
   - `Resources/Icons/ic_*.png` (sprites) and `Resources/Fonts/Poppins-SemiBold/Bold/ExtraBold.ttf`.
   - Art (Characters incl. Atlas_Monsters.png, Audio 16 wav, Textures), Materials (M_*, GR_*), Prefabs/Level/*,
     Prefabs/Gameplay/* (visuals only, no script references).
   - NOT `Resources/GoldRushAssets.asset` (students create it in phase 1) and NOT `DayNightPalette.asset` (phase 10).
2. Hero.fbx .meta: either default import or the repo settings (Generic, clips named Idle/Wave, loops). Phase 1
   step 8 says "check (and set if needed)" and "the clip that ends with Idle", so both work.

## Phase versions / imported scripts
None (no scripts in phase 0).

## Screenshots (data-shot ids)
| id | window | select | note |
|---|---|---|---|
| phase-00/step-02 | project | Unity Hub/New project | **manual** (Unity Hub, `data-manual="true"`) |
| phase-00/step-05 | project | Packages/com.unity.ai.navigation | **manual** (Package Manager window) |
| phase-00/step-06 | inspector | ProjectSettings/Player | **manual** (Project Settings window) |
| phase-00/step-08 | project | Assets/GoldRush | folder tree |
| phase-00/step-09 | hierarchy | Floor_40m | |
| phase-00/step-10-a | inspector | Hero | scale 0.337 |
| phase-00/step-10-b | scene | Hero | |
| phase-00/step-11 | game | Main Camera | play mode |
