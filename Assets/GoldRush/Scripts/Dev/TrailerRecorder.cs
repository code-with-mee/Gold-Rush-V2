#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace GoldRush
{
    /// <summary>Editor-only trailer capture. Runs when Temp/gr_trailer.run exists (created by
    /// Gold Rush > Record Trailer Clips). Plays scripted shots (menu, autopilot runs, orbit, chase) at a
    /// locked 30 fps and writes JPG frames + an event log per shot into Temp/gr_trailer/&lt;shot&gt;/.
    /// Save data is restored afterwards.</summary>
    public class TrailerRecorder : MonoBehaviour
    {
        const int Fps = 30;
        const string SaveKey = "goldrush_save_v1";
        static string Dir => Path.Combine(Application.dataPath, "../Temp/gr_trailer");
        static string RunFlag => Path.Combine(Application.dataPath, "../Temp/gr_trailer.run");

        readonly StringBuilder log = new StringBuilder();
        StringBuilder events;
        string savedSave;
        NavMeshPath path;
        Vector3 lastPos;
        float stuckT, unstickT;
        int unstickSign = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!File.Exists(RunFlag)) return;
            if (FindAnyObjectByType<TrailerRecorder>()) return;
            var go = new GameObject("[TrailerRecorder]");
            DontDestroyOnLoad(go);
            go.AddComponent<TrailerRecorder>();
        }

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }
        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) log.AppendLine($"[{type}] {msg}\n{stack}");
        }

        IEnumerator Start()
        {
            path = new NavMeshPath();
            savedSave = PlayerPrefs.GetString(SaveKey, "");
            if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            Directory.CreateDirectory(Dir);
            Note($"trailer start, screen {Screen.width}x{Screen.height}");
            yield return Real(3.5f);

            // ---------------- menu
            var menu = GoldRushMenu.I;
            if (menu)
            {
                DayNight.Set(true, true);
                menu.DebugShowTitle();
                yield return Real(1.5f);
                yield return Record("menu_title", 150, _ => true);
                menu.DebugShowLevels(); yield return Real(0.2f);
                yield return Record("menu_levels", 90, _ => true);
                menu.DebugShowShop(); yield return Real(0.2f);
                yield return Record("menu_shop", 100, _ => true);
                menu.DebugShowTitle();
            }
            else Note("menu missing");

            // ---------------- level 1: full run in daylight (with the intro swoop), no damage
            DayNight.Set(false, true);
            yield return Load(0);
            GameManager.NoDamage = true;
            yield return Record("l1_run", 1500, AutoUntilWon(150, false), fromIntro: true);

            // ---------------- level 3: hero orbit + day/night switch (night level)
            DayNight.Set(true, true);
            yield return Load(2);
            yield return WaitPlaying();
            yield return Record("l3_orbit", 300, f =>
            {
                var gm = GameManager.I;
                PlayerMotor.ScriptedInput = Vector2.zero;
                gm.Cam.yaw += 360f / 300f;
                gm.Cam.pitch = Mathf.Lerp(gm.Cam.pitch, 10f, 0.05f);
                if (f == 100 || f == 220) DayNight.Toggle();
                return true;
            });

            // ---------------- level 4: power-ups first, at night
            DayNight.Set(true, true);
            yield return Load(3);
            GameManager.NoDamage = true;
            yield return WaitPlaying();
            yield return Record("l4_power", 720, AutoUntilWon(120, true));

            // ---------------- level 2: real danger, the monsters can catch us
            DayNight.Set(false, true);
            yield return Load(1);
            GameManager.NoDamage = false;
            yield return WaitPlaying();
            int lostAt = -1;
            yield return Record("l2_chase", 600, f =>
            {
                var gm = GameManager.I;
                if (gm.State == GameState.Playing) Autopilot(gm, false);
                else PlayerMotor.ScriptedInput = Vector2.zero;
                if (gm.State == GameState.Lost && lostAt < 0) lostAt = f;
                return lostAt < 0 || f - lostAt < 100;
            });
            if (GameManager.I && GameManager.I.State == GameState.Playing)
            {
                GameManager.I.DebugLose();
                yield return Record("lose", 100, _ => true);
            }

            // ---------------- done
            Cleanup();
            Note("done");
            File.WriteAllText(Path.Combine(Dir, "log.txt"), log.ToString());
            File.WriteAllText(Path.Combine(Dir, "done.txt"), "ok");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        void OnDestroy() { Cleanup(); }

        void Cleanup()
        {
            Time.captureFramerate = 0;
            PlayerMotor.ScriptedInput = null;
            PlayerMotor.ScriptedSprint = false;
            ThirdPersonCamera.ScriptedLook = false;
            GameManager.NoDamage = false;
            if (savedSave != null)
            {
                PlayerPrefs.SetString(SaveKey, savedSave);
                PlayerPrefs.Save();
                Save.Load();
            }
            if (File.Exists(RunFlag)) File.Delete(RunFlag);
        }

        // ------------------------------------------------------------------ behaviours

        System.Func<int, bool> AutoUntilWon(int tail, bool powerUpsFirst)
        {
            int wonAt = -1;
            return f =>
            {
                var gm = GameManager.I;
                if (!gm) return false;
                if (gm.State == GameState.Playing) Autopilot(gm, powerUpsFirst);
                else PlayerMotor.ScriptedInput = Vector2.zero;
                if ((gm.State == GameState.Won || gm.State == GameState.Lost) && wonAt < 0) wonAt = f;
                return wonAt < 0 || f - wonAt < tail;
            };
        }

        /// <summary>Runs toward the next coin (or power-up / open gate) along the NavMesh, camera chasing behind.</summary>
        void Autopilot(GameManager gm, bool powerUpsFirst)
        {
            var player = gm.Player;
            Vector3 pos = player.transform.position;
            Vector3 target;
            if (gm.Gate && gm.Gate.IsOpen) target = gm.Gate.WinPoint;
            else
            {
                Transform best = null;
                if (powerUpsFirst)
                {
                    float bestSq = float.MaxValue;
                    foreach (var p in FindObjectsByType<PowerUp>())
                    {
                        if (p.Collected) continue;
                        float sq = (p.transform.position - pos).sqrMagnitude;
                        if (sq < bestSq) { bestSq = sq; best = p.transform; }
                    }
                }
                if (!best) best = gm.ArrowTarget;
                if (!best) { PlayerMotor.ScriptedInput = Vector2.zero; return; }
                target = best.position;
            }

            Vector3 next = target;
            if (NavMesh.SamplePosition(pos, out var a, 2f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(target, out var b, 3f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.corners.Length > 1)
            {
                next = path.corners[path.corners.Length - 1];
                for (int i = 1; i < path.corners.Length; i++)
                {
                    Vector3 c = path.corners[i] - pos; c.y = 0;
                    if (c.sqrMagnitude > 0.5f * 0.5f) { next = path.corners[i]; break; }
                }
            }
            Vector3 dir = next - pos; dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) dir = player.transform.forward;
            dir.Normalize();

            // stuck on a corner? side-step for a moment
            float dt = 1f / Fps;
            if ((pos - lastPos).sqrMagnitude < 0.02f * 0.02f) stuckT += dt; else stuckT = 0f;
            lastPos = pos;
            if (stuckT > 0.6f) { unstickT = 0.5f; unstickSign = -unstickSign; stuckT = 0f; }
            if (unstickT > 0f) { unstickT -= dt; dir = Quaternion.Euler(0, 80f * unstickSign, 0) * dir; }

            // camera eases round behind the running direction
            float want = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            gm.Cam.yaw = Mathf.MoveTowardsAngle(gm.Cam.yaw, want, 150f * dt);
            gm.Cam.pitch = Mathf.Lerp(gm.Cam.pitch, 18f, 0.05f);

            Vector3 local = Quaternion.Euler(0, -gm.Cam.Yaw, 0) * dir;
            PlayerMotor.ScriptedInput = new Vector2(local.x, local.z);
            PlayerMotor.ScriptedSprint = true;
        }

        // ------------------------------------------------------------------ recording

        IEnumerator Record(string shot, int maxFrames, System.Func<int, bool> step, bool fromIntro = false)
        {
            string dir = Path.Combine(Dir, shot);
            Directory.CreateDirectory(dir);
            events = new StringBuilder();
            ThirdPersonCamera.ScriptedLook = true;
            int coins = -1, lives = -1; bool gate = false; var state = GameState.Intro;
            bool speed = false, ghost = false, magnet = false;
            Time.captureFramerate = Fps;
            int f = 0;
            for (; f < maxFrames; f++)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"f{f:D4}.jpg"), tex.EncodeToJPG(92));
                Destroy(tex);

                var gm = GameManager.I;
                if (gm)
                {
                    if (coins >= 0 && gm.Collected > coins) Ev(f, "coin");
                    if (lives >= 0 && gm.Lives < lives) Ev(f, "hit");
                    if (gm.Gate && gm.Gate.IsOpen && !gate) Ev(f, "gate");
                    if (gm.State != state)
                    {
                        if (gm.State == GameState.Playing) Ev(f, "go");
                        if (gm.State == GameState.Won) Ev(f, "win");
                        if (gm.State == GameState.Lost) Ev(f, "lose");
                    }
                    if (gm.SpeedActive && !speed || gm.GhostActive && !ghost || gm.MagnetActive && !magnet) Ev(f, "power");
                    coins = gm.Collected; lives = gm.Lives; gate = gm.Gate && gm.Gate.IsOpen; state = gm.State;
                    speed = gm.SpeedActive; ghost = gm.GhostActive; magnet = gm.MagnetActive;
                }
                if (!step(f)) { f++; break; }
            }
            Time.captureFramerate = 0;
            PlayerMotor.ScriptedInput = null;
            PlayerMotor.ScriptedSprint = false;
            File.WriteAllText(Path.Combine(dir, "events.txt"), events.ToString());
            Note($"shot {shot}: {f} frames");
        }

        void Ev(int f, string type) => events.AppendLine($"{f} {type}");

        IEnumerator Load(int level)
        {
            var old = GameManager.I;
            GameSession.LevelIndex = level;
            Fader.Load(GameSession.SceneName);
            float t = 0;
            while (t < 20f && (GameManager.I == null || GameManager.I == old)) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
        }

        IEnumerator WaitPlaying()
        {
            float t = 0;
            while (t < 20f && (GameManager.I == null || GameManager.I.State != GameState.Playing)) { t += Time.unscaledDeltaTime; yield return null; }
            Note(GameManager.I ? $"playing {GameManager.I.Def.title}" : "no GameManager");
        }

        static IEnumerator Real(float s) { yield return new WaitForSecondsRealtime(s); }
        void Note(string s) { log.AppendLine($"[{Time.realtimeSinceStartup:F1}] {s}"); }
    }
}
#endif
