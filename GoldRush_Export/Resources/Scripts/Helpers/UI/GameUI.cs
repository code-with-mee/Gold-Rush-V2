using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// In-game HUD + modal screens (intro, pause, win, lose).
    /// Everything is a normal GameObject saved in the level scene (menu: Gold Rush > Level Design > Rebuild Game UI):
    /// move, resize or recolour it in the Inspector. This script fills in the numbers (coins, hearts, timer,
    /// results) and plays the animations. Buttons use a <see cref="GameAction"/> component.
    /// If a scene has no GameUI, GameManager builds one from code (Create).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public VirtualJoystick Joystick => joystick;
        public WorldMarkers Markers => markers;

        GameManager gm;
        [Header("Layers")]
        [SerializeField] RectTransform canvasRt;
        [SerializeField] RectTransform hud, modal;
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] WorldMarkers markers;

        [Header("HUD")]
        [SerializeField] Text coinText;
        [SerializeField] RectTransform coinPill, coinIcon;
        [SerializeField] List<Image> hearts = new List<Image>();
        [SerializeField] Text timerText, parText;
        [SerializeField] Image parStar;
        [SerializeField] Image staminaFill;
        [SerializeField] Text sprintText;
        [SerializeField] RectTransform staminaRoot;
        [SerializeField] CanvasGroup staminaGroup;
        [System.Serializable] public class PowerRow { public RectTransform root; public Image fill; public Text name; [System.NonSerialized] public PowerType type; }
        [SerializeField] List<PowerRow> powerRows = new List<PowerRow>();
        [SerializeField] RectTransform arrowRoot, arrowTri;
        [SerializeField] Image arrowIconBg;
        [SerializeField] GameObject arrowCoin;
        [SerializeField] CanvasGroup toastGroup;
        [SerializeField] RectTransform toastRt;
        [SerializeField] Text toastTitle, toastSub;
        Coroutine toastCo;
        [SerializeField] RectTransform heartsRoot, timerRoot, pauseButton, dayNightRoot;
        [SerializeField] Image flash;
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField] Text hintText;

        /// <summary>One dialog saved in the scene: the parts the code fills in.</summary>
        [System.Serializable]
        public class DialogUI
        {
            public RectTransform root, panel, badge;
            public Text title, subtitle;
            public Text[] values, captions;
            public Image[] stars, critStars;
            public Text[] critTexts;
            public Button[] buttons;
            public float[] weights;
            public Text tip;
            public Button music, sound;
            public float width, height;
        }
        [Header("Dialogs (hidden until needed)")]
        [SerializeField] DialogUI pauseDialog = new DialogUI();
        [SerializeField] DialogUI winDialog = new DialogUI();
        [SerializeField] DialogUI loseDialog = new DialogUI();
        [SerializeField] RectTransform introRoot, introBand;
        [SerializeField] Text introLevel, introTitle, introGoal, countText;

        public static GameUI Create(GameManager gm)
        {
            var canvas = UIKit.Canvas("GameUI", 10);
            var ui = canvas.gameObject.AddComponent<GameUI>();
            ui.BuildAll();
            ui.Bind(gm);
            return ui;
        }

#if UNITY_EDITOR
        /// <summary>Editor only: builds the whole game UI as scene objects (see LevelBaker).</summary>
        public static GameUI BuildForEditor()
        {
            var canvas = UIKit.Canvas("GameUI", 10);
            var ui = canvas.gameObject.AddComponent<GameUI>();
            ui.BuildAll();
            return ui;
        }
