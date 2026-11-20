# Phase 5 notes: Coins and the HUD

Guide: `phase-05.html` (17 steps). Sources I read: `World/Coin.cs`, `Level/CoinSpot.cs`, `Level/LevelScene.cs`,
`Gameplay/GameManager.cs`, `UI/GameUI.cs`, `UI/UIKit.cs`, `Audio/AudioFX.cs`, `Core/Data.cs`, `Core/GameAssets.cs`,
`Gameplay/ThirdPersonCamera.cs`, `Prefabs/Level/Gameplay/*.prefab`, `Scenes/Level1.unity` (coin positions),
plus `phase-00..04` and `phase-07..10` notes and the phase 3/4 pages (to match their code).

**Compile check:** every phase 5 and phase 6 file set was compiled with Roslyn (`csc`) against the Unity 6000.6.10f1
DLLs (UnityEngine modules, UnityEngine.UI, URP, AI Navigation). The baseline was the phase 4 versions of `PlayerMotor`
and `PlayerSpawn` taken from `phase-03.html` / `phase-04.html`. The phase 6 file set and the half-way state (phase 6
steps 4–10 done, step 11 not done yet) both compile. I did not run Unity.

## Design decisions
- **Full `GameUI.cs` is imported in phase 5**, as phases 7–10 expect (see their notes). That means phase 5 also has to
  import the helpers GameUI needs, and add two small **placeholders** for `PowerUp` (phase 8) and `ExitGate` (phase 6).
- **Coins are placed by hand** (SPEC "Provided prefabs"): students open the **provided** `Coin` prefab, add the
  `Coin` component and wire `Visual` (step 13), then drop it inside each `CoinSpot` marker (local 0,1,0, step 14).
  The GameManager uses those coins. The real game spawns coins at runtime with `Coin.Create` (code change 4b).
- Pickup stays **distance-based** (1.45 m, no colliders), as in the real code. The SPEC table says "trigger"; the
  guide explains why there is no trigger.
- **Sounds** come from `Art/Audio/*.wav` through a new `GameAssets.sounds` list (code changes 1 + 2).
  `GoldRushAssets.asset` is created by hand in phase 5 (in `Assets/GoldRush/Resources/`), with the GR_ base materials
  from phase 1 and the 16 sounds. Phase 5 is the first phase that needs the asset (AudioFX, EnvironmentSetup skybox).
- The phase 4 camera is replaced by the **real ThirdPersonCamera + `Init()`/`Start()`** (= phase 4 code change 1),
  because Tween now exists. This also removes the phase 4 cursor block (GameManager owns the cursor from now on),
  as the phase 4 notes asked.
- Level1 is added to **Build Profiles → Scene List** in phase 5, because the pause dialog already has RETRY.

## Provided prefabs I assume (starter package, phase 0)
- `Assets/GoldRush/Prefabs/Gameplay/Coin.prefab` – **visual only, no scripts, no colliders**:
  root `Coin` (at 0,0,0) › child `Visual` (0,0,0) › `Rim` (Cylinder, rot 90,0,0, scale 0.72,0.06,0.72, M_Gold),
  `Face` (Cylinder, rot 90,0,0, scale 0.5,0.085,0.5, M_GoldDark, no shadows), `Star` (Cube, rot 0,0,45, scale 0.2, M_Gold,
  no shadows). Values = `Coin.Create`. The script needs the child named `Visual` (it moves/spins only that).
  Students add `Coin` to the root in step 13.
- (Phase 6) `ExitGate.prefab` – see phase-06-notes.md.

## Code changes needed
1. **`Scripts/Core/GameAssets.cs`**: add `[CreateAssetMenu(fileName = "GoldRushAssets", menuName = "Gold Rush/Game Assets")]`
   on the class (same as phase-01 notes change 2) and, under `public Material skybox;`:
   ```csharp
   [Header("Sounds (Art/Audio). The file name is the sound id, e.g. coin")]
   public AudioClip[] sounds;
   ```
   Optional: change the summary and the "GoldRushAssets missing" error text (the guide shows:
   `"[GoldRush] GoldRushAssets missing. Create it in Assets/GoldRush/Resources (Create > Gold Rush > Game Assets)."`).
   Why: students create the asset by hand and need a place for the .wav clips.
2. **`Scripts/Audio/AudioFX.cs`** `Init()`: replace `clips = Synth.BuildAll();` with
   ```csharp
   // the sound files (Art/Audio/*.wav) are listed in the GoldRushAssets asset
   clips = new Dictionary<string, AudioClip>();
   var assets = GameAssets.I;
   if (assets && assets.sounds != null)
       foreach (var clip in assets.sounds)
           if (clip) clips[clip.name] = clip; // "coin.wav" -> id "coin"
   ```
   Why: SPEC says the clips come from the exported .wav files. `Synth.cs` is then only needed to export them.
   The repo's own `GoldRushAssets.asset` must get the 16 clips too (or keep a Synth fallback when `sounds` is empty).
3. **`Scripts/World/Coin.cs`**: add
   ```csharp
   void Start()
   {
       // a coin placed by hand gets its own random start, so the coins don't all bob together
       if (phase == 0f) phase = Random.Range(0f, Mathf.PI * 2f);
   }
   ```
   Why: `phase` is only randomised in `Create()`; hand-placed coins would all bob in sync.
