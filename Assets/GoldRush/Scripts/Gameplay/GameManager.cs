using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    public enum GameState { Intro, Playing, Paused, Won, Lost }

    public struct RunResult
    {
        public bool won;
        public int stars;
        public bool parBeaten, noHits;
        public float time, best, par;
        public bool newBest;
        public int collected, total;
        public int goldEarned, bonus;
        public bool hasNext;
    }

    /// <summary>Gameplay scene root: builds the level, owns game state, rules, scoring and saving.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public const float SpeedDuration = 6f, GhostDuration = 6f, MagnetDuration = 8f;

        public LevelDef Def { get; private set; }
        public GameState State { get; private set; } = GameState.Intro;
        public float Elapsed { get; private set; }
        public int Collected { get; private set; }
        public int TotalCoins { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get; private set; }
        public int Hits { get; private set; }

        public PlayerMotor Player { get; private set; }
        public ThirdPersonCamera Cam { get; private set; }
        public GameUI UI { get; private set; }
        public ExitGate Gate { get; private set; }

        public float SpeedLeft { get; private set; }
        public float GhostLeft { get; private set; }
        public float MagnetLeft { get; private set; }
        public bool SpeedActive => SpeedLeft > 0f;
        public bool GhostActive => GhostLeft > 0f;
        public bool MagnetActive => MagnetLeft > 0f;
        public float SpeedMultiplier => SpeedActive ? 1.55f : 1f;

        readonly List<Coin> coins = new List<Coin>();
        readonly List<PowerUp> powerUps = new List<PowerUp>();
        readonly List<EnemyAI> enemies = new List<EnemyAI>();
        LevelLayout layout;
        readonly List<PowerType> powerTypes = new List<PowerType>();
        bool chasedLastFrame;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            // pressing Play directly in Level2.unity etc. should play that level
            string active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            for (int i = 0; i < Levels.All.Length; i++)
                if (Levels.All[i].scene == active) GameSession.LevelIndex = i;
            Def = GameSession.Current;
        }

        // ---------------------------------------------------------------- mouse cursor (computer only)

        /// <summary>True on computers: hide + lock the cursor while playing so the mouse turns the camera.</summary>
        static bool UseMouseLook => !Application.isMobilePlatform && !Input.touchSupported;

        static void LockCursor(bool locked)
        {
            if (!UseMouseLook) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus && State == GameState.Playing) LockCursor(true);
        }

        void OnDestroy()
        {
            LockCursor(false);
            if (I == this) I = null;
            Time.timeScale = 1f;
            AudioFX.SetTension(false);
        }

        void Start()
        {
            Build();
            StartCoroutine(IntroCo());
        }

        void Build()
        {
            UIKit.EnsureEventSystem();
            AudioFX.StartMusic();

            // hand-made level in this scene? otherwise generate one from the level's seed
            var handMade = FindAnyObjectByType<LevelScene>();
            layout = handMade ? LevelFromScene(handMade) : LevelBuilder.Build(Def);
            // a Player / camera placed in the scene is used; otherwise they are made in code
            Player = FindAnyObjectByType<PlayerMotor>();
            if (Player) { Player.Teleport(layout.spawn, layout.spawnYaw); Player.SetupPlaced(); }
            else Player = PlayerMotor.Create(layout.spawn, layout.spawnYaw);
            Cam = FindAnyObjectByType<ThirdPersonCamera>();
            if (Cam) Cam.Init(Player.transform, layout.spawnYaw);
            else
            {
                foreach (var c in FindObjectsByType<Camera>())
                    if (c.CompareTag("MainCamera")) Destroy(c.gameObject); // a plain overview camera from level design
                Cam = ThirdPersonCamera.Create(Player.transform, layout.spawnYaw);
            }
            EnvironmentSetup.Apply(Cam.GetComponent<Camera>(), false, Def.night);
            Player.AddLantern(); // only visible at night (DayNight fades it)

            // the game UI saved in the scene is used; otherwise it is built in code
            UI = FindAnyObjectByType<GameUI>();
            if (UI) UI.Bind(this);
            else UI = GameUI.Create(this);
            Player.joystick = UI.Joystick;
            Player.cam = Cam;

            foreach (var p in layout.coinPoints) coins.Add(Coin.Create(layout.dynamicRoot, p));
            for (int i = 0; i < layout.powerUpPoints.Count; i++)
            {
                var type = i < powerTypes.Count ? powerTypes[i] : (PowerType)((i + Mathf.Abs(Def.seed)) % 3);
                powerUps.Add(PowerUp.Create(layout.dynamicRoot, layout.powerUpPoints[i], type));
            }
            foreach (var pu in powerUps) UI.AddPowerUpLabel(pu); // made in code + placed by hand
            int guardIndex = 0;
            for (int i = 0; i < layout.enemyPoints.Count; i++)
            {
                var type = i < layout.enemyTypes.Count ? layout.enemyTypes[i] : EnemyType.Patroller;
                Vector3 home = layout.enemyPoints[i];
                if (type == EnemyType.Guard && guardIndex < layout.guardHomes.Count) home = layout.guardHomes[guardIndex++];
                enemies.Add(EnemyAI.Create(layout.dynamicRoot, layout.enemyPoints[i], type, home, Def.enemySpeed, layout.half, UI.Markers));
            }
            // monster prefabs placed in the scene by hand
            foreach (var e in FindObjectsByType<EnemyAI>())
            {
                if (enemies.Contains(e)) continue;
                e.SetupPlaced(Def.enemySpeed, layout.half, UI.Markers);
                enemies.Add(e);
            }
            Gate = layout.gate;
            if (Gate) UI.AddGateLabel(Gate);

            TotalCoins = coins.Count;
            MaxLives = Lives = Mathf.Max(1, Def.lives);
            UI.SetCoins(0, TotalCoins, false);
            UI.SetLives(Lives, MaxLives);
            Fader.FadeIn();
        }

        /// <summary>Reads a hand-made level: marker objects placed in the Hierarchy (see Scripts/Level/).</summary>
        LevelLayout LevelFromScene(LevelScene scene)
        {
            // rebuild the NavMesh so students can move walls around freely
            foreach (var surface in scene.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>()) surface.BuildNavMesh();

            var l = new LevelLayout
            {
                root = scene.transform,
                dynamicRoot = new GameObject("Dynamic (runtime)").transform,
                half = scene.arenaHalf,
                gate = scene.GetComponentInChildren<ExitGate>(),
                coinPoints = new List<Vector3>(), enemyPoints = new List<Vector3>(), enemyTypes = new List<EnemyType>(),
                guardHomes = new List<Vector3>(), powerUpPoints = new List<Vector3>(),
            };
            var spawn = scene.GetComponentInChildren<PlayerSpawn>();
            l.spawn = spawn ? spawn.transform.position : Vector3.zero;
            l.spawnYaw = spawn ? spawn.transform.eulerAngles.y : 0f;
            foreach (var c in scene.GetComponentsInChildren<CoinSpot>())
            {
                var placed = c.GetComponentInChildren<Coin>();
                if (placed) coins.Add(placed);               // a Coin prefab placed by hand
                else l.coinPoints.Add(c.transform.position); // an empty marker: Build() makes a coin here
            }
            foreach (var m in scene.GetComponentsInChildren<MonsterSpawn>())
            {
                if (m.GetComponent<EnemyAI>()) continue;     // a monster prefab placed by hand: set up in Build()
                Vector3 p = m.transform.position;
                if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) p = hit.position;
                l.enemyPoints.Add(p);
                l.enemyTypes.Add(m.type);
                if (m.type == EnemyType.Guard) l.guardHomes.Add(m.HomePosition);
            }
            foreach (var pu in scene.GetComponentsInChildren<PowerUpSpot>())
            {
                var placed = pu.GetComponentInChildren<PowerUp>();
                if (placed) { powerUps.Add(placed); continue; } // a PowerUp prefab placed by hand
                l.powerUpPoints.Add(pu.transform.position);
                powerTypes.Add(pu.type);
            }
            if (l.coinPoints.Count == 0 && coins.Count == 0) Debug.LogWarning("[GoldRush] This level has no CoinSpot objects — add some from Prefabs/Level.");
            return l;
        }

        IEnumerator IntroCo()
        {
            State = GameState.Intro;
            Cam.PlayIntro(2.8f);
            yield return UI.Intro(Def, GameSession.IsDaily ? 0 : GameSession.LevelIndex + 1);
            State = GameState.Playing;
            LockCursor(true);
            bool firstTime = !Save.Data.seenTutorial;
            if (firstTime || (!GameSession.IsDaily && GameSession.LevelIndex == 0)) UI.ShowControlsHint();
            if (firstTime) { Save.Data.seenTutorial = true; Save.Persist(); }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
            if (State != GameState.Playing || !Player) return;
            // clicked back into the game window after the cursor was freed (e.g. alt-tab)
            if (UseMouseLook && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) LockCursor(true);

            float dt = Time.deltaTime;
            Elapsed += dt;
            SpeedLeft = Mathf.Max(0, SpeedLeft - dt);
            GhostLeft = Mathf.Max(0, GhostLeft - dt);
            MagnetLeft = Mathf.Max(0, MagnetLeft - dt);

            Vector3 pp = Player.transform.position + Vector3.up * 1f;

            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                if (c == null || c.Collected) { coins.RemoveAt(i); continue; }
                Vector3 d = c.transform.position - pp;
                float sq = d.sqrMagnitude;
                if (MagnetActive && sq < 11f * 11f)
                {
                    float dist = Mathf.Sqrt(sq);
                    c.transform.position = Vector3.MoveTowards(c.transform.position, pp, (7f + (11f - dist) * 2.2f) * dt);
                    sq = (c.transform.position - pp).sqrMagnitude;
                }
                if (sq < 1.45f * 1.45f) { coins.RemoveAt(i); CollectCoin(c); }
            }

            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                var p = powerUps[i];
                if (p == null || p.Collected) { powerUps.RemoveAt(i); continue; }
                if ((p.transform.position + Vector3.up - pp).sqrMagnitude < 1.6f * 1.6f) { powerUps.RemoveAt(i); CollectPowerUp(p); }
            }

            if (Gate && Gate.IsOpen)
            {
                Vector3 d = Gate.WinPoint - Player.transform.position; d.y = 0;
                if (d.sqrMagnitude < 2.4f * 2.4f) Win();
            }

            bool chased = false;
            foreach (var e in enemies) if (e && e.IsChasing) { chased = true; break; }
            if (chased != chasedLastFrame) { chasedLastFrame = chased; AudioFX.SetTension(chased); }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && State == GameState.Playing) Pause();
        }

        // ---------------------------------------------------------------- pickups

        void CollectCoin(Coin c)
        {
            Vector3 pos = c.transform.position;
            c.Collect();
            Collected++;
            FX.CoinBurst(pos);
            AudioFX.Play("coin", 1f, 1f + 0.05f * (Collected % 5));
            UI.Markers.FloatText(pos + Vector3.up * 0.6f, "+1", UIKit.Gold, 64);
            UI.FlyCoin(pos, Collected, TotalCoins);
            if (Collected >= TotalCoins) AllCollected();
        }

        void AllCollected()
        {
            if (Gate) Gate.Open();
            AudioFX.Play("gate");
            Cam.Shake(0.12f, 0.8f);
            UI.Toast("ALL GOLD COLLECTED!", "The gate is open — run to the EXIT!", UIKit.Green);
            foreach (var e in enemies) if (e) e.Enrage(1.1f);
        }

        void CollectPowerUp(PowerUp p)
        {
            var type = p.Type;
            Vector3 pos = p.transform.position + Vector3.up;
            p.Collect();
            Color col = PowerUp.ColorOf(type);
            FX.Burst(pos, col, Color.white, 30, 2f, 6f, 0.4f, 0.8f, 0.2f, true);
            AudioFX.Play("power");
            string desc;
            switch (type)
            {
                case PowerType.Speed: SpeedLeft = SpeedDuration; desc = "Super speed!"; break;
                case PowerType.Ghost: GhostLeft = GhostDuration; desc = "Monsters can't see you!"; break;
                default: MagnetLeft = MagnetDuration; desc = "Gold flies to you!"; break;
            }
            UI.Toast(PowerUp.NameOf(type), desc, col);
        }

        // ---------------------------------------------------------------- hits

        /// <summary>Editor tooling (trailer recorder): monsters can't hurt the player.</summary>
        public static bool NoDamage;

        public void OnPlayerCaught(EnemyAI enemy)
        {
            if (NoDamage) return;
            if (State != GameState.Playing || !Player || Player.Invulnerable) return;
            Hits++;
            Lives--;
            AudioFX.Play("hit");
            Cam.Shake(0.45f, 0.45f);
            UI.Flash(new Color(0.9f, 0.1f, 0.1f, 0.45f));
            UI.SetLives(Lives, MaxLives);
            if (Lives <= 0) { Lose(); return; }
            Player.Hit(enemy.transform.position, 2.2f);
            enemy.Stun(2.6f);
            UI.Toast("OUCH!", Lives == 1 ? "Last life — be careful!" : "Lives left: " + Lives, UIKit.Red);
        }

        // ---------------------------------------------------------------- end states

        void Lose()
        {
            State = GameState.Lost;
            LockCursor(false);
            Player.PlayCaught();
            AudioFX.SetTension(false);
            var d = Save.Data;
            d.gold += Collected;
            Save.Persist();
            var r = new RunResult
            {
                won = false, collected = Collected, total = TotalCoins, time = Elapsed, par = Def.parTime,
                goldEarned = Collected, hasNext = false,
            };
            StartCoroutine(LoseCo(r));
        }

        IEnumerator LoseCo(RunResult r)
        {
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(1.1f);
            Time.timeScale = 1f;
            AudioFX.Duck(0.7f, 2f);
            AudioFX.Play("lose");
            UI.ShowLose(r);
        }

        void Win()
        {
            State = GameState.Won;
            LockCursor(false);
            AudioFX.SetTension(false);
            Player.PlayVictory();
            FX.Confetti(Player.transform.position + Vector3.up * 0.5f);
            Cam.Shake(0.1f, 0.4f);
            AudioFX.Duck(0.7f, 2.5f);
            AudioFX.Play("win");

            var d = Save.Data;
            var r = new RunResult
            {
                won = true, collected = Collected, total = TotalCoins, time = Elapsed, par = Def.parTime,
                parBeaten = Elapsed <= Def.parTime, noHits = Hits == 0,
            };
            r.stars = 1 + (r.parBeaten ? 1 : 0) + (r.noHits ? 1 : 0);
            r.goldEarned = Collected + r.stars * 5;

            if (Def.daily)
            {
                if (!d.dailyCleared) { r.bonus = 50; d.dailyCleared = true; }
                r.newBest = d.dailyBest < 0 || Elapsed < d.dailyBest;
                if (r.newBest) d.dailyBest = Elapsed;
                r.best = d.dailyBest;
                r.hasNext = false;
            }
            else
            {
                int idx = GameSession.LevelIndex;
                var lp = d.levels[idx];
                r.newBest = lp.bestTime < 0 || Elapsed < lp.bestTime;
                if (r.newBest) lp.bestTime = Elapsed;
                lp.stars = Mathf.Max(lp.stars, r.stars);
                r.best = lp.bestTime;
                d.unlocked = Mathf.Clamp(Mathf.Max(d.unlocked, idx + 2), 1, Levels.All.Length);
                r.hasNext = GameSession.HasNext;
            }
            d.gold += r.goldEarned + r.bonus;
            Save.Persist();
            StartCoroutine(WinCo(r));
        }

        IEnumerator WinCo(RunResult r)
        {
            yield return new WaitForSeconds(1.5f);
            UI.ShowWin(r);
        }

        // ---------------------------------------------------------------- navigation

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            LockCursor(false);
            Time.timeScale = 0f;
            UI.ShowPause();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            UI.HidePause();
            LockCursor(true);
        }

        public void Restart() { Time.timeScale = 1f; Fader.Load(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); }
        public void BackToMenu() { Time.timeScale = 1f; Fader.Load(Scenes.Menu); }

        /// <summary>Called by GameAction buttons in the game UI ("pause", "resume", "retry", "menu", "next").</summary>
        public void Run(string action)
        {
            switch (action)
            {
                case "pause": Pause(); break;
                case "resume": Resume(); break;
                case "retry": Restart(); break;
                case "menu": BackToMenu(); break;
                case "next": NextLevel(); break;
                default: Debug.LogWarning("[GoldRush] Unknown game action: " + action); break;
            }
        }

        public void NextLevel()
        {
            if (!GameSession.HasNext) { BackToMenu(); return; }
            GameSession.LevelIndex++;
            Fader.Load(GameSession.SceneName);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Where the off-screen arrow should point: nearest coin, or the gate once it is open.</summary>
        public Transform ArrowTarget
        {
            get
            {
                if (!Player) return null;
                if (Gate && Gate.IsOpen) return Gate.transform;
                Transform best = null;
                float bestSq = float.MaxValue;
                Vector3 p = Player.transform.position;
                foreach (var c in coins)
                {
                    if (!c || c.Collected) continue;
                    float sq = (c.transform.position - p).sqrMagnitude;
                    if (sq < bestSq) { bestSq = sq; best = c.transform; }
                }
                return best;
            }
        }

        // ---------------------------------------------------------------- smoke-test hooks

        public void DebugCollectAll()
        {
            foreach (var c in coins.ToArray()) if (c && !c.Collected) { coins.Remove(c); CollectCoin(c); }
        }

        public void DebugWin()
        {
            if (State == GameState.Playing) { if (Gate && !Gate.IsOpen) DebugCollectAll(); Win(); }
        }

        public void DebugLose()
        {
            if (State != GameState.Playing) return;
            Lives = 1;
            if (enemies.Count > 0 && enemies[0]) OnPlayerCaught(enemies[0]);
        }
    }
}
