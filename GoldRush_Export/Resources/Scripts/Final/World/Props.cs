using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoldRush
{
    public enum Accent { None, Red, Orange, Teal }

    /// <summary>
    /// Procedural, primitive-built "grey-box" sci-fi props: chunky light-grey blocks with lighter bevelled top slabs,
    /// accent blocks, pillars, barriers, lamp posts, terminals and emissive floor strips.
    /// Materials that change between day and night are collected here and moved
    /// into the <see cref="DayNightTint"/> on the level root (see <see cref="ApplyTints"/>); lamp lights get a <see cref="NightLight"/>.
    /// Everything works in edit mode (only DestroyImmediate is used).
    /// </summary>
    public static class Props
    {
        // ------------------------------------------------------------ palette (day / night)
        public static readonly Color FloorDay = new Color(0.86f, 0.87f, 0.9f), FloorNight = new Color(0.13f, 0.17f, 0.32f);
        public static readonly Color PadDay = new Color(0.7f, 0.72f, 0.78f), PadNight = new Color(0.09f, 0.12f, 0.25f);
        public static readonly Color WallDay = new Color(0.8f, 0.82f, 0.86f), WallNight = new Color(0.32f, 0.36f, 0.5f);
        public static readonly Color WallTopDay = new Color(0.93f, 0.94f, 0.97f), WallTopNight = new Color(0.44f, 0.5f, 0.68f);
        public static readonly Color TrimDay = new Color(0.42f, 0.45f, 0.52f), TrimNight = new Color(0.15f, 0.18f, 0.3f);
        public static readonly Color OuterDay = new Color(0.6f, 0.63f, 0.69f), OuterNight = new Color(0.07f, 0.09f, 0.19f);
        public static readonly Color TowerDay = new Color(0.7f, 0.73f, 0.79f), TowerNight = new Color(0.19f, 0.22f, 0.35f);
        public static readonly Color TowerTopDay = new Color(0.83f, 0.85f, 0.9f), TowerTopNight = new Color(0.27f, 0.31f, 0.47f);

        public static readonly Color RedDay = new Color(0.93f, 0.3f, 0.3f), RedNight = new Color(0.62f, 0.17f, 0.26f);
        public static readonly Color RedTopDay = new Color(1f, 0.47f, 0.44f), RedTopNight = new Color(0.76f, 0.26f, 0.33f);
        public static readonly Color OrangeDay = new Color(1f, 0.6f, 0.22f), OrangeNight = new Color(0.72f, 0.38f, 0.2f);
        public static readonly Color OrangeTopDay = new Color(1f, 0.74f, 0.42f), OrangeTopNight = new Color(0.85f, 0.5f, 0.28f);
        public static readonly Color TealDay = new Color(0.2f, 0.74f, 0.74f), TealNight = new Color(0.1f, 0.42f, 0.52f);
        public static readonly Color TealTopDay = new Color(0.42f, 0.87f, 0.85f), TealTopNight = new Color(0.16f, 0.56f, 0.63f);

        public static readonly Color GlowCyan = new Color(0.3f, 0.85f, 1f);
        public static readonly Color GlowOrange = new Color(1f, 0.55f, 0.2f);
        public static readonly Color GlowRed = new Color(1f, 0.22f, 0.2f);
        public static readonly Color GlowGreen = new Color(0.3f, 1f, 0.45f);
        public static readonly Color LampColor = new Color(1f, 0.88f, 0.7f);

        // ------------------------------------------------------------ day/night tint collection
        // Materials created while building are collected here; LevelBuilder hands them to the
        // DayNightTint component on the level / diorama root (works in edit mode and survives scene saves).
        struct PendingTint { public Material m; public Color day, night; }
        static readonly List<PendingTint> pendingTints = new List<PendingTint>();
        static readonly HashSet<Material> pendingSet = new HashSet<Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pendingTints.Clear(); pendingSet.Clear(); }

        /// <summary>Collects a material for day/night colour blending (deduplicated) and sets its day colour.</summary>
        public static void QueueTint(Material m, Color day, Color night)
        {
            if (!m || !pendingSet.Add(m)) return;
            m.SetColor("_BaseColor", day);
            pendingTints.Add(new PendingTint { m = m, day = day, night = night });
        }

        /// <summary>Moves every collected tint into the given DayNightTint component and clears the list.</summary>
        public static void ApplyTints(DayNightTint tint)
        {
            if (tint)
                for (int i = 0; i < pendingTints.Count; i++)
                    if (pendingTints[i].m) tint.Add(pendingTints[i].m, pendingTints[i].day, pendingTints[i].night);
            pendingTints.Clear();
            pendingSet.Clear();
        }

        /// <summary>Makes a light night-only: it keeps its night intensity, the NightLight component fades it at runtime.</summary>
        public static void MakeNightLight(Light l, float nightIntensity)
        {
            if (!l) return;
            l.intensity = nightIntensity;
            var n = l.GetComponent<NightLight>();
            if (!n) n = l.gameObject.AddComponent<NightLight>();
            n.nightIntensity = nightIntensity;
        }

        /// <summary>Lit material whose colour blends between day and night.</summary>
        public static Material Tinted(Color day, Color night, float smooth = 0.2f, Texture tex = null, float tiling = 1f)
        {
            var m = Mats.Lit(day, smooth, 0f, tex, tiling);
            QueueTint(m, day, night);
            return m;
        }

        // ------------------------------------------------------------ materials
        public static Material Floor(float tiling) => Tinted(FloorDay, FloorNight, 0.35f, ProcTex.Grid, tiling);
        public static Material OuterFloor(float tiling) => Tinted(OuterDay, OuterNight, 0.2f, ProcTex.Grid, tiling);
        public static Material WallMat => Tinted(WallDay, WallNight, 0.15f);
        public static Material WallTopMat => Tinted(WallTopDay, WallTopNight, 0.2f);
        public static Material TrimMat => Tinted(TrimDay, TrimNight, 0.3f);
        public static Material PadMat => Tinted(PadDay, PadNight, 0.4f);
        public static Material TowerMat => Tinted(TowerDay, TowerNight, 0.1f);
        public static Material TowerTopMat => Tinted(TowerTopDay, TowerTopNight, 0.15f);

        public static Material AccentBody(Accent a)
        {
            switch (a)
            {
                case Accent.Red: return Tinted(RedDay, RedNight, 0.3f);
                case Accent.Orange: return Tinted(OrangeDay, OrangeNight, 0.3f);
                case Accent.Teal: return Tinted(TealDay, TealNight, 0.3f);
                default: return WallMat;
            }
        }

        public static Material AccentTop(Accent a)
        {
            switch (a)
            {
                case Accent.Red: return Tinted(RedTopDay, RedTopNight, 0.35f);
                case Accent.Orange: return Tinted(OrangeTopDay, OrangeTopNight, 0.35f);
                case Accent.Teal: return Tinted(TealTopDay, TealTopNight, 0.35f);
                default: return WallTopMat;
            }
        }

        /// <summary>Emissive glow material (blooms, brightest at night thanks to the lower bloom threshold).</summary>
        public static Material Glow(Color c, float intensity = 2.2f) => Mats.Emissive(Color.Lerp(c, Color.white, 0.25f), c, intensity, 0.5f, 0f);

        public static Accent RandomAccent(Rng r)
        {
            float v = r.Value;
            return v < 0.34f ? Accent.Red : v < 0.67f ? Accent.Orange : Accent.Teal;
        }

        // ------------------------------------------------------------ helpers
        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 scale, Material m, bool collider, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(t);
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c) UnityEngine.Object.DestroyImmediate(c);
            }
            var tr = go.transform;
            tr.SetParent(parent, false);
            tr.localPosition = localPos;
            tr.localRotation = Quaternion.Euler(euler);
            tr.localScale = scale;
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        public static Transform Root(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        static void NoShadow(GameObject g)
        {
            var rd = g.GetComponent<Renderer>();
            if (rd) rd.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Removes every collider under go immediately (for decor-only copies of solid props).</summary>
        public static void StripColliders(GameObject go)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) UnityEngine.Object.DestroyImmediate(cols[i]);
        }

        // ------------------------------------------------------------ blocks

        /// <summary>
        /// Chunky block: body + slightly inset, lighter top slab (reads as a bevelled top face).
        /// pos is the centre of the block's footprint on the floor.
        /// </summary>
        public static GameObject Block(Transform parent, Vector3 pos, Vector3 size, float yaw = 0f,
            Material body = null, Material top = null, bool collider = true)
        {
            if (!body) body = WallMat;
            if (!top) top = WallTopMat;
            var root = Root("Block", parent, pos, yaw);
            float capH = Mathf.Min(0.14f, size.y * 0.2f);
            float inset = Mathf.Min(0.09f, Mathf.Min(size.x, size.z) * 0.08f);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, (size.y - capH) * 0.5f, 0f), new Vector3(size.x, size.y - capH, size.z), body, collider).name = "Body";
            Prim(PrimitiveType.Cube, root, new Vector3(0f, size.y - capH * 0.5f, 0f), new Vector3(size.x - inset, capH, size.z - inset), top, collider).name = "Top";
            return root.gameObject;
        }

        public static GameObject AccentBlock(Transform parent, Vector3 pos, Vector3 size, Accent a, float yaw = 0f, bool collider = true)
            => Block(parent, pos, size, yaw, AccentBody(a), AccentTop(a), collider);

        /// <summary>Straight wall from `from` to `to` (floor points), split into ~segLen blocks so the top shows seams.</summary>
        public static GameObject Wall(Transform parent, Vector3 from, Vector3 to, float height, float thickness,
            float segLen = 4f, bool collider = true, Material body = null, Material top = null)
        {
            var root = Root("Wall", parent, Vector3.zero);
            Vector3 d = to - from; d.y = 0f;
            float len = d.magnitude;
            if (len < 0.01f) return root.gameObject;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / segLen));
            float step = len / n;
            Vector3 dir = d / len;
            for (int i = 0; i < n; i++)
            {
                Vector3 c = from + dir * (step * (i + 0.5f));
                c.y = from.y;
                Block(root, c, new Vector3(thickness, height, step), yaw, body, top, collider);
            }
            return root.gameObject;
        }

        /// <summary>Square pillar with a trim base and a glowing band under its cap.</summary>
        public static GameObject Pillar(Transform parent, Vector3 pos, float height, float width = 1f, Accent a = Accent.None, bool collider = true)
        {
            var root = Root("Pillar", parent, pos);
            var b = Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.11f, 0f), new Vector3(width + 0.24f, 0.22f, width + 0.24f), TrimMat, false);
            NoShadow(b);
            Block(root, Vector3.zero, new Vector3(width, height, width), 0f, AccentBody(a), AccentTop(a), collider);
            var band = Prim(PrimitiveType.Cube, root, new Vector3(0f, height - 0.45f, 0f), new Vector3(width + 0.04f, 0.1f, width + 0.04f),
                Glow(a == Accent.Orange || a == Accent.Red ? GlowOrange : GlowCyan, 2f), false);
            NoShadow(band);
            return root.gameObject;
        }

        /// <summary>Low cover barrier (~0.95 m) with an accent top.</summary>
        public static GameObject Barrier(Transform parent, Vector3 pos, float length, float yaw, Accent a = Accent.Orange, bool collider = true)
        {
            var root = Root("Barrier", parent, pos, yaw);
            Block(root, Vector3.zero, new Vector3(length, 0.95f, 0.6f), 0f, WallMat, AccentTop(a), collider);
            // feet
            for (int s = -1; s <= 1; s += 2)
            {
                var f = Prim(PrimitiveType.Cube, root, new Vector3(s * (length * 0.5f - 0.35f), 0.08f, 0f), new Vector3(0.5f, 0.16f, 0.9f), TrimMat, false);
                NoShadow(f);
            }
            return root.gameObject;
        }

        /// <summary>Thin post with a glowing cap; its point light only shines at night.</summary>
        public static Light LampPost(Transform parent, Vector3 pos, float nightIntensity = 2.4f, bool collider = true)
        {
            var root = Root("LampPost", parent, pos);
            var trim = TrimMat;
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.14f, 0f), new Vector3(0.6f, 0.28f, 0.6f), trim, collider);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.6f, 0f), new Vector3(0.22f, 2.9f, 0.22f), trim, collider);
            var cap = Prim(PrimitiveType.Cube, root, new Vector3(0f, 3.2f, 0f), new Vector3(0.5f, 0.32f, 0.5f), Glow(LampColor, 2.6f), false);
            NoShadow(cap);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 3.41f, 0f), new Vector3(0.64f, 0.1f, 0.64f), WallTopMat, false);

            var l = new GameObject("LampLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.localPosition = new Vector3(0f, 2.9f, 0f);
            l.type = LightType.Point;
            l.color = LampColor;
            l.range = 9f;
            l.shadows = LightShadows.None;
            MakeNightLight(l, nightIntensity);
            return l;
        }

        /// <summary>Console box with an emissive screen on its local -Z face.</summary>
        public static GameObject Terminal(Transform parent, Vector3 pos, float yaw, Color screen, bool collider = true)
        {
            var root = Root("Terminal", parent, pos, yaw);
            var trim = TrimMat;
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 0.75f), trim, collider);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.95f, 0.18f), new Vector3(1.1f, 1.0f, 0.4f), WallMat, collider);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.48f, 0.18f), new Vector3(1.04f, 0.08f, 0.36f), WallTopMat, false);
            var s = Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.05f, -0.03f), new Vector3(0.84f, 0.52f, 0.03f), Glow(screen, 1.8f), false);
            NoShadow(s);
            var k = Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.92f, -0.22f), new Vector3(0.8f, 0.05f, 0.3f), WallTopMat, false, new Vector3(-14f, 0f, 0f));
            NoShadow(k);
            var led = Prim(PrimitiveType.Cube, root, new Vector3(0.38f, 0.6f, -0.38f), new Vector3(0.12f, 0.12f, 0.02f), Glow(GlowGreen, 2.5f), false);
            NoShadow(led);
            return root.gameObject;
        }

        // ------------------------------------------------------------ floor decor (no colliders, no shadows)

        public static GameObject FloorStrip(Transform parent, Vector3 from, Vector3 to, float width, Material glow)
        {
            Vector3 d = to - from; d.y = 0f;
            float len = d.magnitude;
            float yaw = len > 0.001f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : 0f;
            Vector3 c = (from + to) * 0.5f;
            c.y = 0.012f;
            var g = Prim(PrimitiveType.Cube, parent, c, new Vector3(width, 0.024f, len), glow, false, new Vector3(0f, yaw, 0f));
            g.name = "FloorStrip";
            NoShadow(g);
            return g;
        }

        /// <summary>Square floor pad: darker plate with a glowing border.</summary>
        public static GameObject FloorPad(Transform parent, Vector3 center, float size, Material glow, bool plate = true)
        {
            var root = Root("FloorPad", parent, center);
            if (plate)
            {
                var p = Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.006f, 0f), new Vector3(size - 0.1f, 0.012f, size - 0.1f), PadMat, false);
                NoShadow(p);
            }
            float h = size * 0.5f, w = 0.14f;
            FloorStrip(root, new Vector3(-h, 0f, -h + w * 0.5f), new Vector3(h, 0f, -h + w * 0.5f), w, glow);
            FloorStrip(root, new Vector3(-h, 0f, h - w * 0.5f), new Vector3(h, 0f, h - w * 0.5f), w, glow);
            FloorStrip(root, new Vector3(-h + w * 0.5f, 0f, -h + w), new Vector3(-h + w * 0.5f, 0f, h - w), w, glow);
            FloorStrip(root, new Vector3(h - w * 0.5f, 0f, -h + w), new Vector3(h - w * 0.5f, 0f, h - w), w, glow);
            return root.gameObject;
        }

        /// <summary>
        /// Square grid floor: tiled top quad (one texture repeat = one 2 m tile, grout on even coordinates when
        /// size is a multiple of 4) on a plinth whose side is visible above the darker outer ground.
        /// </summary>
        public static GameObject GridPlatform(Transform parent, float size, float plinthDepth)
        {
            var root = Root("Platform", parent, Vector3.zero);
            var top = Prim(PrimitiveType.Quad, root, Vector3.zero, new Vector3(size, size, 1f), Floor(size / 2f), false, new Vector3(90f, 0f, 0f));
            top.name = "Floor";
            NoShadow(top);
            var plinth = Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.02f - plinthDepth * 0.5f, 0f), new Vector3(size, plinthDepth, size), TrimMat, false);
            plinth.name = "Plinth";
            NoShadow(plinth);
            // light lip along the edge
            float h = size * 0.5f;
            var lip = WallTopMat;
            for (int i = 0; i < 4; i++)
            {
                bool x = i < 2;
                float s = (i & 1) == 0 ? -1f : 1f;
                var g = Prim(PrimitiveType.Cube, root, x ? new Vector3(0f, -0.1f, s * (h + 0.04f)) : new Vector3(s * (h + 0.04f), -0.1f, 0f),
                    x ? new Vector3(size + 0.16f, 0.2f, 0.08f) : new Vector3(0.08f, 0.2f, size + 0.16f), lip, false);
                NoShadow(g);
            }
            return root.gameObject;
        }

        /// <summary>Big flat ground plane around everything (4 m tiles, darker).</summary>
        public static GameObject OuterGround(Transform parent, float size, float y)
        {
            var g = Prim(PrimitiveType.Quad, parent, new Vector3(0f, y, 0f), new Vector3(size, size, 1f), OuterFloor(size / 4f), false, new Vector3(90f, 0f, 0f));
            g.name = "OuterGround";
            NoShadow(g);
            return g;
        }

        // ------------------------------------------------------------ outside-the-arena scenery (no colliders)

        /// <summary>Distant tower block with optional glowing bands.</summary>
        public static GameObject Tower(Transform parent, Vector3 pos, Vector3 size, float yaw, Rng r)
        {
            bool accent = r.Chance(0.12f);
            Material body = accent ? AccentBody(RandomAccent(r)) : TowerMat;
            var g = Block(parent, pos, size, yaw, body, TowerTopMat, false);
            g.name = "Tower";
            var t = g.transform;
            if (size.y > 5f && r.Chance(0.65f))
            {
                int bands = size.y > 14f ? r.Int(1, 3) : 1;
                var glow = Glow(r.Chance(0.7f) ? GlowCyan : GlowOrange, 1.6f);
                for (int i = 0; i < bands; i++)
                {
                    float y = size.y * (bands == 1 ? r.Range(0.55f, 0.8f) : 0.45f + i * 0.3f);
                    Prim(PrimitiveType.Cube, t, new Vector3(0f, y, 0f), new Vector3(size.x + 0.08f, 0.28f, size.z + 0.08f), glow, false);
                }
            }
            if (size.y > 8f && r.Chance(0.5f))
            {
                // stepped top
                float s = r.Range(0.45f, 0.7f), h = r.Range(1.5f, 4f);
                Block(t, new Vector3(r.Range(-0.15f, 0.15f) * size.x, size.y, r.Range(-0.15f, 0.15f) * size.z),
                    new Vector3(size.x * s, h, size.z * s), 0f, TowerMat, TowerTopMat, false);
            }
            Mats.NoShadows(g);
            return g;
        }

        /// <summary>Low wide block on the outer ground (forms a broken outer rim).</summary>
        public static GameObject RimBlock(Transform parent, Vector3 pos, Vector3 size, float yaw)
        {
            var g = Block(parent, pos, size, yaw, TowerMat, TowerTopMat, false);
            g.name = "Rim";
            Mats.NoShadows(g);
            return g;
        }

        // ------------------------------------------------------------ perimeter

        /// <summary>
        /// Light-grey block wall whose inner face sits on the arena edge (±half), with a gap of `gateGap`
        /// centred on the +Z side, corner pillars, pilasters and an invisible blocker behind the gate gap.
        /// </summary>
        public static void PerimeterWall(Transform parent, float half, float gateGap, float height = 2.3f)
        {
            var root = Root("PerimeterWall", parent, Vector3.zero);
            const float t = 1f;
            float o = half + t * 0.5f, e = half + t, g = gateGap * 0.5f;
            Wall(root, new Vector3(-e, 0f, -o), new Vector3(e, 0f, -o), height, t);
            Wall(root, new Vector3(-e, 0f, o), new Vector3(-g, 0f, o), height, t);
            Wall(root, new Vector3(g, 0f, o), new Vector3(e, 0f, o), height, t);
            Wall(root, new Vector3(-o, 0f, -half), new Vector3(-o, 0f, half), height, t);
            Wall(root, new Vector3(o, 0f, -half), new Vector3(o, 0f, half), height, t);

            // corner pillars
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -o : o, z = (i & 2) == 0 ? -o : o;
                Block(root, new Vector3(x, 0f, z), new Vector3(1.7f, height + 0.9f, 1.7f), 0f, WallMat, WallTopMat, true);
            }
            // pilasters every ~10 m (kept clear of the gate frame)
            int n = Mathf.Max(1, Mathf.RoundToInt(2f * half / 10f));
            float step = 2f * half / n;
            for (int i = 1; i < n; i++)
            {
                float a = -half + i * step;
                Vector3 ps = new Vector3(1.4f, height + 0.4f, 1.4f);
                Block(root, new Vector3(a, 0f, -o), ps, 0f, WallMat, WallTopMat, true);
                Block(root, new Vector3(-o, 0f, a), ps, 0f, WallMat, WallTopMat, true);
                Block(root, new Vector3(o, 0f, a), ps, 0f, WallMat, WallTopMat, true);
                if (Mathf.Abs(a) > g + 2.5f) Block(root, new Vector3(a, 0f, o), ps, 0f, WallMat, WallTopMat, true);
            }

            // invisible blocker behind the gate opening
            var b = new GameObject("GateBlocker");
            b.transform.SetParent(root, false);
            b.transform.localPosition = new Vector3(0f, 2.5f, half + 1.25f);
            b.AddComponent<BoxCollider>().size = new Vector3(gateGap + 2f, 5f, 1f);
        }
    }
}