4. **`Scripts/Gameplay/GameManager.cs`** (final file):
   a) `Build()`: use the Player and the Main Camera that are already in the scene = phase-03 notes change 3 + phase-04
      notes change 2 (my phase versions do exactly that, without the `Create` fallback).
   b) `LevelFromScene()`: a CoinSpot with a Coin inside uses that coin; an empty CoinSpot still spawns one
      (keeps Level2–4 / generated levels working). Replace the CoinSpot line with:
   ```csharp
   foreach (var c in scene.GetComponentsInChildren<CoinSpot>())
   {
       var placed = c.GetComponentInChildren<Coin>();
       if (placed) coins.Add(placed);               // a Coin prefab placed by hand
       else l.coinPoints.Add(c.transform.position); // empty marker: Build() spawns a coin here
   }
   ```
   (`coins` is a field, so this works; `Build()` then adds the spawned ones from `layout.coinPoints` as before.)
   And change the warning to `if (l.coinPoints.Count == 0 && coins.Count == 0)`.
5. **Resources pack `Scripts/Helpers/`** must contain (real files, unchanged): `Tween.cs`, `TweenRunner.cs`, `Fader.cs`,
   `UIKit.cs`, `GameUI.cs`, `ButtonFX.cs`, `CoinFlip.cs`, `Bob.cs`, `WorldMarkers.cs`, `DayNightButton.cs`,
   `VirtualJoystick.cs`, `TouchLookArea.cs`, `DayNight.cs`, `NightLight.cs`, `EnvironmentSetup.cs`.
   Note: `GameUI.cs` and `UIKit.cs` were being edited while I wrote this; the guide only names the public calls
   (`Create`, `SetCoins`, `FlyCoin`, `Toast`, `ShowPause`, `Intro`, `ShowWin`, `ShowLose`, `AddGateLabel`, `SetLives`).
   If one of those changes, update steps 6/11.

## Phase versions (compile rule)
| File | Phase 5 version | Later |
|------|-----------------|-------|
| `Gameplay/GameManager.cs` | Real file minus: LevelBuilder fallback/`LevelLayout`, NavMesh rebuild, monsters, power-ups (timers kept, never set), `IntroCo`, gate open + win check, Win/Lose, `OnPlayerCaught`, tension, FX, `AddLantern`, `StartMusic`, debug hooks. Uses `LevelScene` + `PlayerSpawn` + scene Player/Camera (change 4a), coins from CoinSpot children (4b). Keeps `RunResult`, `Pause/Resume/Restart/BackToMenu/NextLevel`, `ArrowTarget`, `Lives`, `SetLives`, `FloatText` because GameUI needs them. Toast text: "Well done! The exit gate comes in phase 6." | phase 6 |
| `World/ExitGate.cs` | **Placeholder**: `IsOpen`, `WinPoint`, `MarkerAnchor => transform`, `Open()`. | phase 6 |
| `World/PowerUp.cs` | **Placeholder**: `Type`, real `ColorOf`, `NameOf`. | phase 8 (real file) |
| `World/Coin.cs` | Real file **without `Create()`** (needs Props/Mats, not imported yet) + change 3. | real file when LevelBuilder/Props/Mats are imported (phase 7?) |
| `Gameplay/ThirdPersonCamera.cs` | Real file + `Init()`/`Start()` (phase-04 change 1, with the `curDist <= 0` guard). Final. | – |
| `Core/Data.cs`, `Level/CoinSpot.cs`, `Level/LevelScene.cs` | Real files. | – |
| `Core/GameAssets.cs`, `Audio/AudioFX.cs` | Real files + changes 1 / 2. | – |
| Phase 3/4 stubs `VirtualJoystick.cs`, `TouchLookArea.cs` | Replaced by the real helper files in step 6 (as phase 3/4 notes asked). | – |

## For other phases (please check when merging)
- **Not imported in phases 5–6:** `LevelBuilder.cs`, `Props.cs`, `Mats.cs` (+`ProcTex`), `DayNightTint.cs`,
  `DayNightPalette.cs`, `FX.cs`, `CharacterFactory.cs`. Phase 7 assumes LevelBuilder/Props/Mats exist. When they are
  imported, `Coin.cs` and `ExitGate.cs` must become the **real** files (LevelBuilder calls `Coin.Create` and
  `ExitGate.Create`), and `Mats` needs the GR_ materials in GoldRushAssets (done in phase 5 step 12).
- Phase 7: remove the phase 6 test line `if (Input.GetKeyDown(KeyCode.L)) DebugLose();` and `DebugLose()`.
  Keep `PlayVictory()` in the phase 7 PlayerMotor (added in phase 6).
- Phase 10 (music): my GameManagers do not call `AudioFX.StartMusic()`; the real `Build()` does. The `music` clip is
  already in the Sounds list.
- Phase 8: `PowerUp.cs` exists as a placeholder since phase 5 → "replace the whole file".
- The HUD moon button (DayNightButton) already toggles night in phase 5 (no tints yet); phase 10 explains it.
- Phase 2's Global Volume was deleted; `EnvironmentSetup.Apply` (called by my GameManager) adds the PostFX volume.

## Screenshot placeholders
1. GoldRushAssets Inspector – base materials filled, Sounds list with the 16 clips (step 12)
2. Provided Coin prefab in prefab mode – Hierarchy Coin › Visual › Rim, Face, Star; Coin component with Visual assigned (step 13)
3. Scene view of Level1 from above with the 6 CoinSpot gizmos and coins; Hierarchy Gameplay › Coins › CoinSpot 1 › Coin (step 14)
4. Game view while playing – hero next to a coin, "+1", HUD "3 / 6", hearts, timer (step 16)
