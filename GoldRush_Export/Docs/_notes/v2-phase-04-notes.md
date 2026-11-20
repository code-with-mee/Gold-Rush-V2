# V2 Phase 4 notes: Player and camera (old phases 3 + 4)

Guide: `phase-04.html` (17 steps). Built from `_old_order/phase-03.html` + `_old_order/phase-04.html`.
Real sources read: `Scripts/Gameplay/PlayerMotor.cs`, `ThirdPersonCamera.cs`, `GameManager.cs` (Build: placed
Player → `Teleport` + `SetupPlaced`; placed camera → `Init`; cursor lock), `Scripts/Core/GameAssets.cs`
(CharacterDef.PlaybackSpeed, AnimParams), `Editor/CharacterSetup.cs` (controller), `Editor/CharacterImportRules.cs`,
`Scripts/Gameplay/CharacterFactory.cs` (BuildModel), `Resources/GoldRushAssets.asset` (hero walk 0.587 / run 0.836),
tester checkpoints `GoldRush_Phase02_Level` / `Phase04_Camera` (Hero.fbx.meta, Player.prefab).

## What changed vs. the old order
- One PlayerMotor version only (the old phase-3 version without `cam` is gone); written as close to the real file
  as possible: same `Awake` (finds the Animator on a placed Player) and the real `Teleport()`.
- ThirdPersonCamera phase version = real file minus `Create()`, `PlayIntro`/intro block (needs `Tween`) and
  `ScriptedLook` (editor tooling), plus the PHASE-4-ONLY cursor block. `Init()` and `Start()` (with the
  `curDist <= 0f` guard) are identical to the real file.
- Import settings step is now a CHECK: the starter package `Hero.fbx.meta` already has Generic / Create From This
  Model / cameras+lights off / MaterialDescription + embedded / short clip names / loops on Idle, Walk, Run, Wave
  (verified in the Phase02 package).
- Player is created directly at the spawn (0, 0, -9) at the scene root (old step 14 merged into step 11).
- Theory step "How the camera works" and "Tune it" removed; bullets under the script instead. Controls table moved
  into Press Play.
- Press Play starts from MainMenu → PLAY. Back to the menu: stop Play mode (Esc only frees the cursor here).
- Model scale `0.36`: the tester's Player.prefab (Phase04 package) kept 0.36, so I kept it (with "fit the capsule").

## Animator Controller (from CharacterSetup.cs)
Params Speed (Float), Attack/Hit/Win/Die (Trigger). Default "Locomotion" = 1D blend tree on Speed, manual
thresholds Idle 0 / Walk 0.5 / Run 1. States Attack=Punch, Hit=HitReact, Win=Wave, Die=Death. Any State → each:
trigger condition, no exit time, duration 0.1, can transition to self off. Back to Locomotion: exit time 0.85
(Attack, Hit), 3 (Win), duration 0.2. Die: no way back. Saved at `Assets/GoldRush/Animations/Hero.controller`.
**Coordination with phase 1:** the menu hero waves via `AnimParams.Win`, so phase 1 may already create
`Hero.controller`. Step 3 says "if it exists, use it and add what is missing; do not make a second one".
Phase 1 writer: please tell me if you create it (then step 3 can be shortened to a check).

## Phase versions (later phases must say "Replace the whole file with this updated version")
| File | Phase 4 content | Replace in |
|---|---|---|
| `Scripts/UI/VirtualJoystick.cs` | STUB: `Value` (always 0), `Held => false` | the phase that imports `GameUI.cs` / touch controls (real file needs UIKit + Sprites) |
| `Scripts/UI/TouchLookArea.cs` | STUB: `static Vector2 Consume() => Vector2.zero` | same as above |
| `Scripts/Gameplay/PlayerMotor.cs` | PHASE 4 VERSION (no GameManager, no SpeedMultiplier, no knockback/Hit/blink/ghost/footsteps/dust/trails/PlayVictory/PlayCaught/AddLantern/SetupPlaced/Create, `SetFloat("Speed")` string instead of `AnimParams.Speed`, own `clipWalkSpeed 0.59` / `clipRunSpeed 0.84` + private `PlaybackSpeed()` instead of `GameAssets.I.hero.PlaybackSpeed`) | phase 5 (GameManager) and later |
| `Scripts/Gameplay/ThirdPersonCamera.cs` | PHASE 4 VERSION (see above; cursor block marked `PHASE 4 ONLY`) | phase 5 (GameManager takes over the cursor; real file needs `Tween.InOutSine`) |
| `Scripts/Level/PlayerSpawn.cs` | real file (final) | never |

