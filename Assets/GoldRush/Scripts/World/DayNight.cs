using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GoldRush
{
    /// <summary>
    /// Real-time day/night blend (0 = day, 1 = night). Everything that should look different at night
    /// registers here: material colours (floor, walls), lights that only shine at night (lanterns,
    /// monster glows, lamps). Call <see cref="Toggle"/> to switch smoothly at any time.
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        public static DayNight I { get; private set; }

        /// <summary>Current blend value, 0 = full day, 1 = full night.</summary>
        public static float Value => I ? I.value : 0f;
        /// <summary>True when heading to (or at) night.</summary>
        public static bool IsNight => I && I.target > 0.5f;
        public static event System.Action<bool> Changed;

        [Tooltip("Seconds for a full day <-> night transition.")]
        public float transitionTime = 1.5f;

        float value, target;
        Light sun;
        Camera cam;
        Material sky;
        Bloom bloom;
        Vignette vignette;

        struct Tint { public Material mat; public Color day, night; }
        struct NightLight { public Light light; public float intensity; }
        readonly List<Tint> tints = new List<Tint>();
        readonly List<NightLight> nightLights = new List<NightLight>();

        // registrations made before DayNight exists (e.g. scene objects whose Start runs first)
        static readonly List<Tint> pendingTints = new List<Tint>();
        static readonly List<NightLight> pendingLights = new List<NightLight>();

        // ---- presets
        static readonly Color DaySky = new Color(0.78f, 0.82f, 0.9f), NightSky = new Color(0.2f, 0.22f, 0.42f);
        static readonly Color DayEquator = new Color(0.62f, 0.65f, 0.72f), NightEquator = new Color(0.12f, 0.13f, 0.27f);
        static readonly Color DayGround = new Color(0.4f, 0.4f, 0.45f), NightGround = new Color(0.05f, 0.05f, 0.1f);
        static readonly Color DayFog = new Color(0.72f, 0.78f, 0.88f), NightFog = new Color(0.05f, 0.06f, 0.14f);
        static readonly Color DaySun = new Color(1f, 0.95f, 0.86f), NightSun = new Color(0.5f, 0.6f, 1f);
        static readonly Quaternion DaySunRot = Quaternion.Euler(50f, 35f, 0f), NightSunRot = Quaternion.Euler(40f, -35f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; Changed = null; pendingTints.Clear(); pendingLights.Clear(); }

        public static DayNight Create(GameObject host, Camera cam, Light sun, Material skyboxAsset, Bloom bloom, Vignette vignette, bool night)
        {
            var d = host.AddComponent<DayNight>();
            I = d;
            d.cam = cam;
            d.sun = sun;
            d.bloom = bloom;
            d.vignette = vignette;
            if (skyboxAsset)
            {
                d.sky = new Material(skyboxAsset) { name = "Sky (runtime)" };
                RenderSettings.skybox = d.sky;
            }
            d.value = d.target = night ? 1f : 0f;
            d.tints.AddRange(pendingTints);
            d.nightLights.AddRange(pendingLights);
            pendingTints.Clear();
            pendingLights.Clear();
            d.ApplyNow();
            return d;
        }

        void OnDestroy() { if (I == this) I = null; }

        // ------------------------------------------------------------------ public API

        public static void Toggle() { if (I) Set(!IsNight); }

        public static void Set(bool night, bool instant = false)
        {
            if (!I) return;
            I.target = night ? 1f : 0f;
            if (instant) { I.value = I.target; I.ApplyNow(); }
            Changed?.Invoke(night);
        }

        /// <summary>Material whose _BaseColor blends between day and night colours.</summary>
        public static void RegisterTint(Material m, Color day, Color night)
        {
            if (!m) return;
            var tint = new Tint { mat = m, day = day, night = night };
            if (!I) { pendingTints.Add(tint); m.SetColor("_BaseColor", day); return; }
            I.tints.Add(tint);
            m.SetColor("_BaseColor", Color.Lerp(day, night, Smooth(I.value)));
        }

        /// <summary>Light that fades in at night (to the given intensity) and switches off by day.</summary>
        public static void RegisterNightLight(Light l, float nightIntensity = -1f)
        {
            if (!l) return;
            float k = nightIntensity >= 0 ? nightIntensity : l.intensity;
            var nl = new NightLight { light = l, intensity = k };
            if (!I) { pendingLights.Add(nl); return; }
            I.nightLights.Add(nl);
            I.ApplyLight(I.nightLights[I.nightLights.Count - 1], Smooth(I.value));
        }

        // ------------------------------------------------------------------ blending

        static float Smooth(float t) => t * t * (3f - 2f * t);

        void Update()
        {
            if (Mathf.Approximately(value, target)) return;
            value = Mathf.MoveTowards(value, target, Time.unscaledDeltaTime / Mathf.Max(0.05f, transitionTime));
            ApplyNow();
        }

        void ApplyNow()
        {
            float t = Smooth(value);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(DaySky, NightSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(DayEquator, NightEquator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(DayGround, NightGround, t);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(DayFog, NightFog, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(40f, 18f, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(140f, 75f, t);

            if (sun)
            {
                sun.color = Color.Lerp(DaySun, NightSun, t);
                sun.intensity = Mathf.Lerp(1.35f, 0.45f, t);
                sun.transform.rotation = Quaternion.Slerp(DaySunRot, NightSunRot, t);
            }
            if (sky)
            {
                sky.SetFloat("_Exposure", Mathf.Lerp(1.1f, 0.06f, t));
                sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.7f, 0.4f, t));
                sky.SetColor("_SkyTint", Color.Lerp(new Color(0.5f, 0.55f, 0.65f), new Color(0.2f, 0.22f, 0.5f), t));
                sky.SetColor("_GroundColor", Color.Lerp(new Color(0.62f, 0.66f, 0.74f), new Color(0.05f, 0.06f, 0.14f), t));
                sky.SetFloat("_SunSize", 0.035f);
            }
            if (cam)
            {
                cam.backgroundColor = Color.Lerp(DayFog, NightFog, t);
                cam.clearFlags = sky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            }
            if (bloom != null)
            {
                bloom.intensity.Override(Mathf.Lerp(0.6f, 1.1f, t));
                bloom.threshold.Override(Mathf.Lerp(0.95f, 0.8f, t));
            }
            if (vignette != null) vignette.intensity.Override(Mathf.Lerp(0.25f, 0.38f, t));

            for (int i = tints.Count - 1; i >= 0; i--)
            {
                if (!tints[i].mat) { tints.RemoveAt(i); continue; }
                tints[i].mat.SetColor("_BaseColor", Color.Lerp(tints[i].day, tints[i].night, t));
            }
            for (int i = nightLights.Count - 1; i >= 0; i--)
            {
                if (!nightLights[i].light) { nightLights.RemoveAt(i); continue; }
                ApplyLight(nightLights[i], t);
            }
        }

        void ApplyLight(NightLight n, float t)
        {
            n.light.intensity = n.intensity * t;
            n.light.enabled = t > 0.01f;
        }
    }
}
