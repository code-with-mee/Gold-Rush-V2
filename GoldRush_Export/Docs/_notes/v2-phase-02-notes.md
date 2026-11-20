# V2 Phase 2 notes: Level select, shop and settings

Guide: `Docs/phase-02.html` (15 steps). Sources: `_old_order/phase-09.html`, `GoldRushMenu.BuildLevelsScreen/BuildShop/
BuildSettings/BuildUI(top bar)`, `GameUI.ToggleButton/BindToggle`, baked `Scenes/MainMenu.unity` (all values copied).

## Scripts in phase 2
| Path (Assets/GoldRush/Scripts/…) | Typed / imported | Source |
|---|---|---|
| Core/Data.cs | typed ("replace the whole file") | **real file** (229 lines) – final from now on |
| UI/GoldRushMenu.cs | imported ("Replace") | **PHASE 2 VERSION** from `GoldRush_Export/Resources/Scripts/Phases/Phase02/UI/GoldRushMenu.cs` (**must be created**, full text below, 408 lines) |
| everything from phase 1 | unchanged | |

No new helper scripts. Compile check: the phase-2 GoldRushMenu only uses UIKit (Img, Label, Fit, GlassCard, InfoChip,
StarIcon, IconGraphic, ChipButton, Hoverable, FormatTime, ButtonLabel, colours), Sprites, Tween, AudioFX, Save,
Levels, GameSession, Catalog, ShopItem, AnimParams, EnvironmentSetup, DayNight, Fader – all present after phase 1 + full Data.

### Phase 2 GoldRushMenu = real file with these differences
- removed: `BuildForEditor`, `WithAction`, `BuildWorld`, `Monster`, `BuildUI`, `BuildTitle`, `BuildSettings`,
  `AddHeavyOutline`, `ScreenHeader`, `BuildLevelsScreen`, `BuildShop`, `BackButton`, the smoke-test hooks;
  Start no longer has `if (!cam) BuildWorld(); if (!titleScreen) BuildUI();`.
- `Cosmetics.Apply` (Start and OnShopItem), `FX.Confetti`, `FX.Burst` replaced by `// phase 10:` comments
  (CharacterFactory/FX need LevelBuilder, Mats, Props… which arrive later). OnShopItem guards playerAnim/playerRoot.
- `GameUI.BindToggle` → a local copy `BindToggle` (identical code; GameUI arrives in phase 9).
- `StartLevel` checks `Application.CanStreamedLevelBeLoaded(GameSession.SceneName)` and only logs
  "Scene 'Level1' is not in the build yet. You make Level1 in phase 3." → **phase 3 needs no code change**: as soon as
  Level1 is in Build Profiles → Scene List, PLAY / the Level 1 card load it (with the Fader). Daily card → scene "Game"
  → logs until phase 10.
- null-guards: `topRight` items, `Shake(goldPill)`.
- Same serialized fields as phase 1 and the final file.

## Code changes needed
1. **Create `GoldRush_Export/Resources/Scripts/Phases/Phase02/UI/GoldRushMenu.cs`** with the exact text at the end of
   this file (new export folder for phase versions that are too long to type). The guide (step 4) imports it.
2. Same ButtonFX change as phase 1 (code change 1 there) is needed for `ChipButton`, `CLOSEBtn`, `MUSIC/SOUND`.
3. (Phase 10 writer) The final GoldRushMenu works with this scene unchanged: all fields filled, so BuildWorld/BuildUI
   are skipped; `GameUI.BindToggle` finds `Glass/IconDisc` in the hand-made ChipButtons; Cosmetics needs
   `GoldRushAssets.hero` (filled in phase 1). The menu diorama has no DayNightTint/palette entries (phase 10 may add).

## Values / names (from the baked scene)
- MenuUI child order: Title, Levels, Shop, GoldPill, DayNight, Btn_Gear, Settings (Settings last, inactive at start –
  script sets it inactive anyway).
- Top-right bar: GoldPill (GlassPill 300×100, PPU 1.28, edge gold 140, TR (-40,-34), Coin 76 at (52,3), Label Bold 50,
  L106 R16), DayNight (100, (-362,-34), icons 56), Btn_Gear (100, (-484,-34), icon 46). topRight = [GoldPill, DayNight, Btn_Gear].
- ChipButton recipe (BACKBtn 200×80, PPU 1.32): Halo(-60,-45), Shadow/Tint(97)/Glass(20, raycast)/Edge(179),
  IconDisc (pad 15.2, 49.6, slate 217) / Icon_Back 29.76, Content L80 B6 R15.2 T6, Label Bold 30 best fit 16–30.
  Button transition None, target Glass; ButtonFX flat, base 616B85; CanvasGroup. Prefab `Prefabs/UI/ChipButton`
  (no MenuAction in the prefab; BACK instances add `back`).
