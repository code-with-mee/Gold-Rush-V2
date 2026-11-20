# Phase 1 notes: The prefab kit

User decision: prefabs and materials are PROVIDED in the starter package `GoldRush_Phase00_Project.unitypackage`.
Phase 1 is a short tour only (no material creation, no cube building). The guide shows the kit table with
thumbnail placeholders, Prefab Mode (LampPost), one optional colour change (M_BlockTop → yellow → back to EDF0F7),
overrides/revert on a Block_1x1x1 instance.

## Starter package contents (for the code owner)
Build it from the repo assets, renaming the materials (mapping below). **No script components** may be on any
prefab (package is imported before scripts exist): strip `NightLight`, `DayNightTint`, `ExitGate`, `Coin`,
`PowerUp`, `PlayerSpawn`, `CoinSpot`, `MonsterSpawn`, `PowerUpSpot`. Keep colliders and lights.

### Art / Resources
- `Art/Characters/{Hero,Monsters,Licenses}` (Hero.fbx, Slime.fbx, Ghost.fbx, MushroomKing.fbx, Atlas_Monsters.png).
  Default import settings are fine (phase 3/7 change them by hand) – or ship CharacterImportRules results; the
  phase 3/7 writers decide.
- `Art/Audio/*.wav`, `Art/UI/*.png` (shapes), `Art/Textures/grid.png` (+ badge).
- `Resources/Icons/*.png` with Sprite import settings in the .meta, `Resources/Fonts/Poppins-*.ttf`.

### Materials (`Assets/GoldRush/Materials/`)
| Name | What it is |
|---|---|
| M_Floor | grid floor, tiling 20 (Level1, 40 m floor) |
| M_OuterGround | dark grid ground outside, tiling 120 |
| M_Block / M_BlockTop / M_Trim | grey block sides / lighter top slab / dark feet, poles, plinth |
| M_Red, M_RedTop, M_Orange, M_OrangeTop, M_Teal, M_TealTop | accent block bodies and tops |
| M_GlowCyan, M_GlowOrange, M_GlowLamp, M_GlowRed, M_GlowGreen | emissive glows (bands, strips, lamp caps, gate lights) |
| M_GateGlow, M_GateBeam | transparent green gate disc / light column (repo Cube 4 / Cube 5) |
| M_Gold, M_GoldDark | coin outer ring / centre (from Coin.cs values; not in repo as assets) |
| M_PowerSpeed, M_PowerGhost, M_PowerMagnet (+ shell variants) | power-up orb materials from PowerUp.cs (glow = Emissive(c,c,2.2,0.6,0.1), shell = Transparent(c, a 0.28, 0.9)); c = (0.2,0.9,1) / (0.76,0.56,1) / (1,0.26,0.24). Names suggested; not referenced in my guides |
| M_Tower, M_TowerTop, M_Pad | only if Terminal/FloorPad/scenery need them (M_Pad = FloorPad plate) |
| GR_Lit, GR_LitEmissive, GR_LitTransparent, GR_Particle, GR_SunsetSky | base materials for code + sky (as GoldRushSetup makes them; already in the repo under these names) |
| (texture) tex_grid → use Art/Textures/grid.png | grid texture used by M_Floor, M_OuterGround |

Duplicate repo materials (several "Cube n"/"FloorStrip n" with slightly different glow strength) should be merged
into the single M_Glow* names; the guides only name the materials in the table above.

### Prefabs/Level (the kit; guide table in phase 1 step 3)
| Prefab | Size | Source |
|---|---|---|
| Floor_40m | 40×40, top at Y 0, solid | NEW: from `Props.GridPlatform(40, 0.6)` + GroundCollider (box 34×1×34 at y -0.5, or a box over the whole 40×40) – repo Level1 `Static/Floor/Platform` + `GroundCollider` |
| OuterGround | 480 m quad at local y -0.6, no collider | NEW: from repo Level1 `Scenery/OuterGround` (pivot at 0, child/quad at -0.6) |
| Block_1x1x1, Block_2x2x2, Block_4x1x2 | as named | repo prefabs |
| AccentBlock_Red / Orange / Teal | 1.5 cube | repo prefabs |
| Wall_4m, Wall_8m | 4/8 × 2.3 × 1, length along X | repo prefabs |
| Pillar | 1×3×1, cyan band | repo |
| Barrier_Orange | 3 × 0.95 × 0.6 | repo |
| LampPost | point light 2.4 / range 9 / no shadows | repo, **remove NightLight** (phase 10 adds it) |
| Terminal | 1.1 × 1.5 × 0.75, screen on local -Z | repo |
| FloorPad_Cyan | 3 m, no collider | repo |

