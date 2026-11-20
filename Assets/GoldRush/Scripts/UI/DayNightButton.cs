using UnityEngine;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Round HUD button that switches day ↔ night in real time (also the N key).
    /// Shows a sun by day and a crescent moon by night.</summary>
    public class DayNightButton : MonoBehaviour
    {
        [SerializeField] RectTransform sun, moon;

        public static DayNightButton Create(Transform parent, Vector2 anchor, Vector2 pos, float size = 100f, bool glass = false)
        {
            float k = size / 100f;
            var root = UIKit.Rect("DayNight", parent).At(anchor, pos, new Vector2(size, size));
            var face = glass ? UIKit.Glass(root, Sprites.Circle) : UIKit.Chip3D(root, Sprites.Circle, UIKit.HudBg);
            var b = root.gameObject.AddComponent<DayNightButton>();

            // sun: disc + 8 rays
            // Material Icons: light_mode (sun) / dark_mode (moon)
            float g = size * 0.56f;
            b.sun = UIKit.IconGraphic(face.transform, UIKit.Icon.Sun, g, UIKit.Gold);
            b.sun.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(g, g), new Vector2(0.5f, 0.5f));
            b.moon = UIKit.IconGraphic(face.transform, UIKit.Icon.Moon, g, UIKit.Cream);
            b.moon.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(g, g), new Vector2(0.5f, 0.5f));

            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            b.Refresh(DayNight.IsNight);
            return b;
        }

        void OnChanged(bool night)
        {
            if (!this) return;
            Refresh(night);
            Tween.Punch(transform, 0.2f, 0.3f);
        }

        void Refresh(bool night)
        {
            // the icon shows the current time of day
            sun.gameObject.SetActive(!night);
            moon.gameObject.SetActive(night);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.N)) { AudioFX.Play("whoosh", 0.7f); DayNight.Toggle(); }
            if (sun.gameObject.activeSelf) sun.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 12f);
        }

        void Awake()
        {
            // connected here (not in Create) so it also works when saved in a scene
            var btn = GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => { AudioFX.Play("whoosh", 0.7f); DayNight.Toggle(); });
        }

        void OnEnable() { DayNight.Changed += OnChanged; }
        void OnDisable() { DayNight.Changed -= OnChanged; }
        void Start() { Refresh(DayNight.IsNight); }
    }
}
