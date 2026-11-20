using System;
using System.Collections;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Tiny coroutine tween helper. All tweens use unscaled time by default (UI works while paused).</summary>
    public static class Tween
    {
        static TweenRunner runner;
        static TweenRunner R
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("[Tween]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    runner = go.AddComponent<TweenRunner>();
                }
                return runner;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { runner = null; }

        // ---- easing
        public static float Linear(float t) => t;
        public static float OutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
        public static float InCubic(float t) => t * t * t;
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
        public static float OutElastic(float t)
        {
            if (t <= 0) return 0; if (t >= 1) return 1;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
        }

        /// <summary>Runs step(eased t) for dur seconds. Stops early if owner gets destroyed.</summary>
        public static Coroutine Run(UnityEngine.Object owner, float dur, Action<float> step, Func<float, float> ease = null,
            float delay = 0f, Action done = null, bool unscaled = true)
        {
            if (!Application.isPlaying) { step?.Invoke((ease ?? OutCubic)(1f)); done?.Invoke(); return null; } // editor bake: jump to the end
            return R.StartCoroutine(Co(owner, dur, step, ease ?? OutCubic, delay, done, unscaled));
        }

        public static Coroutine Delay(UnityEngine.Object owner, float delay, Action done, bool unscaled = true)
        {
            if (!Application.isPlaying) { done?.Invoke(); return null; }
            return R.StartCoroutine(Co(owner, 0f, null, Linear, delay, done, unscaled));
        }

        static IEnumerator Co(UnityEngine.Object owner, float dur, Action<float> step, Func<float, float> ease,
            float delay, Action done, bool unscaled)
        {
            bool hasOwner = owner != null;
            while (delay > 0)
            {
                delay -= unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
                if (hasOwner && owner == null) yield break;
            }
            float t = 0;
            while (t < dur)
            {
                if (hasOwner && owner == null) yield break;
                step?.Invoke(ease(Mathf.Clamp01(t / dur)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            if (hasOwner && owner == null) yield break;
            step?.Invoke(ease(1f));
            done?.Invoke();
        }

        public static void Stop(Coroutine c) { if (c != null && runner != null) runner.StopCoroutine(c); }

        // ---- helpers
        public static Coroutine Scale(Transform t, Vector3 from, Vector3 to, float dur, Func<float, float> ease = null, float delay = 0f, Action done = null)
        {
            t.localScale = from;
            return Run(t, dur, k => t.localScale = Vector3.LerpUnclamped(from, to, k), ease ?? OutBack, delay, done);
        }

        public static Coroutine PopIn(Transform t, float dur = 0.35f, float delay = 0f)
            => Scale(t, Vector3.zero, Vector3.one, dur, OutBack, delay);

        public static Coroutine Punch(Transform t, float amount = 0.25f, float dur = 0.3f)
        {
            Vector3 baseScale = Vector3.one;
            return Run(t, dur, k => t.localScale = baseScale * (1 + amount * Mathf.Sin(k * Mathf.PI) * (1 - k * 0.5f)), Linear);
        }

        public static Coroutine Fade(CanvasGroup g, float to, float dur, float delay = 0f, Action done = null)
        {
            float from = g.alpha;
            return Run(g, dur, k => g.alpha = Mathf.LerpUnclamped(from, to, k), OutCubic, delay, done);
        }

        public static Coroutine Move(RectTransform rt, Vector2 to, float dur, Func<float, float> ease = null, float delay = 0f, Action done = null)
        {
            Vector2 from = rt.anchoredPosition;
            return Run(rt, dur, k => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k), ease ?? OutCubic, delay, done);
        }
    }
}
