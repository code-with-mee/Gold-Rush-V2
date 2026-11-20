using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoldRush
{
    /// <summary>Full-screen fade transition between scenes. Persistent singleton.</summary>
    public class Fader : MonoBehaviour
    {
        static Fader inst;
        CanvasGroup group;
        bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { inst = null; }

        static Fader I
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("[Fader]");
                    DontDestroyOnLoad(go);
                    inst = go.AddComponent<Fader>();
                    inst.Build();
                }
                return inst;
            }
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            var img = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(transform, false);
            img.color = UIKit.WithAlpha(UIKit.HudBg, 1f);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            group.alpha = 1f;
            group.blocksRaycasts = false;
        }

        /// <summary>Call from a scene's Start: fades from black (if the fader exists) or does a first fade-in.</summary>
        public static void FadeIn(float dur = 0.45f)
        {
            var f = I;
            if (f.busy) return;
            f.StopAllCoroutines();
            f.StartCoroutine(f.FadeTo(0f, dur));
        }

        public static void Load(string scene)
        {
            var f = I;
            if (f.busy) return;
            f.StartCoroutine(f.LoadCo(scene));
        }

        IEnumerator FadeTo(float target, float dur)
        {
            float start = group.alpha, t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.5f;
        }

        IEnumerator LoadCo(string scene)
        {
            busy = true;
            group.blocksRaycasts = true;
            yield return FadeTo(1f, 0.3f);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            busy = false;
            yield return FadeTo(0f, 0.4f);
        }
    }
}
