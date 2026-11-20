using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>In-game HUD + modal screens (intro, pause, win, lose), all built in code.</summary>
    public class GameUI : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }
        public WorldMarkers Markers { get; private set; }

        GameManager gm;
        RectTransform canvasRt, hud, modal;

        Text coinText; RectTransform coinPill, coinIcon;
        readonly List<Image> hearts = new List<Image>();
        Text timerText, parText; Image parStar;
        Image staminaFill; Text sprintText; RectTransform staminaRoot; CanvasGroup staminaGroup;
        class PowerRow { public RectTransform root; public Image fill; public PowerType type; }
        readonly List<PowerRow> powerRows = new List<PowerRow>();
        RectTransform arrowRoot, arrowTri; Image arrowIconBg;
        CanvasGroup toastGroup; RectTransform toastRt; Text toastTitle, toastSub; Image toastBg, toastLip, toastShadow; Coroutine toastCo;
        RectTransform heartsRoot, timerRoot, pauseRoot2, dayNightRoot;
        Image flash;
        CanvasGroup hintGroup;

        public static GameUI Create(GameManager gm)
        {
            var canvas = UIKit.Canvas("GameUI", 10);
            var ui = canvas.gameObject.AddComponent<GameUI>();
            ui.gm = gm;
            ui.canvasRt = (RectTransform)canvas.transform;
            ui.Build();
            return ui;
        }

        // ================================================================ HUD

        void Build()
        {
            // input layers first (lowest), then markers, then HUD widgets, then modal
            TouchLookArea.Create(canvasRt);
            Joystick = VirtualJoystick.Create(canvasRt);
            Markers = WorldMarkers.Create(canvasRt);
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
            pb.targetGraphic = pf;
            pb.onClick.AddListener(() => { AudioFX.Play("click"); gm.Pause(); });

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
            arrowRoot.gameObject.SetActive(false);

            // toast (message pop-up): compact glass pill tinted with the message colour
            toastRt = UIKit.Rect("Toast", hud).At(new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(640, 96), new Vector2(0.5f, 0.5f));
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
            toastShadow = UIKit.Img(toastRt, Sprites.Pill, new Color(0, 0, 0, 0.18f), "Shadow"); toastShadow.rectTransform.Fill(2, -6, 2, 4); toastShadow.pixelsPerUnitMultiplier = 128f / 96f;
            toastBg = UIKit.Glass(toastRt, Sprites.Pill, 128f / 96f, 0.55f); // same glass as the HUD chips
            toastLip = toastBg;
            toastTitle = UIKit.Label(toastRt, "", 38, UIKit.Cream); toastTitle.rectTransform.Fill(0, 34, 0, 0);
            toastSub = UIKit.Label(toastRt, "", 22, UIKit.Cream); toastSub.rectTransform.Fill(0, 8, 0, 60);

            // hearts
            for (int i = 0; i < 3; i++)
            {
                var h = UIKit.Img(heartsRt, Sprites.Heart, UIKit.Red, "Heart" + i);
                h.rectTransform.At(new Vector2(0, 0.5f), new Vector2(i * 48, 0), new Vector2(40, 40), new Vector2(0, 0.5f));
                hearts.Add(h);
            }

            heartsRoot = heartsRt; timerRoot = timer; pauseRoot2 = pause;
            coinIcon.gameObject.AddComponent<CoinFlip>();
            PlayHudIntro();

            flash = UIKit.Img(canvasRt, null, new Color(1, 0, 0, 0), "Flash");
            flash.rectTransform.Fill();
            modal = UIKit.Rect("Modal", canvasRt); modal.Fill();
        }

        /// <summary>HUD pieces drop in from above the screen, one after another.</summary>
        void PlayHudIntro()
        {
            var items = new[] { coinPill, heartsRoot, timerRoot, dayNightRoot, pauseRoot2 };
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

        public void ShowControlsHint()
        {
            var root = UIKit.Rect("Hint", hud).At(new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1000, 56), new Vector2(0.5f, 0));
            hintGroup = root.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            UIKit.Glass(root, Sprites.Pill, 128f / 56f);
            bool touch = Application.isMobilePlatform;
            string msg = touch
                ? "<color=#FFC233>Left side</color>: move (push to the edge to run)  •  <color=#FFC233>Right side</color>: look around"
                : "<color=#FFC233>WASD</color> move  •  <color=#FFC233>Mouse</color> look  •  <color=#FFC233>Shift</color> sprint  •  <color=#FFC233>Esc</color> pause / free mouse";
            var t = UIKit.Label(root, msg, 24, UIKit.Cream); t.rectTransform.Fill();
            Tween.Fade(hintGroup, 0f, 0.6f, 7f, () => { if (root) Destroy(root.gameObject); });
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
                    r.root.Find("Name").GetComponent<Text>().text = PowerUp.NameOf(t);
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
            arrowRoot.Find("CoinIcon").gameObject.SetActive(!gate);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            arrowRoot.localScale = Vector3.one * pulse;
        }

        // ================================================================ INTRO

        public IEnumerator Intro(LevelDef def, int number)
        {
            var root = UIKit.Rect("Intro", modal); root.Fill();
            var g = root.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false;
            var band = UIKit.Img(root, null, UIKit.WithAlpha(UIKit.Navy, 0.55f), "Band");
            band.rectTransform.At(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(4000, 300), new Vector2(0.5f, 0.5f));
            var lv = UIKit.Label(band.transform, number > 0 ? "LEVEL " + number : "DAILY CHALLENGE", 44, UIKit.Gold);
            lv.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(1200, 60), new Vector2(0.5f, 0.5f));
            var title = UIKit.Label(band.transform, def.title.ToUpper(), 120, UIKit.Cream);
            title.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1600, 140), new Vector2(0.5f, 0.5f));
            var goal = UIKit.Label(band.transform, $"Collect all <color=#FFC233>{def.coins} gold</color>, then escape through the <color=#6BE08F>EXIT</color>!", 38, UIKit.Cream);
            goal.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(1600, 50), new Vector2(0.5f, 0.5f));
            Tween.Scale(band.rectTransform, new Vector3(1, 0, 1), Vector3.one, 0.35f, Tween.OutBack);
            Tween.PopIn(title.rectTransform, 0.5f, 0.15f);

            yield return new WaitForSeconds(1.4f);
            Tween.Fade(g, 0f, 0.3f);

            var count = UIKit.Label(modal, "", 220, UIKit.Gold);
            count.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 260), new Vector2(0.5f, 0.5f));
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
            var cg = count.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(cg, 0f, 0.4f, 0.1f, () => { if (count) Destroy(count.gameObject); });
            Tween.Scale(count.rectTransform, Vector3.one, Vector3.one * 1.6f, 0.5f, Tween.OutCubic);
            Destroy(root.gameObject, 0.5f);
        }

        // ================================================================ MODALS

        // ---------------------------------------------------------------- dialog building blocks
        // Every dialog = glass panel with HEADER (icon badge + title + subtitle), BODY and FOOTER (buttons).

        class Dialog { public RectTransform root, panel; public Transform body; public float width, height; }

        const float HeaderH = 124f, FooterH = 132f, Pad = 40f;

        Dialog OpenDialog(string name, Vector2 size, string title, string subtitle, Color accent, UIKit.Icon icon)
        {
            var d = new Dialog { width = size.x, height = size.y };
            d.root = UIKit.Rect(name, modal); d.root.Fill();
            UIKit.Dim(d.root, 0.65f);
            var cg = d.root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            Tween.Fade(cg, 1f, 0.25f);

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
            Tween.Run(badge, 0.6f, k => badge.rectTransform.localScale = Vector3.one * k, Tween.OutElastic, 0.2f);
            var t = UIKit.SoftLabel(face.transform, title, 50, UIKit.Cream, TextAnchor.LowerLeft, "Title");
            t.rectTransform.At(new Vector2(0, 1), new Vector2(Pad + 108, -18), new Vector2(size.x - Pad * 2 - 108, 60), new Vector2(0, 1));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var st = UIKit.Label(face.transform, subtitle, 24, UIKit.WithAlpha(UIKit.Cream, 0.65f), TextAnchor.UpperLeft, false, "Subtitle");
                st.rectTransform.At(new Vector2(0, 1), new Vector2(Pad + 110, -80), new Vector2(size.x - Pad * 2 - 110, 32), new Vector2(0, 1));
            }
            Divider(face.transform, -HeaderH);
            Divider(face.transform, -(size.y - FooterH));

            // entrance: flip down + pop
            var prt = d.panel;
            Tween.Run(prt, 0.5f, k =>
            {
                prt.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(55f, 0f, k), 0, 0);
                prt.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, k);
            }, Tween.OutBack);
            AudioFX.Play("whoosh", 0.6f);
            return d;
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

        struct FooterButton { public string label; public UIKit.Icon icon; public Color color; public System.Action onClick; public float weight; }

        /// <summary>Evenly spaced glass buttons in the footer; weight makes the main action wider.</summary>
        void Footer(Dialog d, params FooterButton[] buttons)
        {
            float gap = 16f, total = 0f;
            foreach (var b in buttons) total += b.weight;
            float avail = d.width - Pad * 2 - gap * (buttons.Length - 1);
            float x = Pad, h = 92f, y = -(d.height - FooterH) - (FooterH - h) * 0.5f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var spec = buttons[i];
                float w = avail * spec.weight / total;
                var btn = UIKit.ChipButton(d.body, spec.icon, spec.label, spec.color, new Vector2(w, h), spec.onClick, 30, spec.weight > 1f);
                var rt = (RectTransform)btn.transform;
                rt.At(new Vector2(0, 1), new Vector2(x, y), new Vector2(w, h), new Vector2(0, 1));
                Tween.PopIn(rt, 0.35f, 0.35f + i * 0.07f);
                x += w + gap;
            }
        }

        static FooterButton FB(string label, UIKit.Icon icon, Color c, System.Action a, float weight = 1f)
            => new FooterButton { label = label, icon = icon, color = c, onClick = a, weight = weight };

        // ---------------------------------------------------------------- pause

        RectTransform pauseRoot;

        public void ShowPause()
        {
            if (pauseRoot) return;
            var d = OpenDialog("Pause", new Vector2(780, 520), "PAUSED", gm.Def.title.ToUpper(), UIKit.Cyan, UIKit.Icon.Pause);
            pauseRoot = d.root;
            SetHudVisible(false);

            var slots = CardSlots(d, 3, -HeaderH - 24);
            StatCard(d.body, slots[0], cardSize, null, true, "GOLD", $"{gm.Collected} / {gm.TotalCoins}", UIKit.Gold);
            StatCard(d.body, slots[1], cardSize, UIKit.Icon.Clock, false, "TIME", UIKit.FormatTime(gm.Elapsed), UIKit.Cream);
            StatCard(d.body, slots[2], cardSize, UIKit.Icon.Heart, false, "LIVES", $"{gm.Lives} / {gm.MaxLives}", UIKit.Cream);

            float toggleY = -HeaderH - 24 - 116 - 22;
            AddToggle(d.body, new Vector2(-150, 0), "MUSIC", () => Save.Data.music, v => { Save.Data.music = v; Save.Persist(); AudioFX.RefreshMute(); });
            AddToggle(d.body, new Vector2(150, 0), "SOUND", () => Save.Data.sfx, v => { Save.Data.sfx = v; Save.Persist(); AudioFX.RefreshMute(); });
            // AddToggle anchors to the bottom; move both toggles into the body row
            foreach (var b in d.body.GetComponentsInChildren<Button>())
            {
                var rt = (RectTransform)b.transform;
                if (rt.parent != d.body) continue;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, toggleY);
            }

            Footer(d,
                FB("MENU", UIKit.Icon.Home, UIKit.Gray, () => gm.BackToMenu()),
                FB("RETRY", UIKit.Icon.Retry, UIKit.Blue, () => gm.Restart()),
                FB("RESUME", UIKit.Icon.Play, UIKit.Green, () => gm.Resume(), 1.3f));
        }

        public static void AddToggle(Transform parent, Vector2 pos, string label, System.Func<bool> get, System.Action<bool> set)
        {
            Button b = null;
            b = UIKit.ChipButton(parent, label == "MUSIC" ? UIKit.Icon.Music : UIKit.Icon.Sound, "", get() ? UIKit.Green : UIKit.Gray, new Vector2(260, 76), () =>
            {
                set(!get());
                Refresh();
            }, 28);
            void Refresh()
            {
                UIKit.ButtonLabel(b).text = label + (get() ? "  ON" : "  OFF");
                var disc = b.transform.Find("Glass/IconDisc");
                if (disc) disc.GetComponent<Image>().color = UIKit.WithAlpha(get() ? UIKit.Green : UIKit.Gray, 0.85f);
            }
            Refresh();
            ((RectTransform)b.transform).At(new Vector2(0.5f, 0), pos, new Vector2(260, 76), new Vector2(0.5f, 0.5f));
        }

        public void HidePause()
        {
            if (!pauseRoot) return;
            var r = pauseRoot;
            pauseRoot = null;
            var cg = r.GetComponent<CanvasGroup>();
            Tween.Fade(cg, 0f, 0.18f, 0f, () => { if (r) Destroy(r.gameObject); });
            SetHudVisible(true);
        }

        // ---------------------------------------------------------------- win

        public void ShowWin(RunResult r)
        {
            HideHud();
            var d = OpenDialog("Win", new Vector2(860, 740), GameSession.IsDaily ? "DAILY CLEARED!" : "LEVEL COMPLETE!",
                gm.Def.title.ToUpper(), UIKit.Green, UIKit.Icon.Star);
            var pt = d.body;

            // stars
            var starRow = UIKit.Rect("Stars", pt).At(new Vector2(0.5f, 1), new Vector2(0, -HeaderH - 100), new Vector2(560, 170), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 150 : 118;
                var pos = new Vector2((i - 1) * 160, i == 1 ? 18 : -6);
                var back = UIKit.StarIcon(starRow, size, false, "Back" + i);
                back.color = new Color(1, 1, 1, 0.1f);
                back.rectTransform.At(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
                back.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 12);
                if (i >= r.stars) continue;
                var star = UIKit.StarIcon(starRow, size, true, "Star" + i);
                var srt = star.rectTransform;
                srt.At(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
                srt.localRotation = back.rectTransform.localRotation;
                srt.localScale = Vector3.zero;
                int idx = i;
                Quaternion endRot = srt.localRotation;
                Tween.Run(srt, 0.55f, k =>
                {
                    srt.localScale = Vector3.one * k;
                    srt.localRotation = endRot * Quaternion.Euler(0, (1 - k) * 180f, (1 - k) * -90f);
                }, Tween.OutBack, 0.45f + idx * 0.35f);
                Tween.Delay(star, 0.45f + idx * 0.35f, () => AudioFX.Play("star", 1f, 1f + idx * 0.12f));
            }

            // criteria (three compact rows)
            string[] crit = { "Collect all gold & escape", "Beat par time " + UIKit.FormatTime(r.par).Replace(".0", ""), "Don't get caught" };
            bool[] ok = { true, r.parBeaten, r.noHits };
            float cy = -HeaderH - 196;
            for (int i = 0; i < 3; i++)
            {
                var row = UIKit.Rect("Crit" + i, pt).At(new Vector2(0.5f, 1), new Vector2(-200, cy - i * 40), new Vector2(460, 36), new Vector2(0, 1));
                var s = UIKit.StarIcon(row, 28, ok[i]); s.rectTransform.At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 28), new Vector2(0, 0.5f));
                if (!ok[i]) s.color = new Color(1, 1, 1, 0.18f);
                UIKit.Label(row, crit[i], 26, ok[i] ? UIKit.Cream : UIKit.WithAlpha(UIKit.Cream, 0.4f), TextAnchor.MiddleLeft, false).rectTransform.Fill(42, 0, 0, 0);
            }

            // stat cards: time · best · gold earned
            var slots = CardSlots(d, 3, cy - 3 * 40 - 18);
            StatCard(pt, slots[0], cardSize, UIKit.Icon.Clock, false, "TIME", UIKit.FormatTime(r.time), UIKit.Cream);
            StatCard(pt, slots[1], cardSize, UIKit.Icon.Star, false, r.newBest ? "NEW BEST!" : "BEST", UIKit.FormatTime(r.best), r.newBest ? UIKit.Gold : UIKit.Cream);
            int total = r.goldEarned + r.bonus;
            var gt = StatCard(pt, slots[2], cardSize, null, true, r.bonus > 0 ? $"GOLD (+{r.bonus} daily)" : "GOLD EARNED", "+0", UIKit.Gold);
            Tween.Run(gt, 0.9f, k => gt.text = "+" + Mathf.RoundToInt(total * k), Tween.OutCubic, 1.3f);

            if (r.hasNext)
                Footer(d,
                    FB("MENU", UIKit.Icon.Home, UIKit.Gray, () => gm.BackToMenu()),
                    FB("RETRY", UIKit.Icon.Retry, UIKit.Blue, () => gm.Restart()),
                    FB("NEXT", UIKit.Icon.Play, UIKit.Green, () => gm.NextLevel(), 1.3f));
            else
                Footer(d,
                    FB("MENU", UIKit.Icon.Home, UIKit.Gray, () => gm.BackToMenu(), 1.2f),
                    FB("RETRY", UIKit.Icon.Retry, UIKit.Blue, () => gm.Restart()));
        }

        // ---------------------------------------------------------------- lose

        public void ShowLose(RunResult r)
        {
            HideHud();
            var d = OpenDialog("Lose", new Vector2(780, 520), "CAUGHT!", "The monsters got you...", UIKit.Red, UIKit.Icon.Close);
            var pt = d.body;

            var slots = CardSlots(d, 3, -HeaderH - 24);
            StatCard(pt, slots[0], cardSize, null, true, "COLLECTED", $"{r.collected} / {r.total}", UIKit.Gold);
            StatCard(pt, slots[1], cardSize, null, true, "BANKED", $"+{r.goldEarned}", UIKit.Gold);
            StatCard(pt, slots[2], cardSize, UIKit.Icon.Clock, false, "TIME", UIKit.FormatTime(r.time), UIKit.Cream);

            string[] tips =
            {
                "Push the stick to the edge to sprint past slow slimes.",
                "Hide behind walls to break a ghost's line of sight.",
                "GHOST power-up makes monsters forget you instantly.",
                "The Mushroom King never strays far from its gold.",
                "Slimes only see what's in front of them — sneak behind!",
                "Grab MAGNET near a cluster of coins.",
            };
            var tip = UIKit.Rect("Tip", pt).At(new Vector2(0, 1), new Vector2(Pad, -HeaderH - 24 - 116 - 20), new Vector2(d.width - Pad * 2, 80), new Vector2(0, 1));
            var tb = UIKit.Img(tip, Sprites.RoundRect, UIKit.WithAlpha(UIKit.Cyan, 0.1f), "Bg"); tb.rectTransform.Fill(); tb.pixelsPerUnitMultiplier = 1.8f;
            var te = UIKit.Img(tip, Sprites.RoundOutline, UIKit.WithAlpha(UIKit.Cyan, 0.35f), "Edge"); te.rectTransform.Fill(); te.pixelsPerUnitMultiplier = 1.8f;
            var ib = UIKit.Img(tip, Sprites.Circle, UIKit.WithAlpha(UIKit.Cyan, 0.8f), "Icon");
            ib.rectTransform.At(new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(44, 44), new Vector2(0, 0.5f));
            UIKit.IconGraphic(ib.transform, UIKit.Icon.Info, 24, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24), new Vector2(0.5f, 0.5f));
            var tt = UIKit.Label(tip, "<b>TIP</b>  " + tips[Random.Range(0, tips.Length)], 24, UIKit.WithAlpha(UIKit.Cream, 0.9f), TextAnchor.MiddleLeft, false);
            tt.horizontalOverflow = HorizontalWrapMode.Wrap;
            tt.rectTransform.Fill(80, 6, 20, 6);

            Footer(d,
                FB("MENU", UIKit.Icon.Home, UIKit.Gray, () => gm.BackToMenu()),
                FB("RETRY", UIKit.Icon.Retry, UIKit.Green, () => gm.Restart(), 1.3f));
        }

        /// <summary>Fades the HUD out while a modal panel (pause) is open, so nothing overlaps it.</summary>
        void SetHudVisible(bool visible)
        {
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
