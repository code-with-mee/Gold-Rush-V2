using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>
    /// Touch movement for the LEFT half of the screen. Invisible until a finger touches down;
    /// then a stick appears under the thumb. Pushing the stick to the outer edge = sprint.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform area, stickBase, knob;
        public float radius = 120f;
        public Vector2 Value { get; private set; }
        /// <summary>True while a finger is on the stick.</summary>
        public bool Held => pointer != int.MinValue;

        CanvasGroup group;
        Image edge;
        int pointer = int.MinValue;
        float shown;

        public static VirtualJoystick Create(Transform parent)
        {
            var area = UIKit.Rect("MoveArea (left half)", parent);
            area.anchorMin = Vector2.zero; area.anchorMax = new Vector2(0.5f, 1f);
            area.pivot = Vector2.zero; area.offsetMin = area.offsetMax = Vector2.zero;
            area.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // invisible touch target

            var b = UIKit.Rect("Stick", area);
            b.anchorMin = b.anchorMax = Vector2.zero;
            b.pivot = new Vector2(0.5f, 0.5f);
            b.sizeDelta = new Vector2(300, 300);
            var bg = UIKit.Img(b, Sprites.Circle, UIKit.WithAlpha(UIKit.HudBg, 0.45f), "Bg"); bg.rectTransform.Fill();
            var ring = UIKit.Img(b, Sprites.Ring, UIKit.WithAlpha(UIKit.Cyan, 0.6f), "Ring"); ring.rectTransform.Fill();
            var k = UIKit.Rect("Knob", b);
            k.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            var ks = UIKit.Img(k, Sprites.Circle, new Color(0, 0, 0, 0.25f), "Shadow"); ks.rectTransform.Fill(-4, -10, -4, 2);
            var kf = UIKit.Img(k, Sprites.Circle, UIKit.Cream, "Face"); kf.rectTransform.Fill();
            var kg = UIKit.Img(k, Sprites.Circle, UIKit.Cyan, "Dot"); kg.rectTransform.Fill(34, 34, 34, 34);

            var j = area.gameObject.AddComponent<VirtualJoystick>();
            j.area = area; j.stickBase = b; j.knob = k; j.edge = ring;
            j.group = b.gameObject.AddComponent<CanvasGroup>();
            j.group.blocksRaycasts = false;
            j.group.alpha = 0f; // hidden until touched
            return j;
        }

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var p);
            return p;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Held) return;
            pointer = e.pointerId;
            stickBase.anchoredPosition = Local(e);
            knob.anchoredPosition = Vector2.zero;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            Vector2 d = Local(e) - stickBase.anchoredPosition;
            // the stick follows the thumb if it drags far away
            if (d.magnitude > radius * 1.6f)
            {
                stickBase.anchoredPosition += d.normalized * (d.magnitude - radius * 1.6f);
                d = Local(e) - stickBase.anchoredPosition;
            }
            Vector2 c = Vector2.ClampMagnitude(d, radius);
            knob.anchoredPosition = c;
            Vector2 v = c / radius;
            Value = v.magnitude < 0.12f ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            Release();
        }

        public void Release()
        {
            pointer = int.MinValue;
            Value = Vector2.zero;
            if (knob) knob.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, Held ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            group.alpha = shown;
            // the outer ring lights up orange while sprinting (stick pushed to the edge)
            if (edge) edge.color = Held && Value.magnitude > 0.9f ? UIKit.WithAlpha(UIKit.Orange, 0.9f) : UIKit.WithAlpha(UIKit.Cyan, 0.6f);
        }

        void OnDisable() { Release(); }
    }
}