All five files were compiled together with Roslyn against the Unity 6000.6 engine DLLs: 0 errors, 0 warnings.
They depend on nothing from phases 0–2, and use no class names that clash with them.

## Imported helpers
None in phase 4.

## Code changes needed
None required: the repo already has everything the old notes asked for (`PlayerMotor.Awake` finds the Animator,
`SetupPlaced()`, `Teleport()`; `ThirdPersonCamera.Init()` + `Start()` guard; `GameManager.Build` uses a placed
Player and a placed ThirdPersonCamera). Points for the code owner / phase 5 writer:
1. **Real PlayerMotor needs GoldRushAssets** (`GameAssets.I.hero.PlaybackSpeed`, `Cosmetics.Apply` in
   `SetupPlaced`). When phase 5 replaces PlayerMotor, the asset must exist with hero `walkSpeed 0.5873` /
   `runSpeed 0.8363` (otherwise `PlaybackSpeed` estimates from height = different foot speed than phase 4).
   Optional code change: move `AnimParams` into its own file so phase versions can use it.
2. **Phase 5 must replace ThirdPersonCamera with the real file** (removes the cursor block; GameManager then does
   Esc = pause, click = re-lock). If phase 5 keeps a phase version, delete the `PHASE 4 ONLY` block there.
3. **Helper copies are stale:** `GoldRush_Export/Resources/Scripts/Helpers/UI/TouchLookArea.cs` and
   `VirtualJoystick.cs` lack the `[SerializeField]` attributes that `Assets/GoldRush/Scripts/UI/*.cs` now have.
   Re-export the helpers.
4. **Screenshot tool:** steps 3–6 (parameters, blend tree, states, transitions) need the **Animator window**, which
   is not an allowed `data-window`. I used `project`/`inspector` with `data-select` = the controller asset. Please add
   `data-window="animator"` (select the controller, open Window → Animation → Animator) and the blend-tree node
   for step 4.
5. Scene setup the code relies on: Player at the scene root (not under the LevelScene root), layer Ignore Raycast
   on Player + Model, Main Camera tagged MainCamera with ThirdPersonCamera (Target = Player), PlayerMotor.cam set in
   the scene only (not applied to the prefab).
6. Sidebar uses the V2 titles (see v2-phase-03-notes.md, point 4).

## Screenshots
Existing image used: `images/phase-04/camera-behind.jpg` (step 16).

Placeholders (`figure.shot.todo`):
| data-shot | window | select | shows |
|---|---|---|---|
| phase-04/step-02-a | inspector | Assets/GoldRush/Art/Characters/Hero/Hero.fbx | Rig tab |
| phase-04/step-02-b | inspector | Assets/GoldRush/Art/Characters/Hero/Hero.fbx | Animation tab, Run loops |
| phase-04/step-03 | project | Assets/GoldRush/Animations/Hero.controller | controller + 5 parameters (Animator) |
| phase-04/step-04 | inspector | Assets/GoldRush/Animations/Hero.controller | blend tree 0 / 0.5 / 1 (needs Animator, see 4) |
| phase-04/step-05 | inspector | Assets/GoldRush/Animations/Hero.controller | 5 states (needs Animator) |
| phase-04/step-06 | inspector | Assets/GoldRush/Animations/Hero.controller | 7 transitions (needs Animator) |
| phase-04/step-10 | inspector | Level 1 - Training Yard/Gameplay/PlayerSpawn | PlayerSpawn component |
| phase-04/step-11 | inspector | Player | layer + Character Controller |
| phase-04/step-12-a | scene | Player | hero inside the capsule |
| phase-04/step-12-b | inspector | Player/Model | scale 0.36, Animator settings |
| phase-04/step-13-a | inspector | Player | Character Controller + PlayerMotor |
| phase-04/step-13-b | project | Assets/GoldRush/Prefabs/Gameplay/Player.prefab | the new prefab |
| phase-04/step-14 | inspector | Main Camera | Camera 55/0.1/400 + ThirdPersonCamera Target Player |
| phase-04/step-15 | inspector | Player | PlayerMotor Cam = Main Camera |
| phase-04/step-16 | game | Player | third-person view in Play mode |
