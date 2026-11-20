using UnityEngine;

namespace GoldRush
{
    /// <summary>Makes a flat UI coin look like a spinning 3D coin: every few seconds it flips around
    /// its vertical axis (scale.x follows a cosine, like seeing the coin edge-on).</summary>
    public class CoinFlip : MonoBehaviour
    {
        public float interval = 2.6f, flipTime = 0.55f;
        float t;

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float cycle = t % interval;
            float x = cycle < flipTime ? Mathf.Cos(cycle / flipTime * Mathf.PI * 2f) : 1f;
            // keep a sliver of width so it never fully disappears
            transform.localScale = new Vector3(Mathf.Sign(x) * Mathf.Max(0.08f, Mathf.Abs(x)), 1f, 1f);
        }
    }
}
