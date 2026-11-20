# Phase 6 notes: Exit gate, win and lose

Guide: `phase-06.html` (10 steps). Sources I read: `World/ExitGate.cs`, `Gameplay/GameManager.cs`, `Gameplay/PlayerMotor.cs`,
`UI/GameUI.cs` (Intro, ShowWin, ShowLose, ShowPause), `Core/Data.cs` (LevelDef.parTime, Save), `World/Props.cs`
(Block, Glow, PerimeterWall), `Prefabs/Level/Gameplay/ExitGate.prefab`, `Scenes/Level1.unity`, phase-01/02 notes
(material names, GateBlocker, ExitGateSpot).
Compile-checked with Roslyn against the Unity 6000.6.10f1 DLLs (see phase-05-notes.md); not run in Unity.

## Design decisions
- **The gate model is a provided prefab** (SPEC "Provided prefabs"); students add the `ExitGate` component and wire
  its fields (step 5), then place it (step 6). All `ExitGate` fields are `[SerializeField]`, so **no code change**.
- The gate goes under `Level 1 - Training Yard/Gameplay` at `ExitGateSpot` (0,0,16), not static; `ExitGateSpot` is
  deleted. The phase 2 `GateBlocker` (0,2.5,17.25) stops the player, so the door panel has no collider (as in the repo).
  The repo has the gate under `Static` (inside the NavMesh). Phase 7 bakes NavMeshSurface on `Static`, so the gate
  posts are not carved into the NavMesh; harmless (monsters don't go there), but phase 7 may want to know.
- **Lose** has no cause before monsters, so phase 6 adds a test key: `L` → `DebugLose()` → real `Lose()`.
- Win/lose dialogs come from the full GameUI (imported in phase 5). MENU and NEXT buttons are visible but their
  scenes come in phase 9; the guide says so (Fader just logs an error and fades back).

## Provided prefab I assume (starter package, phase 0)
`Assets/GoldRush/Prefabs/Gameplay/ExitGate.prefab` – **visual only, no ExitGate script** (front = -Z side, pivot on the floor
at the middle of the opening). Easiest: the repo's `Prefabs/Level/Gameplay/ExitGate.prefab` with the `ExitGate`
component removed (it is the same model). Children the guide/script need (names are what step 4 shows; the repo prefab
uses "Cube"/"Block" names, so please rename or adjust the guide):
- `PostLeft`, `PostRight` (±3.4, 1.3×4.6×1.7, colliders), `Lintel` (8.1×1.25×1.5 at y 3.5–4.75) – frame, M_Block/M_BlockTop.
- `Door` (empty at 0,0,0, **no collider**) › `Panel` (0,1.75,0.5, 5.3×3.5×0.4) – `door` field; slides down 3.6 m.
- `StripTop` (0,3.6,-0.26), `StripLeft`/`StripRight` (±3.05,1.65,-0.38) – Renderers with `M_GlowRed` – `strips[3]`.
- `Text` (TextMesh "EXIT", 0,4.13,-0.34, size 0.065, font 100, colour FF6157) – `text`.
- `Glow` (0,0,-1.6, **inactive**) › `Pad` (3.4×0.012×3.4, `M_GateGlow` transparent 4DFF73 a0.3), `Ring` (4 green strips) – `glow`, `glowRing`.
- `GlowLight` (Point, 0,2,-1.5, colour 4DFF73, range 9, intensity 0, **Light component disabled**) – `glowLight`.
- `MarkerAnchor` (0,5.6,0) – `markerAnchor`.
- Material `M_GlowGreen` must exist (phase 1 / starter) – `greenStrip`.
Decor in the repo prefab (feet, trim, sign panel, lanterns + LanternLight/NightLight) can stay.

## Code changes needed
None new. Phase 6 relies on phase-05-notes changes 1–5 (GameAssets sounds, AudioFX wav loading, Coin.Start,
GameManager scene Player/Camera/coins, helpers in Resources).

## Phase versions (compile rule)
| File | Phase 6 version | Later |
|------|-----------------|-------|
| `World/ExitGate.cs` | Real file **without `Create()` / `Build()`** and the constants only they use (`Opening`, `Red`, `Green`). `Open()`, `Update()`, `WinPoint`, `MarkerAnchor`, all fields unchanged. | real file when LevelBuilder/Props are imported (phase 7?) or phase 10 `Scripts/Final` |
| `Gameplay/PlayerMotor.cs` | Phase 4 version (from phase-04.html) + `using System.Collections;` + real `canMove = !Frozen && gm != null && gm.State == GameState.Playing` + real `PlayVictory()`/`VictoryCo()` + `PlayCaught()` **without** `SetVisible`, the `CaughtCo` delay and `FX.Puff` (it just sets Frozen and fires `Die`). Uses `AnimParams.Win/Die/Speed` (GameAssets.cs exists since phase 5). | phase 7 version |
| `Gameplay/GameManager.cs` | Phase 5 version + `Hits`, `IntroCo` (`Cam.PlayIntro`, `UI.Intro`, controls hint, `seenTutorial`), gate win check, real `AllCollected` (Open, "gate" sound, shake, toast) **minus** `Enrage`, real `Lose`/`LoseCo`/`Win`/`WinCo` **minus** `FX.Confetti`, plus TEST-ONLY `L` key + `DebugLose()`. | phase 7 (remove the L key) |

## Screenshot placeholders
1. Provided ExitGate prefab in prefab mode – Hierarchy children and the closed gate (step 4)
2. Inspector of ExitGate with all fields filled (step 5)
3. 3-2-1-GO intro over the level (step 9)
4. Open gate with green glow, hero in front (step 9)
5. LEVEL COMPLETE! dialog with 3 stars (step 9)
6. CAUGHT! dialog (step 9)
