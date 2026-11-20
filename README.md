# Gold Rush

A small third-person mobile game made in **Unity 6** (URP). You play a cute hero who has to grab
every gold coin in the arena, avoid the monsters, and escape through the exit gate.

Almost everything is built **from code**: the levels, the user interface, the sound effects and the
music. Only the characters are imported 3D models. That makes this project a good one to read
from top to bottom.

---

## 1. Open and play

1. Install **Unity 6000.6.10f1** or newer from Unity Hub. Older versions may not open the project.
2. In Unity Hub choose **Add → Add project from disk** and pick this folder.
3. Open `Assets/GoldRush/Scenes/MainMenu.unity` and press **Play**.
   You can also use the top menu: **Gold Rush → Play**.

> The first time the project opens, the menu **Gold Rush → Setup Project** runs by itself. It creates
> the materials, animator controllers, scenes and build settings. If something ever looks broken,
> run it again.

### Controls

| Action        | Phone / tablet                            | Computer (keyboard + mouse)                  |
|---------------|-------------------------------------------|----------------------------------------------|
| Move          | Touch the **left half** (stick appears)   | `W A S D` / arrow keys                       |
| Look around   | Drag on the **right half**                | **Move the mouse** (cursor is hidden in play)|
| Sprint        | Push the stick all the way to the edge    | `Shift`                                      |
| Day / night   | Sun / moon button                         | `N`                                          |
| Pause         | Pause button                              | `Esc` (also frees the mouse cursor)          |

### How to win
- Collect **all the gold**. When you have it all, the **EXIT** gate opens. Run through it.
- A monster touch costs a heart. You have **2 hearts**, so you survive the first hit.
- You earn **stars** for: ① finishing the level ② beating the par time ③ not getting hit.
- The gold you collect is saved. Spend it in the **Shop** on colours and hats.
- Power-ups: **SPEED** makes you faster, **GHOST** makes monsters lose sight of you, and **MAGNET**
  pulls nearby coins to you.

---

## 2. Project layout

Everything that belongs to the game lives in `Assets/GoldRush/`:

```
Assets/GoldRush/
├── Art/Characters/      imported models (Quaternius, CC0) + generated animator controllers
├── Editor/              editor-only tools (project setup, model import rules, previews)
├── Materials/           5 base materials that the code copies and tints at runtime
├── Resources/           GoldRushAssets.asset — the references the code loads at runtime
├── Scenes/              MainMenu.unity and Game.unity (each holds just ONE GameObject)
└── Scripts/
    ├── Core/            data, save system, tween helper, materials, scene fader
    ├── Audio/           synthesised sound effects + music (no audio files!)
    ├── World/           level generator, props, coins, power-ups, exit gate, day/night
    ├── Gameplay/        game rules, player, monster AI, camera, particles, characters
    ├── UI/              HUD, menus, joystick and buttons (all built in code)
    └── Dev/             automated smoke test used while developing
```

`Assets/Settings/` holds the URP render pipeline assets that come with Unity's URP template.

---

## 3. How it works (the big picture)

```
MainMenu scene                         Game scene
┌──────────────┐  PLAY / level card   ┌──────────────────────────────────────────┐
│ GoldRushMenu │ ───────────────────▶ │ GameManager                              │
│  - builds 3D │   GameSession        │  1. LevelBuilder.Build(level)  → arena   │
│    stage     │   .LevelIndex        │  2. PlayerMotor.Create         → hero    │
│  - title /   │                      │  3. ThirdPersonCamera.Create             │
│    levels /  │ ◀─────────────────── │  4. EnvironmentSetup.Apply     → light   │
│    shop UI   │   MENU button        │  5. GameUI.Create              → HUD     │
└──────────────┘                      │  6. coins, power-ups, EnemyAI monsters   │
                                      │  Update(): timer, pickups, win / lose    │
                                      └──────────────────────────────────────────┘
```

Each scene file contains only **one GameObject** with one script. That script builds everything
else in `Start()`, so you can follow the code instead of hunting through the Inspector.

### Read the code in this order

| # | File | What you learn |
|---|------|----------------|
| 1 | `Core/Data.cs` | Level settings (`Levels.All`), the save system (`PlayerPrefs` + JSON), shop items |
| 2 | `Gameplay/GameManager.cs` | The game loop: states (Intro → Playing → Won/Lost), pickups, scoring |
| 3 | `Gameplay/PlayerMotor.cs` | `CharacterController` movement, sprint/stamina, animation parameters |
| 4 | `Gameplay/ThirdPersonCamera.cs` | Orbit camera, smoothing, wall collision, screen shake |
| 5 | `Gameplay/EnemyAI.cs` | `NavMeshAgent` AI as a **state machine**: Patrol → Alert → Chase → Search |
| 6 | `World/LevelBuilder.cs` | Seeded random level layout + runtime NavMesh baking |
| 7 | `World/DayNight.cs` | Blending lighting, fog and colours between day and night in real time |
| 8 | `UI/UIKit.cs`, `UI/GameUI.cs` | Building uGUI from code: canvases, anchors, buttons, tweens |
| 9 | `Audio/Synth.cs` | Making sound from maths (sine/square waves + envelopes) |
| 10 | `Editor/CharacterSetup.cs` | Making an `AnimatorController` (blend tree + triggers) from code |

### The three monsters (`EnemyAI.cs`)

| Monster | Role | Behaviour |
|---------|------|-----------|
| 🟢 Slime | Patroller | Slow. Only sees what is **in front** of it. |
| 🩷 Ghost | Chaser | Fast. Sees **all around** itself. |
| 🟡 Mushroom King | Guard | Big. Stays near its gold and goes back home if you run too far. |

Every monster wears an **ITSTEP** name patch (see `Gameplay/Badge.cs`). The letters come from a tiny
pixel font defined in code.

---

## 4. Exercises for students

Start with the easy ones:

1. **Tune a level.** In `Core/Data.cs`, change Level 1 to have 10 coins and 4 slimes. Play it.
2. **New shop colour.** Add a `ShopItem` to `Catalog.Colors` (for example "Ocean", price 40).
3. **Change the badge.** Make the monsters wear your name. You will need to add letters to `Badge.Glyphs`.
4. **Faster ghosts.** In `EnemyAI.Configure()`, change the chaser's `chaseSpeed`. How does that change the game?
5. **New power-up.** Add `PowerType.Freeze` that stops every monster for 4 seconds
   (look at how `GhostLeft` is used in `GameManager`).
6. **New sound.** In `Synth.cs`, make a "freeze" sound for your power-up.
7. **Level 5.** Add a fifth `LevelDef` and make sure the level select screen still fits.
8. **Harder AI.** Let a monster that spots you call nearby monsters to join the chase.

---

## 5. Useful editor menus (`Gold Rush` in the top menu bar)

| Menu | What it does |
|------|--------------|
| Setup Project | (Re)creates materials, controllers, scenes and build settings |
| Play | Starts from the main menu, whatever scene is open |
| Run Smoke Test | Plays through the game by itself and saves screenshots to `Temp/gr_smoke/` |
| Render Character Preview | Saves pictures of the characters to `Temp/gr_preview/` |
| Reset Save Data / Give 500 Gold | Testing helpers |

---

## 6. Credits

- Characters: **Quaternius** (https://quaternius.com), CC0 / public domain.
  See `Assets/GoldRush/Art/Characters/Licenses/`.
- Everything else (code, levels, UI, sound and music) was made for this project.
