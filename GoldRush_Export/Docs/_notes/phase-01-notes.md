# Phase 1 notes: Materials and prefabs

## Code changes needed
1. **Coins use runtime materials, not M_Gold.** `World/Coin.cs` `Coin.Create` builds its materials with
   `Mats.Emissive(...)` at runtime, and `GameManager.Build()` calls `Coin.Create` for every CoinSpot. So the
   hand-made `M_Gold` / `M_GoldDark` (phase 1 step 6) are only used if phase 5 makes a Coin prefab.
   Suggested change (owner/phase 5 decides): give `GameAssets` a `public GameObject coinPrefab;` (or
   `Material gold, goldInner;`) and let `Coin.Create` use it when set, falling back to the current code.
   Without the change the guide is still correct (materials exist, game works), only M_Gold is unused.
2. **GameAssets has no `[CreateAssetMenu]`** (`Core/GameAssets.cs` line 52). The GR_ base materials made in phase 1
   step 7 must be dragged into `GoldRushAssets.asset` (fields `lit`, `litEmissive`, `litTransparent`, `particle`,
   `skybox`). Add `[CreateAssetMenu(fileName = "GoldRushAssets", menuName = "Gold Rush/Game Assets")]` so
   students can create it by hand (in `Assets/GoldRush/Resources/`) in the phase that first needs it
   (`Mats.*` is used by Coin/ExitGate/FX; `EnvironmentSetup` reads `skybox`).
3. **ExitGate** (`World/ExitGate.cs`) builds itself in code via `Props.*` (runtime-cloned materials). If phase 6
   builds a gate prefab by hand, its serialized fields (`strips`, `greenStrip`, `door`, `glow`...) must be filled;
   `greenStrip` = `M_GlowGreen`, strips use `M_GlowRed`. Two transparent gate materials exist in the repo
   (`Cube 4.mat` = (0.3,1,0.45,a 0.3) smooth 0.3 = floor disc, `Cube 5.mat` = (0.4,1,0.5,a 0.07) smooth 0.1 = light
   column). They were NOT made in phase 1; phase 6 can make them (`M_GateGlow`, `M_GateBeam`) from GR_LitTransparent.
4. **Day/night colours (phase 10).** In the repo the level materials are tinted by a `DayNightTint` on the level
   root (entries: material, day, night) and saved in `Resources/DayNightPalette.asset`. Hand-made M_ materials
   need entries. Night colours from `Props.cs`:

| Guide name | Day (= base colour) | Night |
|---|---|---|
| M_Floor | 0.86,0.87,0.90 | 0.13,0.17,0.32 |
| M_OuterGround | 0.60,0.63,0.69 | 0.07,0.09,0.19 |
| M_Block | 0.80,0.82,0.86 | 0.32,0.36,0.50 |
| M_BlockTop | 0.93,0.94,0.97 | 0.44,0.50,0.68 |
| M_Trim | 0.42,0.45,0.52 | 0.15,0.18,0.30 |
| M_Red / M_RedTop | 0.93,0.30,0.30 / 1,0.47,0.44 | 0.62,0.17,0.26 / 0.76,0.26,0.33 |
| M_Orange / M_OrangeTop | 1,0.60,0.22 / 1,0.74,0.42 | 0.72,0.38,0.20 / 0.85,0.50,0.28 |
| M_Teal / M_TealTop | 0.20,0.74,0.74 / 0.42,0.87,0.85 | 0.10,0.42,0.52 / 0.16,0.56,0.63 |
| (Pad, not built) | 0.70,0.72,0.78 | 0.09,0.12,0.25 |

   Glow materials are not tinted. The lamp `LampLight` needs a `NightLight` component (nightIntensity 2.4) in
   phase 10 (`Props.MakeNightLight`); in phase 1 it is a plain Point Light, always on.

## Material name mapping (repo `Materials/Level/*.mat` → guide name)
The repo names come from `LevelBaker.PersistAssets` (named after the first GameObject using them). Values copied
from the .mat files and `Props.cs`.

