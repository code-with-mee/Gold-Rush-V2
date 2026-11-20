using UnityEngine;

namespace GoldRush
{
    /// <summary>Instantiates the imported character models and applies cosmetics.</summary>
    public static class CharacterFactory
    {
        /// <summary>Instantiate a character under parent: assigns its animator controller,
        /// scales it to def.height and puts its feet on the parent's origin.</summary>
        public static Animator BuildModel(CharacterDef def, Transform parent)
        {
            var m = Object.Instantiate(def.model, parent);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one;
            var anim = m.GetComponentInChildren<Animator>();
            if (!anim) anim = m.AddComponent<Animator>();
            anim.runtimeAnimatorController = def.controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = Bounds(rs);
                if (b.size.y > 0.05f) m.transform.localScale = Vector3.one * (def.height / b.size.y);
                b = Bounds(rs);
                m.transform.position += Vector3.up * (parent.position.y - b.min.y);
            }
            foreach (var smr in m.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            return anim;
        }

        static Bounds Bounds(Renderer[] rs)
        {
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <summary>Coloured point light that makes a character "glow" at night (neon toy look).</summary>
        public static Light AddGlow(Transform parent, Color color, float height, float intensity = 2.2f)
        {
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = new Vector3(0, height * 0.55f, 0.35f);
            l.type = LightType.Point;
            l.color = color;
            l.range = height * 1.6f + 1.5f;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        public static Transform Head(Animator a)
        {
            if (!a) return null;
            if (a.isHuman) { var h = a.GetBoneTransform(HumanBodyBones.Head); if (h) return h; }
            foreach (var t in a.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("head") && !n.Contains("top") && !n.Contains("end")) return t;
            }
            return null;
        }
    }

    /// <summary>Player colours and hats (shop items) for the Hero model.</summary>
    public static class Cosmetics
    {
        public const string HatName = "GR_Hat";

        static string BaseName(Material m) => m ? m.name.Replace(" (Instance)", "") : "";

        /// <summary>Recolours the hero's body materials ("Main", "Main2", "Main_Light") and sets the hat.</summary>
        public static void Apply(Animator model, CharacterDef def, string colorId, string hatId, bool allowLight)
        {
            if (!model) return;
            var item = Catalog.Find(colorId) ?? Catalog.Colors[0];
            Color c = item.color;
            bool gold = item.id == "color_gold";
            Material main = gold ? Mats.Emissive(c, new Color(1f, 0.6f, 0.1f), 0.35f, 0.85f, 0.9f) : Mats.Lit(c, 0.45f, 0.05f);
            Material dark = Mats.Lit(UIKit.Darken(c, 0.7f), 0.4f, gold ? 0.6f : 0.05f);
            Material light = Mats.Lit(Color.Lerp(c, Color.white, 0.55f), 0.4f, 0f);

            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    // remember the imported material name so recolouring works repeatedly (shop preview)
                    string n = r.GetComponent<OriginalMaterials>() is OriginalMaterials om && i < om.names.Length
                        ? om.names[i] : BaseName(mats[i]);
                    if (n == "Main") mats[i] = main;
                    else if (n == "Main2") mats[i] = dark;
                    else if (n == "Main_Light") mats[i] = light;
                }
                if (!r.GetComponent<OriginalMaterials>())
                {
                    var om = r.gameObject.AddComponent<OriginalMaterials>();
                    om.names = System.Array.ConvertAll(r.sharedMaterials, BaseName);
                }
                r.sharedMaterials = mats;
            }
            SetHat(model, def, hatId, allowLight);
        }

        public static void SetHat(Animator model, CharacterDef def, string hatId, bool allowLight)
        {
            var old = FindHat(model.transform);
            if (old) Object.DestroyImmediate(old.gameObject);
            if (string.IsNullOrEmpty(hatId) || hatId == "hat_none") return;
            var head = CharacterFactory.Head(model);
            Transform root = model.transform;
            Transform anchor = head ? head : root;

            var hat = new GameObject(HatName).transform;
            Build(hat, hatId, allowLight);
            hat.localScale = Vector3.one * def.hatScale;
            Vector3 basePos = root.parent ? root.parent.position : root.position;
            float y = basePos.y + def.height * def.hatHeight;
            hat.position = new Vector3(anchor.position.x, y, anchor.position.z);
            hat.rotation = Quaternion.LookRotation(root.forward, Vector3.up);
            hat.SetParent(anchor, true);
            Mats.NoShadows(hat.gameObject);
        }

