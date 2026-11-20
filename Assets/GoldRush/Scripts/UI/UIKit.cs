using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Procedurally generated (SDF, anti-aliased) UI sprites and textures.</summary>
    public static class Sprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        // signed distance helpers (in "units"; negative inside)
        static float SdRoundBox(float px, float py, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px) - hx + r, qy = Mathf.Abs(py) - hy + r;
            float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float SdStar5(float px, float py, float r, float rf)
        {
            const float k1x = 0.809016994375f, k1y = -0.587785252292f;
            const float k2x = -k1x, k2y = k1y;
            px = Mathf.Abs(px);
            float d1 = Mathf.Max(k1x * px + k1y * py, 0) * 2; px -= d1 * k1x; py -= d1 * k1y;
            float d2 = Mathf.Max(k2x * px + k2y * py, 0) * 2; px -= d2 * k2x; py -= d2 * k2y;
            px = Mathf.Abs(px);
            py -= r;
            float bax = rf * -k1y - 0, bay = rf * k1x - 1;
            float h = Mathf.Clamp((px * bax + py * bay) / (bax * bax + bay * bay), 0, r);
            float dx = px - bax * h, dy = py - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) * Mathf.Sign(py * bax - px * bay);
        }

        static float SdHeart(float px, float py)
        {
            px = Mathf.Abs(px);
            if (py + px > 1f)
            {
                float dx = px - 0.25f, dy = py - 0.75f;
                return Mathf.Sqrt(dx * dx + dy * dy) - Mathf.Sqrt(2f) / 4f;
            }
            float ax = px, ay = py - 1f;
            float m = 0.5f * Mathf.Max(px + py, 0);
            float bx = px - m, by = py - m;
            return Mathf.Sqrt(Mathf.Min(ax * ax + ay * ay, bx * bx + by * by)) * Mathf.Sign(px - py);
        }

        static float SdTriangle(float px, float py, float qx, float qy)
        {
            px = Mathf.Abs(px);
            float t = Mathf.Clamp01((px * qx + py * qy) / (qx * qx + qy * qy));
            float ax = px - qx * t, ay = py - qy * t;
            float bx = px - qx * Mathf.Clamp01(px / qx), by = py - qy;
            float s = -Mathf.Sign(qy);
            float dax = ax * ax + ay * ay, day = s * (px * qy - py * qx);
            float dbx = bx * bx + by * by, dby = s * (py - qy);
            float dx = Mathf.Min(dax, dbx), dy = Mathf.Min(day, dby);
            return -Mathf.Sqrt(dx) * Mathf.Sign(dy);
        }

        /// <summary>Builds a white sprite whose alpha comes from sdf(px,py) in pixel units (negative = inside).</summary>
        static Sprite Make(string key, int n, Func<float, float, float> sdfPixels, float border = 0, bool soft = false)
        {
            if (cache.TryGetValue(key, out var s) && s) return s;
            // the editor saves every shape as a PNG in Resources/UISprites (so scenes can reference real assets)
            s = Resources.Load<Sprite>("UISprites/" + key);
            if (s) { cache[key] = s; return s; }
            s = Generate(key, n, sdfPixels, border, soft);
            cache[key] = s;
            return s;
        }

        /// <summary>Editor: forget cached sprites (after they were saved as PNG files).</summary>
        public static void ClearCache() => cache.Clear();

        /// <summary>Draws the sprite into a new texture (used when no saved PNG exists).</summary>
        public static Sprite Generate(string key, int n, Func<float, float, float> sdfPixels, float border, bool soft)
        {
            Sprite s;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = sdfPixels(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f);
                    float a = soft ? d : Mathf.Clamp01(0.5f - d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            s.name = key;
            return s;
        }

        public static Sprite RoundRect => Make("rr", 96, (x, y) => SdRoundBox(x, y, 47, 47, 30), 34);
        public static Sprite Pill => Make("pill", 128, (x, y) => SdRoundBox(x, y, 63, 63, 62), 62);
        public static Sprite Circle => Make("circle", 128, (x, y) => Mathf.Sqrt(x * x + y * y) - 62);
        public static Sprite Ring => Make("ring", 128, (x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 54) - 8);
        public static Sprite Square => Make("square", 8, (x, y) => -10);
        public static Sprite Star => Make("star", 128, (x, y) => SdStar5(x / 60f, y / 60f + 0.08f, 1f, 0.45f) * 60f);
        public static Sprite Heart => Make("heart", 128, (x, y) => SdHeart(x / 92f, y / 92f + 0.55f) * 92f);
        public static Sprite Triangle => Make("tri", 128, (x, y) => SdTriangle(x / 60f, (52 - y) / 60f, 0.85f, 1.6f) * 60f);
        public static Sprite Soft => Make("soft", 64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y) / 31f;
            float a = Mathf.Clamp01(1 - r);
            return a * a;
        }, 0, true);

        /// <summary>Horizontal fade: opaque on the left, transparent on the right (for text backdrops).</summary>
        public static Sprite FadeRight => Make("fadeR", 64, (x, y) =>
        {
            float u = (x + 32f) / 64f;
            return Mathf.SmoothStep(1f, 0f, u);
        }, 0, true);

        /// <summary>Vertical fade: opaque at the bottom, transparent at the top.</summary>
        public static Sprite FadeUp => Make("fadeU", 64, (x, y) =>
        {
            float v = (y + 32f) / 64f;
            return Mathf.SmoothStep(1f, 0f, v);
        }, 0, true);

        /// <summary>Vertical fade: opaque at the top, transparent at the bottom (glass shine).</summary>
        public static Sprite FadeDown => Make("fadeD", 64, (x, y) =>
        {
            float v = (y + 32f) / 64f;
            return Mathf.SmoothStep(0f, 1f, v);
        }, 0, true);

        /// <summary>Rounded-rect outline (9-sliced) for thin borders.</summary>
        public static Sprite RoundOutline => Make("rro2", 96, (x, y) => Mathf.Abs(SdRoundBox(x, y, 46, 46, 29)) - 1.4f, 34);
        /// <summary>Outline with exactly the Pill shape (same size/border), so borders hug pill fills.</summary>
        public static Sprite PillOutline => Make("pillo", 128, (x, y) => Mathf.Abs(SdRoundBox(x, y, 62, 62, 61)) - 1.6f, 62);
        /// <summary>Thin ring matching the Circle sprite.</summary>
        public static Sprite ThinRing => Make("ring2", 128, (x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 61f) - 1.6f);

        /// <summary>The outline sprite that matches a fill sprite's shape.</summary>
        public static Sprite OutlineFor(Sprite fill) => fill == Pill ? PillOutline : fill == Circle ? ThinRing : RoundOutline;

        public static Texture2D SoftTex => Soft.texture;
        public static Texture2D StarTex => Star.texture;
        public static Texture2D SquareTex => Square.texture;
    }

    /// <summary>Runtime UI construction helpers + palette.</summary>
    public static class UIKit
    {
        // ---- theme: clean "grey-box" sci-fi — navy/slate panels, white text, cyan accents
        public static readonly Color Cream = new Color(0.95f, 0.97f, 1f);          // main text (near white)
        public static readonly Color Dark = new Color(0.06f, 0.08f, 0.16f);        // dark text / outlines
        public static readonly Color PanelBg = new Color(0.06f, 0.08f, 0.17f, 0.96f);
        public static readonly Color PanelLight = new Color(0.12f, 0.15f, 0.27f, 1f);
        public static readonly Color HudBg = new Color(0.07f, 0.1f, 0.2f, 0.78f);  // HUD pills / round buttons
        public static readonly Color Navy = new Color(0.03f, 0.05f, 0.12f);        // dims, fades
        public static readonly Color Cyan = new Color(0.2f, 0.78f, 0.9f);          // primary accent
        public static readonly Color Gold = new Color(1f, 0.76f, 0.2f);
        public static readonly Color Orange = new Color(1f, 0.56f, 0.2f);
        public static readonly Color Green = new Color(0.26f, 0.8f, 0.5f);
        public static readonly Color Red = new Color(0.93f, 0.3f, 0.34f);
        public static readonly Color Blue = new Color(0.25f, 0.55f, 0.95f);
        public static readonly Color Purple = new Color(0.56f, 0.42f, 0.95f);
        public static readonly Color Gray = new Color(0.38f, 0.42f, 0.52f);         // slate
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.28f);

        static Font font, fontBold, fontHeavy;

        static Font LoadFont(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Body font: Poppins SemiBold (Google Fonts, SIL Open Font License).</summary>
        public static Font Font => font ? font : (font = LoadFont("Poppins-SemiBold"));
        /// <summary>Headings / buttons: Poppins Bold.</summary>
        public static Font FontBold => fontBold ? fontBold : (fontBold = LoadFont("Poppins-Bold"));
        /// <summary>Logo / big numbers: Poppins ExtraBold.</summary>
        public static Font FontHeavy => fontHeavy ? fontHeavy : (fontHeavy = LoadFont("Poppins-ExtraBold"));

        /// <summary>Picks the weight by size: small text SemiBold, medium Bold, big ExtraBold.</summary>
        public static Font FontFor(int size) => size >= 90 ? FontHeavy : size >= 30 ? FontBold : Font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { font = null; fontBold = null; fontHeavy = null; }

        public static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null);
        }

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor at a single point. Pivot defaults to the anchor.</summary>
        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        /// <summary>Stretch to parent with insets (left, bottom, right, top).</summary>
        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Img(Transform parent, Sprite sprite, Color color, string name = "Img", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            bool outline = true, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = FontFor(size);
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Normal; // real weights from the font files (no fake bold)
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.02f, 0.04f, 0.12f, 0.45f);
                o.effectDistance = new Vector2(Mathf.Max(1f, size / 44f), -Mathf.Max(1f, size / 44f));
            }
            return t;
        }

        /// <summary>Rounded panel with a soft drop shadow. Returns the panel image.</summary>
        public static Image Panel(Transform parent, Vector2 size, Color color, string name = "Panel")
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var sh = Img(root, Sprites.RoundRect, Shadow, "Shadow");
            sh.rectTransform.Fill(-4, -14, -4, 6);
            var face = Img(root, Sprites.RoundRect, color, "Face", true);
            face.rectTransform.Fill();
            var rim = Img(face.transform, Sprites.RoundRect, new Color(1, 1, 1, 0.035f), "Rim");
            rim.rectTransform.Fill(10, 10, 10, 10);
            var accent = Img(face.transform, Sprites.Pill, WithAlpha(Cyan, 0.85f), "Accent");
            accent.rectTransform.anchorMin = new Vector2(0, 1); accent.rectTransform.anchorMax = new Vector2(1, 1);
            accent.rectTransform.pivot = new Vector2(0.5f, 1);
            accent.rectTransform.offsetMin = new Vector2(40, -14); accent.rectTransform.offsetMax = new Vector2(-40, -8);
            accent.pixelsPerUnitMultiplier = 128f / 6f;
            return face;
        }

        /// <summary>Chunky 3D-looking button (face over darker lip) with press animation and click sound.</summary>
        public static Button Button(Transform parent, string label, Color color, Vector2 size, Action onClick, int fontSize = 46)
        {
            var root = Rect(label + "Btn", parent);
            root.sizeDelta = size;
            var shadow = Img(root, Sprites.RoundRect, Shadow, "Shadow");
            shadow.rectTransform.Fill(2, -8, 2, 8);
            var lip = Img(root, Sprites.RoundRect, Darken(color, 0.62f), "Lip");
            lip.rectTransform.Fill();
            var face = Img(root, Sprites.RoundRect, color, "Face", true);
            face.rectTransform.Fill(0, 10, 0, 0);
            var gloss = Img(face.transform, Sprites.RoundRect, new Color(1, 1, 1, 0.16f), "Gloss");
            gloss.rectTransform.anchorMin = new Vector2(0, 0.52f);
            gloss.rectTransform.anchorMax = new Vector2(1, 1);
            gloss.rectTransform.offsetMin = new Vector2(8, 0);
            gloss.rectTransform.offsetMax = new Vector2(-8, -6);
            gloss.pixelsPerUnitMultiplier = 1.6f;
            var text = Label(face.transform, label, fontSize, Cream);
            text.rectTransform.Fill(8, 0, 8, 0);

            var b = root.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.transition = Selectable.Transition.None;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(() =>
            {
                AudioFX.Play("click");
                onClick();
            });
            var fx = root.gameObject.AddComponent<ButtonFX>();
            fx.face = face.rectTransform;
            fx.baseColor = color;
            return b;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            var fx = b.GetComponent<ButtonFX>();
            if (fx) fx.SetColor(c);
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        public enum Icon { Play, Levels, Calendar, Bag, Gear, Back, Star, Close, Home, Retry, Pause, Info, Clock, Heart, Lock, Music, Sound, Sun, Moon, Trophy, Chevron }

        static readonly Dictionary<Icon, string> IconFiles = new Dictionary<Icon, string>
        {
            [Icon.Play] = "ic_play_arrow", [Icon.Levels] = "ic_apps", [Icon.Calendar] = "ic_calendar_today", [Icon.Bag] = "ic_shopping_bag",
            [Icon.Gear] = "ic_settings", [Icon.Back] = "ic_arrow_back", [Icon.Star] = "ic_star", [Icon.Close] = "ic_close",
            [Icon.Home] = "ic_home", [Icon.Retry] = "ic_replay", [Icon.Pause] = "ic_pause", [Icon.Info] = "ic_info",
            [Icon.Clock] = "ic_schedule", [Icon.Heart] = "ic_favorite", [Icon.Lock] = "ic_lock", [Icon.Music] = "ic_music_note",
            [Icon.Sound] = "ic_volume_up", [Icon.Sun] = "ic_light_mode", [Icon.Moon] = "ic_dark_mode", [Icon.Trophy] = "ic_emoji_events",
            [Icon.Chevron] = "ic_chevron_right",
        };
        static readonly Dictionary<Icon, Sprite> iconCache = new Dictionary<Icon, Sprite>();

        /// <summary>Material Icons (Apache 2.0) sprite from Resources/Icons, or null if missing.</summary>
        public static Sprite IconSprite(Icon icon)
        {
            if (iconCache.TryGetValue(icon, out var sp) && sp) return sp;
            sp = IconFiles.TryGetValue(icon, out var file) ? Resources.Load<Sprite>("Icons/" + file) : null;
            iconCache[icon] = sp;
            return sp;
        }

        /// <summary>Icon image (Material Icons). Falls back to simple drawn shapes if the sprite is missing.</summary>
        public static RectTransform IconGraphic(Transform parent, Icon icon, float size, Color color, Color? hole = null)
        {
            var root = Rect("Icon_" + icon, parent);
            root.sizeDelta = new Vector2(size, size);
            var sprite = IconSprite(icon);
            if (sprite)
            {
                var im = Img(root, sprite, color, "Glyph");
                im.preserveAspect = true;
                im.rectTransform.Fill();
                return root;
            }
            Color cut = hole ?? Dark;
            Image Part(Sprite sp, Vector2 pos, Vector2 sz, Color c, float rot = 0f, string n = "Part")
            {
                var im = Img(root, sp, c, n);
                im.rectTransform.At(new Vector2(0.5f, 0.5f), pos * size, sz * size, new Vector2(0.5f, 0.5f));
                im.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                if (sp == Sprites.RoundRect) im.pixelsPerUnitMultiplier = 96f / Mathf.Max(8f, Mathf.Min(sz.x, sz.y) * size * 0.9f);
                return im;
            }
            switch (icon)
            {
                case Icon.Play:
                    Part(Sprites.Triangle, new Vector2(0.06f, 0f), new Vector2(0.8f, 0.8f), color, -90f);
                    break;
                case Icon.Levels:
                    for (int i = 0; i < 4; i++)
                        Part(Sprites.RoundRect, new Vector2((i % 2) * 0.46f - 0.23f, (i / 2) * 0.46f - 0.23f), new Vector2(0.38f, 0.38f), color);
                    break;
                case Icon.Calendar:
                    Part(Sprites.RoundRect, new Vector2(0f, -0.06f), new Vector2(0.84f, 0.74f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, 0.2f), new Vector2(0.84f, 0.2f), Darken(color, 0.75f));
                    Part(Sprites.RoundRect, new Vector2(-0.22f, 0.36f), new Vector2(0.1f, 0.22f), color);
                    Part(Sprites.RoundRect, new Vector2(0.22f, 0.36f), new Vector2(0.1f, 0.22f), color);
                    Part(Sprites.Star, new Vector2(0f, -0.14f), new Vector2(0.36f, 0.36f), cut);
                    break;
                case Icon.Bag:
                    Part(Sprites.Ring, new Vector2(0f, 0.18f), new Vector2(0.48f, 0.5f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, -0.12f), new Vector2(0.82f, 0.62f), color);
                    Part(Sprites.Circle, new Vector2(0f, -0.1f), new Vector2(0.16f, 0.16f), cut);
                    break;
                case Icon.Gear:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f * Mathf.Deg2Rad;
                        Part(Sprites.RoundRect, new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 0.36f, new Vector2(0.2f, 0.22f), color, -i * 45f);
                    }
                    Part(Sprites.Circle, Vector2.zero, new Vector2(0.72f, 0.72f), color);
                    Part(Sprites.Circle, Vector2.zero, new Vector2(0.3f, 0.3f), cut);
                    break;
                case Icon.Back:
                    Part(Sprites.Triangle, new Vector2(-0.16f, 0f), new Vector2(0.6f, 0.6f), color, 90f);
                    Part(Sprites.RoundRect, new Vector2(0.14f, 0f), new Vector2(0.5f, 0.22f), color);
                    break;
                case Icon.Star:
                    Part(Sprites.Star, Vector2.zero, new Vector2(1f, 1f), color);
                    break;
                case Icon.Home:
                    Part(Sprites.Triangle, new Vector2(0f, 0.2f), new Vector2(0.9f, 0.62f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, -0.18f), new Vector2(0.62f, 0.5f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, -0.26f), new Vector2(0.18f, 0.3f), cut);
                    break;
                case Icon.Retry:
                    Part(Sprites.Ring, Vector2.zero, new Vector2(0.86f, 0.86f), color);
                    Part(Sprites.RoundRect, new Vector2(0.3f, 0.3f), new Vector2(0.34f, 0.34f), cut, 45f);   // gap in the ring
                    Part(Sprites.Triangle, new Vector2(0.36f, 0.14f), new Vector2(0.38f, 0.34f), color, 200f); // arrow head
                    break;
                case Icon.Pause:
                    Part(Sprites.RoundRect, new Vector2(-0.17f, 0f), new Vector2(0.22f, 0.72f), color);
                    Part(Sprites.RoundRect, new Vector2(0.17f, 0f), new Vector2(0.22f, 0.72f), color);
                    break;
                case Icon.Info:
                    Part(Sprites.Circle, new Vector2(0f, 0.3f), new Vector2(0.18f, 0.18f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, -0.12f), new Vector2(0.18f, 0.52f), color);
                    break;
                case Icon.Clock:
                    Part(Sprites.Ring, Vector2.zero, new Vector2(0.9f, 0.9f), color);
                    Part(Sprites.RoundRect, new Vector2(0f, 0.12f), new Vector2(0.1f, 0.3f), color);
                    Part(Sprites.RoundRect, new Vector2(0.1f, 0f), new Vector2(0.26f, 0.1f), color);
                    break;
                case Icon.Heart:
                    Part(Sprites.Heart, Vector2.zero, new Vector2(1f, 1f), color);
                    break;
                case Icon.Close:
                    Part(Sprites.RoundRect, Vector2.zero, new Vector2(0.86f, 0.18f), color, 45f);
                    Part(Sprites.RoundRect, Vector2.zero, new Vector2(0.86f, 0.18f), color, -45f);
                    break;
            }
            return root;
        }

        /// <summary>
        /// Gives a HUD element "thickness": drop shadow + darker lip underneath + raised face + top gloss
        /// + thin cyan rim. Call it first on an empty RectTransform; returns the face image.
        /// </summary>
        public static Image Chip3D(RectTransform root, Sprite sprite, Color color, float ppu = 1f, float depth = 7f)
        {
            var sh = Img(root, sprite, new Color(0, 0, 0, 0.3f), "Shadow"); sh.rectTransform.Fill(2, -depth - 6, 2, depth + 2);
            var lip = Img(root, sprite, new Color(color.r * 0.45f, color.g * 0.5f, color.b * 0.7f, Mathf.Max(0.9f, color.a)), "Lip"); lip.rectTransform.Fill();
            var face = Img(root, sprite, color, "Face", true); face.rectTransform.Fill(0, depth, 0, 0);
            var gloss = Img(face.transform, sprite, new Color(1, 1, 1, 0.09f), "Gloss");
            gloss.rectTransform.anchorMin = new Vector2(0, 0.5f); gloss.rectTransform.anchorMax = Vector2.one;
            gloss.rectTransform.offsetMin = new Vector2(6, 0); gloss.rectTransform.offsetMax = new Vector2(-6, -4);
            var rim = Img(face.transform, sprite == Sprites.Circle ? Sprites.Ring : Sprites.RoundOutline, WithAlpha(Cyan, 0.28f), "Rim");
            rim.rectTransform.Fill(sprite == Sprites.Circle ? 2 : 0, sprite == Sprites.Circle ? 2 : 0, sprite == Sprites.Circle ? 2 : 0, sprite == Sprites.Circle ? 2 : 0);
            foreach (var im in new[] { sh, lip, face, gloss })
                if (im.type == Image.Type.Sliced) im.pixelsPerUnitMultiplier = ppu;
            if (rim.type == Image.Type.Sliced) rim.pixelsPerUnitMultiplier = 0.7f;
            return face;
        }

        /// <summary>
        /// Frosted-glass HUD background: a light see-through face with a thin bright edge and a soft shine.
        /// Keeps the game world visible behind it. Call first on an empty RectTransform; returns the face.
        /// </summary>
        public static Image Glass(RectTransform root, Sprite sprite, float ppu = 1f, float tintAlpha = 0.38f)
        {
            // THE game UI style: neutral frosted glass with a bright white edge (used by the HUD, dialogs, chips)
            var sh = Img(root, sprite, new Color(0, 0, 0, 0.14f), "Shadow"); sh.rectTransform.Fill(0, -4, 0, 2);
            var tint = Img(root, sprite, WithAlpha(Navy, tintAlpha), "Tint"); tint.rectTransform.Fill();      // contrast for text
            var face = Img(root, sprite, new Color(1, 1, 1, 0.08f), "Glass", true); face.rectTransform.Fill(); // frost
            var edge = Img(face.transform, Sprites.OutlineFor(sprite), new Color(1, 1, 1, 0.7f), "Edge"); edge.rectTransform.Fill();
            foreach (var im in new[] { sh, tint, face, edge })
                if (im.type == Image.Type.Sliced) im.pixelsPerUnitMultiplier = ppu; // same ppu = corners line up exactly
            return face;
        }

        /// <summary>
        /// Button in the game-UI glass style (same as the HUD chips): neutral glass + white edge,
        /// a coloured round icon, white text. Hover: grows, coloured halo, brighter edge, icon pop.
        /// </summary>
        public static Button ChipButton(Transform parent, Icon icon, string label, Color accent, Vector2 size, Action onClick,
            int fontSize = 30, bool primary = false)
        {
            var root = Rect(label + "Btn", parent);
            root.sizeDelta = size;
            float ppu = 96f / Mathf.Min(size.y, 140f) * 1.1f;
            var halo = Img(root, Sprites.Soft, WithAlpha(accent, 0f), "Halo"); halo.rectTransform.Fill(-60, -45, -60, -45);
            var face = Glass(root, Sprites.RoundRect, ppu, primary ? 0.45f : 0.38f);
            var edge = face.transform.Find("Edge").GetComponent<Image>();
            if (primary) edge.color = WithAlpha(Color.Lerp(accent, Color.white, 0.45f), 0.95f);
            float iconBox = size.y * 0.62f, pad = (size.y - iconBox) * 0.5f;
            var disc = Img(face.transform, Sprites.Circle, WithAlpha(accent, 0.85f), "IconDisc");
            disc.rectTransform.At(new Vector2(0, 0.5f), new Vector2(pad, 0), new Vector2(iconBox, iconBox), new Vector2(0, 0.5f));
            float glyph = iconBox * 0.6f;
            IconGraphic(disc.transform, icon, glyph, Cream, accent)
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(glyph, glyph), new Vector2(0.5f, 0.5f));
            var content = Rect("Content", face.transform); content.Fill(pad * 2 + iconBox, 6, pad, 6);
            Fit(SoftLabel(content, label, fontSize, Cream, TextAnchor.MiddleLeft), 16).rectTransform.Fill();
            var b = GlassBehaviour(root, face, accent, onClick);
            var fx = b.GetComponent<ButtonFX>();
            fx.SetupHover(halo, edge, null, disc, null, content, null);
            return b;
        }

        // ------------------------------------------------------------------ flat glass widgets
        // Same signatures as Panel / Ribbon / Button so screens switch style with a rename.
        // Every outline uses the SAME sprite shape and pixelsPerUnitMultiplier as its fill.

        /// <summary>Frosted glass panel: dark see-through body, faint frost, thin bright edge, slim cyan top line.</summary>
        public static Image GlassPanel(Transform parent, Vector2 size, Color tint, string name = "Panel")
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var sh = Img(root, Sprites.RoundRect, new Color(0, 0, 0, 0.2f), "Shadow"); sh.rectTransform.Fill(-2, -10, -2, 2);
            var body = Img(root, Sprites.RoundRect, new Color(tint.r, tint.g, tint.b, Mathf.Min(tint.a, 0.82f)), "Face", true); body.rectTransform.Fill();
            var frost = Img(body.transform, Sprites.RoundRect, new Color(1, 1, 1, 0.04f), "Frost"); frost.rectTransform.Fill();
            var shine = Img(body.transform, Sprites.FadeDown, new Color(1, 1, 1, 0.06f), "Shine"); // soft fade, no hard edge
            shine.rectTransform.anchorMin = new Vector2(0, 0.6f); shine.rectTransform.anchorMax = Vector2.one;
            shine.rectTransform.offsetMin = new Vector2(24, 0); shine.rectTransform.offsetMax = new Vector2(-24, -12);
            var edge = Img(body.transform, Sprites.RoundOutline, new Color(1, 1, 1, 0.32f), "Edge"); edge.rectTransform.Fill();
            var accent = Img(body.transform, Sprites.Pill, WithAlpha(Cyan, 0.85f), "Accent");
            accent.rectTransform.anchorMin = new Vector2(0.5f, 1); accent.rectTransform.anchorMax = new Vector2(0.5f, 1);
            accent.rectTransform.pivot = new Vector2(0.5f, 1);
            accent.rectTransform.sizeDelta = new Vector2(Mathf.Min(220f, size.x * 0.3f), 4f);
            accent.rectTransform.anchoredPosition = new Vector2(0, -8);
            accent.pixelsPerUnitMultiplier = 128f / 4f;
            return body;
        }

        /// <summary>Glass title pill. Returns the label, like Ribbon() (label → face → root).</summary>
        public static Text GlassRibbon(Transform parent, string text, Color color, Vector2 size, int fontSize = 64)
        {
            var root = Rect("Ribbon", parent);
            root.sizeDelta = size;
            float ppu = 128f / size.y;
            var sh = Img(root, Sprites.Pill, new Color(0, 0, 0, 0.18f), "Shadow"); sh.rectTransform.Fill(0, -5, 0, 2);
            var back = Img(root, Sprites.Pill, WithAlpha(Navy, 0.3f), "Tint"); back.rectTransform.Fill();
            var face = Img(root, Sprites.Pill, WithAlpha(color, 0.7f), "Face"); face.rectTransform.Fill();
            var edge = Img(face.transform, Sprites.PillOutline, new Color(1, 1, 1, 0.45f), "Edge"); edge.rectTransform.Fill();
            foreach (var im in new[] { sh, back, face, edge }) im.pixelsPerUnitMultiplier = ppu;
            var t = Label(face.transform, text, Mathf.RoundToInt(fontSize * 0.92f), Cream);
            t.rectTransform.Fill();
            return t;
        }

        /// <summary>Glass button with centred text (same signature as Button()).</summary>
        public static Button GlassTextButton(Transform parent, string label, Color color, Vector2 size, Action onClick, int fontSize = 46)
        {
            var root = Rect(label + "Btn", parent);
            root.sizeDelta = size;
            var face = GlassFace(root, color, size, 0.5f);
            var content = Rect("Content", face.transform); content.Fill(8, 0, 8, 0);
            var text = Fit(SoftLabel(content, label, Mathf.RoundToInt(fontSize * 0.9f), Cream, TextAnchor.MiddleCenter), 16);
            text.rectTransform.Fill(0, 6, 0, 6);
            return GlassBehaviour(root, face, color, onClick);
        }

        /// <summary>Shared look of glass buttons: navy tint + coloured see-through face + matching thin edge.</summary>
        static Image GlassFace(RectTransform root, Color color, Vector2 size, float tintAlpha)
        {
            float ppu = 96f / Mathf.Min(size.y, 140f) * 1.1f;
            // halo behind the button (invisible until hovered — see ButtonFX)
            var halo = Img(root, Sprites.Soft, WithAlpha(color, 0f), "Halo"); halo.rectTransform.Fill(-80, -60, -80, -60);
            var sh = Img(root, Sprites.RoundRect, new Color(0, 0, 0, 0.22f), "Shadow"); sh.rectTransform.Fill(2, -8, 2, 3);
            var outer = Img(root, Sprites.RoundOutline, WithAlpha(Color.Lerp(color, Color.white, 0.35f), 0.5f), "EdgeGlow"); outer.rectTransform.Fill(-3, -3, -3, -3);
            var back = Img(root, Sprites.RoundRect, WithAlpha(Navy, 0.35f), "Tint"); back.rectTransform.Fill();
            var face = Img(root, Sprites.RoundRect, WithAlpha(color, tintAlpha), "Face", true); face.rectTransform.Fill();
            var mask = face.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true; // clip children to the rounded shape
            // inner gradient: lighter at the top, deeper at the bottom (gives the glass volume)
            var hi = Img(face.transform, Sprites.FadeDown, new Color(1, 1, 1, 0.26f), "Gradient");
            hi.rectTransform.anchorMin = new Vector2(0, 0.35f); hi.rectTransform.anchorMax = Vector2.one; hi.rectTransform.offsetMin = hi.rectTransform.offsetMax = Vector2.zero;
            var lo = Img(face.transform, Sprites.FadeUp, WithAlpha(Navy, 0.3f), "Depth");
            lo.rectTransform.anchorMin = Vector2.zero; lo.rectTransform.anchorMax = new Vector2(1, 0.55f); lo.rectTransform.offsetMin = lo.rectTransform.offsetMax = Vector2.zero;
            // thin bright line along the top edge
            var line = Img(face.transform, Sprites.Pill, new Color(1, 1, 1, 0.55f), "TopLine");
            line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 1); line.rectTransform.offsetMin = new Vector2(24, -4); line.rectTransform.offsetMax = new Vector2(-24, -2);
            line.pixelsPerUnitMultiplier = 128f / 2f;
            var sweep = Img(face.transform, Sprites.Soft, new Color(1, 1, 1, 0f), "Sweep");
            sweep.rectTransform.At(new Vector2(0, 0.5f), new Vector2(-120, 0), new Vector2(110, size.y * 2.4f), new Vector2(0.5f, 0.5f));
            sweep.rectTransform.localRotation = Quaternion.Euler(0, 0, -22f);
            var edge = Img(face.transform, Sprites.RoundOutline, new Color(1, 1, 1, 0.8f), "Edge"); edge.rectTransform.Fill();
            foreach (var im in new[] { sh, back, outer, face, edge }) im.pixelsPerUnitMultiplier = ppu;
            outer.pixelsPerUnitMultiplier = ppu * 0.9f;
            return face;
        }

        /// <summary>Single-line text that shrinks to fit its box (never spills out of buttons/cards).</summary>
        public static Text Fit(Text t, int min = 14)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Min(min, t.fontSize);
            t.resizeTextMaxSize = t.fontSize;
            return t;
        }

        /// <summary>Text with a soft drop shadow instead of a thick outline (cleaner look for glass UI).</summary>
        public static Text SoftLabel(Transform parent, string text, int size, Color color, TextAnchor anchor, string name = "Label")
        {
            var t = Label(parent, text, size, color, anchor, false, name);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.02f, 0.04f, 0.12f, 0.45f);
            sh.effectDistance = new Vector2(0, -Mathf.Max(1.5f, size / 24f));
            return t;
        }

        static Button GlassBehaviour(RectTransform root, Image face, Color color, Action onClick)
        {
            var b = root.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.transition = Selectable.Transition.None;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(() => { AudioFX.Play("click"); onClick(); });
            var fx = root.gameObject.AddComponent<ButtonFX>();
            fx.flat = true; fx.face = face.rectTransform; fx.baseColor = color;
            fx.SetupHover(root.Find("Halo")?.GetComponent<Image>(), face.transform.Find("Edge")?.GetComponent<Image>(),
                (RectTransform)face.transform.Find("Sweep"), face.transform.Find("IconDisc")?.GetComponent<Image>(),
                root.Find("EdgeGlow")?.GetComponent<Image>(), (RectTransform)face.transform.Find("Content"),
                (RectTransform)face.transform.Find("Chevron"));
            return b;
        }

        /// <summary>A neutral glass card (game-UI style). Returns the face; root = face.transform.parent.</summary>
        public static Image GlassCard(Transform parent, Vector2 size, string name, float tintAlpha = 0.5f)
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var face = Glass(root, Sprites.RoundRect, 0.9f, tintAlpha);
            return face;
        }

        /// <summary>Makes a card clickable with the glass hover effect (grow, coloured halo, bright edge).</summary>
        public static Button Hoverable(Image face, Color accent, Action onClick)
        {
            var root = (RectTransform)face.transform.parent;
            var halo = Img(root, Sprites.Soft, WithAlpha(accent, 0f), "Halo"); halo.rectTransform.Fill(-70, -60, -70, -60);
            halo.transform.SetAsFirstSibling();
            var b = GlassBehaviour(root, face, accent, onClick);
            b.GetComponent<ButtonFX>().SetupHover(halo, face.transform.Find("Edge")?.GetComponent<Image>(), null, null);
            return b;
        }

        /// <summary>Small glass pill with an optional icon and text (tags, info chips).</summary>
        public static RectTransform InfoChip(Transform parent, Icon? icon, string text, Color textColor, Vector2 size, Color? edge = null)
        {
            var root = Rect("Chip", parent);
            root.sizeDelta = size;
            var face = Glass(root, Sprites.Pill, 128f / size.y, 0.45f);
            if (edge.HasValue) face.transform.Find("Edge").GetComponent<Image>().color = edge.Value;
            float ic = size.y * 0.56f, x = size.y * 0.32f;
            if (icon.HasValue) IconGraphic(root, icon.Value, ic, textColor).At(new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(ic, ic), new Vector2(0, 0.5f));
            var t = Fit(Label(root, text, Mathf.RoundToInt(size.y * 0.42f), textColor, icon.HasValue ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, false), 12);
            t.rectTransform.Fill(icon.HasValue ? x + ic + 8 : 10, 2, 12, 2);
            return root;
        }

        /// <summary>Round icon-only button (top bar style). glass = flat frosted look.</summary>
        public static Button RoundIconButton(Transform parent, Icon icon, Action onClick, float size = 104f, bool glass = false)
        {
            var root = Rect("Btn_" + icon, parent);
            root.sizeDelta = new Vector2(size, size);
            var face = glass ? Glass(root, Sprites.Circle) : Chip3D(root, Sprites.Circle, HudBg);
            if (glass) root.gameObject.AddComponent<ButtonFX>().flat = true;
            IconGraphic(face.transform, icon, size * 0.46f, Cream, HudBg);
            var b = root.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            if (onClick != null) b.onClick.AddListener(() => { AudioFX.Play("click"); onClick(); });
            return b;
        }

        /// <summary>
        /// Flat glass button: see-through tinted fill, bright thin edge, soft shine, icon + title (+ subtitle).
        /// tintAlpha controls how strong the colour is (PLAY uses a stronger tint than the others).
        /// </summary>
        public static Button GlassButton(Transform parent, Icon icon, string title, string subtitle, Color tint, Vector2 size,
            Action onClick, int titleSize = 44, float tintAlpha = 0.42f, bool chevron = true)
        {
            var root = Rect(title + "Btn", parent);
            root.sizeDelta = size;
            var face = GlassFace(root, tint, size, tintAlpha);
            float pad = size.y * 0.18f;
            float iconBox = size.y - pad * 2;
            var disc = Img(face.transform, Sprites.Circle, new Color(1, 1, 1, 0.14f), "IconDisc");
            disc.rectTransform.At(new Vector2(0, 0.5f), new Vector2(pad + 6, 0), new Vector2(iconBox, iconBox), new Vector2(0, 0.5f));
            var ring = Img(disc.transform, Sprites.ThinRing, new Color(1, 1, 1, 0.5f), "Ring"); ring.rectTransform.Fill();
            IconGraphic(disc.transform, icon, iconBox * 0.56f, Cream, WithAlpha(tint, 1f))
                .At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconBox * 0.56f, iconBox * 0.56f), new Vector2(0.5f, 0.5f));
            float left = pad * 2 + iconBox + 8;
            var content = Rect("Content", face.transform); content.Fill(left, 0, chevron ? pad + 40 : pad * 0.5f, 0);
            var label = Fit(SoftLabel(content, title, titleSize, Cream, TextAnchor.MiddleLeft), 18);
            if (string.IsNullOrEmpty(subtitle)) label.rectTransform.Fill();
            else
            {
                label.alignment = TextAnchor.LowerLeft;
                label.rectTransform.Fill(0, size.y * 0.44f, 0, 0);
                var sub = Fit(Label(content, subtitle, Mathf.RoundToInt(titleSize * 0.36f), WithAlpha(Cream, 0.78f), TextAnchor.UpperLeft, false, "Sub"), 12);
                sub.rectTransform.Fill(2, 6, 0, size.y * 0.58f);
            }
            if (chevron)
            {
                var chev = Img(face.transform, Sprites.Triangle, WithAlpha(Cream, 0.55f), "Chevron");
                chev.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-pad - 6, 0), new Vector2(size.y * 0.22f, size.y * 0.22f), new Vector2(0.5f, 0.5f));
                chev.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
            }
            return GlassBehaviour(root, face, tint, onClick);
        }

        /// <summary>Wide button with an icon on the left and a title (+ optional subtitle) next to it.</summary>
        public static Button IconButton(Transform parent, Icon icon, string title, string subtitle, Color color, Vector2 size,
            Action onClick, int titleSize = 48)
        {
            var b = Button(parent, "", color, size, onClick, titleSize);
            var face = b.GetComponent<ButtonFX>().face;
            float pad = size.y * 0.16f;
            float iconBox = size.y - 10 - pad * 2;
            var disc = Img(face, Sprites.Circle, new Color(1, 1, 1, 0.22f), "IconDisc");
            disc.rectTransform.At(new Vector2(0, 0.5f), new Vector2(pad, 0), new Vector2(iconBox, iconBox), new Vector2(0, 0.5f));
            IconGraphic(disc.transform, icon, iconBox * 0.5f, Cream, color).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconBox * 0.5f, iconBox * 0.5f), new Vector2(0.5f, 0.5f));
            var label = ButtonLabel(b);
            label.alignment = TextAnchor.MiddleLeft;
            float left = pad * 2 + iconBox;
            if (string.IsNullOrEmpty(subtitle))
            {
                label.text = title;
                label.rectTransform.Fill(left, 0, pad, 0);
            }
            else
            {
                label.text = title;
                label.rectTransform.Fill(left, size.y * 0.42f, pad, 0);
                label.alignment = TextAnchor.LowerLeft;
                var sub = Label(face, subtitle, Mathf.RoundToInt(titleSize * 0.42f), WithAlpha(Cream, 0.85f), TextAnchor.UpperLeft, false, "Sub");
                sub.rectTransform.Fill(left + 2, 0, pad, size.y * 0.62f);
            }
            return b;
        }

        /// <summary>Gold coin icon (circle + rim + shine).</summary>
        public static RectTransform CoinIcon(Transform parent, float size, string name = "Coin")
        {
            var root = Rect(name, parent);
            root.sizeDelta = new Vector2(size, size);
            var rim = Img(root, Sprites.Circle, new Color(0.78f, 0.5f, 0.08f), "Rim"); rim.rectTransform.Fill();
            var body = Img(root, Sprites.Circle, Gold, "Body"); body.rectTransform.Fill(size * 0.08f, size * 0.1f, size * 0.08f, size * 0.06f);
            var inner = Img(root, Sprites.Ring, new Color(0.85f, 0.55f, 0.1f, 0.9f), "Inner"); inner.rectTransform.Fill(size * 0.22f, size * 0.22f, size * 0.22f, size * 0.2f);
            var shine = Img(root, Sprites.Circle, new Color(1, 1, 1, 0.65f), "Shine");
            shine.rectTransform.At(new Vector2(0.32f, 0.7f), Vector2.zero, new Vector2(size * 0.18f, size * 0.18f), new Vector2(0.5f, 0.5f));
            return root;
        }

        public static Image StarIcon(Transform parent, float size, bool filled, string name = "Star")
        {
            var img = Img(parent, Sprites.Star, filled ? Gold : new Color(0, 0, 0, 0.35f), name);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>Full-screen dim layer that blocks input behind modal panels.</summary>
        public static Image Dim(Transform parent, float alpha = 0.6f)
        {
            var d = Img(parent, null, WithAlpha(Navy, alpha), "Dim", true);
            d.rectTransform.Fill();
            return d;
        }

        /// <summary>A ribbon/banner header with text.</summary>
        public static Text Ribbon(Transform parent, string text, Color color, Vector2 size, int fontSize = 64)
        {
            var root = Rect("Ribbon", parent);
            root.sizeDelta = size;
            var sh = Img(root, Sprites.Pill, Shadow, "Shadow"); sh.rectTransform.Fill(0, -10, 0, 10);
            var lip = Img(root, Sprites.Pill, Darken(color, 0.65f), "Lip"); lip.rectTransform.Fill();
            var face = Img(root, Sprites.Pill, color, "Face"); face.rectTransform.Fill(0, 8, 0, 0);
            face.pixelsPerUnitMultiplier = 128f / size.y;
            lip.pixelsPerUnitMultiplier = sh.pixelsPerUnitMultiplier = face.pixelsPerUnitMultiplier;
            var t = Label(face.transform, text, fontSize, Cream);
            t.rectTransform.Fill();
            return t;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0) return "--:--";
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60;
            return $"{m}:{s:00.0}";
        }
    }
}
