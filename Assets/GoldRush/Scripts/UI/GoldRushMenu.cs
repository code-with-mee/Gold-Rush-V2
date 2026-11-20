using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// Main menu scene root: 3D diorama + Title / Level Select / Shop / Settings screens.
    /// The stage and the screens are normal GameObjects saved in MainMenu.unity (menu: Gold Rush > Level Design >
    /// Rebuild Menu Scene). This script only connects them: buttons with a <see cref="MenuAction"/> call
    /// <see cref="Run"/>, and the data (gold, stars, level cards, shop items) is filled in when the scene starts.
    /// If the scene is empty, everything is built from code instead.
    /// </summary>
    public class GoldRushMenu : MonoBehaviour
    {
        enum Screen { Title, Levels, Shop }

        public static GoldRushMenu I { get; private set; }

        [Header("Stage (saved in the scene)")]
        [SerializeField] Camera cam;
        [SerializeField] Transform playerRoot;
        [SerializeField] Animator playerAnim;
        [SerializeField] Light lantern;
        [SerializeField] List<Light> menuGlows = new List<Light>();

        [Header("Screens (saved in the scene)")]
        [SerializeField] RectTransform canvasRt;
        [SerializeField] RectTransform titleScreen, levelScreen, shopScreen, settingsRoot, settingsPanel;
        [SerializeField] GameObject[] topRight;
        [SerializeField] RectTransform[] entrance;

        [Header("Filled in at runtime")]
        [SerializeField] RectTransform goldPill;
        [SerializeField] Text goldText, titleGoldText, starsText, playSubtitle;
        [SerializeField] RectTransform starsBar, levelCards, shopTabs, shopGrid;
        [SerializeField] Button musicToggle, soundToggle;

        Screen current = Screen.Title;
        float lookSide = -1.3f, lookSideTarget = -1.3f, camDist = 5.6f, camDistTarget = 5.6f;
        bool shopHats;
        int shownGold;

        void Awake() { I = this; Application.targetFrameRate = 60; Time.timeScale = 1f; }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            UIKit.EnsureEventSystem();
            AudioFX.StartMusic();
            // anything not placed in the scene is built from code
            if (!cam) BuildWorld();
            if (!titleScreen) BuildUI();
            EnvironmentSetup.Apply(cam, true, true); // the menu uses the spooky night mood
            if (lantern) DayNight.RegisterNightLight(lantern);
            foreach (var g in menuGlows) DayNight.RegisterNightLight(g);
            Cosmetics.Apply(playerAnim, GameAssets.I.hero, Save.Data.color, Save.Data.hat, false);
            RefreshTitle();
            BindSettings();
            settingsRoot.gameObject.SetActive(false);
            UpdateCamera(1f);
            Show(Screen.Title, true);
            PlayEntrance();
            Fader.FadeIn();
        }

#if UNITY_EDITOR
        /// <summary>Editor only (LevelBaker): builds the stage and every screen as scene objects.</summary>
        public void BuildForEditor()
        {
            BuildWorld();
            BuildUI();
        }
