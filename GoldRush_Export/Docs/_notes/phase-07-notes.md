# Phase 7 – Monsters: notes for the code owner

Guide: `phase-07.html` (16 steps). Sources: EnemyAI, Badge, CharacterFactory, MonsterSpawn, GameManager, PlayerMotor,
LevelBuilder (NavMeshSurface settings), Editor/CharacterSetup + CharacterImportRules (clips, loop flags, transitions),
GameAssets (heights), Level1.unity, phase-02/03/04 pages + notes (aligned with them).

## Design decision
The real game spawns monsters at runtime from bare `MonsterSpawn` markers (`EnemyAI.Create`, model from
`CharacterFactory.BuildModel`). Per the brief, students instead:
- **bake** the NavMesh in the editor: NavMeshSurface on `Level 1 - Training Yard/Static` (phase 2 group), Humanoid,
  Collect Objects = Current Object Hierarchy (= `CollectObjects.Children`), Use Geometry = Physics Colliders
  (same as LevelBuilder / Level1.unity). GameManager still rebuilds it at Start (real code, unchanged; phase 9 relies on that).
- build **monster prefabs by hand** in `Prefabs/Gameplay/`: root `Monster_Slime|Ghost|MushroomKing` = `MonsterSpawn` +
  `NavMeshAgent` + `EnemyAI`; child `Model` = the FBX with its hand-made controller `Animations/<Name>.controller`
  (made by duplicating Hero.controller and swapping clips – clip table from CharacterSetup).
- replace the phase-2 empty markers `Gameplay/Monsters/Slime 1` (5.5, 0, 10.5) and `Slime 2` (14, 0, -9.5) with prefab instances.
Bare `MonsterSpawn` markers keep working (spawned by code), so generated levels / Daily and the repo's Level2–4 still work.

## Code changes needed

### 1. `Scripts/Gameplay/EnemyAI.cs` – support hand-placed prefabs (new public method + private helper)
Why: GameManager must set up monsters that already exist in the scene, and their model is already a child.
Add inside the class (e.g. after `Create`):
```csharp
        /// <summary>A Monster prefab you placed in the scene by hand. GameManager calls this when the level starts.</summary>
        public void SetupPlaced(float speedMul, float arenaHalf, WorldMarkers markers)
        {
            if (agent) return;                        // already set up by Create()
            agent = GetComponent<NavMeshAgent>();
            var spawn = GetComponent<MonsterSpawn>();
            Type = spawn ? spawn.type : EnemyType.Patroller;
            home = spawn ? spawn.HomePosition : transform.position;
            this.speedMul = speedMul;
            half = arenaHalf;
            var def = GameAssets.I.Enemy(Type);
            height = def.height;
            catchRadius = Type == EnemyType.Guard ? 1.6f : 1.05f;
            agent.updateRotation = false;             // we turn the monster ourselves
            agent.avoidancePriority = Random.Range(30, 70);
            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas)) agent.Warp(hit.position);

            anim = GetComponentInChildren<Animator>();
            if (anim)
            {
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                FitHeight(anim.gameObject, height);  // Slime 0.95 m, Ghost 1.3 m, King 2.6 m
            }
            Configure();
            Decorate();
            if (markers) BuildBubble(markers);
        }

        /// <summary>Scales a placed model to `height` metres and puts its feet on the monster's origin
        /// (the same thing CharacterFactory.BuildModel does for monsters made by code).</summary>
        void FitHeight(GameObject m, float h)
        {
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.05f) m.transform.localScale *= h / b.size.y;
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            m.transform.position += Vector3.up * (transform.position.y - b.min.y);
            foreach (var smr in m.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
        }
```
`Update()` already returns while `agent` is null, so a placed monster is inert until GameManager calls this.
The guide shows `SetupPlaced` as an excerpt and calls the file "about 370 lines": it is **imported** from
`Resources/Scripts/Helpers/EnemyAI.cs` – please export the changed file there.
NavMeshAgent Inspector values students type (from `Create`): Angular Speed 720, Acceleration 22, Stopping Distance 0.2,
Auto Braking on, Obstacle Avoidance = Low Quality, Radius 0.45 (Guard 0.75), Height 0.95 / 1.3 / 2.6.
Heights for FitHeight/Badge come from `GameAssets.I.Enemy(type).height` (the field defaults are correct even if
the monster CharacterDefs in GoldRushAssets are never filled). `PlaybackSpeed` falls back to a height estimate
when walkSpeed/runSpeed are 0 – fine.

### 2. `Scripts/Gameplay/GameManager.cs` (already inside the full files shown in phase 7 and 8)
a) `Build()`: Player + camera already in the scene – I used exactly phase-03 change 3 + phase-04 change 2, but
   **without** the `PlayerMotor.Create` / `ThirdPersonCamera.Create` fallbacks (the phase 3/4 versions of those
   scripts have no `Create()`, so the fallback would not compile). The final real file can keep the fallbacks.
b) `Build()`, after the `EnemyAI.Create` loop:
```csharp
// monster prefabs you placed in the scene by hand
foreach (var e in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
{
    if (enemies.Contains(e)) continue;
    e.SetupPlaced(Def.enemySpeed, layout.half, UI.Markers);
    enemies.Add(e);
}
```
c) `LevelFromScene()`, first line inside the `MonsterSpawn` loop (a placed prefab also has MonsterSpawn –
   without this it is spawned a second time):
```csharp
if (m.GetComponent<EnemyAI>()) continue; // a placed monster prefab: set up in Build()
```
(The runtime `surface.BuildNavMesh()` stays; `SetupPlaced` warps the agent onto the rebuilt mesh.)

