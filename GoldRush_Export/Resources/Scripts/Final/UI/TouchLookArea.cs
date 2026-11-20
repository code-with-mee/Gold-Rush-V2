using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Touch camera control for the RIGHT half of the screen: drag to look around.
    /// A small ring appears under the finger while dragging.</summary>
    public class TouchLookArea : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        static Vector2 accumulated;
        int pointer = int.MinValue;
        RectTransform area, marker;
        CanvasGroup markerGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { accumulated = Vector2.zero; }

        /// <summary>Returns and clears the drag delta (screen pixels) since the last call.</summary>
        public static Vector2 Consume()
        {
            var a = accumulated;
            accumulated = Vector2.zero;
            return a;
        }

        public static TouchLookArea Create(Transform parent)
        {
            var rt = UIKit.Rect("LookArea (right half)", parent);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = Vector2.zero;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var t = rt.gameObject.AddComponent<TouchLookArea>();
            t.area = rt;
            t.marker = UIKit.Rect("Finger", rt);
            t.marker.anchorMin = t.marker.anchorMax = Vector2.zero;
            t.marker.sizeDelta = new Vector2(150, 150);
            UIKit.Img(t.marker, Sprites.Ring, UIKit.WithAlpha(UIKit.Cyan, 0.5f), "Ring").rectTransform.Fill();
            UIKit.Img(t.marker, Sprites.Soft, UIKit.WithAlpha(UIKit.Cyan, 0.25f), "Glow").rectTransform.Fill(-30, -30, -30, -30);
            t.markerGroup = t.marker.gameObject.AddComponent<CanvasGroup>();
            t.markerGroup.alpha = 0f;
            t.markerGroup.blocksRaycasts = false;
            return t;
        }

        void Place(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var p);
            marker.anchoredPosition = p;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            Place(e);
        }

        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) pointer = int.MinValue; }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            accumulated += e.delta;
            Place(e);
        }

        void Update()
        {
            if (markerGroup) markerGroup.alpha = Mathf.MoveTowards(markerGroup.alpha, pointer != int.MinValue ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        }

        void OnDisable() { pointer = int.MinValue; }
    }
}
