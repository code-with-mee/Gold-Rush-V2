# Phase 3 notes: The player

Guide: `phase-03.html`. Real sources read: `Editor/CharacterImportRules.cs`, `Editor/CharacterSetup.cs`,
`Scripts/Core/GameAssets.cs` (CharacterDef, PlaybackSpeed, AnimParams), `Scripts/Gameplay/CharacterFactory.cs`,
`Scripts/Gameplay/PlayerMotor.cs`, `Scripts/Level/PlayerSpawn.cs`, `Scripts/UI/VirtualJoystick.cs`,
`Resources/GoldRushAssets.asset` (measured hero values).

## What the students do by hand (instead of the Editor tools)
- Hero.fbx import (= CharacterImportRules): Rig Generic, Avatar Create From This Model, Import Animation on,
  Import Cameras/Lights off, Materials Import via MaterialDescription + Use Embedded Materials.
- Clips: rename `CharacterArmature|X` -> `X` for Idle, Walk, Run, Wave, Punch, HitReact, Death.
  Loop Time + Loop Pose on for Idle, Walk, Run, Wave (keywords idle/walk/run/wave in the rule).
- Hero.controller (= CharacterSetup.BuildController): params Speed (Float), Attack/Hit/Win/Die (Trigger);
  default state "Locomotion" = 1D blend tree on Speed, manual thresholds Idle 0 / Walk 0.5 / Run 1;
  states Attack=Punch, Hit=HitReact, Win=Wave, Die=Death; Any State -> each: trigger condition, no exit time,
  duration 0.1, can transition to self OFF; back to Locomotion: exit time 0.85 (Attack, Hit), 3 (Win), duration 0.2;
  Die has no way back.
- Controller saved at `Assets/GoldRush/Animations/Hero.controller` (SPEC layout). The repo has it at
  `Art/Characters/Controllers/Hero.controller` (no code impact; only the Editor tool path differs).
- Player object (= PlayerMotor.Create): layer Ignore Raycast (2) on Player AND children; CharacterController
  height 1.35, radius 0.36, center (0, 0.695, 0), skin 0.05, step 0.35, slope 50.
- Model child (= CharacterFactory.BuildModel): Hero.fbx instance named "Model", pos/rot 0, Animator controller
  Hero, Apply Root Motion off, Culling Always Animate. Scale: guide says 0.36 and "fit the green capsule".
  (0.36 is my estimate: the FBX rest pose is ~3.76 units tall (Lcl Scaling 100, cm units), target 1.35 m.
  **Please verify in Unity** and fix the number in step 12 if needed.)
  Not done by hand: `SkinnedMeshRenderer.updateWhenOffscreen = true` (minor).
- Player saved as `Prefabs/Gameplay/Player.prefab`, instance placed on PlayerSpawn (Level1: 0, 0, -9, yaw 0).
- Temporary fixed camera for phase 3 only: Main Camera pos (0, 16, -26), rot (35, 0, 0). Phase 4 replaces it.
- PlayerSpawn.cs is pasted in step 10 "if you didn't in Phase 2" -> phase 2 writer: tell me/the editor whether
  phase 2 already creates it; then step 10 can be cut down to one line.

## Phase versions (later phases must say "Replace the whole file")
1. `Scripts/UI/VirtualJoystick.cs` - PHASE 3 STUB: only `Value` (always zero) and `Held` (always false).
   **Must be replaced by the real file in the phase that imports GameUI.cs** (phase 5: `GameUI.Create` calls
   `VirtualJoystick.Create`, and the real one needs UIKit + Sprites). Phase 10 explains touch controls.
2. `Scripts/Gameplay/PlayerMotor.cs` - PHASE 3 VERSION (replaced again in phase 4). Differences from the real file:
   - no `Create()`; Awake finds the Animator with `GetComponentInChildren<Animator>()`.
   - no `cam` field, `yaw = 0` (phase 4 adds it).
   - `canMove = !Frozen` (real: also `GameManager.I.State == Playing`), no `SpeedMultiplier`.
   - no knockback/Hit/invulnerability/blink, ghost look, footsteps (AudioFX), dust/boost trail (FX),
     PlayVictory/PlayCaught, AddLantern, Cosmetics.
   - `anim.SetFloat("Speed", ...)` with a string, not `AnimParams.Speed` (AnimParams lives in GameAssets.cs).
   - feet-matching uses own fields `clipWalkSpeed = 0.59`, `clipRunSpeed = 0.84` and a private
     `PlaybackSpeed()` copied from `CharacterDef.PlaybackSpeed` (see code change 2).
3. `Scripts/Level/PlayerSpawn.cs` - full real file (16 lines), no change needed later.

## Code changes needed
1. **PlayerMotor.cs - work with a Player placed in the scene.** In `Awake()`:
   `if (!anim) anim = GetComponentInChildren<Animator>(); if (anim) model = anim.transform;`
   `if (renderers == null) renderers = GetComponentsInChildren<Renderer>();`
   Move the dust/boost trail creation (`FX.Trail`) into `Start()` when they are null, so they exist for a scene
   Player too. Keep `Create()` as fallback. Cosmetics.Apply should be called by GameManager (or PlayerMotor.Start)
   for the scene Player.
2. **PlayerMotor.cs - no GameAssets for the feet speed.** Replace `GameAssets.I.hero.PlaybackSpeed(blend, horiz)`
   with fields `public float clipWalkSpeed = 0.59f, clipRunSpeed = 0.84f;` (values from GoldRushAssets.asset:
   0.5873 / 0.8363) and the private `PlaybackSpeed()` shown in the guide. Why: students never run the Editor setup
   that creates GoldRushAssets.asset. (Optional: use `AnimParams.Speed` again once GameAssets.cs/AnimParams is in
   the course; or move `AnimParams` into its own small file so phase 3 can use it.)
   FYI: with the measured values (~0.6 m/s walk) the ratio is always > 7, so the clamp gives 2.2x playback whenever
   the hero moves. The measurement may be wrong (scale?), or a simple fixed speed would do the same.
3. **GameManager.cs (Build) - use the Player already in the scene.** Replace
   `Player = PlayerMotor.Create(layout.spawn, layout.spawnYaw);` with:
   ```csharp
   Player = FindAnyObjectByType<PlayerMotor>();
   if (Player)
   {
       var cc = Player.GetComponent<CharacterController>();
       cc.enabled = false;                       // a CharacterController ignores teleports while enabled
       Player.transform.SetPositionAndRotation(layout.spawn, Quaternion.Euler(0, layout.spawnYaw, 0));
       cc.enabled = true;
   }
   else Player = PlayerMotor.Create(layout.spawn, layout.spawnYaw);
   ```
   (Camera part: see phase-04-notes.md.)
4. Same idea will be needed for monsters (EnemyAI uses `GameAssets.I.Enemy(Type).PlaybackSpeed`) - phase 7 owner.

## Screenshot placeholders
1. Hero.fbx Import Settings, Rig tab (Generic, Create From This Model)
2. Animation tab with the Run clip selected, Loop Time and Loop Pose ticked
3. Blend Tree Inspector - 1D, Speed, Idle 0 / Walk 0.5 / Run 1, Automate Thresholds off
4. Animator window - Any State with 4 arrows, Attack/Hit/Win arrows back to Locomotion
5. Scene view - hero inside the green CharacterController capsule; Hierarchy Player -> Model
6. Inspector of Model - Animator with Controller Hero, Apply Root Motion off, Always Animate
7. Inspector of Player - Layer Ignore Raycast, Character Controller, PlayerMotor
8. Game view - hero running in Level1, seen from the high temporary camera