Phase 1 step 6 describes LampPost children generically (dark base and pole, glowing cap, hat, `LampLight`), so
the repo child names ("Cube") are fine; nicer names (Base/Pole/Cap/Hat) welcome. Step 8 uses `Block_1x1x1/Top`
(repo name is `Top`, OK).

### Prefabs/Gameplay (visual models only, no scripts)
| Prefab | From |
|---|---|
| Coin | `Coin.Create` visual: root + `Visual` child (cylinder 0.72×0.06 M_Gold, cylinder 0.5×0.085 M_GoldDark, 0.2 cube star rotated 45° on Z, all rotated 90° on X, no colliders). Phase 5 adds the Coin script. |
| ExitGate | repo `Prefabs/Level/Gameplay/ExitGate.prefab` without the ExitGate component (keep children Door, Glow, GlowLight, Text, MarkerAnchor, strips). Phase 6 adds the script and wires `door/glow/glowRing/markerAnchor/glowLight/text/strips/greenStrip` (= M_GlowGreen). |
| PowerUp_Speed / _Ghost / _Magnet | `PowerUp.Create` visual per type (orb shell sphere 0.95 + glow core etc.). Phase 8 adds the script. Note: repo `Prefabs/Level/Gameplay/PowerUp_*.prefab` are MARKERS (PowerUpSpot) – different thing; avoid the name clash (markers in my guide are plain empties named `PowerUp Magnet`). |

## Code changes needed
1. **Runtime-built objects vs. provided visuals.** `GameManager.Build()` calls `Coin.Create`, `PowerUp.Create`
   (and the generated level uses `ExitGate.Create`); these build their own meshes/materials in code. For the
   provided visual prefabs to be used, the code needs a prefab path: e.g. `GameAssets.coinPrefab`,
   `GameAssets.powerUpPrefabs[3]`, and `Coin.Create`/`PowerUp.Create` instantiate the prefab when set (fallback:
   current code). ExitGate is found in the scene (`GetComponentInChildren<ExitGate>`), so a scene gate prefab
   already works once the component is added and its serialized fields are filled (phase 6).
2. **GameAssets has no `[CreateAssetMenu]`** (`Core/GameAssets.cs` line 52). Add
   `[CreateAssetMenu(fileName = "GoldRushAssets", menuName = "Gold Rush/Game Assets")]` so students can create
   `Resources/GoldRushAssets.asset` and drag in GR_Lit, GR_LitEmissive, GR_LitTransparent, GR_Particle,
   GR_SunsetSky (fields lit, litEmissive, litTransparent, particle, skybox). Or ship it in the starter package
   (then it must not reference scripts that do not exist yet – it IS a script asset, so it cannot be in the phase 0
   package before `GameAssets.cs` exists; ship it with the phase that adds GameAssets.cs).
3. **Rename the repo material assets** per the mapping below (owner plans to do this) and point the repo
   prefabs/scenes at the merged M_Glow* materials.
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

## Phase versions of scripts
None (no scripts in phase 1).

## Screenshot placeholders
1–15. Thumbnail of each level-kit prefab (phase 1 step 3 table): Floor_40m, OuterGround, Block_1x1x1,
   Block_2x2x2, Block_4x1x2, AccentBlock_Red, AccentBlock_Orange, AccentBlock_Teal, Wall_4m, Wall_8m, Pillar,
   Barrier_Orange, LampPost, Terminal, FloorPad_Cyan
16–20. Thumbnail of each gameplay model (step 4 table): Coin, ExitGate, PowerUp_Speed, PowerUp_Ghost, PowerUp_Magnet
21. Sandbox scene with the hero, three blocks, a wall, a pillar and a lamp post
22. Prefab Mode of LampPost (blue background, < arrow at the top of the Hierarchy, LampLight selected)
