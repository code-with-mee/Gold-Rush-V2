using UnityEngine;

namespace GoldRush
{
    /// <summary>Put this on any Light: it fades in at night and switches off during the day.</summary>
    [RequireComponent(typeof(Light))]
    public class NightLight : MonoBehaviour
    {
        [Tooltip("Brightness of the light at full night.")]
        public float nightIntensity = 2f;

        void Start()
        {
            if (Application.isPlaying) DayNight.RegisterNightLight(GetComponent<Light>(), nightIntensity);
        }

        void Reset() { nightIntensity = GetComponent<Light>().intensity; }
    }
}
