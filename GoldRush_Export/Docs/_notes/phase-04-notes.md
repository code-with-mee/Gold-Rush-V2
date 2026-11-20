# Phase 4 notes: The camera

Guide: `phase-04.html`. Real sources read: `Scripts/Gameplay/ThirdPersonCamera.cs`, `PlayerMotor.cs`,
`GameManager.cs` (cursor lock: `UseMouseLook`, `LockCursor`, Esc, click re-lock), `Scripts/UI/TouchLookArea.cs`,
`Scripts/Core/Tween.cs`.

## What the students do by hand (instead of ThirdPersonCamera.Create)
- Main Camera (tag MainCamera): FOV 55, near 0.1, far 400, Audio Listener.
- Add ThirdPersonCamera, Target = Player. Defaults: distance 6.2, height 1.15, pitch 4..62, touch 0.16,
  mouse 2.4, yaw 0, pitch 20.
- PlayerMotor.Cam = Main Camera (scene override, not applied to the prefab).
- Player + Model on Ignore Raycast (camera rays use Physics.DefaultRaycastLayers).

## Phase versions (later phases must say "Replace the whole file")
1. `Scripts/UI/TouchLookArea.cs` - PHASE 4 STUB: only `static Vector2 Consume() => Vector2.zero`.
   **Must be replaced by the real file in the phase that imports GameUI.cs** (phase 5: `GameUI.Create` calls
   `TouchLookArea.Create`).
2. `Scripts/Gameplay/ThirdPersonCamera.cs` - PHASE 4 VERSION. Differences from the real file:
   - no `Create()`; instead `Start()` calls a new public `Init(target, target.eulerAngles.y)` (see code change 1).
   - no intro swoop: `PlayIntro`, `intro/introT/introDur` and the `if (intro)` block are removed
     (it needs `Tween.InOutSine`, and only GameManager calls it). The wall-avoidance block runs always.
   - `Shake()` is kept unchanged (no dependencies).
   - PHASE 4 ONLY cursor block: `UseMouseLook`, `LockCursor()`, `Update()` (Esc frees, left click re-locks),
     `OnDestroy()` unlocks, `Start()` locks. Same logic as GameManager. **Remove when GameManager arrives**
     (phase 5): replacing the file with the real one does that. If phase 5 keeps a phase version of the camera
     (e.g. because Tween is not in the course yet), delete this block there, otherwise Esc both frees the cursor
     here and pauses in GameManager (harmless, but duplicated).
3. `Scripts/Gameplay/PlayerMotor.cs` - PHASE 4 VERSION = phase 3 version + `public ThirdPersonCamera cam;` and
   `float yaw = cam ? cam.Yaw : 0f;` (same as the real file). See phase-03-notes.md for all other differences.

## Code changes needed
1. **ThirdPersonCamera.cs - support a camera placed in the scene.** Add:
   ```csharp
   void Start() { if (target && curDist <= 0f) Init(target, target.eulerAngles.y); }

   public void Init(Transform newTarget, float startYaw)
   {
       target = newTarget;
       yaw = startYaw;
       focus = target.position + Vector3.up * height;
       curDist = targetDist = distance;
       curYaw = yaw;
       curPitch = pitch;
       Snap();
   }
   ```
   and let `Create()` call `Init()` instead of setting the fields itself. (The `curDist <= 0` guard keeps Start
   from undoing an `Init` + `PlayIntro` done by GameManager before Start runs. The guide's phase version has no
   guard because nothing else calls Init yet.) Why: without it a scene camera starts at distance 0 / focus at
   the world origin and glides in.
2. **GameManager.cs (Build) - use the Main Camera already in the scene.** Replace
   `Cam = ThirdPersonCamera.Create(Player.transform, layout.spawnYaw);` with:
   ```csharp
   Cam = FindAnyObjectByType<ThirdPersonCamera>();
   if (Cam) Cam.Init(Player.transform, layout.spawnYaw);
   else Cam = ThirdPersonCamera.Create(Player.transform, layout.spawnYaw);
   ```
   Must run AFTER the player is moved to the spawn (phase-03 code change 3). `Player.cam = Cam;` stays.
3. Cursor: no change needed in GameManager; it already locks on Playing, frees on pause/destroy, re-locks on click.

## Screenshot placeholders
1. Diagram: hero, focus point 1.15 m above him, camera on the orbit at distance 6.2, yaw/pitch, rays to a wall
2. Inspector of Main Camera - Camera (FOV 55, Near 0.1, Far 400) and ThirdPersonCamera with Target = Player
3. Game view - third-person view behind the hero in Level1
4. Game view - hero next to a wall, the camera pulled in close
