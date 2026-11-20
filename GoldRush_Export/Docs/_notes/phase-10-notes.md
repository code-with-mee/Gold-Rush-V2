# Phase 10 notes: Polish and build

Guide: `phase-10.html` (18 steps): day/night (DayNight, NightLight, DayNightTint, DayNightPalette, EnvironmentSetup,
DayNightButton), touch (VirtualJoystick, TouchLookArea), music (AudioFX), final scripts, final checks,
Build Profiles for PC and Android. Code blocks are generated from the real files in `Assets/GoldRush/Scripts`
(HTML-escaped), so they match the repo exactly, except the one patch below.

## Code changes needed

### 1. `Scripts/World/DayNightPalette.cs`: add `[CreateAssetMenu]`
Students create `Assets/GoldRush/Resources/DayNightPalette.asset` by hand (LevelBaker does it in the repo).
The guide already shows this line above the class:
```csharp
[CreateAssetMenu(fileName = "DayNightPalette", menuName = "Gold Rush/Day Night Palette")]
public class DayNightPalette : ScriptableObject
```

### 2. Resources pack: `GoldRush_Export/Resources/Scripts/Final/`
Step 13 tells students to copy the folders `Core`, `Audio`, `World`, `Level`, `Gameplay`, `UI` from
`Resources/Scripts/Final/` over `Assets/GoldRush/Scripts` (Replace all). Please export every runtime script
there (all of `Assets/GoldRush/Scripts` **except** `Dev/SmokeTester.cs`; no `Editor/`), after the menu refactor
(phase 9 notes: hand-built scene support + LogoTitle) and the change above are applied.

### 3. `AudioFX.cs` and the .wav clips
The real `AudioFX.Init` uses `clips = Synth.BuildAll();`. The SPEC says the code will load the exported .wav clips.
Step 12 pastes the real file as it is now. If you change the clip loading, tell me and I regenerate the page
(the page is built from the source file, so it is a one-command update).

## Please check (depends on other phases)
- Step 13 "files that had a phase version" table lists: GameManager, PlayerMotor, ThirdPersonCamera, EnemyAI,
  DayNight, NightLight, DayNightTint, DayNightPalette, EnvironmentSetup, ExitGate, PowerUp, Coin, GameUI,
  GoldRushMenu, DayNightButton, VirtualJoystick, TouchLookArea, ButtonFX, LogoTitle, MenuAction, AudioFX, Data, Fader.
  This is a best guess: please reconcile it with the "phase versions" lists in phase-03…phase-08 notes.
- `DayNight` is used by PlayerMotor (lantern), EnemyAI (glow), ExitGate, GameUI (via DayNightButton) and
  EnvironmentSetup, so earlier phases must already have some version of DayNight/DayNightButton/VirtualJoystick/
  TouchLookArea for GameUI to compile. Phase 10 says "replace the whole file (or create it)".
- Palette table (step 6) uses phase-1 material names `M_Floor`, `M_Wall`, `M_Block` and "Red/Orange/Teal accent".
  Colours are from `Props.cs`: Floor DBDEE6→212B52, Wall CCD1DB→525C80, Block (Tower) B2BAC9→303859,
  Red ED4C4C→9E2B42, Orange FF9938→B86133, Teal 33BDBD→1A6B85.
- LampPost prefab: `LampLight` point light colour FFE0B3, range 9, NightLight nightIntensity 2.4 (from the prefab).
- Android orientation settings from `GoldRushSetup.cs` (Auto Rotation, landscape left/right only).

## Screenshots (placeholders)
1. Inspector of Resources/DayNightPalette with 6 entries (material, day colour, night colour)
2. Device Simulator (landscape phone) with the joystick visible on the left half
3. Build Profiles window with Windows selected, Scene List and the Build button
4. Player Settings for Android, Resolution and Presentation with only Landscape Left/Right ticked

## Regenerating the pages
Both pages were built by a small script that inserts the real source files into the page. If code changes, the
code blocks can be regenerated from `Assets/GoldRush/Scripts` (ask the guide writer).
