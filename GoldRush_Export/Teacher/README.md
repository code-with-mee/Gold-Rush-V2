# Teacher only — do not share with students

One solution package per practical. Each holds the finished level scene (and the assets it uses, no scripts).
Import it into a project that has the **Phase 10** package, then add the scene to Build Profiles and the LevelDef line
in `Data.cs` (shown in the practical guide) to play it from the menu — or just open the scene and press Play.

| Package | Practical | Scene |
|---|---|---|
| GoldRush_Practical1_Solution | P1 Coin Garden | Level5 |
| GoldRush_Practical2_Solution | P2 The Maze | Level6 |
| GoldRush_Practical3_Solution | P3 Guard Tower | Level7 (built) |
| GoldRush_Practical4_Solution | P4 Night Run | Level8 (built) |

## P3 and P4: LevelDef lines

Add these at the end of `Levels.All` in `Assets/GoldRush/Scripts/Core/Data.cs`, then put `Scenes/Level7` and `Scenes/Level8`
in File → Build Profiles → Scene List (under the other levels):

```csharp
new LevelDef { title = "Guard Tower", subtitle = "Sneak past the King", seed = 707, scene = "Level7", size = 32, coins = 9, patrollers = 1, guards = 1, powerUps = 1, enemySpeed = 1f, parTime = 70 },
new LevelDef { title = "Night Run", subtitle = "Gold under the moon", seed = 808, scene = "Level8", size = 36, coins = 12, patrollers = 2, chasers = 1, guards = 1, powerUps = 1, enemySpeed = 1.08f, parTime = 100, night = true },
```

Without the line the scene still plays when you open it and press Play, but the intro and the par time come from another level.

**Level7 (Guard Tower)** is built exactly from the tables in practical-3. It was play-tested: the King patrols inside his
4.5 m circle and gives up a chase about 13 m from KingHome, then walks back. With GHOST active he walks past you. No monster sees
the start, and all 9 coins can be reached. A scripted run (sprinting, no dodging) won in 24 s with 1 hit, so the par of 70 s is fine for students.

**Level8 (Night Run)** is the base level from the tables in practical-4, plus a small example of step 15 "Your own idea" (group `Static/Market`):
1. Night market: two stalls (FloorPad_Cyan + Terminal, "Stall West" at -7, 0, -13 and "Stall East" at 9, 0, -13) light up the start square at night.
2. A secret: "Secret Wall" (Wall_4m, rot 90 at 15, 0, 11.5) turns the NE building into a U, so coin 4 is seen only from the north.
3. A hiding tower (Block_2x2x2 + Block_1x1x1 on top, at 4, 0, 12.5) gives cover from the Ghost on the last run to the gate.

The coin count and the LevelDef stay the same. It was play-tested at night. The MAGNET from 8.5 m east of KingHome pulls both
plaza coins. In one of two tests the King saw you there, so be fast. The King stays home, no monster sees the start, and all 12 coins can be reached.
A scripted run won in 27 s with 1 hit (from the Ghost, which roams the whole arena), so the par of 100 s is fine.

Pictures (teacher only): `images/level7-top.png`, `images/level7-game-vault.png`, `images/level8-top.png`, `images/level8-game-magnet.png`.