#endif

        void BuildAll()
        {
            canvasRt = (RectTransform)transform;
            Build();
            BuildIntro();
            BuildHint();
            BuildPause();
            BuildWin();
            BuildLose();
        }

        /// <summary>Connects the saved UI to this level's GameManager (buttons, toggles) and plays the HUD intro.</summary>
        public void Bind(GameManager gameManager)
        {
            gm = gameManager;
            if (!hud) BuildAll(); // an empty GameUI object: build everything now
            var pb = pauseButton ? pauseButton.GetComponent<Button>() : null;
            if (pb) pb.onClick.AddListener(() => { AudioFX.Play("click"); gm.Pause(); });
            BindToggle(pauseDialog.music, "MUSIC", () => Save.Data.music, v => { Save.Data.music = v; Save.Persist(); AudioFX.RefreshMute(); });
            BindToggle(pauseDialog.sound, "SOUND", () => Save.Data.sfx, v => { Save.Data.sfx = v; Save.Persist(); AudioFX.RefreshMute(); });
            foreach (var d in new[] { pauseDialog, winDialog, loseDialog }) if (d.root) d.root.gameObject.SetActive(false);
            if (introRoot) introRoot.gameObject.SetActive(false);
            if (countText) countText.gameObject.SetActive(false);
            if (hintGroup) hintGroup.gameObject.SetActive(false);
            if (arrowRoot) arrowRoot.gameObject.SetActive(false);
            if (toastGroup) toastGroup.alpha = 0f;
            if (flash) flash.color = new Color(1, 0, 0, 0);
            foreach (var r in powerRows) if (r.root) r.root.gameObject.SetActive(false);
            PlayHudIntro();
        }

        // ================================================================ HUD

        void Build()
        {
            // input layers first (lowest), then markers, then HUD widgets, then modal
            TouchLookArea.Create(canvasRt);
            joystick = VirtualJoystick.Create(canvasRt);
            markers = WorldMarkers.Create(canvasRt);
            hud = UIKit.Rect("HUD", canvasRt); hud.Fill();

            // ---- compact frosted-glass HUD: small chips in the corners so the map stays visible
            // coin counter (top-left)
            coinPill = UIKit.Rect("CoinPill", hud).At(new Vector2(0, 1), new Vector2(32, -28), new Vector2(214, 70));
            UIKit.Glass(coinPill, Sprites.Pill, 128f / 70f, 0.5f);
            coinIcon = UIKit.CoinIcon(coinPill, 54);
            coinIcon.At(new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(54, 54), new Vector2(0.5f, 0.5f)); // centred pivot so the flip spins in place
            coinText = UIKit.Label(coinPill, "0 / 0", 38, UIKit.Cream, TextAnchor.MiddleLeft);
            coinText.rectTransform.Fill(76, 0, 8, 0);

            // hearts under the coin counter
            var heartsRt = UIKit.Rect("Hearts", hud).At(new Vector2(0, 1), new Vector2(40, -108), new Vector2(200, 44));

            // timer (top-right group: [timer] [day/night] [pause]) — keeps the top centre of the view clear
            var timer = UIKit.Rect("Timer", hud).At(new Vector2(1, 1), new Vector2(-200, -28), new Vector2(188, 70), new Vector2(1, 1));
            UIKit.Glass(timer, Sprites.Pill, 128f / 70f, 0.5f);
            timerText = UIKit.Label(timer, "0:00.0", 36, UIKit.Cream);
            timerText.rectTransform.Fill(0, 20, 0, 2);
            parStar = UIKit.StarIcon(timer, 18, true);
            parStar.rectTransform.At(new Vector2(0.5f, 0), new Vector2(-44, 16), new Vector2(18, 18), new Vector2(0.5f, 0.5f));
            parText = UIKit.Label(timer, "PAR 0:40", 18, UIKit.Gold, TextAnchor.MiddleLeft, false);
            parText.rectTransform.At(new Vector2(0.5f, 0), new Vector2(-32, 16), new Vector2(120, 22), new Vector2(0, 0.5f));

            // pause button (top-right)
            var pause = UIKit.Rect("Pause", hud).At(new Vector2(1, 1), new Vector2(-32, -28), new Vector2(70, 70));
            var pf = UIKit.Glass(pause, Sprites.Circle, 1f, 0.5f);
            UIKit.IconGraphic(pause, UIKit.Icon.Pause, 40, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), new Vector2(0.5f, 0.5f));
            var pb = pause.gameObject.AddComponent<Button>();
            pb.targetGraphic = pf; // the click is connected in Bind()

            // day / night toggle (left of pause)
            dayNightRoot = (RectTransform)DayNightButton.Create(hud, new Vector2(1, 1), new Vector2(-116, -28), 70f, true).transform;

            // power-up timers (left column, under the hearts)
            for (int i = 0; i < 3; i++)
            {
                var row = new PowerRow();
                row.root = UIKit.Rect("Power" + i, hud).At(new Vector2(0, 1), new Vector2(32, -196 - i * 50), new Vector2(214, 40), new Vector2(0, 1));
                UIKit.Glass(row.root, Sprites.Pill, 128f / 40f);
                row.fill = UIKit.Img(row.root, Sprites.Pill, Color.white, "Fill");
                row.fill.pixelsPerUnitMultiplier = 128f / 30f;
                row.fill.rectTransform.anchorMin = new Vector2(0, 0); row.fill.rectTransform.anchorMax = new Vector2(1, 1);
                row.fill.rectTransform.pivot = new Vector2(0, 0.5f);
                row.fill.rectTransform.offsetMin = new Vector2(5, 5); row.fill.rectTransform.offsetMax = new Vector2(-5, -5);
                var lbl = UIKit.Label(row.root, "", 22, UIKit.Cream); lbl.rectTransform.Fill(); lbl.name = "Name";
                row.name = lbl;
                row.root.gameObject.SetActive(false);
                powerRows.Add(row);
            }

            // stamina bar under the hearts (only visible while sprinting / recovering)
            staminaRoot = UIKit.Rect("Stamina", hud).At(new Vector2(0, 1), new Vector2(40, -158), new Vector2(150, 24));
            staminaGroup = staminaRoot.gameObject.AddComponent<CanvasGroup>();
            staminaGroup.alpha = 0f;
            UIKit.Glass(staminaRoot, Sprites.Pill, 128f / 24f);
            staminaFill = UIKit.Img(staminaRoot, Sprites.Pill, UIKit.Gold, "Fill");
            staminaFill.rectTransform.anchorMin = Vector2.zero; staminaFill.rectTransform.anchorMax = Vector2.one;
            staminaFill.rectTransform.offsetMin = new Vector2(4, 4); staminaFill.rectTransform.offsetMax = new Vector2(-4, -4);
            staminaFill.pixelsPerUnitMultiplier = 128f / 16f;
            sprintText = UIKit.Label(staminaRoot, "RUN", 16, UIKit.Cream, TextAnchor.MiddleCenter);
            sprintText.rectTransform.Fill();

            // off-screen target arrow
            arrowRoot = UIKit.Rect("Arrow", hud); arrowRoot.anchorMin = arrowRoot.anchorMax = Vector2.zero; arrowRoot.sizeDelta = new Vector2(70, 70);
            arrowTri = UIKit.Rect("Tri", arrowRoot); arrowTri.Fill();
            var tri = UIKit.Img(arrowTri, Sprites.Triangle, UIKit.Gold, "Shape");
            tri.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0, 18), new Vector2(38, 38), new Vector2(0.5f, 0.5f));
            arrowIconBg = UIKit.Img(arrowRoot, Sprites.Circle, UIKit.WithAlpha(UIKit.HudBg, 0.6f), "Bg"); arrowIconBg.rectTransform.Fill(8, 8, 8, 8);
            var ai = UIKit.CoinIcon(arrowRoot, 40); ai.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), new Vector2(0.5f, 0.5f));
            ai.name = "CoinIcon";
            arrowCoin = ai.gameObject;
            arrowRoot.gameObject.SetActive(false);

            // toast (message pop-up): compact glass pill tinted with the message colour
            toastRt = UIKit.Rect("Toast", hud).At(new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(640, 96), new Vector2(0.5f, 0.5f));
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
            var toastShadow = UIKit.Img(toastRt, Sprites.Pill, new Color(0, 0, 0, 0.18f), "Shadow"); toastShadow.rectTransform.Fill(2, -6, 2, 4); toastShadow.pixelsPerUnitMultiplier = 128f / 96f;
            UIKit.Glass(toastRt, Sprites.Pill, 128f / 96f, 0.55f); // same glass as the HUD chips
            toastTitle = UIKit.Label(toastRt, "", 38, UIKit.Cream); toastTitle.rectTransform.Fill(0, 34, 0, 0);
            toastSub = UIKit.Label(toastRt, "", 22, UIKit.Cream); toastSub.rectTransform.Fill(0, 8, 0, 60);

            // hearts
            for (int i = 0; i < 3; i++)
            {
                var h = UIKit.Img(heartsRt, Sprites.Heart, UIKit.Red, "Heart" + i);
                h.rectTransform.At(new Vector2(0, 0.5f), new Vector2(i * 48, 0), new Vector2(40, 40), new Vector2(0, 0.5f));
                hearts.Add(h);
            }

            heartsRoot = heartsRt; timerRoot = timer; pauseButton = pause;
            coinIcon.gameObject.AddComponent<CoinFlip>();

            flash = UIKit.Img(canvasRt, null, new Color(1, 0, 0, 0), "Flash");
            flash.rectTransform.Fill();
            modal = UIKit.Rect("Modal", canvasRt); modal.Fill();
        }

        /// <summary>HUD pieces drop in from above the screen, one after another.</summary>
        void PlayHudIntro()
        {
            var items = new[] { coinPill, heartsRoot, timerRoot, dayNightRoot, pauseButton };
            for (int i = 0; i < items.Length; i++)
            {
                var rt = items[i];
                if (!rt) continue;
                Vector2 target = rt.anchoredPosition;
                rt.anchoredPosition = target + Vector2.up * 220f;
                Tween.Move(rt, target, 0.55f, Tween.OutBack, 0.25f + i * 0.08f);
            }
        }

        public void AddPowerUpLabel(PowerUp p)
        {
            var root = UIKit.Rect("PULabel", Markers.transform);
            root.sizeDelta = new Vector2(118, 34);
            var bg = UIKit.Img(root, Sprites.Pill, UIKit.WithAlpha(UIKit.Darken(PowerUp.ColorOf(p.Type), 0.55f), 0.85f), "Bg"); bg.rectTransform.Fill(); bg.pixelsPerUnitMultiplier = 128f / 34f;
            var t = UIKit.Label(root, PowerUp.NameOf(p.Type), 20, UIKit.Cream); t.rectTransform.Fill();
            Markers.Add(p.transform, Vector3.up * 2.2f, root, 22f);
        }

        public void AddGateLabel(ExitGate g)
        {
            var root = UIKit.Rect("GateLabel", Markers.transform);
            root.sizeDelta = new Vector2(124, 42);
            var bg = UIKit.Img(root, Sprites.Pill, UIKit.WithAlpha(UIKit.Green, 0.9f), "Bg"); bg.rectTransform.Fill(); bg.pixelsPerUnitMultiplier = 128f / 42f;
            var t = UIKit.Label(root, "EXIT", 26, UIKit.Cream); t.rectTransform.Fill();
            var m = Markers.Add(g.MarkerAnchor, Vector3.up * 0.6f, root, 200f);
            m.visible = false;
            StartCoroutine(GateLabelCo(g, m));
        }

        IEnumerator GateLabelCo(ExitGate g, WorldMarkers.Marker m)
        {
            while (g && !g.IsOpen) yield return null;
            if (m == null || !m.rt) yield break;
            m.visible = true;
            var baseOffset = m.offset;
            while (m.rt)
            {
                m.offset = baseOffset + Vector3.up * (0.25f * Mathf.Sin(Time.unscaledTime * 4f));
                yield return null;
            }
        }

        public void SetCoins(int got, int total, bool punch)
        {
            coinText.text = $"{got} <size=40><color=#BFD3F2>/ {total}</color></size>";
            if (punch)
            {
                Tween.Punch(coinPill, 0.18f, 0.3f);
                Tween.Punch(coinIcon, 0.45f, 0.35f);
            }
        }

        public void SetLives(int lives, int max)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                var h = hearts[i];
                h.gameObject.SetActive(i < max);
                bool full = i < lives;
                Color target = full ? UIKit.Red : new Color(0, 0, 0, 0.35f);
                if (h.color != target && !full)
                {
                    Tween.Punch(h.rectTransform, 0.6f, 0.35f);
                }
                h.color = target;
                var beat = h.GetComponent<Bob>();
                bool danger = lives == 1 && i == 0;
                if (danger && !beat) h.gameObject.AddComponent<Bob>().Setup(0f, 7f, 0f, 0.14f);
                else if (!danger && beat) { Destroy(beat); h.rectTransform.localScale = Vector3.one; }
            }
        }

        public void FlyCoin(Vector3 world, int got, int total)
        {
            var cam = Camera.main;
            if (!cam) { SetCoins(got, total, true); return; }
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) { SetCoins(got, total, true); return; }
            var icon = UIKit.CoinIcon(hud, 70, "FlyCoin");
            icon.anchorMin = icon.anchorMax = Vector2.zero;
            icon.position = new Vector3(sp.x, sp.y, 0);
            Vector3 from = icon.position;
            Tween.Run(icon, 0.55f, k =>
            {
                Vector3 to = coinIcon.position;
                Vector3 mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * Screen.height * 0.12f;
                Vector3 a = Vector3.Lerp(from, mid, k), b = Vector3.Lerp(mid, to, k);
                icon.position = Vector3.Lerp(a, b, k);
                icon.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, k);
            }, Tween.InCubic, 0f, () =>
            {
                Destroy(icon.gameObject);
                SetCoins(got, total, true);
            });
        }

        public void Flash(Color c)
        {
            Tween.Run(flash, 0.5f, k => flash.color = new Color(c.r, c.g, c.b, c.a * (1 - k)), Tween.OutCubic);
            var v = EnvironmentSetup.Vignette;
            if (v != null)
            {
                float baseI = v.intensity.value;
                Color baseC = v.color.value;
                Tween.Run(flash, 0.7f, k =>
                {
                    v.color.Override(Color.Lerp(new Color(0.8f, 0f, 0f), baseC, k));
                    v.intensity.Override(Mathf.Lerp(0.5f, baseI, k));
                });
            }
        }

        public void Toast(string title, string sub, Color color)
        {
            toastTitle.text = title;
            toastSub.text = sub;
            toastTitle.color = Color.Lerp(color, Color.white, 0.25f); // message colour shows in the title
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo());
        }

        IEnumerator ToastCo()
        {
            toastGroup.alpha = 1;
            // 3D flip-in: rotate down from "lying flat" while popping up
            Tween.Run(toastRt, 0.45f, k =>
            {
                toastRt.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(80f, 0f, k), 0, 0);
                toastRt.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k);
            }, Tween.OutBack);
            yield return new WaitForSecondsRealtime(2.2f);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.35f)
            {
                toastGroup.alpha = 1 - t;
                yield return null;
            }
            toastGroup.alpha = 0;
        }

        const string HintDesktop = "<color=#FFC233>WASD</color> move  •  <color=#FFC233>Mouse</color> look  •  <color=#FFC233>Shift</color> sprint  •  <color=#FFC233>Esc</color> pause / free mouse";
        const string HintTouch = "<color=#FFC233>Left side</color>: move (push to the edge to run)  •  <color=#FFC233>Right side</color>: look around";

        void BuildHint()
        {
            var root = UIKit.Rect("Hint", hud).At(new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1000, 56), new Vector2(0.5f, 0));
            hintGroup = root.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            UIKit.Glass(root, Sprites.Pill, 128f / 56f);
            hintText = UIKit.Label(root, HintDesktop, 24, UIKit.Cream); hintText.rectTransform.Fill();
            root.gameObject.SetActive(false);
        }

        public void ShowControlsHint()
        {
            if (!hintGroup) return;
            hintText.text = Application.isMobilePlatform ? HintTouch : HintDesktop;
            hintGroup.gameObject.SetActive(true);
            hintGroup.alpha = 1f;
            var go = hintGroup.gameObject;
            Tween.Fade(hintGroup, 0f, 0.6f, 7f, () => { if (go) go.SetActive(false); });
        }

        void Update()
        {
            if (!gm || gm.Def == null || timerText == null) return;
            timerText.text = UIKit.FormatTime(gm.Elapsed);
            float parLeft = gm.Def.parTime - gm.Elapsed;
            if (gm.State == GameState.Playing && parLeft > 0f && parLeft < 10f)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                timerText.color = Color.Lerp(UIKit.Cream, UIKit.Orange, k);
                timerText.rectTransform.localScale = Vector3.one * (1f + 0.06f * k);
            }
            else { timerText.color = UIKit.Cream; timerText.rectTransform.localScale = Vector3.one; }
            bool overPar = gm.Elapsed > gm.Def.parTime;
            parText.text = "PAR " + UIKit.FormatTime(gm.Def.parTime).Replace(".0", "");
            parText.color = overPar ? new Color(1, 1, 1, 0.35f) : UIKit.Gold;
            parStar.color = overPar ? new Color(0, 0, 0, 0.35f) : UIKit.Gold;

            var p = gm.Player;
            if (p)
            {
                staminaFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, p.Stamina), 1);
                staminaFill.color = p.Exhausted ? UIKit.Red : (p.Stamina < 0.3f ? UIKit.Orange : UIKit.Gold);
                sprintText.text = p.Exhausted ? "TIRED" : "RUN";
                float want = p.Sprinting || p.Stamina < 0.99f ? 1f : 0f;
                staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, want, Time.unscaledDeltaTime * 3f);
            }

            UpdatePowerRows();
            UpdateArrow();
        }

        void UpdatePowerRows()
        {
            int idx = 0;
            void Row(PowerType t, float left, float dur)
            {
                if (left <= 0 || idx >= powerRows.Count) return;
                var r = powerRows[idx++];
                if (!r.root.gameObject.activeSelf || r.type != t)
                {
                    r.type = t;
                    r.root.gameObject.SetActive(true);
                    r.fill.color = UIKit.WithAlpha(PowerUp.ColorOf(t), 0.85f);
                    if (r.name) r.name.text = PowerUp.NameOf(t);
                    Tween.PopIn(r.root, 0.3f);
                }
                r.fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(left / dur), 1);
                // blink when about to expire
                float a = left < 1.5f ? (Mathf.Sin(Time.unscaledTime * 18f) > 0 ? 0.4f : 0.9f) : 0.85f;
                var c = r.fill.color; c.a = a; r.fill.color = c;
            }
            Row(PowerType.Speed, gm.SpeedLeft, GameManager.SpeedDuration);
            Row(PowerType.Ghost, gm.GhostLeft, GameManager.GhostDuration);
            Row(PowerType.Magnet, gm.MagnetLeft, GameManager.MagnetDuration);
            for (int i = idx; i < powerRows.Count; i++)
                if (powerRows[i].root.gameObject.activeSelf) powerRows[i].root.gameObject.SetActive(false);
        }

        void UpdateArrow()
        {
            var cam = Camera.main;
            var target = gm.State == GameState.Playing ? gm.ArrowTarget : null;
            if (!cam || !target) { if (arrowRoot.gameObject.activeSelf) arrowRoot.gameObject.SetActive(false); return; }

            Vector3 sp = cam.WorldToScreenPoint(target.position);
            float margin = Screen.height * 0.09f;
            float topBand = Screen.height * 0.22f; // keep the arrow clear of the top HUD bar
            bool behind = sp.z < 0;
            bool onScreen = !behind && sp.x > margin && sp.x < Screen.width - margin && sp.y > margin && sp.y < Screen.height - margin;
            if (onScreen) { if (arrowRoot.gameObject.activeSelf) arrowRoot.gameObject.SetActive(false); return; }

            if (!arrowRoot.gameObject.activeSelf) arrowRoot.gameObject.SetActive(true);
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 d = new Vector2(sp.x, sp.y) - center;
            if (behind) d = -d;
            if (d.sqrMagnitude < 1f) d = Vector2.down;
            float ex = Screen.width * 0.5f - margin * 1.3f, ey = Screen.height * 0.5f - margin * 1.3f;
            float s = Mathf.Min(ex / Mathf.Max(0.001f, Mathf.Abs(d.x)), ey / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            Vector2 pos = center + d * s;
            pos.y = Mathf.Clamp(pos.y, margin * 1.3f, Screen.height - topBand);
            arrowRoot.position = new Vector3(pos.x, pos.y, 0);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            arrowTri.localRotation = Quaternion.Euler(0, 0, ang);
            bool gate = gm.Gate && gm.Gate.IsOpen;
            arrowIconBg.color = gate ? UIKit.Green : UIKit.WithAlpha(UIKit.HudBg, 0.8f);
            if (arrowCoin) arrowCoin.SetActive(!gate);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            arrowRoot.localScale = Vector3.one * pulse;
        }

        // ================================================================ INTRO

        void BuildIntro()
        {
            introRoot = UIKit.Rect("Intro", modal); introRoot.Fill();
            var g = introRoot.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false;
            var band = UIKit.Img(introRoot, null, UIKit.WithAlpha(UIKit.Navy, 0.55f), "Band");
            band.rectTransform.At(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(4000, 300), new Vector2(0.5f, 0.5f));
            introBand = band.rectTransform;
            introLevel = UIKit.Label(band.transform, "LEVEL 1", 44, UIKit.Gold);
            introLevel.name = "Level";
            introLevel.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(1200, 60), new Vector2(0.5f, 0.5f));
            introTitle = UIKit.Label(band.transform, "TRAINING YARD", 120, UIKit.Cream);
            introTitle.name = "Title";
            introTitle.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1600, 140), new Vector2(0.5f, 0.5f));
            introGoal = UIKit.Label(band.transform, "Collect all gold, then escape through the EXIT!", 38, UIKit.Cream);
            introGoal.name = "Goal";
            introGoal.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(1600, 50), new Vector2(0.5f, 0.5f));
            introRoot.gameObject.SetActive(false);

            countText = UIKit.Label(modal, "3", 220, UIKit.Gold);
            countText.name = "Countdown";
            countText.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 260), new Vector2(0.5f, 0.5f));
            countText.gameObject.SetActive(false);
        }

        public IEnumerator Intro(LevelDef def, int number)
        {
            var root = introRoot;
            var g = root.GetComponent<CanvasGroup>();
            g.alpha = 1f;
            root.gameObject.SetActive(true);
            introLevel.text = number > 0 ? "LEVEL " + number : "DAILY CHALLENGE";
            introTitle.text = def.title.ToUpper();
            introGoal.text = $"Collect all <color=#FFC233>{def.coins} gold</color>, then escape through the <color=#6BE08F>EXIT</color>!";
            Tween.Scale(introBand, new Vector3(1, 0, 1), Vector3.one, 0.35f, Tween.OutBack);
            Tween.PopIn(introTitle.rectTransform, 0.5f, 0.15f);

            yield return new WaitForSeconds(1.4f);
            Tween.Fade(g, 0f, 0.3f, 0f, () => { if (root) root.gameObject.SetActive(false); });

            var count = countText;
            count.gameObject.SetActive(true);
            var cg = count.GetComponent<CanvasGroup>();
            if (!cg) cg = count.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            string[] steps = { "3", "2", "1", "GO!" };
            for (int i = 0; i < steps.Length; i++)
            {
                bool go = i == steps.Length - 1;
                count.text = steps[i];
                count.color = go ? UIKit.Green : UIKit.Gold;
                AudioFX.Play(go ? "go" : "count");
                Tween.Scale(count.rectTransform, Vector3.one * 1.8f, Vector3.one, 0.3f, Tween.OutBack);
                yield return new WaitForSeconds(go ? 0.25f : 0.45f);
            }
            Tween.Fade(cg, 0f, 0.4f, 0.1f, () => { if (count) { count.gameObject.SetActive(false); count.rectTransform.localScale = Vector3.one; } });
            Tween.Scale(count.rectTransform, Vector3.one, Vector3.one * 1.6f, 0.5f, Tween.OutCubic);
        }

        // ================================================================ MODALS

        // ---------------------------------------------------------------- dialog building blocks
        // Every dialog = glass panel with HEADER (icon badge + title + subtitle), BODY and FOOTER (buttons).

        class Dialog { public RectTransform root, panel, badge; public Transform body; public float width, height; public Text title, subtitle; }

        const float HeaderH = 124f, FooterH = 132f, Pad = 40f;

        Dialog OpenDialog(string name, Vector2 size, string title, string subtitle, Color accent, UIKit.Icon icon)
        {
            var d = new Dialog { width = size.x, height = size.y };
            d.root = UIKit.Rect(name, modal); d.root.Fill();
            UIKit.Dim(d.root, 0.65f);
            d.root.gameObject.AddComponent<CanvasGroup>();

            d.panel = UIKit.Rect("Panel", d.root);
            d.panel.sizeDelta = size;
            var face = UIKit.Glass(d.panel, Sprites.RoundRect, 0.8f, 0.72f); // same glass as the HUD chips, more opaque for reading
            d.panel.At(new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f));
            d.body = face.transform;

            // header: icon badge + title + subtitle
            var badge = UIKit.Img(face.transform, Sprites.Circle, UIKit.WithAlpha(accent, 0.9f), "Badge");
            badge.rectTransform.At(new Vector2(0, 1), new Vector2(Pad, -24), new Vector2(84, 84), new Vector2(0, 1));
            UIKit.Img(badge.transform, Sprites.ThinRing, new Color(1, 1, 1, 0.6f), "Ring").rectTransform.Fill();
            UIKit.IconGraphic(badge.transform, icon, 40, UIKit.Cream, accent).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), new Vector2(0.5f, 0.5f));
            d.badge = badge.rectTransform;
            var t = d.title = UIKit.SoftLabel(face.transform, title, 50, UIKit.Cream, TextAnchor.LowerLeft, "Title");
            t.rectTransform.At(new Vector2(0, 1), new Vector2(Pad + 108, -18), new Vector2(size.x - Pad * 2 - 108, 60), new Vector2(0, 1));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var st = d.subtitle = UIKit.Label(face.transform, subtitle, 24, UIKit.WithAlpha(UIKit.Cream, 0.65f), TextAnchor.UpperLeft, false, "Subtitle");
                st.rectTransform.At(new Vector2(0, 1), new Vector2(Pad + 110, -80), new Vector2(size.x - Pad * 2 - 110, 32), new Vector2(0, 1));
            }
            Divider(face.transform, -HeaderH);
            Divider(face.transform, -(size.y - FooterH));
            return d;
        }

        static DialogUI SaveDialog(Dialog d) => new DialogUI { root = d.root, panel = d.panel, badge = d.badge, title = d.title, subtitle = d.subtitle, width = d.width, height = d.height };

        /// <summary>Shows a saved dialog with its entrance animation (fade, flip down, badge pop, buttons pop).</summary>
        void ShowDialog(DialogUI d)
        {
            d.root.gameObject.SetActive(true);
            d.root.SetAsLastSibling();
            var cg = d.root.GetComponent<CanvasGroup>();
            cg.alpha = 0; cg.blocksRaycasts = true;
            Tween.Fade(cg, 1f, 0.25f);
            var prt = d.panel;
            Tween.Run(prt, 0.5f, k =>
            {
                prt.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(55f, 0f, k), 0, 0);
                prt.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, k);
            }, Tween.OutBack);
            if (d.badge) { var b = d.badge; b.localScale = Vector3.zero; Tween.Run(b, 0.6f, k => b.localScale = Vector3.one * k, Tween.OutElastic, 0.2f); }
            AudioFX.Play("whoosh", 0.6f);
            int n = 0;
            if (d.buttons != null)
                foreach (var btn in d.buttons)
                    if (btn && btn.gameObject.activeSelf) Tween.PopIn((RectTransform)btn.transform, 0.35f, 0.35f + n++ * 0.07f);
        }

        /// <summary>Lays out the visible footer buttons again (e.g. without NEXT on the last level).</summary>
        static void LayoutFooter(DialogUI d)
        {
            if (d.buttons == null) return;
            float gap = 16f, total = 0f; int count = 0;
            for (int i = 0; i < d.buttons.Length; i++) if (d.buttons[i] && d.buttons[i].gameObject.activeSelf) { total += d.weights[i]; count++; }
            if (count == 0) return;
            float avail = d.width - Pad * 2 - gap * (count - 1);
            float x = Pad;
            for (int i = 0; i < d.buttons.Length; i++)
            {
                var b = d.buttons[i];
                if (!b || !b.gameObject.activeSelf) continue;
                var rt = (RectTransform)b.transform;
                float w = avail * d.weights[i] / total;
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
                x += w + gap;
            }
        }

        static void Divider(Transform parent, float y)
        {
            var line = UIKit.Img(parent, null, new Color(1, 1, 1, 0.1f), "Divider");
            line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.rectTransform.offsetMin = new Vector2(Pad, y - 1); line.rectTransform.offsetMax = new Vector2(-Pad, y + 1);
        }

        /// <summary>Small glass card: icon, caption on top, big value. Returns the value label.</summary>
        static Text StatCard(Transform parent, Vector2 topLeft, Vector2 size, UIKit.Icon? icon, bool coin, string caption, string value, Color valueColor)
        {
            var card = UIKit.Rect("Stat_" + caption, parent).At(new Vector2(0, 1), topLeft, size, new Vector2(0, 1));
            var bg = UIKit.Img(card, Sprites.RoundRect, new Color(1, 1, 1, 0.06f), "Bg"); bg.rectTransform.Fill(); bg.pixelsPerUnitMultiplier = 1.6f;
            var edge = UIKit.Img(card, Sprites.RoundOutline, new Color(1, 1, 1, 0.45f), "Edge"); edge.rectTransform.Fill(); edge.pixelsPerUnitMultiplier = 1.6f;
            if (coin) UIKit.CoinIcon(card, 40).At(new Vector2(0, 1), new Vector2(18, -16), new Vector2(40, 40), new Vector2(0, 1));
            else if (icon.HasValue) UIKit.IconGraphic(card, icon.Value, 34, UIKit.Gold).At(new Vector2(0, 1), new Vector2(20, -19), new Vector2(34, 34), new Vector2(0, 1));
            UIKit.Fit(UIKit.Label(card, caption, 20, UIKit.WithAlpha(UIKit.Cream, 0.6f), TextAnchor.MiddleLeft, false, "Caption"), 12)
                .rectTransform.At(new Vector2(0, 1), new Vector2(68, -18), new Vector2(size.x - 80, 36), new Vector2(0, 1));
            var v = UIKit.Fit(UIKit.SoftLabel(card, value, 40, valueColor, TextAnchor.MiddleLeft, "Value"), 20);
            v.rectTransform.At(new Vector2(0, 0), new Vector2(20, 12), new Vector2(size.x - 30, 56), new Vector2(0, 0));
            return v;
        }

        /// <summary>Lays out a row of stat cards across the body at the given y.</summary>
        static Vector2[] CardSlots(Dialog d, int count, float y, float h = 116f)
        {
            float gap = 16f, w = (d.width - Pad * 2 - gap * (count - 1)) / count;
            var slots = new Vector2[count];
            for (int i = 0; i < count; i++) slots[i] = new Vector2(Pad + i * (w + gap), y);
            cardSize = new Vector2(w, h);
            return slots;
        }
        static Vector2 cardSize;

        struct FooterButton { public string label; public UIKit.Icon icon; public Color color; public string action; public float weight; }

        /// <summary>Evenly spaced glass buttons in the footer; weight makes the main action wider.
        /// Each button gets a GameAction (its action name), so the click survives being saved in the scene.</summary>
        static void Footer(Dialog d, DialogUI ui, params FooterButton[] buttons)
        {
            ui.buttons = new Button[buttons.Length];
            ui.weights = new float[buttons.Length];
            float gap = 16f, total = 0f;
            foreach (var b in buttons) total += b.weight;
            float avail = d.width - Pad * 2 - gap * (buttons.Length - 1);
            float x = Pad, h = 92f, y = -(d.height - FooterH) - (FooterH - h) * 0.5f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var spec = buttons[i];
                float w = avail * spec.weight / total;
                var btn = UIKit.ChipButton(d.body, spec.icon, spec.label, spec.color, new Vector2(w, h), null, 30, spec.weight > 1f);
                btn.name = spec.label;
                btn.gameObject.AddComponent<GameAction>().action = spec.action;
                var rt = (RectTransform)btn.transform;
                rt.At(new Vector2(0, 1), new Vector2(x, y), new Vector2(w, h), new Vector2(0, 1));
                ui.buttons[i] = btn; ui.weights[i] = spec.weight;
                x += w + gap;
            }
        }

        static FooterButton FB(string label, UIKit.Icon icon, Color c, string action, float weight = 1f)
            => new FooterButton { label = label, icon = icon, color = c, action = action, weight = weight };

        // ---------------------------------------------------------------- pause

        void BuildPause()
        {
            var d = OpenDialog("Pause", new Vector2(780, 520), "PAUSED", "LEVEL NAME", UIKit.Cyan, UIKit.Icon.Pause);
            var ui = pauseDialog = SaveDialog(d);
            var slots = CardSlots(d, 3, -HeaderH - 24);
            ui.values = new[]
            {
                StatCard(d.body, slots[0], cardSize, null, true, "GOLD", "0 / 0", UIKit.Gold),
                StatCard(d.body, slots[1], cardSize, UIKit.Icon.Clock, false, "TIME", "0:00.0", UIKit.Cream),
                StatCard(d.body, slots[2], cardSize, UIKit.Icon.Heart, false, "LIVES", "2 / 2", UIKit.Cream),
            };
            float toggleY = -HeaderH - 24 - 116 - 22;
            ui.music = ToggleButton(d.body, new Vector2(-150, 0), "MUSIC");
            ui.sound = ToggleButton(d.body, new Vector2(150, 0), "SOUND");
            foreach (var b in new[] { ui.music, ui.sound }) // into the body row, under the cards
            {
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, toggleY);
            }
            Footer(d, ui,
                FB("MENU", UIKit.Icon.Home, UIKit.Gray, "menu"),
                FB("RETRY", UIKit.Icon.Retry, UIKit.Blue, "retry"),
                FB("RESUME", UIKit.Icon.Play, UIKit.Green, "resume", 1.3f));
            d.root.gameObject.SetActive(false);
        }

        public void ShowPause()
        {
            var ui = pauseDialog;
            if (ui.root.gameObject.activeSelf) return;
            if (ui.subtitle) ui.subtitle.text = gm.Def.title.ToUpper();
            ui.values[0].text = $"{gm.Collected} / {gm.TotalCoins}";
            ui.values[1].text = UIKit.FormatTime(gm.Elapsed);
            ui.values[2].text = $"{gm.Lives} / {gm.MaxLives}";
            SetHudVisible(false);
            ShowDialog(ui);
        }

        public static void AddToggle(Transform parent, Vector2 pos, string label, System.Func<bool> get, System.Action<bool> set)
            => BindToggle(ToggleButton(parent, pos, label), label, get, set);

        /// <summary>An ON/OFF chip button (MUSIC or SOUND) without a click action yet: see BindToggle.</summary>
        public static Button ToggleButton(Transform parent, Vector2 pos, string label)
        {
            var b = UIKit.ChipButton(parent, label == "MUSIC" ? UIKit.Icon.Music : UIKit.Icon.Sound, label + "  ON", UIKit.Green, new Vector2(260, 76), null, 28);
            ((RectTransform)b.transform).At(new Vector2(0.5f, 0), pos, new Vector2(260, 76), new Vector2(0.5f, 0.5f));
            return b;
        }

        /// <summary>Connects a toggle button to a setting: click flips it, the label shows ON / OFF.</summary>
        public static void BindToggle(Button b, string label, System.Func<bool> get, System.Action<bool> set)
        {
            if (!b) return;
            b.onClick.AddListener(() =>
            {
                AudioFX.Play("click");
                set(!get());
                Refresh();
            });
            void Refresh()
            {
                UIKit.ButtonLabel(b).text = label + (get() ? "  ON" : "  OFF");
                var disc = b.transform.Find("Glass/IconDisc");
                if (disc) disc.GetComponent<Image>().color = UIKit.WithAlpha(get() ? UIKit.Green : UIKit.Gray, 0.85f);
            }
            Refresh();
        }

        public void HidePause()
        {
            var r = pauseDialog.root;
            if (!r || !r.gameObject.activeSelf) return;
            var cg = r.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Fade(cg, 0f, 0.18f, 0f, () => { if (r) r.gameObject.SetActive(false); });
            SetHudVisible(true);
        }

        // ---------------------------------------------------------------- win

        static readonly string[] CritNames = { "Collect all gold & escape", "Beat par time", "Don't get caught" };

        void BuildWin()
        {
            var d = OpenDialog("Win", new Vector2(860, 740), "LEVEL COMPLETE!", "LEVEL NAME", UIKit.Green, UIKit.Icon.Star);
            var ui = winDialog = SaveDialog(d);
            var pt = d.body;

            // stars: a faint back star + the gold star for each of the 3 stars
            var starRow = UIKit.Rect("Stars", pt).At(new Vector2(0.5f, 1), new Vector2(0, -HeaderH - 100), new Vector2(560, 170), new Vector2(0.5f, 0.5f));
            ui.stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 150 : 118;
                var pos = new Vector2((i - 1) * 160, i == 1 ? 18 : -6);
                var back = UIKit.StarIcon(starRow, size, false, "Back" + i);
                back.color = new Color(1, 1, 1, 0.1f);
                back.rectTransform.At(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
                back.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 12);
                var star = UIKit.StarIcon(starRow, size, true, "Star" + i);
                star.rectTransform.At(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
                star.rectTransform.localRotation = back.rectTransform.localRotation;
                ui.stars[i] = star;
            }

            // criteria (three compact rows)
            float cy = -HeaderH - 196;
            ui.critStars = new Image[3]; ui.critTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var row = UIKit.Rect("Crit" + i, pt).At(new Vector2(0.5f, 1), new Vector2(-200, cy - i * 40), new Vector2(460, 36), new Vector2(0, 1));
                var s = UIKit.StarIcon(row, 28, true); s.rectTransform.At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 28), new Vector2(0, 0.5f));
                var t = UIKit.Label(row, CritNames[i], 26, UIKit.Cream, TextAnchor.MiddleLeft, false);
                t.rectTransform.Fill(42, 0, 0, 0);
                ui.critStars[i] = s; ui.critTexts[i] = t;
            }

            // stat cards: time · best · gold earned
            var slots = CardSlots(d, 3, cy - 3 * 40 - 18);
            ui.values = new[]
            {
                StatCard(pt, slots[0], cardSize, UIKit.Icon.Clock, false, "TIME", "0:00.0", UIKit.Cream),
                StatCard(pt, slots[1], cardSize, UIKit.Icon.Star, false, "BEST", "0:00.0", UIKit.Cream),
                StatCard(pt, slots[2], cardSize, null, true, "GOLD EARNED", "+0", UIKit.Gold),
            };
            ui.captions = new[] { Caption(ui.values[0]), Caption(ui.values[1]), Caption(ui.values[2]) };

            Footer(d, ui,
                FB("MENU", UIKit.Icon.Home, UIKit.Gray, "menu"),
                FB("RETRY", UIKit.Icon.Retry, UIKit.Blue, "retry"),
                FB("NEXT", UIKit.Icon.Play, UIKit.Green, "next", 1.3f));
            d.root.gameObject.SetActive(false);
        }

        static Text Caption(Text value) => value.transform.parent.Find("Caption")?.GetComponent<Text>();

        public void ShowWin(RunResult r)
        {
            HideHud();
            var ui = winDialog;
            ui.title.text = GameSession.IsDaily ? "DAILY CLEARED!" : "LEVEL COMPLETE!";
            if (ui.subtitle) ui.subtitle.text = gm.Def.title.ToUpper();

            for (int i = 0; i < 3; i++)
            {
                var srt = ui.stars[i].rectTransform;
                bool got = i < r.stars;
                ui.stars[i].gameObject.SetActive(got);
                if (!got) continue;
                Quaternion endRot = Quaternion.Euler(0, 0, (1 - i) * 12);
                srt.localRotation = endRot;
                srt.localScale = Vector3.zero;
                int idx = i;
                Tween.Run(srt, 0.55f, k =>
                {
                    srt.localScale = Vector3.one * k;
                    srt.localRotation = endRot * Quaternion.Euler(0, (1 - k) * 180f, (1 - k) * -90f);
                }, Tween.OutBack, 0.45f + idx * 0.35f);
                Tween.Delay(srt, 0.45f + idx * 0.35f, () => AudioFX.Play("star", 1f, 1f + idx * 0.12f));
            }

            bool[] ok = { true, r.parBeaten, r.noHits };
            ui.critTexts[1].text = "Beat par time " + UIKit.FormatTime(r.par).Replace(".0", "");
            for (int i = 0; i < 3; i++)
            {
                ui.critStars[i].color = ok[i] ? UIKit.Gold : new Color(1, 1, 1, 0.18f);
                ui.critTexts[i].color = ok[i] ? UIKit.Cream : UIKit.WithAlpha(UIKit.Cream, 0.4f);
            }

            ui.values[0].text = UIKit.FormatTime(r.time);
            if (ui.captions[1]) ui.captions[1].text = r.newBest ? "NEW BEST!" : "BEST";
            ui.values[1].text = UIKit.FormatTime(r.best);
            ui.values[1].color = r.newBest ? UIKit.Gold : UIKit.Cream;
            int total = r.goldEarned + r.bonus;
            if (ui.captions[2]) ui.captions[2].text = r.bonus > 0 ? $"GOLD (+{r.bonus} daily)" : "GOLD EARNED";
            var gt = ui.values[2];
            gt.text = "+0";
            Tween.Run(gt, 0.9f, k => gt.text = "+" + Mathf.RoundToInt(total * k), Tween.OutCubic, 1.3f);

            ui.buttons[2].gameObject.SetActive(r.hasNext); // no NEXT after the last level
            LayoutFooter(ui);
            ShowDialog(ui);
        }

        // ---------------------------------------------------------------- lose

        static readonly string[] Tips =
        {
            "Push the stick to the edge to sprint past slow slimes.",
            "Hide behind walls to break a ghost's line of sight.",
            "GHOST power-up makes monsters forget you instantly.",
            "The Mushroom King never strays far from its gold.",
            "Slimes only see what's in front of them — sneak behind!",
            "Grab MAGNET near a cluster of coins.",
        };

        void BuildLose()
        {
            var d = OpenDialog("Lose", new Vector2(780, 520), "CAUGHT!", "The monsters got you...", UIKit.Red, UIKit.Icon.Close);
            var ui = loseDialog = SaveDialog(d);
            var pt = d.body;
            var slots = CardSlots(d, 3, -HeaderH - 24);
            ui.values = new[]
            {
                StatCard(pt, slots[0], cardSize, null, true, "COLLECTED", "0 / 0", UIKit.Gold),
                StatCard(pt, slots[1], cardSize, null, true, "BANKED", "+0", UIKit.Gold),
                StatCard(pt, slots[2], cardSize, UIKit.Icon.Clock, false, "TIME", "0:00.0", UIKit.Cream),
            };
            var tip = UIKit.Rect("Tip", pt).At(new Vector2(0, 1), new Vector2(Pad, -HeaderH - 24 - 116 - 20), new Vector2(d.width - Pad * 2, 80), new Vector2(0, 1));
            var tb = UIKit.Img(tip, Sprites.RoundRect, UIKit.WithAlpha(UIKit.Cyan, 0.1f), "Bg"); tb.rectTransform.Fill(); tb.pixelsPerUnitMultiplier = 1.8f;
            var te = UIKit.Img(tip, Sprites.RoundOutline, UIKit.WithAlpha(UIKit.Cyan, 0.35f), "Edge"); te.rectTransform.Fill(); te.pixelsPerUnitMultiplier = 1.8f;
            var ib = UIKit.Img(tip, Sprites.Circle, UIKit.WithAlpha(UIKit.Cyan, 0.8f), "Icon");
            ib.rectTransform.At(new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(44, 44), new Vector2(0, 0.5f));
            UIKit.IconGraphic(ib.transform, UIKit.Icon.Info, 24, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24), new Vector2(0.5f, 0.5f));
            var tt = UIKit.Label(tip, "<b>TIP</b>  " + Tips[0], 24, UIKit.WithAlpha(UIKit.Cream, 0.9f), TextAnchor.MiddleLeft, false, "TipText");
            tt.horizontalOverflow = HorizontalWrapMode.Wrap;
            tt.rectTransform.Fill(80, 6, 20, 6);
            ui.tip = tt;
            Footer(d, ui,
                FB("MENU", UIKit.Icon.Home, UIKit.Gray, "menu"),
                FB("RETRY", UIKit.Icon.Retry, UIKit.Green, "retry", 1.3f));
            d.root.gameObject.SetActive(false);
        }

        public void ShowLose(RunResult r)
        {
            HideHud();
            var ui = loseDialog;
            ui.values[0].text = $"{r.collected} / {r.total}";
            ui.values[1].text = $"+{r.goldEarned}";
            ui.values[2].text = UIKit.FormatTime(r.time);
            if (ui.tip) ui.tip.text = "<b>TIP</b>  " + Tips[Random.Range(0, Tips.Length)];
            ShowDialog(ui);
        }

        /// <summary>Fades the HUD out while a modal panel (pause) is open, so nothing overlaps it.</summary>
        void SetHudVisible(bool visible)
        {
            if (!hud) return;
            var g = hud.GetComponent<CanvasGroup>();
            if (g == null) g = hud.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = visible;
            Tween.Fade(g, visible ? 1f : 0f, 0.2f);
            Markers.gameObject.SetActive(visible);
        }

        void HideHud()
        {
            Markers.gameObject.SetActive(false);
            var g = hud.GetComponent<CanvasGroup>();
            if (g == null) g = hud.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false;
            Tween.Fade(g, 0f, 0.3f);
            Joystick.Release();
            Joystick.gameObject.SetActive(false);
        }
    }
}