- Levels: Dim (no sprite, navy 140, no raycast), Header "SELECT LEVEL" Bold 60 + Shadow 2.5, SubHeader
  "Collect all the gold, then run to the exit!" (the baked text; the old screenshot 02_levels.jpg shows an older
  subtitle and NIGHT chips that the current code does not make), BACKBtn, Cards (0,-50) 1612×440.
- Shop: BACKBtn, ShopPanel MR pivot (1,0.5) (-150,-40) 900×880 Glass rr PPU 0.8 Tint 230; Glass children Badge (orange
  230, 84) + Ring + Icon_Bag 44, Title Bold 50 LowerLeft (148,-20), Subtitle SemiBold 24 (150,-82), Divider, Tabs
  (0,-150) 560×76, Grid (0,-250) 820×610. Paths: `Shop/ShopPanel/Glass/Tabs|Grid`.
- Settings: Dim (raycast ON), Panel 600×440 (GlassPanel: Shadow 51, Face 0F142B·209 raycast, Frost 10, Shine 15,
  Edge 82, Accent cyan 217 180×4 PPU 32), Ribbon (0,36) 380×96 PPU 1.333 (Shadow 46, Tint 77, Face cyan 179, Edge 115,
  Label Bold 44 + Outline), `MUSIC  ONBtn` / `SOUND  ONBtn` (ChipButton green 260×76, PPU 1.389, (0,285)/(0,180),
  label SemiBold 28, no MenuAction), CLOSEBtn (GlassButton unpacked, no IconDisc/Chevron, slate, 240×86, PPU 1.228,
  EdgeGlow 989FAF ppu 1.105, Face alpha 128, Content L8 R8, Label 34 centre B6 T6, action closeSettings).
  Names with two spaces kept exactly like the baked scene.
- Test gold: guide step 13 tells students to set `public int gold = 500;` temporarily + Clear All PlayerPrefs.

## Decisions other phases rely on
- Data.cs is FINAL from phase 2 (Levels, GameSession, Catalog, Rng). Phase 5+ must not re-type or "phase-version" it.
- Phase 3: add `MainMenu` (index 0) and `Level1` to Build Profiles → Scene List; then PLAY works (no menu code change).
  There is no way back to the menu until phase 6 (Fader.Load(Scenes.Menu)).
- Phase 9 may reuse `Prefabs/UI/ChipButton` for the pause dialog MUSIC/SOUND toggles (GameUI.ToggleButton look).

## Screenshots (data-shot ids)
| id | window | select |
|---|---|---|
| phase-02/step-05 | inspector | GoldRushMenu/MenuUI/Levels |
| phase-02/step-06-a | hierarchy | GoldRushMenu/MenuUI/GoldPill |
| phase-02/step-06-b | inspector | GoldRushMenu/MenuUI/GoldPill |
| phase-02/step-07-a | hierarchy | GoldRushMenu/MenuUI/Levels/BACKBtn |
| phase-02/step-07-b | inspector | GoldRushMenu/MenuUI/Levels/BACKBtn |
| phase-02/step-08-a | hierarchy | GoldRushMenu/MenuUI/Levels |
| phase-02/step-08-b | inspector | GoldRushMenu/MenuUI/Levels/Cards |
| phase-02/step-09-a | hierarchy | GoldRushMenu/MenuUI/Shop/ShopPanel |
| phase-02/step-09-b | scene | GoldRushMenu/MenuUI/Shop/ShopPanel |
| phase-02/step-10-a | hierarchy | GoldRushMenu/MenuUI/Settings/Panel |
| phase-02/step-10-b | inspector | GoldRushMenu/MenuUI/Settings/Dim |
| phase-02/step-11-a | inspector | GoldRushMenu/MenuUI/Settings/Panel/Face/MUSIC  ONBtn (two spaces) |
| phase-02/step-11-b | game | GoldRushMenu/MenuUI/Settings (edit mode, Settings ticked) |
| phase-02/step-11-c | hierarchy | GoldRushMenu/MenuUI/Settings/Panel/Face/CLOSEBtn |
| phase-02/step-12 | inspector | GoldRushMenu |
| phase-02/step-13 | game | GoldRushMenu/MenuUI/Shop (play mode, 500 test gold, Sunset bought) |
| phase-02/step-14-a | game | GoldRushMenu/MenuUI/Levels (play mode) |
| phase-02/step-14-b | game | GoldRushMenu/MenuUI/Settings (play mode, MUSIC OFF) |
Reference images (real): `images/02_levels.jpg`, `images/03_shop.jpg` (step 1).

