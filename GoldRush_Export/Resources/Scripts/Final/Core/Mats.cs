using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>Cached runtime materials cloned from the base materials in GameAssets.</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        static string K(Color c) => $"{c.r:F3},{c.g:F3},{c.b:F3},{c.a:F3}";

        /// <summary>Opaque URP Lit material.</summary>
        public static Material Lit(Color color, float smoothness = 0.2f, float metallic = 0f, Texture tex = null, float tiling = 1f)
        {
            string key = $"lit|{K(color)}|{smoothness}|{metallic}|{(tex ? tex.name : "")}|{tiling}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(GameAssets.I.lit) { name = "Lit_" + cache.Count };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (tex)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            }
            cache[key] = m;
            return m;
        }

        /// <summary>Opaque URP Lit with HDR emission (glows with bloom). intensity &gt; 1 = HDR.</summary>
        public static Material Emissive(Color color, Color emission, float intensity = 1.5f, float smoothness = 0.7f, float metallic = 0.5f)
        {
            string key = $"emi|{K(color)}|{K(emission)}|{intensity}|{smoothness}|{metallic}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(GameAssets.I.litEmissive) { name = "Emi_" + cache.Count };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetColor("_EmissionColor", emission * intensity);
            m.EnableKeyword("_EMISSION");
            cache[key] = m;
            return m;
        }

        /// <summary>Transparent URP Lit (alpha from color.a).</summary>
        public static Material Transparent(Color color, float smoothness = 0.6f)
        {
            string key = $"tr|{K(color)}|{smoothness}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(GameAssets.I.litTransparent) { name = "Tr_" + cache.Count };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            cache[key] = m;
            return m;
        }

        /// <summary>Alpha-blended particle material using the given texture.</summary>
        public static Material Particle(Texture tex)
        {
            string key = "pt|" + (tex ? tex.name : "");
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(GameAssets.I.particle) { name = "Pt_" + cache.Count };
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            cache[key] = m;
            return m;
        }

        /// <summary>Sets an object's renderers (recursively) to cast or not cast shadows.</summary>
        public static void NoShadows(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Procedural, tileable grayscale textures (multiply with material color).</summary>
    public static class ProcTex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        /// <summary>Tileable value noise; period = number of cells across the texture.</summary>
        public static float Noise(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int Wrap(int i) => ((i % period) + period) % period;
            float a = Hash(Wrap(x0), Wrap(y0), seed), b = Hash(Wrap(x0 + 1), Wrap(y0), seed);
            float c = Hash(Wrap(x0), Wrap(y0 + 1), seed), d = Hash(Wrap(x0 + 1), Wrap(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        public static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0; int p = basePeriod;
            for (int i = 0; i < octaves; i++) { sum += Noise(u, v, p, seed + i) * amp; norm += amp; amp *= 0.5f; p *= 2; }
            return sum / norm;
        }

        static Texture2D Make(string name, int n, System.Func<float, float, float> f)
        {
            if (cache.TryGetValue(name, out var t) && t) return t;
            t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    byte g = (byte)(Mathf.Clamp01(f((x + 0.5f) / n, (y + 0.5f) / n)) * 255);
                    px[y * n + x] = new Color32(g, g, g, 255);
                }
            t.SetPixels32(px);
            t.Apply(true);
            cache[name] = t;
            return t;
        }

        public static Texture2D Sand => Make("tex_sand", 256, (u, v) =>
            0.82f + 0.12f * Fbm(u, v, 4, 4, 11) + 0.06f * (Noise(u, v, 64, 3) - 0.5f) + 0.04f * Mathf.Sin((u + Fbm(u, v, 2, 2, 5) * 0.6f) * Mathf.PI * 2 * 6));

        public static Texture2D Rock => Make("tex_rock", 256, (u, v) =>
        {
            float n = Fbm(u, v, 4, 5, 21);
            float cracks = Mathf.Abs(Fbm(u, v, 3, 3, 33) - 0.5f) < 0.02f ? 0.75f : 1f;
            return (0.62f + 0.45f * n) * cracks;
        });

        public static Texture2D Wood => Make("tex_wood", 256, (u, v) =>
        {
            float plank = Mathf.Repeat(v * 4f, 1f);
            float seam = plank < 0.04f || plank > 0.96f ? 0.55f : 1f;
            int idx = Mathf.FloorToInt(v * 4f);
            float grain = Noise(u * 1f, v * 0.12f + idx * 0.25f, 16, 41 + idx);
            float streak = 0.5f + 0.5f * Mathf.Sin((u * 40f + grain * 6f));
            return (0.72f + 0.14f * grain + 0.1f * streak + 0.06f * Hash(idx, 0, 9)) * seam;
        });

        /// <summary>
        /// One square floor tile per texture repeat: ~0.9 surface, thin ~0.55 grout lines on the tile borders,
        /// a faint bevel highlight just inside the grout and very subtle surface variation.
        /// </summary>
        public static Texture2D Grid => Make("tex_grid", 256, (u, v) =>
        {
            float du = Mathf.Min(u, 1f - u), dv = Mathf.Min(v, 1f - v);
            float d = Mathf.Min(du, dv);                       // distance to the nearest tile border (0..0.5)
            const float grout = 0.012f, soft = 0.006f;
            float g = Mathf.Clamp01((d - grout) / soft);       // 0 in grout, 1 on the tile
            float surface = 0.9f + 0.025f * (Fbm(u, v, 2, 3, 71) - 0.5f) + 0.012f * (Noise(u, v, 32, 72) - 0.5f);
            // bevel: lighter on the top/left inner edge, slightly darker on the bottom/right
            float edge = Mathf.Clamp01(1f - (d - grout) / 0.03f);
            float side = du < dv ? (u < 0.5f ? 1f : -1f) : (v > 0.5f ? 1f : -1f);
            surface += edge * 0.03f * side;
            return Mathf.Lerp(0.55f, surface, g);
        });
    }
}
