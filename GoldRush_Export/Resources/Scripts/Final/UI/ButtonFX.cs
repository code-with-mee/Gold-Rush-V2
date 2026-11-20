using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Press animation for UIKit buttons: face sinks into the lip and the button squashes slightly.</summary>
    public class ButtonFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public RectTransform face;
        public Color baseColor;
        [Tooltip("Flat/glass buttons: only scale down when pressed (no sinking face).")]
        public bool flat;
        Button button;
        bool pressed, hovered;
        float k; // 0 = up, 1 = pressed
        float h; // 0 = normal, 1 = hovered

        // optional hover parts (glass buttons)
        [SerializeField] Image halo, edge, disc, edgeGlow;
        [SerializeField] RectTransform sweep, content, chevron;
        [SerializeField] Color haloColor, discColor, glowColor;
        [SerializeField] Vector2 contentMin, contentMax, chevronPos;
        Coroutine sweepCo;
        [SerializeField] CanvasGroup group;
        float dim; // 0..1: dimmed because a sibling button has focus

        /// <summary>The glass button the mouse is currently over (siblings dim to show focus).</summary>
        static ButtonFX focused;

        /// <summary>Glass buttons: halo glow, brighter edge, light sweep and icon highlight on hover.</summary>
        public void SetupHover(Image halo, Image edge, RectTransform sweep, Image disc,
            Image edgeGlow = null, RectTransform content = null, RectTransform chevron = null)
        {
            this.halo = halo; this.edge = edge; this.sweep = sweep; this.disc = disc;
            this.edgeGlow = edgeGlow; this.content = content; this.chevron = chevron;
            if (halo) haloColor = halo.color;
            if (disc) discColor = disc.color;
            if (edgeGlow) glowColor = edgeGlow.color;
            if (content) { contentMin = content.offsetMin; contentMax = content.offsetMax; }
            if (chevron) chevronPos = chevron.anchoredPosition;
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (button != null && !button.interactable) return;
            hovered = true;
            focused = this;
            if (disc) Tween.Punch(disc.transform, 0.18f, 0.3f);
            if (sweep && face)
            {
                if (sweepCo != null) Tween.Stop(sweepCo);
                var img = sweep.GetComponent<Image>();
                float w = face.rect.width;
                sweepCo = Tween.Run(sweep, 0.55f, t =>
                {
                    sweep.anchoredPosition = new Vector2(Mathf.Lerp(-120f, w + 120f, t), 0);
                    img.color = new Color(1, 1, 1, 0.35f * Mathf.Sin(t * Mathf.PI));
                }, Tween.InOutSine);
            }
        }

        void Awake() { button = GetComponent<Button>(); }

        public void SetColor(Color c)
        {
            baseColor = c;
            if (face) face.GetComponent<Image>().color = flat ? UIKit.WithAlpha(c, 0.55f) : c;
            var lip = transform.Find("Lip");
            if (lip) lip.GetComponent<Image>().color = UIKit.Darken(c, 0.62f);
        }

        public void OnPointerDown(PointerEventData e) { if (button == null || button.interactable) pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnPointerExit(PointerEventData e)
        {
            pressed = false; hovered = false;
            if (focused == this) focused = null;
        }
        void OnDisable() { pressed = false; hovered = false; k = 0; h = 0; Apply(); }

        void Update()
        {
            float target = pressed ? 1f : 0f;
            float hTarget = hovered ? 1f : 0f;
            if (disc && h <= 0f && !hovered) discColor = disc.color; // follow colour changes (e.g. toggles)
            // dim when another glass button in the same group has focus
            float dimTarget = group && focused && focused != this && focused.transform.parent == transform.parent ? 1f : 0f;
            dim = Mathf.MoveTowards(dim, dimTarget, Time.unscaledDeltaTime * 6f);
            if (group) group.alpha = Mathf.Lerp(1f, 0.6f, dim);
            if (Mathf.Approximately(k, target) && Mathf.Approximately(h, hTarget)) return;
            k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime * 14f);
            h = Mathf.MoveTowards(h, hTarget, Time.unscaledDeltaTime * 7f);
            Apply();
        }

        void Apply()
        {
            if (flat || !face)
            {
                float e = h * h * (3f - 2f * h); // smooth
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, e) * Mathf.Lerp(1f, 0.95f, k);
                if (halo) halo.color = UIKit.WithAlpha(haloColor, Mathf.Lerp(0f, 0.9f, e));
                if (edge) edge.color = new Color(1, 1, 1, Mathf.Lerp(0.8f, 1f, e));
                if (edgeGlow) edgeGlow.color = Color.Lerp(glowColor, UIKit.WithAlpha(Color.Lerp(baseColor, Color.white, 0.2f), 1f), e);
                if (disc) disc.color = Color.Lerp(discColor, Color.Lerp(discColor, Color.white, 0.3f), e); // brighten, keep colour
                if (content) { content.offsetMin = contentMin + Vector2.right * 10f * e; content.offsetMax = contentMax + Vector2.right * 10f * e; }
                if (chevron) chevron.anchoredPosition = chevronPos + Vector2.right * 8f * e;
                return;
            }
            face.offsetMin = new Vector2(0, Mathf.Lerp(10, 2, k));
            face.offsetMax = new Vector2(0, Mathf.Lerp(0, -8, k));
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.96f, k);
        }
    }
}
