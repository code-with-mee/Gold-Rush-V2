using System.Collections.Generic;
using UnityEngine;

namespace GoldRush
{
    /// <summary>
    /// A name patch ("ITSTEP") worn on a character's chest, like a logo printed on a shirt.
    /// The text is drawn with a tiny built-in pixel font into a texture, so it needs no font
    /// assets, stays crisp, and is lit/fogged like the rest of the scene.
    /// </summary>
    public static class Badge
    {
        public const string DefaultText = "ITSTEP";

        // 5x7 pixel glyphs ('#' = ink). Add letters here to support other words.
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        };

        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        /// <summary>White text on a dark patch, as a texture (crisp point-filtered pixels).</summary>
        public static Texture2D TextTexture(string text)
        {
            if (cache.TryGetValue(text, out var t) && t) return t;
            const int scale = 8, pad = 2;
            int cols = text.Length * 6 - 1 + pad * 2, rows = 7 + pad * 2;
            int w = cols * scale, h = rows * scale;
            t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "badge_" + text, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            var bg = new Color32(28, 32, 52, 255);
            var ink = new Color32(255, 255, 255, 255);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            for (int c = 0; c < text.Length; c++)
            {
                if (!Glyphs.TryGetValue(char.ToUpperInvariant(text[c]), out var g)) continue;
                for (int gy = 0; gy < 7; gy++)
                    for (int gx = 0; gx < 5; gx++)
                    {
                        if (g[gy][gx] != '#') continue;
                        int x0 = (pad + c * 6 + gx) * scale, y0 = (pad + (6 - gy)) * scale; // texture y is bottom-up
                        for (int y = 0; y < scale; y++)
                            for (int x = 0; x < scale; x++)
                                px[(y0 + y) * w + x0 + x] = ink;
                    }
            }
            t.SetPixels32(px);
            t.Apply(false);
            cache[text] = t;
            return t;
        }

        /// <summary>
        /// Puts a name patch on the front of a character's body and parents it to the chest/body bone
        /// so it moves with the animation.
        /// </summary>
        /// <param name="heightFraction">How high on the body (0 = feet, 1 = top of head).</param>
        public static Transform Attach(Animator model, CharacterDef def, float heightFraction, Color accent, string text = DefaultText)
        {
            if (!model) return null;
            Transform root = model.transform.parent ? model.transform.parent : model.transform;

            // how far the body sticks out in front of the root, measured from the mesh bounds
            float front = 0.3f;
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                Vector3 local = root.InverseTransformPoint(b.center);
                front = local.z + Mathf.Abs(root.InverseTransformVector(b.extents).z) * 0.92f;
            }

            float width = Mathf.Clamp(def.height * 0.38f, 0.34f, 0.85f);
            var tex = TextTexture(text);
            float height = width * tex.height / tex.width;

            var badge = new GameObject("Badge_" + text).transform;
            badge.SetParent(root, false);
            badge.localPosition = new Vector3(0f, def.height * heightFraction, front);
            badge.localRotation = Quaternion.identity;

            // coloured border plate behind the text
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.name = "Plate";
            plate.transform.SetParent(badge, false);
            plate.transform.localScale = new Vector3(width * 1.12f, height * 1.4f, 0.025f);
            plate.GetComponent<Renderer>().sharedMaterial = Mats.Emissive(accent, accent, 0.6f, 0.4f, 0f);

            // the text itself (a quad faces -Z, so turn it to face the character's forward)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Text";
            quad.transform.SetParent(badge, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.015f);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(width, height, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = Mats.Lit(Color.white, 0.3f, 0f, tex, 1f);

            Mats.NoShadows(badge.gameObject);

            var bone = FindBodyBone(model);
            if (bone) badge.SetParent(bone, true);
            return badge;
        }

        static Transform FindBodyBone(Animator model)
        {
            string[] prefer = { "chest", "upperchest", "spine", "torso", "body" };
            var all = model.GetComponentsInChildren<Transform>();
            foreach (var key in prefer)
                foreach (var t in all)
                {
                    string n = t.name.ToLowerInvariant();
                    if (n.Contains(key) && !n.Contains("end") && t.GetComponent<Renderer>() == null) return t;
                }
            return null;
        }
    }
}
