using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    public enum EnemyType { Patroller, Chaser, Guard }
    public enum PowerType { Speed, Ghost, Magnet }

    public static class Scenes
    {
        public const string Menu = "MainMenu";
        public const string Game = "Game";
    }

    // ------------------------------------------------------------------ Levels

    public class LevelDef
    {
        public string title, subtitle;
        public int seed;
        public float size;          // playable square side length (metres)
        public int coins;
        public int patrollers, chasers, guards;
        public int powerUps;
        public int props;           // small props (rocks, crates, barrels, cacti, carts)
        public int bigRocks;        // large rock formations used as cover
        public float enemySpeed = 1f;
        public float parTime;       // seconds, for the "beat par" star
        public int lives = 2;       // survive the first hit
        public bool night;          // starts at night (players can still toggle day/night)
        public string scene;        // hand-made level scene (null = generate from seed)
        public bool daily;
    }

    public static class Levels
    {
        public static readonly LevelDef[] All =
        {
            new LevelDef { title = "Training Yard", subtitle = "Learn the ropes",          seed = 101, scene = "Level1", size = 32, coins = 6,  patrollers = 2,                         powerUps = 1, props = 14, bigRocks = 1, enemySpeed = 0.9f,  parTime = 40 },
            new LevelDef { title = "Ghost Alley",   subtitle = "Watch out for chasers",    seed = 202, scene = "Level2", size = 40, coins = 8,  patrollers = 2, chasers = 1,             powerUps = 2, props = 22, bigRocks = 2, enemySpeed = 1f,    parTime = 60 },
            new LevelDef { title = "King's Vault",  subtitle = "Guards protect the gold",  seed = 303, scene = "Level3", size = 48, coins = 10, patrollers = 2, chasers = 2, guards = 2, powerUps = 3, props = 30, bigRocks = 3, enemySpeed = 1.05f, parTime = 85, night = true },
            new LevelDef { title = "Gold Fever",    subtitle = "The final rush",           seed = 404, scene = "Level4", size = 56, coins = 13, patrollers = 3, chasers = 3, guards = 2, powerUps = 4, props = 38, bigRocks = 4, enemySpeed = 1.12f, parTime = 115, night = true },
        };

        public static string TodayKey => DateTime.Now.ToString("yyyy-MM-dd");

        public static LevelDef Daily()
        {
            var now = DateTime.Now;
            int seed = now.Year * 10000 + now.Month * 100 + now.Day;
            var r = new Rng(seed);
            float size = 38 + r.Int(0, 4) * 5;
            int coins = r.Int(7, 13);
            return new LevelDef
            {
                title = "Daily Rush",
                subtitle = now.ToString("dddd, MMM d"),
                seed = seed,
                size = size,
                coins = coins,
                patrollers = r.Int(1, 4),
                chasers = r.Int(1, 4),
                guards = r.Int(0, 3),
                powerUps = r.Int(2, 4),
                props = Mathf.RoundToInt(size * 0.65f),
                bigRocks = r.Int(2, 4),
                enemySpeed = r.Range(1f, 1.12f),
                parTime = Mathf.Round(coins * 7f + size * 0.6f),
                daily = true,
                night = true,
            };
        }
    }

    public static class GameSession
    {
        /// <summary>0..3 = campaign level, -1 = daily challenge.</summary>
        public static int LevelIndex = 0;
        public static bool IsDaily => LevelIndex < 0;
        public static bool HasNext => !IsDaily && LevelIndex < Levels.All.Length - 1;
        /// <summary>Scene to load for the current level (hand-made scene, or the generated "Game" scene).</summary>
        public static string SceneName => string.IsNullOrEmpty(Current.scene) ? Scenes.Game : Current.scene;
        public static LevelDef Current => IsDaily ? Levels.Daily() : Levels.All[Mathf.Clamp(LevelIndex, 0, Levels.All.Length - 1)];
    }

    // ------------------------------------------------------------------ Random

    /// <summary>Deterministic random helper (seeded layouts).</summary>
    public class Rng
    {
        readonly System.Random r;
        public Rng(int seed) { r = new System.Random(seed); }
        public float Value => (float)r.NextDouble();
        public float Range(float a, float b) => a + (b - a) * Value;
        public int Int(int minInclusive, int maxExclusive) => r.Next(minInclusive, maxExclusive);
        public bool Chance(float p) => Value < p;
        public T Pick<T>(IList<T> list) => list[r.Next(list.Count)];
        public Vector2 InsideCircle(float radius)
        {
            float a = Range(0, Mathf.PI * 2), d = Mathf.Sqrt(Value) * radius;
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }
    }

    // ------------------------------------------------------------------ Save

    [Serializable]
    public class LevelProgress
    {
        public int stars;
        public float bestTime = -1f;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int unlocked = 1;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public List<string> owned = new List<string>();
        public string color = "color_blue";
        public string hat = "hat_none";
        public bool music = true;
        public bool sfx = true;
        public bool seenTutorial;
        public string dailyDate = "";
        public float dailyBest = -1f;
        public bool dailyCleared;
    }

    public static class Save
    {
        const string Key = "goldrush_save_v1";
        static SaveData data;

        public static SaveData Data
        {
            get { if (data == null) Load(); return data; }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            data = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("[GoldRush] Save corrupted, resetting: " + e.Message); }
            }
            if (data == null) data = new SaveData();
            Fix();
        }

        static void Fix()
        {
            if (data.levels == null) data.levels = new List<LevelProgress>();
            while (data.levels.Count < Levels.All.Length) data.levels.Add(new LevelProgress());
            if (data.owned == null) data.owned = new List<string>();
            if (!data.owned.Contains("color_blue")) data.owned.Add("color_blue");
            if (!data.owned.Contains("hat_none")) data.owned.Add("hat_none");
            if (!data.owned.Contains(data.color)) data.color = "color_blue";
            if (!data.owned.Contains(data.hat)) data.hat = "hat_none";
            data.unlocked = Mathf.Clamp(data.unlocked, 1, Levels.All.Length);
            if (data.dailyDate != Levels.TodayKey)
            {
                data.dailyDate = Levels.TodayKey;
                data.dailyBest = -1f;
                data.dailyCleared = false;
            }
        }

        public static void Persist()
        {
            if (data == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { data = null; }
    }

    // ------------------------------------------------------------------ Shop

    public class ShopItem
    {
        public readonly string id, name;
        public readonly int price;
        public readonly Color color;
        public readonly bool isHat;
        public ShopItem(string id, string name, int price, Color color, bool isHat = false)
        {
            this.id = id; this.name = name; this.price = price; this.color = color; this.isHat = isHat;
        }
    }

    public static class Catalog
    {
        public static readonly ShopItem[] Colors =
        {
            new ShopItem("color_blue",   "Classic", 0,   new Color(0.25f, 0.55f, 0.95f)),
            new ShopItem("color_orange", "Sunset",  30,  new Color(1f, 0.55f, 0.2f)),
            new ShopItem("color_mint",   "Mint",    30,  new Color(0.3f, 0.85f, 0.65f)),
            new ShopItem("color_purple", "Royal",   50,  new Color(0.6f, 0.35f, 0.9f)),
            new ShopItem("color_red",    "Ruby",    50,  new Color(0.9f, 0.2f, 0.25f)),
            new ShopItem("color_black",  "Shadow",  80,  new Color(0.15f, 0.15f, 0.18f)),
            new ShopItem("color_gold",   "Golden",  200, new Color(1f, 0.78f, 0.2f)),
        };

        public static readonly ShopItem[] Hats =
        {
            new ShopItem("hat_none",   "No Hat",  0,   new Color(0.5f, 0.5f, 0.5f), true),
            new ShopItem("hat_cowboy", "Cowboy",  60,  new Color(0.55f, 0.33f, 0.18f), true),
            new ShopItem("hat_miner",  "Miner",   80,  new Color(1f, 0.8f, 0.1f), true),
            new ShopItem("hat_top",    "Top Hat", 100, new Color(0.12f, 0.12f, 0.14f), true),
            new ShopItem("hat_crown",  "Crown",   250, new Color(1f, 0.82f, 0.25f), true),
        };

        public static ShopItem Find(string id)
        {
            foreach (var i in Colors) if (i.id == id) return i;
            foreach (var i in Hats) if (i.id == id) return i;
            return null;
        }
    }
}