### 3. `Scripts/Gameplay/PlayerMotor.cs` – same as phase-03 changes 1 + 2, written out
- `Awake()`: `anim = GetComponentInChildren<Animator>(); model = anim ? anim.transform : null; renderers = GetComponentsInChildren<Renderer>();`
- `Start()` (phase 8 version): creates `dust` and `boostTrail` with the same `FX.Trail` calls as `Create()`.
- `clipWalkSpeed = 0.59f`, `clipRunSpeed = 0.84f` + private `PlaybackSpeed()` instead of `GameAssets.I.hero.PlaybackSpeed`.
- No `Create()` (and so no `Cosmetics.Apply`) – phase 9/10 restore the final file.

### 4. Suggested: split `Cosmetics` out of `CharacterFactory.cs`
EnemyAI needs `CharacterFactory` (BuildModel, AddGlow). The same file holds `Cosmetics` (shop, needs `Catalog` from the
phase-9 Data.cs and `OriginalMaterials`). Step 8 tells students to import `CharacterFactory.cs` from Helpers "if it is
not there yet". Please either move `Cosmetics` to `Scripts/Gameplay/Cosmetics.cs` (phase 9) or make sure the
Data.cs version that exists in phase 7 already has `Catalog` and that `OriginalMaterials.cs` is imported.

### 5. Assets for the checkpoint package (not code)
- Repo `Prefabs/Level/Gameplay/Monster_*.prefab` are marker-only. The Phase07 package needs the hand-built versions
  in `Prefabs/Gameplay/` (root components + `Model` child + controller), and Level1 with the two prefab instances
  under `Gameplay/Monsters` plus baked NavMesh data on `Static` (repo has `m_NavMeshData: {fileID: 0}`).
- Controllers live in `Animations/` (SPEC/phase 3), the editor tool writes `Art/Characters/Controllers/`.
- Player on layer Ignore Raycast incl. children (phase 3 did it; step 14 re-checks). `EnemyAI.Vision()` raycasts with
  `DefaultRaycastLayers`; the player's own CharacterController would block the ray otherwise.

## Phase versions (compile rule)
| File | Phase 7 version | Phase 8 version |
|------|-----------------|-----------------|
| `GameManager.cs` | real file + changes 2a–c, **minus** power-ups (`powerUps`, `powerTypes`, `PowerUpSpot` loop, `PowerUp.Create` loop, magnet pull, power-up pickup, `CollectPowerUp`) and **minus** `FX.CoinBurst` / `FX.Confetti`. Keeps the `SpeedLeft/GhostLeft/MagnetLeft` timers (never set yet) because EnemyAI, GameUI and PlayerMotor read them. | real file + changes 2a–c |
| `PlayerMotor.cs` | real file + change 3, **minus** all FX (dust, boostTrail, `Start()`, both `FX.Puff`). Adds Hit/knockback/invulnerability blink/PlayCaught/ghost look/SpeedMultiplier compared to phase 3/4. | real file + change 3 (FX back) |

Both shown in full ("replace the whole file with this updated version"). Final real files come back in phase 10 (`Scripts/Final`).

## Assumptions about phases 5–6 (please check when merging)
- By phase 7 these exist (GameUI and its helpers need most of them anyway): GameUI/WorldMarkers/UIKit/Sprites/Tween/Bob,
  Mats + GameAssets(.cs + GoldRushAssets asset with the base materials), Data.cs (EnemyType, PowerType, LevelDef, Levels,
  GameSession, Save), AudioFX, Fader, some DayNight/DayNightButton, EnvironmentSetup, LevelBuilder (LevelLayout), Props,
  VirtualJoystick, TouchLookArea, LevelScene, PlayerSpawn, CoinSpot, Coin, ExitGate, ThirdPersonCamera **with `Init()`,
  `PlayIntro()`, `Shake()`** (GameManager calls them).
- **GameUI.cs references `PowerUp` (ColorOf/NameOf) and `gm.SpeedLeft/GhostLeft/MagnetLeft`, `gm.ArrowTarget`, `gm.Lives`.**
  So `PowerUp.cs` must exist from phase 5 on and the phase 5/6 GameManager must have those members.
- `FX.cs` arrives in phase 8 (SPEC). If phase 5 already imports it, the phase 7 files could keep the FX lines.
- `Badge.cs` and `MonsterSpawn.cs` are new in phase 7 (pasted). MonsterSpawn uses `EnemyAI.SignatureColor`, so the guide
  warns that red errors are expected until step 10.

## Badge decision
Students paste `Badge.cs` (133 lines, pixel-font texture, parented to the chest bone, called from `EnemyAI.Decorate`).
The ITSTEP texture in `Resources/Textures` is only mentioned as optional decoration; no code uses it.

## For other phases
- Phase 9 (Levels 2–4): Ghost / MushroomKing prefabs are ready; a Guard's `Home` can point at an empty.
  Phase 9 currently says "you do not need to bake again, GameManager rebuilds it" – still true with my changes.
- Phase 10: the night glow (`CharacterFactory.AddGlow` + `DayNight.RegisterNightLight`) is already in EnemyAI.

## Screenshots (placeholders)
1. Scene view of Level1 with the blue NavMesh; Inspector shows NavMeshSurface on Static (step 3)
2. Animator window of Slime.controller (Locomotion, Attack, Hit, Win, Die, Any State) (step 5)
3. Inspector of Monster_Slime (Monster Spawn, Nav Mesh Agent, Enemy AI) + Hierarchy with the Model child (step 11)
4. Game view: a Slime with the red "!" bubble chasing the hero, 2 hearts in the HUD (step 15)
