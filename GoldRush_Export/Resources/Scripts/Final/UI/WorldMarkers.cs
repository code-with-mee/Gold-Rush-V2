using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Screen-space UI that follows world objects (enemy "!" bubbles, labels, floating "+1" text).</summary>
    public class WorldMarkers : MonoBehaviour
    {
        public class Marker
        {
            public Transform target;
            public Vector3 offset;
            public RectTransform rt;
            public float maxDistance = 999f;
            public bool visible = true;
        }

        readonly List<Marker> markers = new List<Marker>();
        [SerializeField] RectTransform root;
        Camera cam;

        public static WorldMarkers Create(Transform canvas)
        {
            var rt = UIKit.Rect("WorldMarkers", canvas);
            rt.Fill();
            var m = rt.gameObject.AddComponent<WorldMarkers>();
            m.root = rt;
            return m;
        }

        public Marker Add(Transform target, Vector3 offset, RectTransform ui, float maxDistance = 999f)
        {
            ui.SetParent(root, false);
            ui.anchorMin = ui.anchorMax = Vector2.zero;
            var m = new Marker { target = target, offset = offset, rt = ui, maxDistance = maxDistance };
            markers.Add(m);
            ui.gameObject.SetActive(false);
            return m;
        }

        public void Remove(Marker m)
        {
            if (m == null) return;
            markers.Remove(m);
            if (m.rt) Destroy(m.rt.gameObject);
        }

        /// <summary>Floating text that rises and fades (e.g. "+1").</summary>
        public void FloatText(Vector3 world, string text, Color color, int size = 60)
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) return;
            var t = UIKit.Label(root, text, size, color);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(300, 100);
            rt.position = new Vector3(sp.x, sp.y, 0);
            Vector3 start = rt.position;
            float rise = Screen.height * 0.09f;
            Tween.Run(rt, 0.9f, k =>
            {
                rt.position = start + Vector3.up * rise * k;
                rt.localScale = Vector3.one * (k < 0.2f ? Mathf.Lerp(0.5f, 1.25f, k / 0.2f) : Mathf.Lerp(1.25f, 1f, (k - 0.2f) / 0.8f));
                var c = color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f; t.color = c;
            }, Tween.Linear, 0f, () => Destroy(rt.gameObject), false);
        }

        void LateUpdate()
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;
            Vector3 camPos = cam.transform.position;
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var m = markers[i];
                if (m.target == null || m.rt == null)
                {
                    if (m.rt) Destroy(m.rt.gameObject);
                    markers.RemoveAt(i);
                    continue;
                }
                Vector3 wp = m.target.position + m.offset;
                bool show = m.visible && (wp - camPos).sqrMagnitude < m.maxDistance * m.maxDistance;
                Vector3 sp = Vector3.zero;
                if (show)
                {
                    sp = cam.WorldToScreenPoint(wp);
                    show = sp.z > 0.1f;
                }
                // don't draw world labels underneath the top HUD bar
                if (show && sp.y > Screen.height * 0.84f) show = false;
                if (m.rt.gameObject.activeSelf != show) m.rt.gameObject.SetActive(show);
                if (show) m.rt.position = new Vector3(sp.x, sp.y, 0);
            }
        }
    }
}