| Repo file | Guide name | Base colour (hex) | Smooth / Metal | Emission | Notes |
|---|---|---|---|---|---|
| Floor.mat | M_Floor | 0.86,0.87,0.90 (DBDEE6) | 0.35 / 0 | – | tex_grid, tiling 20 (Level1; Floor 1/2/3 = Level2/3/4 with tiling 24/28/32) |
| Floor 1/2/3.mat | (M_Floor, other tiling) | same | | | not used in phase 1–2 |
| OuterGround.mat | M_OuterGround | 0.60,0.63,0.69 (99A1B0) | 0.2 / 0 | – | tex_grid, tiling 120 |
| Body.mat | M_Block | 0.80,0.82,0.86 (CCD1DB) | 0.15 / 0 | – | Props.WallMat |
| Cube.mat | M_BlockTop | 0.93,0.94,0.97 (EDF0F7) | 0.2 / 0 | – | Props.WallTopMat |
| Plinth.mat | M_Trim | 0.42,0.45,0.52 (6B7385) | 0.3 / 0 | – | Props.TrimMat |
| Body 1.mat | M_Red | 0.93,0.30,0.30 (ED4C4C) | 0.3 | – | |
| Top.mat | M_RedTop | 1,0.47,0.44 (FF7870) | 0.35 | – | |
| Cube 2.mat | M_Orange | 1,0.60,0.22 (FF9938) | 0.3 | – | |
| Top 3.mat | M_OrangeTop | 1,0.74,0.42 (FFBD6B) | 0.35 | – | |
| Body 2.mat | M_Teal | 0.20,0.74,0.74 (33BDBD) | 0.3 | – | |
| Top 1.mat | M_TealTop | 0.42,0.87,0.85 (6BDED9) | 0.35 | – | |
| Body 3.mat / Top 2.mat | (M_Tower / M_TowerTop) | 0.70,0.73,0.79 / 0.83,0.85,0.90 | 0.1 / 0.15 | – | distant scenery, not built |
| Cube 9.mat | (M_Pad) | 0.70,0.72,0.78 | 0.4 | – | FloorPad, not built |
| FloorStrip 1.mat, FloorStrip 3.mat, Cube 7.mat, Cube 10.mat | M_GlowCyan | 0.475,0.8875,1 (79E2FF) | 0.5 / 0 | (0.3,0.85,1) × 2.0 (also ×2.2, ×1.8, ×1.6 in the repo) | guide uses one: hex 4CD9FF, HDR intensity 1 (=×2) |
| FloorStrip 2.mat, Cube 11.mat, Cube 12.mat | M_GlowOrange | 1,0.6625,0.4 (FFA966) | 0.5 / 0 | (1,0.55,0.2) × 2 (×1.6, ×1.8) | hex FF8C33, intensity 1 |
| Cube 6.mat, Cube 3.mat | M_GlowLamp | 1,0.91,0.775 (FFE8C6) | 0.5 / 0 | (1,0.88,0.7) × 2.6 (lamp) / × 2.4 (gate lanterns) | hex FFE0B2, intensity 1.4 (=×2.6) |
| Cube 1.mat | M_GlowRed | 1,0.415,0.4 (FF6A66) | 0.5 / 0 | (1,0.22,0.2) × 2.6 | gate strips (closed) |
| FloorStrip.mat, Cube 8.mat | M_GlowGreen | 0.475,1,0.5875 (79FF96) | 0.5 / 0 | (0.3,1,0.45) × 2.6 (×2.5 terminal LED) | gate open (ExitGate.greenStrip) |
| Cube 4.mat / Cube 5.mat | (M_GateGlow / M_GateBeam) | (0.3,1,0.45,a0.3) / (0.4,1,0.5,a0.07) | 0.3 / 0.1 | – | transparent, gate glow; not built (see code change 3) |
| (runtime only, Coin.cs) | M_Gold | 1,0.7,0.12 (FFB21F) | 0.8 / 0.85 | (1,0.6,0.1) × 0.55 → hex 8C540E, intensity 0 | |
| (runtime only, Coin.cs) | M_GoldDark | 0.85,0.5,0.08 (D98014) | 0.75 / 0.85 | (0.9,0.45,0.08) × 0.35 → hex 502807, intensity 0 | |
| GR_Lit / GR_LitEmissive / GR_LitTransparent / GR_Particle / GR_SunsetSky | same names | white | 0.3 / 0 | emissive: white | from `GoldRushSetup.cs`; kept the GR_ names (they match the repo and GameAssets) |

HDR intensity in the Unity picker is in stops: value = colour × 2^intensity, so ×2 = 1, ×2.6 ≈ 1.38 (guide: 1.4).
Glow base colour = Lerp(c, white, 0.25), as `Props.Glow` does.

## Prefabs built in phase 1 (folder `Assets/GoldRush/Prefabs/Level/`, repo uses `Prefabs/Level/Blocks/`)
Block_1x1x1, Block_2x2x2, Block_4x1x2, AccentBlock_Red/Orange/Teal (1.5 m), Wall_4m, Wall_8m, Pillar,
Barrier_Orange, LampPost. Sizes/children copied from the real prefabs (`Props.Block` cap 0.14, inset ≤0.09).
Simplifications vs. repo:
- **Wall_4m / Wall_8m**: real ones are a "Wall" root with Block children rotated yaw 90 (Body scale 1×2.16×4);
  the guide uses one unrotated Body (4 or 8 × 2.16 × 1) + Top. Same look, except Wall_8m has no middle seam.
- Barrier_Orange top scale 2.952×0.14×0.552 rounded to 2.95×0.14×0.55.
- Child names: repo uses "Cube" for trim/glow parts; the guide uses Base, Band, FootLeft/FootRight, Pole, Cap, Hat.
- Not built: Terminal, FloorPad_Cyan (decor), and the Gameplay prefabs (ExitGate, PlayerSpawn, CoinSpot,
  Monster_*, PowerUp_*): those belong to phases 2/5–8.
- Lamp light: Point, colour (1,0.88,0.7) = FFE0B2, intensity 2.4, range 9, no shadows, at y 2.9 (as repo).
  No NightLight yet (phase 10).

## Phase versions of scripts
None (no scripts in phase 1).

## Screenshot placeholders
1. Inspector of M_Floor (URP Lit, grid texture in Base Map, Tiling 20 × 20, Smoothness 0.35)
2. HDR colour picker for Emission with hex 4CD9FF and Intensity 1
3. Project window, Materials folder with all M_ materials as coloured balls
4. Hierarchy Block_1x1x1 with Body and Top, Scene view of the block
5. Prefab Mode of Block_2x2x2 (blue background, < arrow at the top of the Hierarchy)
6. Pillar and Barrier_Orange side by side in the Scene view
7. Sandbox scene with the hero, three blocks, a wall, a pillar and a lamp post
