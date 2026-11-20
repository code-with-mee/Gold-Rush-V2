# V3 notes: phases 8–10 (package-driven)

Pages rewritten: `phase-08.html`, `phase-09.html`, `phase-10.html` (format: import → check → how it works → Press Play → try it → common problems).
Also updated: `index.html` ("How to use this course" + the checkpoint-packages paragraph).

All `data-select` values were checked against `Assets/GoldRush/Scenes/Level1.unity` (exact Hierarchy paths) or exist as files under `Assets/`.

## Screenshot placeholders

`filled` = a real screenshot from the old page is kept (`<figure class="shot" ...><img>`), with the data attributes added so the tool can recapture it. The tool may save the new capture under the new `data-shot` id; then update the `src`.

| data-shot | window | data-select | status | what it should show |
|---|---|---|---|---|
| phase-08/check-1 | hierarchy | `Level 1 - Training Yard/Gameplay/PowerUps/PowerUp Magnet` | filled (`images/phase-08/step-07-b.png`) | Gameplay › PowerUps › PowerUp Magnet selected |
| phase-08/check-2 | inspector | `Level 1 - Training Yard/Gameplay/PowerUps/PowerUp Magnet` | todo | Transform + Power Up Spot (Type = Magnet) |
| phase-08/check-3 | scene | `Level 1 - Training Yard/Gameplay/PowerUps/PowerUp Magnet` | todo | red wire-ball gizmo (framed with F) |
| phase-08/check-4 | inspector | `Assets/GoldRush/Prefabs/Gameplay/PowerUp_Magnet.prefab` | filled (`images/phase-08/step-06.png`) | model prefab, no script, preview |
| phase-08/check-5 | project | `Assets/GoldRush/Scripts/World/PowerUp.cs` | todo | Scripts/World folder, PowerUp.cs selected |
| phase-08/check-6 | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | todo | Particle field = GR_Particle |
| phase-09/check-1 | hierarchy | `GameUI` | filled (`images/phase-09/step-05-a.png`) | GameUI + EventSystem at the root |
| phase-09/check-2 | inspector | `GameUI` | filled (`images/phase-09/step-05-b.png`) | Canvas Sort Order 10, Scaler 1920×1080 Match 0.5, Game UI |
| phase-09/check-3 | game | `GameUI/HUD` | todo | Game view in edit mode with the HUD chips |
| phase-09/check-4 | hierarchy | `GameUI/Modal` | todo | Modal expanded: Countdown, Intro, Lose, Pause, Win (greyed) |
| phase-09/check-5 | inspector | `GameUI/Modal/Pause/Panel/Glass/RESUME` | todo | Button, Button FX, Game Action (resume) |
| phase-09/check-6 | inspector | `GameUI/HUD/DayNight` | todo | Button + Day Night Button (Sun, Moon) |
| phase-09/check-7 | inspector | `GameUI/WorldMarkers` | todo | World Markers (Root = WorldMarkers) |
| phase-10/check-1 | inspector | `Assets/GoldRush/Resources/DayNightPalette.asset` | filled (`images/phase-10/step-03.png`) | Entries = 20 |
| phase-10/check-2 | inspector | `Level 1 - Training Yard` | todo | Level Scene (Arena Half 16) + Day Night Tint |
| phase-10/check-3 | inspector | `Level 1 - Training Yard/Static/Lamps/LampPost/LampLight` | todo | Light + Night Light (2.4) |
| phase-10/check-4 | hierarchy | `GameUI/MoveArea (left half)` | filled (`images/phase-10/step-06-a.png`) | GameUI children order |
| phase-10/check-5 | inspector | `GameUI/MoveArea (left half)` | todo | Image (alpha 0) + Virtual Joystick (Radius 120) |
| phase-10/check-6 | inspector | `Assets/GoldRush/Resources/GoldRushAssets.asset` | todo | Sounds list open, `music` visible |
| phase-10/check-7 | project | `Assets/GoldRush/Scenes/Level2.unity` | todo | Scenes folder: Game, Level1–4, MainMenu |
| phase-10/check-8 | buildprofiles | `Assets/GoldRush/Scenes/MainMenu.unity` | todo | Scene List with 6 scenes, MainMenu at 0 |

Capture hints:
- `Level 1 - Training Yard/Static/Lamps/LampPost/LampLight`: there are 8 objects with this path (all `LampPost`); the first match is fine.
- phase-09/check-3: the dialogs, Intro, Countdown, Hint, Arrow and Power0–2 are inactive in the saved scene, so the Game view shows only the active HUD chips. That is what the caption says.
- phase-10/check-8 needs the six scenes in the Scene List of the capture project.

Game images reused (not placeholders): `05_level1_play.jpg` (p8), `04_level1_start.jpg`, `09_pause.jpg`, `07_win.jpg`, `10_lose.jpg` (p9), `01_title.jpg`, `05b_level1_night.jpg`, `08_level2.jpg`, `11_level4.jpg`, `12_daily.jpg` (p10).

## Facts checked in the source (for the code owner)
- Level1 `PowerUp Magnet` has **no** PowerUp child: `GameManager` builds the orb with `PowerUp.Create()` at Play. `Prefabs/Gameplay/PowerUp_*` are models without the `PowerUp` script; `Prefabs/Markers/PowerUp_*` carry `PowerUpSpot`. The page says this.
- Phase 8 package has no `GameUI` in the scene, so the HUD is built by `GameUI.Create()` (pages say "made by code for now").
- `DayNightPalette.asset` has 20 entries (the old page listed 12). First = M_Floor, DBDEE6 / 212B52.
- `AudioFX` has no scene object (created at runtime as `[AudioFX]`), so the music check uses `GoldRushAssets` → Sounds.

## Code changes needed
None.
