#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Editor-only automated play-through. Runs when Temp/gr_smoke.run exists (created by
    /// Gold Rush > Run Smoke Test). Captures screenshots + errors into Temp/gr_smoke/ and exits play mode.</summary>
    public class SmokeTester : MonoBehaviour
    {
        static string Dir => Path.Combine(Application.dataPath, "../Temp/gr_smoke");
        static string RunFlag => Path.Combine(Application.dataPath, "../Temp/gr_smoke.run");

        readonly StringBuilder log = new StringBuilder();
        int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!File.Exists(RunFlag)) return;
            if (FindAnyObjectByType<SmokeTester>()) return;
            var go = new GameObject("[SmokeTester]");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTester>();
        }

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                log.AppendLine($"[{type}] {msg}\n{stack}");
            }
            else if (type == LogType.Warning) log.AppendLine($"[Warning] {msg}");
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
            Note("smoke test start");
            yield return Wait(4f);
            bool startNight = Save.Data.night;
            DayNight.Set(true, true); yield return Wait(0.5f);
            yield return Shot("01_title");
            // pick DAY in the menu: the level must start in day too (one setting everywhere)
            DayNight.Set(false); yield return Wait(2f); yield return Shot("01b_title_day");

            var menu = GoldRushMenu.I;
            if (menu)
            {
                menu.DebugShowLevels(); yield return Wait(1.2f); yield return Shot("02_levels");
                menu.DebugShowShop(); yield return Wait(1.5f); yield return Shot("03_shop");
                menu.DebugShowTitle(); yield return Wait(0.3f);
                menu.DebugShowSettings(); yield return Wait(0.8f); yield return Shot("03b_settings");
                menu.DebugHideSettings(); yield return Wait(0.4f);
                menu.StartLevel(0);
            }
            else Note("menu missing!");

            yield return WaitPlaying();
            yield return Wait(0.3f); yield return Shot("04_level1_start");
            Note(DayNight.IsNight ? "THEME BUG: level started at night after choosing day" : "theme ok: level 1 started in day");
            if (DayNight.IsNight) errors++;
            var gm = GameManager.I;
            if (gm)
            {
                // drive the player forward a bit with the camera rotated
                gm.Cam.yaw = 30f;
                yield return Wait(1.0f); yield return Shot("05_level1_play");
                DayNight.Toggle(); yield return Wait(2f); yield return Shot("05b_level1_night");
                DayNight.Toggle();
                gm.DebugCollectAll();
                yield return Wait(1.6f); yield return Shot("06_gate_open");
                gm.Cam.yaw = 0f;
                yield return Wait(0.6f);
                gm.DebugWin();
                yield return Wait(3.4f); yield return Shot("07_win");
                Note($"level1 state={gm.State}");
                gm.NextLevel();
            }

            yield return WaitPlaying();
            yield return Wait(1f); yield return Shot("08_level2");
            gm = GameManager.I;
            if (gm)
            {
                gm.Pause(); yield return Wait(0.8f); yield return Shot("09_pause");
                gm.Resume(); yield return Wait(0.3f);
                gm.DebugLose(); yield return Wait(2.6f); yield return Shot("10_lose");
                Note($"level2 state={gm.State}");
            }

            // level 4 + daily overview
            GameSession.LevelIndex = 3;
            Fader.Load(GameSession.SceneName);
            yield return WaitPlaying();
            yield return Wait(1.2f); yield return Shot("11_level4");
            GameSession.LevelIndex = -1;
            Fader.Load(GameSession.SceneName);
            yield return WaitPlaying();
            yield return Wait(1.2f); yield return Shot("12_daily");

            Note(DayNight.IsNight ? "theme ok: daily kept the saved night" : "theme: daily is day");
            DayNight.Set(startNight, true);
            Note($"done, errors={errors}");
            File.WriteAllText(Path.Combine(Dir, "log.txt"), log.ToString());
            if (File.Exists(RunFlag)) File.Delete(RunFlag);
            File.WriteAllText(Path.Combine(Dir, "done.txt"), "errors=" + errors);
            Time.timeScale = 1f;
            UnityEditor.EditorApplication.isPlaying = false;
        }

        void Note(string s) { log.AppendLine($"[{Time.realtimeSinceStartup:F1}] {s}"); }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator WaitPlaying()
        {
            float t = 0;
            yield return new WaitForSecondsRealtime(0.5f);
            while (t < 15f && (GameManager.I == null || GameManager.I.State != GameState.Playing))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Note(GameManager.I ? $"playing {GameManager.I.Def.title} after {t:F1}s, coins={GameManager.I.TotalCoins}" : "no GameManager!");
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Dir, name + ".png"));
            Note("shot " + name);
            yield return null;
        }
    }
}
#endif
