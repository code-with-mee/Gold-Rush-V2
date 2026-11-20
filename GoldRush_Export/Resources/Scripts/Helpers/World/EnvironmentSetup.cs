using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GoldRush
{
    /// <summary>Lighting moods (sunset day / moonlit night), fog, skybox and URP post-processing.</summary>
    public static class EnvironmentSetup
    {
        /// <summary>The global vignette override (gameplay may pulse it red on hit).</summary>
        public static Vignette Vignette { get; private set; }

        /// <summary>True when the scene is (heading to) night. See <see cref="DayNight"/>.</summary>
        public static bool IsNight => DayNight.IsNight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Vignette = null; }

        public static void Apply(Camera cam, bool menu, bool night = false)
        {
            // ---- sky / ambient / fog
            var assets = GameAssets.I;
            if (assets != null && assets.skybox != null) RenderSettings.skybox = assets.skybox;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.8f, 0.7f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.74f, 0.56f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.46f, 0.33f, 0.24f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.96f, 0.79f, 0.6f);
            RenderSettings.fogStartDistance = menu ? 25f : 35f;
            RenderSettings.fogEndDistance = menu ? 80f : 120f;

            // ---- sun
            Light sun = null;
            var lights = Object.FindObjectsByType<Light>();
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional) { sun = lights[i]; break; }
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.87f, 0.68f);
                sun.intensity = 1.5f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;
                sun.shadowBias = 0.05f;
                sun.shadowNormalBias = 0.4f;
                // low sunset sun behind/left of the default camera direction (+Z)
                go.transform.rotation = Quaternion.Euler(30f, 35f, 0f);
            }
            RenderSettings.sun = sun;
            DynamicGI.UpdateEnvironment();

            // ---- post-processing volume
            var vgo = new GameObject("PostFX");
            var vol = vgo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "GoldRushPost";

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(night ? 1.1f : 0.7f);
            bloom.threshold.Override(night ? 0.8f : 0.95f);
            bloom.scatter.Override(0.65f);

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(night ? 0.38f : 0.28f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(Color.black);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var ca = profile.Add<ColorAdjustments>(true);
            ca.contrast.Override(8f);
            ca.saturation.Override(12f);
            ca.postExposure.Override(0.05f);

            vol.sharedProfile = profile;
            Vignette = vig;

            // ---- real-time day/night blend (colours, fog, sun, sky, bloom, night lights)
            DayNight.Create(vgo, cam, sun, assets != null ? assets.skybox : null, bloom, vig, night);

            // ---- camera
            if (cam != null)
            {
                cam.allowHDR = true;
                cam.clearFlags = CameraClearFlags.Skybox;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }
    }
}
