using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// 3D-looking "GOLD RUSH" logo built from stacked UI text layers:
    /// shadow → extrusion layers (darker colours, stepping downwards) → light rim → coloured face.
    /// The extrusion direction follows the mouse (like a moving light), and hovering makes it wobble.
    /// </summary>
    public class LogoTitle : MonoBehaviour, IPointerEnterHandler
    {
        const string Gold = "FFC233", GoldDark = "B57A0E", GoldDeep = "7A4B06";
        const string Cyan = "33C8E6", CyanDark = "1A8FAE", CyanDeep = "0E5670";

        [SerializeField] RectTransform[] layers;
        [SerializeField] RectTransform face, rim, shadow, root;
        float wobble;          // 0..1, decays after hovering
        float intro;           // 0..1 entrance pop
        Vector2 tilt;          // smoothed mouse direction

        static string Word(string text, string hex) => $"<color=#{hex}>{text}</color>";

        [Tooltip("Letter size, used when the logo builds itself (an empty Logo object in the scene).")]
        [SerializeField] int fontSize = 176;

        void Awake()
        {
            // added by hand to an empty RectTransform: build the text layers now
            if (layers == null || layers.Length == 0) Fill(this, (RectTransform)transform, fontSize, 12);
        }

        public static LogoTitle Build(RectTransform parent, int fontSize, int depth = 12)
        {
            var logo = parent.GetComponent<LogoTitle>();
            if (!logo) logo = parent.gameObject.AddComponent<LogoTitle>();
            logo.fontSize = fontSize;
            if (logo.layers == null || logo.layers.Length == 0) Fill(logo, parent, fontSize, depth);
            return logo;
        }

        static void Fill(LogoTitle logo, RectTransform parent, int fontSize, int depth)
        {
            logo.root = parent;
            // invisible hit area so the mouse can "touch" the logo
            var hit = parent.GetComponent<Image>();
            if (!hit) hit = parent.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            // soft drop shadow
            logo.shadow = Layer(parent, $"{Word("GOLD", "000000")} {Word("RUSH", "000000")}", fontSize, new Color(0, 0, 0, 0.45f), "Shadow");

            // extrusion: from deep (bottom) to lighter (just under the face)
            logo.layers = new RectTransform[depth];
            for (int i = depth - 1; i >= 0; i--)
            {
                float t = i / (float)(depth - 1); // 1 = deepest
                string g = t > 0.5f ? GoldDeep : GoldDark, c = t > 0.5f ? CyanDeep : CyanDark;
                logo.layers[i] = Layer(parent, $"{Word("GOLD", g)} {Word("RUSH", c)}", fontSize, Color.white, "Depth" + i);
            }

            // light rim (peeks out above the face) and the face itself
            logo.rim = Layer(parent, $"{Word("GOLD", "FFF1C2")} {Word("RUSH", "C9F6FF")}", fontSize, Color.white, "Rim");
            logo.face = Layer(parent, $"{Word("GOLD", Gold)} {Word("RUSH", Cyan)}", fontSize, Color.white, "Face");
            var o = logo.face.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.03f, 0.05f, 0.15f, 0.55f);
            o.effectDistance = new Vector2(2, -2);
        }

        static RectTransform Layer(Transform parent, string text, int size, Color c, string name)
        {
            var t = UIKit.Label(parent, text, size, c, TextAnchor.MiddleCenter, false, name);
            t.rectTransform.Fill();
            return t.rectTransform;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            wobble = 1f;
            AudioFX.Play("whoosh", 0.35f, 1.3f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // mouse direction from the logo centre (-1..1), smoothed
            Vector2 mouse = Input.mousePosition;
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, root.position);
            Vector2 dir = new Vector2((mouse.x - centre.x) / Screen.width, (mouse.y - centre.y) / Screen.height) * 2f;
            dir = Vector2.ClampMagnitude(dir, 1f);
            tilt = Vector2.Lerp(tilt, dir, 1f - Mathf.Exp(-5f * dt));

            // extrusion points away from the "light" (the mouse)
            Vector2 step = new Vector2(-tilt.x * 1.6f, -2.2f - tilt.y * 0.8f);
            for (int i = 0; i < layers.Length; i++) layers[i].anchoredPosition = step * (i + 1);
            rim.anchoredPosition = new Vector2(tilt.x * 0.8f, 2.5f + tilt.y * 0.8f);
            shadow.anchoredPosition = step * (layers.Length + 6) + new Vector2(0, -6);

            // hover wobble: quick shake + rotation that settles, plus a slight 3D tilt toward the mouse
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.4f);
            float w = wobble * wobble;
            float t = Time.unscaledTime;
            float rotZ = Mathf.Sin(t * 22f) * 5f * w;
            float shakeX = Mathf.Sin(t * 47f) * 6f * w;
            root.localRotation = Quaternion.Euler(-tilt.y * 6f, tilt.x * 8f, rotZ);
            intro = Mathf.MoveTowards(intro, 1f, dt * 1.8f);
            float pop = Tween.OutBack(intro);
            root.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, pop) * (1f + 0.08f * Mathf.Sin(wobble * Mathf.PI));
            face.anchoredPosition = new Vector2(shakeX, 0);
        }
    }
}
