using UnityEngine;

namespace GoldRush
{
    /// <summary>Idle UI animation: vertical bob, gentle rotation wobble and/or scale pulse (unscaled time).</summary>
    public class Bob : MonoBehaviour
    {
        [SerializeField] float amplitude, speed, wobble, pulse, phase;
        [SerializeField] Vector2 basePos;
        [SerializeField] Quaternion baseRot;
        [SerializeField] RectTransform rt;

        public void Setup(float amplitude, float speed, float wobbleDegrees, float pulse = 0f)
        {
            this.amplitude = amplitude; this.speed = speed; wobble = wobbleDegrees; this.pulse = pulse;
            Remember();
        }

        /// <summary>Remembers where the object starts (added by hand: set the numbers in the Inspector).</summary>
        void Remember()
        {
            rt = transform as RectTransform;
            basePos = rt ? rt.anchoredPosition : Vector2.zero;
            baseRot = transform.localRotation;
            phase = Random.Range(0f, 10f);
        }

        void Awake() { if (!rt) Remember(); }

        void Update()
        {
            if (!rt) return;
            float t = Time.unscaledTime * speed + phase;
            if (amplitude != 0f) rt.anchoredPosition = basePos + Vector2.up * (Mathf.Sin(t) * amplitude);
            if (wobble != 0f) transform.localRotation = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 0.7f) * wobble);
            if (pulse != 0f) transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.3f) * pulse);
        }
    }
}