        static Transform FindHat(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == HatName) return t;
            return null;
        }

        static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static void Build(Transform hat, string id, bool allowLight)
        {
            switch (id)
            {
                case "hat_cowboy":
                {
                    var leather = Mats.Lit(new Color(0.55f, 0.33f, 0.18f), 0.25f);
                    var band = Mats.Lit(new Color(0.2f, 0.12f, 0.07f), 0.3f);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.0f, 0), new Vector3(0.48f, 0.012f, 0.44f), leather);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.07f, 0), new Vector3(0.24f, 0.07f, 0.22f), leather);
                    Part(PrimitiveType.Sphere, hat, new Vector3(0, 0.135f, 0), new Vector3(0.24f, 0.06f, 0.22f), leather);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.025f, 0), new Vector3(0.245f, 0.015f, 0.225f), band);
                    break;
                }
                case "hat_miner":
                {
                    var yellow = Mats.Lit(new Color(1f, 0.78f, 0.1f), 0.6f);
                    var lampMat = Mats.Emissive(new Color(1f, 1f, 0.85f), new Color(1f, 0.95f, 0.7f), 2.5f);
                    var dark = Mats.Lit(new Color(0.15f, 0.15f, 0.15f), 0.4f);
                    Part(PrimitiveType.Sphere, hat, new Vector3(0, 0.0f, 0), new Vector3(0.28f, 0.24f, 0.3f), yellow);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, -0.02f, 0.02f), new Vector3(0.31f, 0.01f, 0.34f), yellow);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.05f, 0.14f), new Vector3(0.07f, 0.025f, 0.07f), dark, new Vector3(90, 0, 0));
                    Part(PrimitiveType.Sphere, hat, new Vector3(0, 0.05f, 0.165f), new Vector3(0.06f, 0.06f, 0.02f), lampMat);
                    if (allowLight)
                    {
                        var l = new GameObject("Lamp").AddComponent<Light>();
                        l.transform.SetParent(hat, false);
                        l.transform.localPosition = new Vector3(0, 0.05f, 0.18f);
                        l.type = LightType.Spot;
                        l.spotAngle = 55;
                        l.range = 12;
                        l.intensity = 3f;
                        l.color = new Color(1f, 0.93f, 0.75f);
                        l.shadows = LightShadows.None;
                    }
                    break;
                }
                case "hat_top":
                {
                    var black = Mats.Lit(new Color(0.1f, 0.1f, 0.12f), 0.5f);
                    var red = Mats.Lit(new Color(0.75f, 0.12f, 0.15f), 0.4f);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0f, 0), new Vector3(0.34f, 0.01f, 0.34f), black);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.13f, 0), new Vector3(0.22f, 0.13f, 0.22f), black);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.04f, 0), new Vector3(0.225f, 0.03f, 0.225f), red);
                    break;
                }
                case "hat_crown":
                {
                    var gold = Mats.Emissive(new Color(1f, 0.8f, 0.25f), new Color(1f, 0.6f, 0.1f), 0.6f, 0.9f, 1f);
                    var ruby = Mats.Emissive(new Color(0.9f, 0.1f, 0.2f), new Color(1f, 0.1f, 0.2f), 1.2f);
                    var sapphire = Mats.Emissive(new Color(0.2f, 0.4f, 1f), new Color(0.2f, 0.4f, 1f), 1.2f);
                    Part(PrimitiveType.Cylinder, hat, new Vector3(0, 0.03f, 0), new Vector3(0.24f, 0.04f, 0.24f), gold);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2 / 5f;
                        var p = new Vector3(Mathf.Sin(a) * 0.105f, 0.1f, Mathf.Cos(a) * 0.105f);
                        Part(PrimitiveType.Cube, hat, p, new Vector3(0.05f, 0.09f, 0.05f), gold, new Vector3(0, a * Mathf.Rad2Deg + 45, 0));
                        Part(PrimitiveType.Sphere, hat, p + Vector3.up * 0.06f, Vector3.one * 0.035f, i % 2 == 0 ? ruby : sapphire);
                    }
                    break;
                }
            }
        }
    }
}