#endif

        /// <summary>Puts the saved data into the title screen: gold, stars and the PLAY subtitle.</summary>
        void RefreshTitle()
        {
            shownGold = Save.Data.gold;
            goldText.text = titleGoldText.text = shownGold.ToString();
            int stars = 0; foreach (var l in Save.Data.levels) stars += l.stars;
            int maxStars = Levels.All.Length * 3;
            starsText.text = $"{stars}<size=24><color=#9FB3D9> / {maxStars}</color></size>";
            starsBar.anchorMax = new Vector2(Mathf.Max(0.08f, stars / (float)maxStars), 1);
            int next = Mathf.Clamp(Save.Data.unlocked - 1, 0, Levels.All.Length - 1);
            if (playSubtitle) playSubtitle.text = $"LEVEL {next + 1}  •  {Levels.All[next].title.ToUpper()}";
        }

        /// <summary>Entrance animation: the buttons slide in from the left.</summary>
        void PlayEntrance()
        {
            for (int k = 0; k < entrance.Length; k++)
            {
                var rt = entrance[k];
                if (!rt) continue;
                Vector2 target = rt.anchoredPosition;
                rt.anchoredPosition = target + Vector2.left * 640f;
                Tween.Move(rt, target, 0.55f, Tween.OutBack, 0.15f + k * 0.08f);
            }
        }

        static Button WithAction(Button b, string action)
        {
            b.gameObject.AddComponent<MenuAction>().action = action;
            return b;
        }

        // ================================================================ WORLD

        void BuildWorld()
        {
            LevelBuilder.BuildMenuDiorama();

            playerRoot = new GameObject("MenuPlayer").transform;
            playerRoot.rotation = Quaternion.Euler(0, 180, 0);
            var a = GameAssets.I;
            playerAnim = CharacterFactory.BuildModel(a.hero, playerRoot);

            // the monster cast hanging around behind the hero
            Monster(a.slime, EnemyType.Patroller, new Vector3(-3.4f, 0, 3.2f), 160);
            Monster(a.ghost, EnemyType.Chaser, new Vector3(3.2f, 0.4f, 3.6f), 205);
            Monster(a.mushroomKing, EnemyType.Guard, new Vector3(5.6f, 0, 8.5f), 210);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();

            // warm lantern light on the hero
            var spot = lantern = new GameObject("Lantern").AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.82f, 0.55f);
            spot.intensity = 20f;
            spot.range = 14f;
            spot.spotAngle = 55f;
            spot.shadows = LightShadows.Soft;
            spot.transform.position = new Vector3(-1.5f, 5.5f, -3.5f);
            spot.transform.LookAt(new Vector3(0, 0.5f, 0));
            UpdateCamera(1f);
        }

        void Monster(CharacterDef def, EnemyType type, Vector3 pos, float yaw)
        {
            if (def == null || !def.model) return;
            var t = new GameObject("Menu" + type).transform;
            t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var anim = CharacterFactory.BuildModel(def, t);
            Badge.Attach(anim, def, EnemyAI.BadgeHeight(type), EnemyAI.SignatureColor(type));
            menuGlows.Add(CharacterFactory.AddGlow(t, EnemyAI.SignatureColor(type), def.height, type == EnemyType.Guard ? 3f : 2.2f));
        }

        float nextWave = 3f;

        void Update()
        {
            // the hero waves at the player now and then
            if (current == Screen.Title && Time.time > nextWave && playerAnim)
            {
                nextWave = Time.time + 9f;
                playerAnim.SetTrigger(AnimParams.Win);
            }

            if (playerRoot)
            {
                float yaw = current == Screen.Shop ? 180f + 28f * Mathf.Sin(Time.time * 0.7f) : 180f;
                playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, Quaternion.Euler(0, yaw, 0), 1f - Mathf.Exp(-3f * Time.deltaTime));
            }
            if (Input.GetKeyDown(KeyCode.Escape) && current != Screen.Title) Show(Screen.Title);

            // animated gold counter
            int g = Save.Data.gold;
            if (shownGold != g)
            {
                shownGold = (int)Mathf.MoveTowards(shownGold, g, Mathf.Max(1, Mathf.Abs(g - shownGold) * 6f * Time.unscaledDeltaTime));
                goldText.text = shownGold.ToString();
                if (titleGoldText) titleGoldText.text = goldText.text;
            }
        }

        void LateUpdate() { UpdateCamera(Time.deltaTime); }

        void UpdateCamera(float dt)
        {
            if (!cam) return;
            float k = 1f - Mathf.Exp(-3.5f * dt);
            lookSide = Mathf.Lerp(lookSide, lookSideTarget, k);
            camDist = Mathf.Lerp(camDist, camDistTarget, k);
            float yaw = 10f * Mathf.Sin(Time.time * 0.22f);
            var rot = Quaternion.Euler(7f, yaw, 0);
            Vector3 right = rot * Vector3.right;
            Vector3 look = new Vector3(0, 0.8f, 0) + right * lookSide;
            cam.transform.SetPositionAndRotation(look - rot * Vector3.forward * camDist, rot);
        }

        // ================================================================ UI

        void BuildUI()
        {
            var canvas = UIKit.Canvas("MenuUI", 10);
            canvas.transform.SetParent(transform, false);
            canvasRt = (RectTransform)canvas.transform;

            titleScreen = UIKit.Rect("Title", canvasRt); titleScreen.Fill();
            levelScreen = UIKit.Rect("Levels", canvasRt); levelScreen.Fill();
            shopScreen = UIKit.Rect("Shop", canvasRt); shopScreen.Fill();
            foreach (var s in new[] { titleScreen, levelScreen, shopScreen }) s.gameObject.AddComponent<CanvasGroup>();

            // ---- top bar (always visible): settings · day/night · gold
            goldPill = UIKit.Rect("GoldPill", canvasRt).At(new Vector2(1, 1), new Vector2(-40, -34), new Vector2(300, 100));
            var gb = UIKit.Glass(goldPill, Sprites.Pill, 128f / 100f);
            gb.transform.Find("Edge").GetComponent<Image>().color = UIKit.WithAlpha(UIKit.Gold, 0.55f);
            var gi = UIKit.CoinIcon(goldPill, 76); gi.At(new Vector2(0, 0.5f), new Vector2(52, 3), new Vector2(76, 76), new Vector2(0.5f, 0.5f));
            gi.gameObject.AddComponent<CoinFlip>();
            goldText = UIKit.Label(goldPill, Save.Data.gold.ToString(), 50, UIKit.Gold, TextAnchor.MiddleLeft);
            goldText.rectTransform.Fill(106, 0, 16, 0);
            shownGold = Save.Data.gold;

            var dn = DayNightButton.Create(canvasRt, new Vector2(1, 1), new Vector2(-362, -34), 100f, true);
            var gear = WithAction(UIKit.RoundIconButton(canvasRt, UIKit.Icon.Gear, null, 100, true), "settings");
            ((RectTransform)gear.transform).At(new Vector2(1, 1), new Vector2(-484, -34), new Vector2(100, 100));
            // top-right bar is used on the Levels / Shop screens; the title screen has its own top-left cluster
            topRight = new[] { goldPill.gameObject, dn.gameObject, gear.gameObject };

            BuildTitle();
            BuildLevelsScreen();
            BuildShop();
            BuildSettings();
        }

        void BuildTitle()
        {
            var t = titleScreen;

            // soft dark backdrops: top (behind the title) and left (behind the buttons)
            var fadeT = UIKit.Img(t, Sprites.FadeDown, UIKit.WithAlpha(UIKit.Navy, 0.6f), "BackdropTop");
            fadeT.rectTransform.anchorMin = new Vector2(0, 1); fadeT.rectTransform.anchorMax = Vector2.one;
            fadeT.rectTransform.pivot = new Vector2(0.5f, 1); fadeT.rectTransform.sizeDelta = new Vector2(0, 420);
            var fadeL = UIKit.Img(t, Sprites.FadeRight, UIKit.WithAlpha(UIKit.Navy, 0.6f), "BackdropLeft");
            fadeL.rectTransform.anchorMin = Vector2.zero; fadeL.rectTransform.anchorMax = new Vector2(0, 1);
            fadeL.rectTransform.pivot = new Vector2(0, 0.5f); fadeL.rectTransform.sizeDelta = new Vector2(820, 0);

            // ---- top-left icon cluster: settings · day/night · gold · stars
            const float chipH = 76f, top = -30f;
            var gear = WithAction(UIKit.RoundIconButton(t, UIKit.Icon.Gear, null, chipH, true), "settings");
            ((RectTransform)gear.transform).At(new Vector2(0, 1), new Vector2(32, top), new Vector2(chipH, chipH));
            DayNightButton.Create(t, new Vector2(0, 1), new Vector2(32 + chipH + 14, top), chipH, true);

            var gold = UIKit.Rect("Gold", t).At(new Vector2(0, 1), new Vector2(32 + (chipH + 14) * 2, top), new Vector2(200, chipH));
            var gg = UIKit.Glass(gold, Sprites.Pill, 128f / chipH);
            gg.transform.Find("Edge").GetComponent<Image>().color = UIKit.WithAlpha(UIKit.Gold, 0.55f);
            var gi = UIKit.CoinIcon(gold, 54); gi.At(new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(54, 54), new Vector2(0.5f, 0.5f));
            gi.gameObject.AddComponent<CoinFlip>();
            titleGoldText = UIKit.Label(gold, Save.Data.gold.ToString(), 38, UIKit.Gold, TextAnchor.MiddleLeft);
            titleGoldText.rectTransform.Fill(78, 0, 12, 0);

            int stars = 0; foreach (var l in Save.Data.levels) stars += l.stars;
            int maxStars = Levels.All.Length * 3;
            var chip = UIKit.Rect("Stars", t).At(new Vector2(0, 1), new Vector2(32 + (chipH + 14) * 2 + 214, top), new Vector2(200, chipH));
            UIKit.Glass(chip, Sprites.Pill, 128f / chipH);
            UIKit.IconGraphic(chip, UIKit.Icon.Star, 44, UIKit.Gold).At(new Vector2(0, 0.5f), new Vector2(18, 2), new Vector2(44, 44), new Vector2(0, 0.5f));
            starsText = UIKit.Label(chip, $"{stars}<size=24><color=#9FB3D9> / {maxStars}</color></size>", 34, UIKit.Cream, TextAnchor.MiddleLeft);
            starsText.rectTransform.Fill(72, 16, 14, 0);
            var barBg = UIKit.Img(chip, Sprites.Pill, new Color(1, 1, 1, 0.14f), "BarBg");
            barBg.rectTransform.At(new Vector2(0, 0), new Vector2(74, 14), new Vector2(104, 8), new Vector2(0, 0)); barBg.pixelsPerUnitMultiplier = 16f;
            var bar = UIKit.Img(barBg.transform, Sprites.Pill, UIKit.Gold, "Bar");
            bar.rectTransform.anchorMin = Vector2.zero; bar.rectTransform.anchorMax = new Vector2(Mathf.Max(0.08f, stars / (float)maxStars), 1);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero; bar.pixelsPerUnitMultiplier = 16f;
            starsBar = bar.rectTransform;

            // ---- title: top-centre, one line "GOLD RUSH" + tagline
            var glow = UIKit.Img(t, Sprites.Soft, UIKit.WithAlpha(UIKit.Gold, 0.26f), "LogoGlow");
            glow.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1500, 420), new Vector2(0.5f, 1));
            glow.gameObject.AddComponent<Bob>().Setup(0f, 1.2f, 0f, 0.05f);
            var logo = UIKit.Rect("Logo", t).At(new Vector2(0.5f, 1), new Vector2(0, -228), new Vector2(1240, 200), new Vector2(0.5f, 0.5f));
            LogoTitle.Build(logo, 176);
            var tag = UIKit.Rect("Tagline", t).At(new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(480, 46), new Vector2(0.5f, 1));
            var tg = UIKit.Glass(tag, Sprites.Pill, 128f / 46f, 0.55f); // glass so it reads on bright day scenes too
            tg.transform.Find("Edge").GetComponent<Image>().color = UIKit.WithAlpha(UIKit.Cyan, 0.8f);
            UIKit.Label(tag, "GRAB   •   DODGE   •   ESCAPE", 24, UIKit.WithAlpha(UIKit.Cream, 0.9f), TextAnchor.MiddleCenter, false).rectTransform.Fill();

            // ---- buttons: one column on the left (built bottom-up)
            const float colX = 40f, colW = 470f, btnH = 90f, gap = 16f, playH = 140f;
            float y = 60f;
            float shopY = y;   y += btnH + gap;
            float levelsY = y; y += btnH + gap;
            float playY = y;
            int next = Mathf.Clamp(Save.Data.unlocked - 1, 0, Levels.All.Length - 1);
            var playGlow = UIKit.Img(t, Sprites.Soft, UIKit.WithAlpha(UIKit.Cyan, 0.35f), "PlayGlow");
            playGlow.rectTransform.At(new Vector2(0, 0), new Vector2(colX + colW * 0.5f, playY + playH * 0.5f), new Vector2(colW + 260, playH + 160), new Vector2(0.5f, 0.5f));
            playGlow.gameObject.AddComponent<Bob>().Setup(0f, 2.4f, 0f, 0.06f);
            string playSub = $"LEVEL {next + 1}  •  {Levels.All[next].title.ToUpper()}";
            var play = WithAction(UIKit.GlassButton(t, UIKit.Icon.Play, "PLAY", playSub,
                UIKit.Cyan, new Vector2(colW, playH), null, 64, 0.62f), "play");
            foreach (var txt in play.GetComponentsInChildren<Text>(true)) if (txt.text == playSub) playSubtitle = txt;
            ((RectTransform)play.transform).At(new Vector2(0, 0), new Vector2(colX, playY), new Vector2(colW, playH), new Vector2(0, 0));
            var levels = WithAction(UIKit.GlassButton(t, UIKit.Icon.Levels, "LEVELS", null, UIKit.Blue, new Vector2(colW, btnH), null, 38), "levels");
            ((RectTransform)levels.transform).At(new Vector2(0, 0), new Vector2(colX, levelsY), new Vector2(colW, btnH), new Vector2(0, 0));
            var shop = WithAction(UIKit.GlassButton(t, UIKit.Icon.Bag, "SHOP", null, UIKit.Orange, new Vector2(colW, btnH), null, 38), "shop");
            ((RectTransform)shop.transform).At(new Vector2(0, 0), new Vector2(colX, shopY), new Vector2(colW, btnH), new Vector2(0, 0));

            UIKit.Label(t, "v1.0", 22, UIKit.WithAlpha(UIKit.Cream, 0.45f), TextAnchor.LowerRight, false)
                .rectTransform.At(new Vector2(1, 0), new Vector2(-32, 28), new Vector2(200, 30), new Vector2(1, 0));

            // these slide in from the left when the menu opens (see PlayEntrance)
            entrance = new[] { (RectTransform)play.transform, (RectTransform)levels.transform, (RectTransform)shop.transform };
        }

        // ---------------------------------------------------------------- settings dialog

        void BuildSettings()
        {
            settingsRoot = UIKit.Rect("Settings", canvasRt); settingsRoot.Fill();
            settingsRoot.gameObject.AddComponent<CanvasGroup>();
            UIKit.Dim(settingsRoot, 0.55f);
            var face = UIKit.GlassPanel(settingsRoot, new Vector2(600, 440), UIKit.PanelBg, "Panel");
            var prt = (RectTransform)face.transform.parent;
            prt.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 440), new Vector2(0.5f, 0.5f));
            settingsPanel = prt;
            var rib = UIKit.GlassRibbon(face.transform, "SETTINGS", UIKit.Cyan, new Vector2(380, 96), 48);
            ((RectTransform)rib.transform.parent.parent).At(new Vector2(0.5f, 1), new Vector2(0, 36), new Vector2(380, 96), new Vector2(0.5f, 0.5f));
            musicToggle = GameUI.ToggleButton(face.transform, new Vector2(0, 285), "MUSIC");
            soundToggle = GameUI.ToggleButton(face.transform, new Vector2(0, 180), "SOUND");
            var close = WithAction(UIKit.GlassTextButton(face.transform, "CLOSE", UIKit.Gray, new Vector2(240, 86), null, 38), "closeSettings");
            ((RectTransform)close.transform).At(new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(240, 86), new Vector2(0.5f, 0.5f));
            settingsRoot.gameObject.SetActive(false);
        }

        void BindSettings()
        {
            GameUI.BindToggle(musicToggle, "MUSIC", () => Save.Data.music, v => { Save.Data.music = v; Save.Persist(); AudioFX.RefreshMute(); });
            GameUI.BindToggle(soundToggle, "SOUND", () => Save.Data.sfx, v => { Save.Data.sfx = v; Save.Persist(); AudioFX.RefreshMute(); });
        }

        void ShowSettings()
        {
            if (settingsRoot.gameObject.activeSelf) return;
            settingsRoot.gameObject.SetActive(true);
            settingsRoot.SetAsLastSibling();
            var cg = settingsRoot.GetComponent<CanvasGroup>();
            cg.alpha = 0; Tween.Fade(cg, 1f, 0.2f);
            if (settingsPanel) Tween.Scale(settingsPanel, Vector3.one * 0.85f, Vector3.one, 0.35f, Tween.OutBack);
        }

        void HideSettings()
        {
            var cg = settingsRoot.GetComponent<CanvasGroup>();
            Tween.Fade(cg, 0f, 0.15f, 0f, () => { if (settingsRoot) settingsRoot.gameObject.SetActive(false); });
        }

        static void AddHeavyOutline(Text t)
        {
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0.04f, 0.07f, 0.2f, 0.85f);
            o2.effectDistance = new Vector2(3, -3);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.02f, 0.03f, 0.1f, 0.55f);
            sh.effectDistance = new Vector2(0, -7);
        }

        // ---------------------------------------------------------------- level select

        /// <summary>Screen title in the game-UI style (no pill): big title + small subtitle.</summary>
        static void ScreenHeader(Transform parent, string title, string subtitle)
        {
            var t = UIKit.SoftLabel(parent, title, 60, UIKit.Cream, TextAnchor.MiddleCenter, "Header");
            t.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 80), new Vector2(0.5f, 1));
            var st = UIKit.Label(parent, subtitle, 24, UIKit.WithAlpha(UIKit.Cream, 0.65f), TextAnchor.MiddleCenter, false, "SubHeader");
            st.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(900, 34), new Vector2(0.5f, 1));
        }

        // ---------------------------------------------------------------- level select

        const float CardW = 300f, CardH = 440f, CardGap = 28f;

        /// <summary>The fixed part of the Level Select screen (saved in the scene). The cards are added by BuildLevels.</summary>
        void BuildLevelsScreen()
        {
            var dim = UIKit.Img(levelScreen, null, UIKit.WithAlpha(UIKit.Navy, 0.55f), "Dim"); dim.rectTransform.Fill();
            ScreenHeader(levelScreen, "SELECT LEVEL", "Collect all the gold, then run to the exit!");
            BackButton(levelScreen);
            levelCards = UIKit.Rect("Cards", levelScreen).At(new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(5 * CardW + 4 * CardGap, CardH), new Vector2(0.5f, 0.5f));
        }

        /// <summary>One card per level (+ the daily challenge), made from the level data and the save.</summary>
        void BuildLevels()
        {
            foreach (Transform c in levelCards) Destroy(c.gameObject);
            var d = Save.Data;
            int n = Levels.All.Length + 1;                       // + the Daily card (always last)
            const int PerRow = 5;
            int rows = Mathf.CeilToInt(n / (float)PerRow);
            levelCards.localScale = Vector3.one * Mathf.Min(1f, 1.7f / rows); // two rows: a bit smaller
            float step = CardW + CardGap, rowStep = CardH + CardGap;
            for (int k = 0; k < n; k++)
            {
                int r = k / PerRow, inRow = Mathf.Min(PerRow, n - r * PerRow);
                var p = new Vector2((k % PerRow - (inRow - 1) * 0.5f) * step, ((rows - 1) * 0.5f - r) * rowStep);
                if (k < Levels.All.Length) LevelCard(levelCards, k, p, d); else DailyCard(levelCards, p, d);
            }
        }

        static RectTransform CardBadge(Transform card, Color color, string number, UIKit.Icon? icon)
        {
            var badge = UIKit.Img(card, Sprites.Circle, UIKit.WithAlpha(color, 0.9f), "Badge");
            badge.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(104, 104), new Vector2(0.5f, 1));
            UIKit.Img(badge.transform, Sprites.ThinRing, new Color(1, 1, 1, 0.6f), "Ring").rectTransform.Fill();
            if (icon.HasValue) UIKit.IconGraphic(badge.transform, icon.Value, 52, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52), new Vector2(0.5f, 0.5f));
            else UIKit.Label(badge.transform, number, 54, UIKit.Dark, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 2, 0, 0);
            return badge.rectTransform;
        }

        static Text CardText(Transform card, string text, int size, Color color, float y, float h)
        {
            var t = UIKit.Fit(UIKit.Label(card, text, size, color, TextAnchor.MiddleCenter, false), 14);
            t.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(CardW - 36, h), new Vector2(0.5f, 1));
            return t;
        }

        void LevelCard(Transform parent, int i, Vector2 pos, SaveData d)
        {
            var def = Levels.All[i];
            bool locked = i + 1 > d.unlocked;
            var prog = d.levels[i];
            var face = UIKit.GlassCard(parent, new Vector2(CardW, CardH), "Level" + (i + 1), 0.92f);
            var root = (RectTransform)face.transform.parent;
            root.At(new Vector2(0.5f, 0.5f), pos, new Vector2(CardW, CardH), new Vector2(0.5f, 0.5f));
            var ct = face.transform;

            CardBadge(ct, locked ? UIKit.Gray : UIKit.Gold, (i + 1).ToString(), null);
            CardText(ct, def.title.ToUpper(), 32, locked ? UIKit.WithAlpha(UIKit.Cream, 0.6f) : UIKit.Cream, -152, 46);

            if (locked)
            {
                UIKit.IconGraphic(ct, UIKit.Icon.Lock, 84, UIKit.WithAlpha(UIKit.Cream, 0.75f)).At(new Vector2(0.5f, 1), new Vector2(0, -218), new Vector2(84, 84), new Vector2(0.5f, 1));
                UIKit.InfoChip(ct, UIKit.Icon.Info, "Clear level " + i, UIKit.WithAlpha(UIKit.Cream, 0.85f), new Vector2(230, 46))
                    .At(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(230, 46), new Vector2(0.5f, 0));
                root.gameObject.AddComponent<CanvasGroup>().alpha = 0.8f;
            }
            else
            {
                CardText(ct, def.subtitle, 21, UIKit.WithAlpha(UIKit.Cream, 0.7f), -198, 30);
                for (int s = 0; s < 3; s++)
                {
                    var st = UIKit.StarIcon(ct, s == 1 ? 64 : 52, s < prog.stars);
                    if (s >= prog.stars) st.color = new Color(1, 1, 1, 0.16f);
                    st.rectTransform.At(new Vector2(0.5f, 1), new Vector2((s - 1) * 66, s == 1 ? -244 : -252), st.rectTransform.sizeDelta, new Vector2(0.5f, 1));
                }
                bool hasBest = prog.bestTime >= 0;
                UIKit.InfoChip(ct, hasBest ? UIKit.Icon.Clock : UIKit.Icon.Star, hasBest ? "BEST " + UIKit.FormatTime(prog.bestTime) : $"{def.coins} GOLD TO FIND", UIKit.Gold, new Vector2(240, 46))
                    .At(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(240, 46), new Vector2(0.5f, 0));
            }

            int idx = i;
            UIKit.Hoverable(face, locked ? UIKit.Gray : UIKit.Gold, () =>
            {
                if (locked) { AudioFX.Play("error"); Shake(root); return; }
                StartLevel(idx);
            });
            Tween.PopIn(root, 0.4f, 0.05f + i * 0.07f);
        }

        void DailyCard(Transform parent, Vector2 pos, SaveData d)
        {
            var def = Levels.Daily();
            var face = UIKit.GlassCard(parent, new Vector2(CardW, CardH), "Daily", 0.92f);
            var root = (RectTransform)face.transform.parent;
            root.At(new Vector2(0.5f, 0.5f), pos, new Vector2(CardW, CardH), new Vector2(0.5f, 0.5f));
            var ct = face.transform;
            CardBadge(ct, UIKit.Purple, null, UIKit.Icon.Calendar);
            CardText(ct, "DAILY RUSH", 32, UIKit.Cream, -152, 46);
            CardText(ct, def.subtitle, 21, UIKit.WithAlpha(UIKit.Cream, 0.7f), -198, 30);
            CardText(ct, $"{def.coins} gold  •  {def.patrollers + def.chasers + def.guards} monsters", 22, UIKit.Cream, -244, 32);
            CardText(ct, d.dailyBest >= 0 ? "BEST " + UIKit.FormatTime(d.dailyBest) : "NEW TODAY", 24, UIKit.Gold, -284, 34);
            UIKit.InfoChip(ct, UIKit.Icon.Trophy, d.dailyCleared ? "CLEARED" : "+50 BONUS", d.dailyCleared ? UIKit.Cream : UIKit.Gold, new Vector2(220, 46),
                    UIKit.WithAlpha(d.dailyCleared ? Color.white : UIKit.Red, 0.85f))
                .At(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(220, 46), new Vector2(0.5f, 0));
            UIKit.Hoverable(face, UIKit.Purple, () => StartLevel(-1));
            Tween.PopIn(root, 0.4f, 0.05f + 4 * 0.07f);
        }

        // ---------------------------------------------------------------- shop

        /// <summary>The fixed part of the Shop screen (saved in the scene). Tabs and items are added by RefreshShop.</summary>
        void BuildShop()
        {
            BackButton(shopScreen);
            var panel = UIKit.Rect("ShopPanel", shopScreen).At(new Vector2(1, 0.5f), new Vector2(-150, -40), new Vector2(900, 880), new Vector2(1, 0.5f));
            var face = UIKit.Glass(panel, Sprites.RoundRect, 0.8f, 0.9f);
            var ft = face.transform;

            // header like the dialogs: icon badge + title + subtitle
            var badge = UIKit.Img(ft, Sprites.Circle, UIKit.WithAlpha(UIKit.Orange, 0.9f), "Badge");
            badge.rectTransform.At(new Vector2(0, 1), new Vector2(40, -26), new Vector2(84, 84), new Vector2(0, 1));
            UIKit.Img(badge.transform, Sprites.ThinRing, new Color(1, 1, 1, 0.6f), "Ring").rectTransform.Fill();
            UIKit.IconGraphic(badge.transform, UIKit.Icon.Bag, 44, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44), new Vector2(0.5f, 0.5f));
            UIKit.SoftLabel(ft, "SHOP", 50, UIKit.Cream, TextAnchor.LowerLeft, "Title").rectTransform.At(new Vector2(0, 1), new Vector2(148, -20), new Vector2(500, 60), new Vector2(0, 1));
            UIKit.Label(ft, "Colours & hats for your hero", 24, UIKit.WithAlpha(UIKit.Cream, 0.65f), TextAnchor.UpperLeft, false, "Subtitle")
                .rectTransform.At(new Vector2(0, 1), new Vector2(150, -82), new Vector2(500, 32), new Vector2(0, 1));
            var line = UIKit.Img(ft, null, new Color(1, 1, 1, 0.1f), "Divider");
            line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1);
            line.rectTransform.offsetMin = new Vector2(40, -127); line.rectTransform.offsetMax = new Vector2(-40, -125);

            shopTabs = UIKit.Rect("Tabs", ft).At(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(560, 76), new Vector2(0.5f, 1));
            shopGrid = UIKit.Rect("Grid", ft).At(new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(820, 610), new Vector2(0.5f, 1));
        }

        void RefreshShop()
        {
            foreach (Transform c in shopTabs) Destroy(c.gameObject);
            var colors = UIKit.ChipButton(shopTabs, UIKit.Icon.Star, "COLORS", shopHats ? UIKit.Gray : UIKit.Cyan, new Vector2(270, 76), () => { shopHats = false; RefreshShop(); }, 28, !shopHats);
            ((RectTransform)colors.transform).At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(270, 76), new Vector2(0, 0.5f));
            var hats = UIKit.ChipButton(shopTabs, UIKit.Icon.Trophy, "HATS", shopHats ? UIKit.Cyan : UIKit.Gray, new Vector2(270, 76), () => { shopHats = true; RefreshShop(); }, 28, shopHats);
            ((RectTransform)hats.transform).At(new Vector2(1, 0.5f), Vector2.zero, new Vector2(270, 76), new Vector2(1, 0.5f));

            foreach (Transform c in shopGrid) Destroy(c.gameObject);
            var items = shopHats ? Catalog.Hats : Catalog.Colors;
            for (int i = 0; i < items.Length; i++)
            {
                int cx = i % 3, cy = i / 3;
                ShopCard(items[i], new Vector2(-276 + cx * 276, -cy * 204), i);
            }
        }

        void ShopCard(ShopItem item, Vector2 pos, int index)
        {
            var d = Save.Data;
            bool owned = d.owned.Contains(item.id);
            bool equipped = item.isHat ? d.hat == item.id : d.color == item.id;
            bool affordable = d.gold >= item.price;

            var face = UIKit.GlassCard(shopGrid, new Vector2(256, 188), item.id, 0.9f);
            var holder = (RectTransform)face.transform.parent;
            holder.At(new Vector2(0.5f, 1), pos, new Vector2(256, 188), new Vector2(0.5f, 1));
            if (equipped) face.transform.Find("Edge").GetComponent<Image>().color = UIKit.WithAlpha(UIKit.Gold, 0.95f);
            var ct = face.transform;

            var sw = UIKit.Img(ct, Sprites.Circle, UIKit.Darken(item.color, 0.6f), "SwatchRim");
            sw.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(78, 78), new Vector2(0.5f, 1));
            UIKit.Img(sw.transform, Sprites.Circle, item.color, "Swatch").rectTransform.Fill(5, 6, 5, 4);
            UIKit.Img(sw.transform, Sprites.ThinRing, new Color(1, 1, 1, 0.5f), "Ring").rectTransform.Fill();
            if (item.isHat && item.id != "hat_none")
                UIKit.IconGraphic(sw.transform, UIKit.Icon.Trophy, 38, UIKit.Cream).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38), new Vector2(0.5f, 0.5f));
            UIKit.Fit(UIKit.Label(ct, item.name, 26, UIKit.Cream, TextAnchor.MiddleCenter, false), 14)
                .rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(230, 34), new Vector2(0.5f, 1));

            RectTransform chip;
            if (equipped) chip = UIKit.InfoChip(ct, UIKit.Icon.Star, "EQUIPPED", UIKit.Cream, new Vector2(196, 40), UIKit.WithAlpha(UIKit.Green, 0.95f));
            else if (owned) chip = UIKit.InfoChip(ct, null, "TAP TO WEAR", UIKit.WithAlpha(UIKit.Cream, 0.85f), new Vector2(196, 40));
            else chip = UIKit.InfoChip(ct, UIKit.Icon.Bag, item.price + " GOLD", affordable ? UIKit.Gold : new Color(1f, 0.55f, 0.5f), new Vector2(196, 40));
            chip.At(new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(196, 40), new Vector2(0.5f, 0));

            UIKit.Hoverable(face, equipped ? UIKit.Gold : UIKit.Cyan, () => OnShopItem(item, holder));
            Tween.PopIn(holder, 0.3f, index * 0.04f);
        }

        void OnShopItem(ShopItem item, RectTransform card)
        {
            var d = Save.Data;
            bool owned = d.owned.Contains(item.id);
            if (!owned)
            {
                if (d.gold < item.price)
                {
                    AudioFX.Play("error");
                    Shake(card);
                    Shake(goldPill);
                    return;
                }
                d.gold -= item.price;
                d.owned.Add(item.id);
                AudioFX.Play("buy");
                FX.Confetti(new Vector3(0, 0.5f, -0.5f));
            }
            else AudioFX.Play("click");
            if (item.isHat) d.hat = item.id; else d.color = item.id;
            Save.Persist();
            Cosmetics.Apply(playerAnim, GameAssets.I.hero, d.color, d.hat, false);
            playerAnim.SetTrigger(AnimParams.Win);
            FX.Burst(new Vector3(0, 1.2f, 0), UIKit.Gold, Color.white, 30, 2f, 5f, 0.35f, 0.8f, 0.3f, true);
            Tween.Punch(playerRoot, 0.12f, 0.35f);
            RefreshShop();
        }

        // ---------------------------------------------------------------- navigation

        void BackButton(Transform parent)
        {
            var b = WithAction(UIKit.ChipButton(parent, UIKit.Icon.Back, "BACK", UIKit.Gray, new Vector2(200, 80), null, 30), "back");
            ((RectTransform)b.transform).At(new Vector2(0, 1), new Vector2(32, -34), new Vector2(200, 80));
        }

        void Show(Screen s, bool instant = false)
        {
            current = s;
            if (topRight != null) foreach (var item in topRight) item.SetActive(s != Screen.Title);
            titleScreen.gameObject.SetActive(s == Screen.Title);
            levelScreen.gameObject.SetActive(s == Screen.Levels);
            shopScreen.gameObject.SetActive(s == Screen.Shop);
            if (s == Screen.Levels) BuildLevels();
            if (s == Screen.Shop) RefreshShop();
            var active = s == Screen.Title ? titleScreen : s == Screen.Levels ? levelScreen : shopScreen;
            var g = active.GetComponent<CanvasGroup>();
            if (!instant) { g.alpha = 0; Tween.Fade(g, 1f, 0.25f); AudioFX.Play("whoosh", 0.5f); }
            else g.alpha = 1;

            switch (s)
            {
                case Screen.Title: lookSideTarget = 0f; camDistTarget = 6.2f; break; // hero centre stage
                case Screen.Levels: lookSideTarget = 0f; camDistTarget = 8f; break;
                case Screen.Shop: lookSideTarget = 1f; camDistTarget = 3.8f; break;
            }
        }

        /// <summary>Called by MenuAction buttons placed in the scene ("play", "levels", "shop", "back", "settings").</summary>
        public void Run(string action)
        {
            switch (action)
            {
                case "play": StartLevel(Mathf.Clamp(Save.Data.unlocked - 1, 0, Levels.All.Length - 1)); break;
                case "levels": Show(Screen.Levels); break;
                case "shop": Show(Screen.Shop); break;
                case "back": Show(Screen.Title); break;
                case "settings": ShowSettings(); break;
                case "closeSettings": HideSettings(); break;
                default: Debug.LogWarning("[GoldRush] Unknown menu action: " + action); break;
            }
        }

        public void StartLevel(int index)
        {
            GameSession.LevelIndex = index;
            Fader.Load(GameSession.SceneName);
        }

        // smoke-test hooks
        public void DebugShowLevels() => Show(Screen.Levels);
        public void DebugShowShop() => Show(Screen.Shop);
        public void DebugShowTitle() => Show(Screen.Title);
        public void DebugShowSettings() => ShowSettings();
        public void DebugHideSettings() => HideSettings();

        static void Shake(RectTransform rt)
        {
            Vector2 basePos = rt.anchoredPosition;
            Tween.Run(rt, 0.35f, k => rt.anchoredPosition = basePos + Vector2.right * (Mathf.Sin(k * Mathf.PI * 8) * 14f * (1 - k)), Tween.Linear);
        }
    }
}
