# Phase 8 – Power-ups and effects: notes for the code owner

## Design
Power-ups stay exactly as in the real code: markers (empty + `PowerUpSpot`) under `Gameplay/PowerUps`; `GameManager`
builds the orbs at runtime (`PowerUp.Create`). Students add `PowerUpSpot` (Magnet) to the phase-2 marker `PowerUp Magnet`
(-14.5, 0, -8.5), save it as prefab `Prefabs/Gameplay/PowerUp_Magnet` and duplicate it to `PowerUp_Speed` / `PowerUp_Ghost`
(same as the repo marker prefabs). SPEED + GHOST are added for testing only ("delete afterwards").
- `PowerUpSpot.cs` (18 lines) and `PowerUp.cs` (142 lines) are pasted in full (real files, unchanged).
- `FX.cs` is imported from `Resources/Scripts/Helpers/` ("skip if already there").
- World markers / arrow: no new code. `WorldMarkers.cs` (already imported with GameUI) is explained with an excerpt of
  `LateUpdate()`; the arrow comes from `GameUI.UpdateArrow()` + `GameManager.ArrowTarget`.

## Code changes needed
No new ones. The phase 8 files contain the phase 7 changes (see `phase-07-notes.md`: GameManager 2a–c, PlayerMotor change 3).

## Phase versions (compile rule)
| File | Phase 8 version |
|------|-----------------|
| `GameManager.cs` | **real file** + phase-7 changes 2a–c (power-ups, magnet pull, `CollectPowerUp`, `FX.CoinBurst`, `FX.Confetti` back). Shown in full. |
| `PlayerMotor.cs` | **real file** + phase-7 change 3 (`Start()` makes dust / boost trail; `FX.Puff` back). Shown in full. |

## Notes / risks
- `GameUI.cs` already uses `PowerUp.ColorOf/NameOf`, so `PowerUp.cs` may exist since phase 5 – the guide tells students to
  compare, and the problems table covers "two PowerUp.cs files".
- `Mats.Particle` needs the Particle material (GR_Particle, phase 1) in `GoldRushAssets`; the problems table mentions pink particles.
- Prefab folder: guide uses `Prefabs/Gameplay/` (SPEC, phase 3); repo has `Prefabs/Level/Gameplay/`.
- `FX.cs` (134 lines) is imported, as the SPEC lists it with the big helpers.

## Screenshots (placeholders)
1. Scene view with the three power-up gizmos (cyan, purple, red wire balls) + Inspector of PowerUp_Magnet (step 8)
2. Game view: hero with the cyan SPEED trail, HUD SPEED bar, a MAGNET orb with its label in the distance (step 11)