## Full text: `Resources/Scripts/Phases/Phase02/UI/GoldRushMenu.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// Main menu scene root: 3D diorama + Title / Level Select / Shop / Settings screens.
    /// PHASE 2 VERSION (course): the stage and the screens are GameObjects you built by hand in MainMenu.unity.
    /// This script only connects them: buttons with a <see cref="MenuAction"/> call <see cref="Run"/>, and the
    /// data (gold, stars, level cards, shop items) is filled in when the scene starts.
    /// Not in this version yet (they need scripts from later phases): building the menu from code,
    /// hero colour/hat (Cosmetics), shop confetti (FX). The final file arrives in phase 10.
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
            EnvironmentSetup.Apply(cam, true, true); // the menu uses the spooky night mood
            if (lantern) DayNight.RegisterNightLight(lantern);
            foreach (var g in menuGlows) DayNight.RegisterNightLight(g);
            // phase 10: Cosmetics.Apply(...) puts the shop colour and hat on the hero
            RefreshTitle();
            BindSettings();
            settingsRoot.gameObject.SetActive(false);
            UpdateCamera(1f);
            Show(Screen.Title, true);
            PlayEntrance();
            Fader.FadeIn();
        }

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

        // ---------------------------------------------------------------- settings dialog

        void BindSettings()
        {
            BindToggle(musicToggle, "MUSIC", () => Save.Data.music, v => { Save.Data.music = v; Save.Persist(); AudioFX.RefreshMute(); });
            BindToggle(soundToggle, "SOUND", () => Save.Data.sfx, v => { Save.Data.sfx = v; Save.Persist(); AudioFX.RefreshMute(); });
        }

        /// <summary>Connects a toggle button to a setting: click flips it, the label shows ON / OFF.
        /// (Same as GameUI.BindToggle, which you get in phase 9.)</summary>
        static void BindToggle(Button b, string label, System.Func<bool> get, System.Action<bool> set)
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

        // ---------------------------------------------------------------- level select

        const float CardW = 300f, CardH = 440f, CardGap = 28f;

        /// <summary>One card per level (+ the daily challenge), made from the level data and the save.</summary>
        void BuildLevels()
        {
            foreach (Transform c in levelCards) Destroy(c.gameObject);
            var row = levelCards;
            var d = Save.Data;
            float x0 = -2f * (CardW + CardGap);
            for (int i = 0; i < Levels.All.Length; i++) LevelCard(row, i, new Vector2(x0 + i * (CardW + CardGap), 0), d);
            DailyCard(row, new Vector2(x0 + 4 * (CardW + CardGap), 0), d);
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
                    if (goldPill) Shake(goldPill);
                    return;
                }
                d.gold -= item.price;
                d.owned.Add(item.id);
                AudioFX.Play("buy");
                // phase 10: FX.Confetti(...) throws confetti
            }
            else AudioFX.Play("click");
            if (item.isHat) d.hat = item.id; else d.color = item.id;
            Save.Persist();
            // phase 10: Cosmetics.Apply(...) shows the new colour / hat on the hero, FX.Burst(...) adds sparkles
            if (playerAnim) playerAnim.SetTrigger(AnimParams.Win);
            if (playerRoot) Tween.Punch(playerRoot, 0.12f, 0.35f);
            RefreshShop();
        }

        // ---------------------------------------------------------------- navigation

        void Show(Screen s, bool instant = false)
        {
            current = s;
            if (topRight != null) foreach (var item in topRight) if (item) item.SetActive(s != Screen.Title);
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
            // course: a level can only start when its scene is in File > Build Profiles > Scene List (phase 3)
            if (!Application.CanStreamedLevelBeLoaded(GameSession.SceneName))
            {
                Debug.Log($"[GoldRush] Scene '{GameSession.SceneName}' is not in the build yet. You make Level1 in phase 3.");
                return;
            }
            Fader.Load(GameSession.SceneName);
        }

        static void Shake(RectTransform rt)
        {
            Vector2 basePos = rt.anchoredPosition;
            Tween.Run(rt, 0.35f, k => rt.anchoredPosition = basePos + Vector2.right * (Mathf.Sin(k * Mathf.PI * 8) * 14f * (1 - k)), Tween.Linear);
        }
    }
}
```
