using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace GoldRush
{
    public class LevelLayout
    {
        public Transform root;
        public Transform dynamicRoot;
        public float half;
        public Vector3 spawn;
        public float spawnYaw;
        public ExitGate gate;
        public List<Vector3> coinPoints;
        public List<Vector3> enemyPoints;
        public List<EnemyType> enemyTypes;
        public List<Vector3> guardHomes;
        public List<Vector3> powerUpPoints;
    }

    /// <summary>
    /// Builds the procedural grey-box arena (deterministic from LevelDef.seed) and the menu diorama.
    /// Works in play mode and in edit mode (an editor baker can save the result as real scene objects).
    /// Day/night colours live in a DayNightTint component on the root; night-only lights carry a NightLight.
    ///
    /// Hierarchy: Level / Static (NavMeshSurface: Floor, Walls, Structures, Obstacles, Lamps, ExitGate) / Decor / Scenery / Dynamic
    /// </summary>
    public static class LevelBuilder
    {
        const float GateGap = 5.5f;
        const float WallH = 2.3f;
        const float InnerWallH = 2.2f;
        static NavMeshPath path;

        struct Spot
        {
            public Vector2 p;
            public float rad;
            public Spot(Vector2 p, float rad) { this.p = p; this.rad = rad; }
        }

        class Placed
        {
            public GameObject go;
            public Spot spot;
        }

        /// <summary>Play mode: static-batch the children now. Edit mode: mark them static so Unity batches them in the saved scene.</summary>
        static void MakeStatic(Transform t)
        {
            if (!t) return;
            if (Application.isPlaying) StaticBatchingUtility.Combine(t.gameObject);
            else foreach (var c in t.GetComponentsInChildren<Transform>(true)) c.gameObject.isStatic = true;
        }

        static Transform Child(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static float Snap(float v, float step) => Mathf.Round(v / step) * step;
        static float OneOf(Rng r, params float[] v) => v[r.Int(0, v.Length)];

        // ================================================================ level

        /// <summary>Builds the whole level: geometry + gate + navmesh, then samples the gameplay points.</summary>
        public static LevelLayout Build(LevelDef def)
        {
            var L = BuildGeometry(def);
            SamplePoints(L, def);
            return L;
        }

        /// <summary>Samples coin / enemy / guard / power-up points on the baked NavMesh (deterministic from def.seed).</summary>
        public static void SamplePoints(LevelLayout layout, LevelDef def)
        {
            if (layout == null) return;
            layout.coinPoints ??= new List<Vector3>();
            layout.enemyPoints ??= new List<Vector3>();
            layout.enemyTypes ??= new List<EnemyType>();
            layout.guardHomes ??= new List<Vector3>();
            layout.powerUpPoints ??= new List<Vector3>();
            layout.coinPoints.Clear(); layout.enemyPoints.Clear(); layout.enemyTypes.Clear();
            layout.guardHomes.Clear(); layout.powerUpPoints.Clear();
            ComputePoints(def, layout, new Rng(def.seed * 7919 + 17));
        }

        /// <summary>Builds only the static geometry, the exit gate and the NavMesh (no gameplay points).</summary>
        public static LevelLayout BuildGeometry(LevelDef def)
        {
            Props.ApplyTints(null); // drop leftovers of an interrupted build
            var r = new Rng(def.seed);
            float half = def.size * 0.5f;
            var L = new LevelLayout
            {
                half = half,
                spawn = new Vector3(0f, 0f, -half + 7f), // far enough from the back wall for the camera
                spawnYaw = 0f,
                coinPoints = new List<Vector3>(),
                enemyPoints = new List<Vector3>(),
                enemyTypes = new List<EnemyType>(),
                guardHomes = new List<Vector3>(),
                powerUpPoints = new List<Vector3>(),
            };

            var root = new GameObject("Level").transform;
            var stat = Child("Static", root);       // everything with colliders; NavMeshSurface lives here
            var floor = Child("Floor", stat);
            var walls = Child("Walls", stat);
            var decor = Child("Decor", root);       // glowing floor strips / pads (no colliders)
            var scenery = Child("Scenery", root);   // outside the arena (no colliders)
            var dyn = Child("Dynamic", root);
            L.root = root;
            L.dynamicRoot = dyn;

            // ---- ground: tiled platform (multiple of 4 m so grout lines sit on even coordinates) + outer ground
            float plat = Mathf.Ceil((def.size + 8f) / 4f) * 4f;
            Props.GridPlatform(floor, plat, 0.6f);
            Props.OuterGround(scenery, 480f, -0.6f);
            var gc = new GameObject("GroundCollider");
            gc.transform.SetParent(floor, false);
            gc.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            gc.AddComponent<BoxCollider>().size = new Vector3(2f * half + 2f, 1f, 2f * half + 2f);

            // ---- perimeter + gate
            Props.PerimeterWall(walls, half, GateGap, WallH);
            L.gate = ExitGate.Create(stat, new Vector3(0f, 0f, half));

            var cyan = Props.Glow(Props.GlowCyan, 2f);
            var orange = Props.Glow(Props.GlowOrange, 2f);

            // glowing strips along the inner base of the perimeter
            float si = half - 0.45f, se = half - 1f, gs = GateGap * 0.5f + 1.6f;
            Props.FloorStrip(decor, new Vector3(-se, 0f, -si), new Vector3(se, 0f, -si), 0.12f, cyan);
            Props.FloorStrip(decor, new Vector3(-si, 0f, -se), new Vector3(-si, 0f, se), 0.12f, cyan);
            Props.FloorStrip(decor, new Vector3(si, 0f, -se), new Vector3(si, 0f, se), 0.12f, cyan);
            Props.FloorStrip(decor, new Vector3(-se, 0f, si), new Vector3(-gs, 0f, si), 0.12f, cyan);
            Props.FloorStrip(decor, new Vector3(gs, 0f, si), new Vector3(se, 0f, si), 0.12f, cyan);
            // spawn pad + gate approach lane
            Props.FloorPad(decor, L.spawn, 4f, cyan);
            Props.FloorStrip(decor, new Vector3(-2.2f, 0f, half - 7f), new Vector3(-2.2f, 0f, half - 3.2f), 0.14f, orange);
            Props.FloorStrip(decor, new Vector3(2.2f, 0f, half - 7f), new Vector3(2.2f, 0f, half - 3.2f), 0.14f, orange);

            var spots = new List<Spot>();
            var placed = new List<Placed>();
            Vector2 spawn2 = new Vector2(L.spawn.x, L.spawn.z), gate2 = new Vector2(0f, half);

            // ---- lamp posts (<= 8 night lights)
            var lamps = Child("Lamps", stat);
            float li = half - 1.6f;
            var lampPos = new List<Vector2>
            {
                new Vector2(-li, -li), new Vector2(li, -li), new Vector2(-li, li), new Vector2(li, li),
                new Vector2(-li, 0f), new Vector2(li, 0f), new Vector2(-4.5f, -li), new Vector2(4.5f, -li),
            };
            for (int i = 0; i < lampPos.Count && i < 8; i++)
            {
                Props.LampPost(lamps, new Vector3(lampPos[i].x, 0f, lampPos[i].y));
                spots.Add(new Spot(lampPos[i], 0.6f));
            }

            // ---- larger structures (rooms, L-cover, corridors, T-walls, block clusters)
            var structRoot = Child("Structures", stat);
            for (int i = 0; i < def.bigRocks; i++)
            {
                for (int a = 0; a < 80; a++)
                {
                    float shrink = 1f - a / 80f * 0.45f;
                    if (!PlanStructure(r, shrink, out var plan)) continue;
                    float lim = half - 3f - plan.rad;
                    if (lim < 1f) continue;
                    var p = new Vector2(Snap(r.Range(-lim, lim), 2f), Snap(r.Range(-lim, lim), 2f));
                    if (Mathf.Abs(p.x) > lim || Mathf.Abs(p.y) > lim) continue;
                    if (!Free(p, plan.rad, spots, spawn2, gate2, 5f + plan.rad)) continue;
                    float yaw = r.Int(0, 4) * 90f;
                    var go = BuildStructure(structRoot, new Vector3(p.x, 0f, p.y), yaw, plan, r, cyan, orange);
                    var s = new Spot(p, plan.rad);
                    spots.Add(s);
                    placed.Add(new Placed { go = go, spot = s });
                    break;
                }
            }

            // ---- small obstacles
            var obsRoot = Child("Obstacles", stat);
            float plim = half - 2f;
            for (int i = 0; i < def.props; i++)
            {
                float w = r.Value;
                int kind = w < 0.26f ? 0 : w < 0.38f ? 1 : w < 0.53f ? 2 : w < 0.7f ? 3 : w < 0.82f ? 4 : w < 0.9f ? 5 : 6;
                // dimensions first (keeps the random stream deterministic per kind)
                float sx = OneOf(r, 1f, 1.5f, 2f, 2f, 3f);
                float sz = r.Chance(0.6f) ? sx : OneOf(r, 1f, 1.5f, 2f, 3f);
                float sh = OneOf(r, 1f, 1.5f, 2f, 2.5f, 3f);
                float rad;
                switch (kind)
                {
                    case 0: rad = 0.5f * Mathf.Sqrt(sx * sx + sz * sz); break;
                    case 1: rad = 1.45f; break;
                    case 2: rad = 0.85f; break;
                    case 3: rad = (sx >= 2f ? 4f : 3f) * 0.5f + 0.1f; break;
                    case 4: rad = 0.75f * 1.414f; break;
                    case 5: rad = 0.8f; break;
                    default: rad = 1.7f; break;
                }
                float lim = plim - rad;
                for (int a = 0; a < 60; a++)
                {
                    var p = new Vector2(Snap(r.Range(-lim, lim), 0.5f), Snap(r.Range(-lim, lim), 0.5f));
                    if (!Free(p, rad, spots, spawn2, gate2, 5f + rad)) continue;
                    var pos = new Vector3(p.x, 0f, p.y);
                    float yaw = r.Int(0, 2) * 90f;
                    GameObject go;
                    switch (kind)
                    {
                        case 0:
                            go = r.Chance(0.15f)
                                ? Props.Block(obsRoot, pos, new Vector3(sx, sh, sz), yaw, Props.WallMat, Props.AccentTop(Props.RandomAccent(r)))
                                : Props.Block(obsRoot, pos, new Vector3(sx, sh, sz), yaw);
                            break;
                        case 1:
                        {
                            var t = Props.Root("Stack", obsRoot, pos, yaw);
                            Props.Block(t, Vector3.zero, new Vector3(2f, 1.4f, 2f));
                            var acc = r.Chance(0.4f) ? Props.RandomAccent(r) : Accent.None;
                            Props.Block(t, new Vector3(r.Range(-0.3f, 0.3f), 1.4f, r.Range(-0.3f, 0.3f)), new Vector3(1.2f, 1f, 1.2f), 0f,
                                Props.AccentBody(acc), Props.AccentTop(acc));
                            go = t.gameObject;
                            break;
                        }
                        case 2: go = Props.Pillar(obsRoot, pos, r.Range(2.6f, 3.4f), 1f, r.Chance(0.25f) ? Accent.Teal : Accent.None); break;
                        case 3: go = Props.Barrier(obsRoot, pos, sx >= 2f ? 4f : 3f, yaw, Props.RandomAccent(r)); break;
                        case 4:
                        {
                            float s = r.Range(1.2f, 1.5f);
                            go = Props.AccentBlock(obsRoot, pos, new Vector3(s, s * r.Range(0.8f, 1.3f), s), Props.RandomAccent(r), yaw);
                            break;
                        }
                        case 5: go = Props.Terminal(obsRoot, pos, r.Int(0, 4) * 90f, r.Chance(0.7f) ? Props.GlowCyan : Props.GlowOrange); break;
                        default:
                        {
                            var t = Props.Root("Pair", obsRoot, pos, yaw);
                            Props.Block(t, new Vector3(-0.6f, 0f, 0f), new Vector3(1.5f, r.Range(2f, 3f), 1.5f));
                            Props.AccentBlock(t, new Vector3(0.75f, 0f, 0.35f), new Vector3(1f, 1f, 1f), Props.RandomAccent(r));
                            go = t.gameObject;
                            break;
                        }
                    }
                    var s2 = new Spot(p, rad);
                    spots.Add(s2);
                    placed.Add(new Placed { go = go, spot = s2 });
                    break;
                }
            }

            // ---- floor pads (decor only)
            int pads = 2 + Mathf.RoundToInt(def.size / 16f);
            for (int i = 0; i < pads; i++)
            {
                bool big = r.Chance(0.4f);
                float size = big ? 4f : 2f;
                for (int a = 0; a < 30; a++)
                {
                    float lim = half - 2f - size;
                    var p = big ? new Vector2(Snap(r.Range(-lim, lim), 2f), Snap(r.Range(-lim, lim), 2f))
                                : new Vector2(Snap(r.Range(-lim, lim) - 1f, 2f) + 1f, Snap(r.Range(-lim, lim) - 1f, 2f) + 1f);
                    if (!FreeDecor(p, size * 0.71f, spots, spawn2, 4f)) continue;
                    Props.FloorPad(decor, new Vector3(p.x, 0f, p.y), size, r.Chance(0.6f) ? cyan : orange);
                    spots.Add(new Spot(p, size * 0.71f - 1.2f)); // flat decor: obstacles never come after, keep pads apart
                    break;
                }
            }

            // ---- outside scenery
            BuildOutside(scenery, half, r);

            // ---- navmesh (+ connectivity guard: remove anything that seals off part of the arena)
            var surface = stat.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            surface.BuildNavMesh();

            if (path == null) path = new NavMeshPath();
            for (int iter = 0; iter < 8; iter++)
            {
                if (!FindSealed(L, out var bad)) break;
                int bi = -1;
                float bd = float.MaxValue;
                var b2 = new Vector2(bad.x, bad.z);
                for (int k = 0; k < placed.Count; k++)
                {
                    float d = (b2 - placed[k].spot.p).magnitude - placed[k].spot.rad;
                    if (d < bd) { bd = d; bi = k; }
                }
                if (bi < 0 || bd > 4f) break; // nothing placed nearby: not caused by our structures
                Object.DestroyImmediate(placed[bi].go);
                placed.RemoveAt(bi);
                Physics.SyncTransforms();
                surface.BuildNavMesh();
            }

            // ---- static batching (gate excluded: its door moves)
            MakeStatic(floor);
            MakeStatic(walls);
            MakeStatic(structRoot);
            MakeStatic(obsRoot);
            MakeStatic(lamps);
            MakeStatic(decor);
            MakeStatic(scenery);

            // day/night colours of every material used above (incl. the gate)
            Props.ApplyTints(root.gameObject.AddComponent<DayNightTint>());
            return L;
        }

        static bool Free(Vector2 p, float rad, List<Spot> spots, Vector2 spawn, Vector2 gate, float clear)
        {
            if ((p - spawn).magnitude < clear) return false;
            if ((p - gate).magnitude < clear) return false;
            for (int i = 0; i < spots.Count; i++)
                if ((p - spots[i].p).magnitude < spots[i].rad + rad + 2.4f) return false;
            return true;
        }

        static bool FreeDecor(Vector2 p, float rad, List<Spot> spots, Vector2 spawn, float clear)
        {
            if ((p - spawn).magnitude < clear + rad) return false;
            for (int i = 0; i < spots.Count; i++)
                if ((p - spots[i].p).magnitude < spots[i].rad + rad + 0.3f) return false;
            return true;
        }

        /// <summary>True if some ground point of the arena (or the gate) can't be reached from spawn.</summary>
        static bool FindSealed(LevelLayout L, out Vector3 bad)
        {
            bad = default;
            Vector3 spawn = L.spawn;
            if (NavMesh.SamplePosition(L.spawn, out var sh, 4f, NavMesh.AllAreas)) spawn = sh.position;
            if (L.gate && NavMesh.SamplePosition(L.gate.WinPoint, out var gh, 1.5f, NavMesh.AllAreas) && !Reachable(spawn, gh.position))
            {
                bad = gh.position;
                return true;
            }
            float lim = L.half - 1.2f;
            const float step = 3f;
            for (float x = -lim; x <= lim + 0.01f; x += step)
                for (float z = -lim; z <= lim + 0.01f; z += step)
                {
                    if (!NavMesh.SamplePosition(new Vector3(x, 0f, z), out var hit, 0.6f, NavMesh.AllAreas)) continue;
                    if (hit.position.y > 0.3f) continue;
                    if (!Reachable(spawn, hit.position)) { bad = hit.position; return true; }
                }
            return false;
        }

        // ================================================================ structures

        struct Plan
        {
            public int type;
            public float a, b, h, rad;
        }

        static bool PlanStructure(Rng r, float shrink, out Plan p)
        {
            p = default;
            float w = r.Value;
            p.type = w < 0.3f ? 0 : w < 0.55f ? 1 : w < 0.75f ? 2 : w < 0.9f ? 3 : 4;
            switch (p.type)
            {
                case 0: // room
                    p.a = Mathf.Max(8f, Snap(OneOf(r, 8f, 10f, 12f) * shrink, 2f));
                    p.b = Mathf.Max(8f, Snap(OneOf(r, 8f, 10f, 12f) * shrink, 2f));
                    p.h = InnerWallH;
                    p.rad = 0.5f * Mathf.Sqrt(p.a * p.a + p.b * p.b) + 0.6f;
                    break;
                case 1: // L cover
                    p.a = Mathf.Max(5f, Snap(r.Range(6f, 10f) * shrink, 1f));
                    p.b = Mathf.Max(5f, Snap(r.Range(6f, 10f) * shrink, 1f));
                    p.h = r.Chance(0.4f) ? 1.3f : InnerWallH;
                    p.rad = 0.5f * Mathf.Sqrt(p.a * p.a + p.b * p.b) + 0.6f;
                    break;
                case 2: // corridor
                    p.a = Mathf.Max(8f, Snap(r.Range(10f, 14f) * shrink, 2f));
                    p.b = 5f;
                    p.h = InnerWallH;
                    p.rad = 0.5f * Mathf.Sqrt(p.a * p.a + p.b * p.b) + 0.6f;
                    break;
                case 3: // T wall
                    p.a = Mathf.Max(8f, Snap(r.Range(10f, 13f) * shrink, 1f));
                    p.b = Mathf.Max(3f, Snap(r.Range(3f, 5f) * shrink, 1f));
                    p.h = InnerWallH;
                    p.rad = Mathf.Max(p.a * 0.5f, p.b) + 0.8f;
                    break;
                default: // block cluster
                    p.a = 4f;
                    p.h = 3f;
                    p.rad = 6.2f;
                    break;
            }
            return true;
        }

        static GameObject BuildStructure(Transform parent, Vector3 pos, float yaw, Plan p, Rng r, Material cyan, Material orange)
        {
            var acc = Props.RandomAccent(r);
            switch (p.type)
            {
                case 0: return Room(parent, pos, yaw, p.a, p.b, p.h, acc, r, cyan, orange);
                case 1:
                {
                    var t = Props.Root("LCover", parent, pos, yaw);
                    float ha = p.a * 0.5f, hb = p.b * 0.5f;
                    Props.Wall(t, new Vector3(-ha + 0.7f, 0f, -hb), new Vector3(ha, 0f, -hb), p.h, 1f);
                    Props.Wall(t, new Vector3(-ha, 0f, -hb + 0.7f), new Vector3(-ha, 0f, hb), p.h, 1f);
                    Props.AccentBlock(t, new Vector3(-ha, 0f, -hb), new Vector3(1.4f, p.h + 0.5f, 1.4f), acc);
                    // end caps
                    Props.Block(t, new Vector3(ha, 0f, -hb), new Vector3(1.2f, p.h + 0.25f, 1.2f));
                    Props.Block(t, new Vector3(-ha, 0f, hb), new Vector3(1.2f, p.h + 0.25f, 1.2f));
                    Props.FloorStrip(t, new Vector3(-ha + 1.2f, 0f, -hb + 1.0f), new Vector3(ha - 1f, 0f, -hb + 1.0f), 0.12f, cyan);
                    return t.gameObject;
                }
                case 2:
                {
                    var t = Props.Root("Corridor", parent, pos, yaw);
                    float hl = p.a * 0.5f, x = p.b * 0.5f;
                    bool window = r.Chance(0.5f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        if (window && s > 0)
                        {
                            Props.Wall(t, new Vector3(s * x, 0f, -hl + 0.6f), new Vector3(s * x, 0f, -1.75f), p.h, 1f);
                            Props.Wall(t, new Vector3(s * x, 0f, 1.75f), new Vector3(s * x, 0f, hl - 0.6f), p.h, 1f);
                            Props.Barrier(t, new Vector3(s * x, 0f, 0f), 3f, 90f, acc);
                        }
                        else Props.Wall(t, new Vector3(s * x, 0f, -hl + 0.6f), new Vector3(s * x, 0f, hl - 0.6f), p.h, 1f);
                        Props.Pillar(t, new Vector3(s * x, 0f, -hl), p.h + 0.7f, 1.3f, acc);
                        Props.Pillar(t, new Vector3(s * x, 0f, hl), p.h + 0.7f, 1.3f, acc);
                    }
                    Props.FloorStrip(t, new Vector3(0f, 0f, -hl + 0.5f), new Vector3(0f, 0f, hl - 0.5f), 0.16f, cyan);
                    return t.gameObject;
                }
                case 3:
                {
                    var t = Props.Root("TWall", parent, pos, yaw);
                    float hl = p.a * 0.5f;
                    Props.Wall(t, new Vector3(-hl, 0f, 0f), new Vector3(hl, 0f, 0f), p.h, 1f);
                    Props.Wall(t, new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, p.b), p.h, 1f);
                    Props.AccentBlock(t, new Vector3(0f, 0f, p.b + 0.6f), new Vector3(1.4f, p.h + 0.4f, 1.4f), acc);
                    Props.Terminal(t, new Vector3(hl * 0.5f, 0f, -0.95f), 0f, Props.GlowCyan);
                    return t.gameObject;
                }
                default:
                {
                    var t = Props.Root("Cluster", parent, pos, yaw);
                    Props.Block(t, Vector3.zero, new Vector3(3f, p.h, 3f), 0f, Props.WallMat, Props.AccentTop(acc));
                    Props.AccentBlock(t, new Vector3(0f, p.h, 0f), new Vector3(1.6f, 0.8f, 1.6f), acc);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = (i & 1) == 0 ? -p.a : p.a, z = (i & 2) == 0 ? -p.a : p.a;
                        Props.Pillar(t, new Vector3(x, 0f, z), r.Range(2.2f, 3.2f));
                    }
                    Props.FloorPad(t, Vector3.zero, 6f, orange, false);
                    return t.gameObject;
                }
            }
        }

        /// <summary>Rectangular room with 2-3 door gaps (4 m) and a terminal against a door-less wall.</summary>
        static GameObject Room(Transform parent, Vector3 pos, float yaw, float w, float d, float h, Accent acc, Rng r, Material cyan, Material orange)
        {
            var t = Props.Root("Room", parent, pos, yaw);
            float hw = w * 0.5f, hd = d * 0.5f;
            const float door = 4f;

            int doors = r.Int(2, 4);
            var order = new List<int> { 0, 1, 2, 3 };
            for (int i = 3; i > 0; i--) { int j = r.Int(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
            var hasDoor = new bool[4];
            for (int i = 0; i < doors; i++) hasDoor[order[i]] = true;

            for (int side = 0; side < 4; side++)
            {
                // 0 = south, 1 = north (along x, full width incl. corners); 2 = west, 3 = east (along z, between corners)
                bool alongX = side < 2;
                float fixedC = alongX ? (side == 0 ? -hd : hd) : (side == 2 ? -hw : hw);
                float a0 = alongX ? -hw - 0.5f : -hd + 0.5f;
                float a1 = alongX ? hw + 0.5f : hd - 0.5f;
                Vector3 P(float s) => alongX ? new Vector3(s, 0f, fixedC) : new Vector3(fixedC, 0f, s);
                if (hasDoor[side])
                {
                    float span = (alongX ? hw : hd) - 1.5f - door * 0.5f;
                    float c = span > 0f ? Snap(r.Range(-span, span), 1f) : 0f;
                    if (c - door * 0.5f - a0 > 0.6f) Props.Wall(t, P(a0), P(c - door * 0.5f), h, 1f);
                    if (a1 - (c + door * 0.5f) > 0.6f) Props.Wall(t, P(c + door * 0.5f), P(a1), h, 1f);
                }
                else Props.Wall(t, P(a0), P(a1), h, 1f);
            }
            // accent corner caps
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -hw : hw, z = (i & 2) == 0 ? -hd : hd;
                Props.AccentBlock(t, new Vector3(x, 0f, z), new Vector3(1.3f, h + 0.35f, 1.3f), acc);
            }
            // terminal against a wall without a door, facing inward
            for (int side = 0; side < 4; side++)
            {
                if (hasDoor[side]) continue;
                Vector3 tp; float ty;
                switch (side)
                {
                    case 0: tp = new Vector3(0f, 0f, -hd + 0.95f); ty = 180f; break;
                    case 1: tp = new Vector3(0f, 0f, hd - 0.95f); ty = 0f; break;
                    case 2: tp = new Vector3(-hw + 0.95f, 0f, 0f); ty = -90f; break;
                    default: tp = new Vector3(hw - 0.95f, 0f, 0f); ty = 90f; break;
                }
                // the screen is on local -Z; facing inward means local -Z points to the room centre
                Props.Terminal(t, tp, ty, r.Chance(0.6f) ? Props.GlowCyan : Props.GlowOrange);
                break;
            }
            Props.FloorPad(t, Vector3.zero, Mathf.Min(w, d) - 4f, r.Chance(0.5f) ? cyan : orange);
            return t.gameObject;
        }

        // ================================================================ outside

        static void BuildOutside(Transform o, float half, Rng r)
        {
            const float gy = -0.6f;

            // broken low rim around the platform
            int rim = 10 + Mathf.RoundToInt(half / 2f);
            for (int i = 0; i < rim; i++)
            {
                int side = r.Int(0, 4);
                float along = r.Range(-half - 9f, half + 9f), depth = half + r.Range(6f, 9f);
                var size = new Vector3(r.Range(3f, 8f), r.Range(0.5f, 1.6f), r.Range(1.5f, 3f));
                Vector3 p; float yaw;
                switch (side)
                {
                    case 0: p = new Vector3(along, gy, -depth); yaw = 0f; break;
                    case 1: p = new Vector3(along, gy, depth); yaw = 0f; break;
                    case 2: p = new Vector3(-depth, gy, along); yaw = 90f; break;
                    default: p = new Vector3(depth, gy, along); yaw = 90f; break;
                }
                Props.RimBlock(o, p, size, yaw);
            }

            // medium blocks
            int mid = 8 + Mathf.RoundToInt(half / 3f);
            for (int i = 0; i < mid; i++)
            {
                Vector3 p = default;
                bool ok = false;
                for (int a = 0; a < 30 && !ok; a++)
                {
                    float ext = half + 22f;
                    p = new Vector3(r.Range(-ext, ext), gy, r.Range(-ext, ext));
                    float cheb = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
                    ok = cheb >= half + 11f;
                }
                if (!ok) continue;
                var size = new Vector3(r.Range(2f, 6f), r.Range(1.5f, 6f), r.Range(2f, 6f));
                Props.Tower(o, p, size, r.Int(0, 2) * 90f, r);
            }

            // distant towers
            int towers = 14 + Mathf.RoundToInt(half / 3f);
            float a0 = r.Range(0f, 360f);
            for (int i = 0; i < towers; i++)
            {
                float ang = (a0 + i * 360f / towers + r.Range(-6f, 6f)) * Mathf.Deg2Rad;
                float d = half * 1.42f + r.Range(16f, 70f);
                var p = new Vector3(Mathf.Cos(ang) * d, gy, Mathf.Sin(ang) * d);
                var size = new Vector3(r.Range(5f, 14f), r.Range(6f, 28f), r.Range(5f, 14f));
                Props.Tower(o, p, size, r.Chance(0.75f) ? 0f : 45f, r);
            }
        }

        // ================================================================ gameplay points

        static bool Reachable(Vector3 from, Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)) return false;
            return path.status == NavMeshPathStatus.PathComplete;
        }

        static float MinDist(Vector3 p, List<Vector3> list)
        {
            float m = float.MaxValue;
            if (list == null) return m;
            for (int i = 0; i < list.Count; i++) m = Mathf.Min(m, Flat(p, list[i]));
            return m;
        }

        /// <summary>Best-candidate sampling: up to `candidates` valid points, returns the one farthest from `spaced`.</summary>
        static bool Pick(Rng r, float lim, Vector3 spawn, float minSpawn,
            List<Vector3> spaced, float spacing, List<Vector3> avoid, float avoidDist, int candidates, out Vector3 result)
        {
            result = default;
            bool found = false;
            float best = -1f;
            int got = 0;
            for (int a = 0; a < 120 && got < candidates; a++)
            {
                var q = new Vector3(r.Range(-lim, lim), 0f, r.Range(-lim, lim));
                if (Flat(q, spawn) < minSpawn - 1f) continue;
                if (!NavMesh.SamplePosition(q, out var hit, 1.2f, NavMesh.AllAreas)) continue;
                var p = hit.position;
                if (Mathf.Abs(p.x) > lim + 0.5f || Mathf.Abs(p.z) > lim + 0.5f) continue;
                if (Flat(p, spawn) < minSpawn) continue;
                float md = MinDist(p, spaced);
                if (md < spacing) continue;
                if (avoid != null && MinDist(p, avoid) < avoidDist) continue;
                if (!Reachable(spawn, p)) continue;
                got++;
                float score = Mathf.Min(md, 1000f) + r.Value * 0.01f;
                if (score > best) { best = score; result = p; found = true; }
            }
            return found;
        }

        static Vector3 Fallback(Rng r, Vector3 spawn)
        {
            for (int a = 0; a < 20; a++)
            {
                var q = spawn + new Vector3(r.Range(-4f, 4f), 0f, r.Range(2f, 6f));
                if (NavMesh.SamplePosition(q, out var hit, 2f, NavMesh.AllAreas) && Reachable(spawn, hit.position)) return hit.position;
            }
            return spawn + new Vector3(0f, 0f, 3f);
        }

        static void ComputePoints(LevelDef def, LevelLayout L, Rng r)
        {
            if (path == null) path = new NavMeshPath();
            float half = L.half;
            float lim = half - 1.5f;
            Vector3 spawn = L.spawn;
            if (NavMesh.SamplePosition(L.spawn, out var sh, 4f, NavMesh.AllAreas)) spawn = sh.position;
            float[] relax = { 1f, 0.75f, 0.5f, 0.25f, 0f };

            // coins
            for (int i = 0; i < def.coins; i++)
            {
                bool ok = false;
                for (int k = 0; k < relax.Length && !ok; k++)
                {
                    float f = relax[k];
                    float minSpawn = 6f * (f > 0f ? 1f : 0.5f);
                    if (Pick(r, lim, spawn, minSpawn, L.coinPoints, 4.5f * f, null, 0f, 8, out var p))
                    {
                        L.coinPoints.Add(p);
                        ok = true;
                    }
                }
                if (!ok) L.coinPoints.Add(Fallback(r, spawn));
            }

            // patrollers + chasers
            float enemyMin = Mathf.Max(12f, def.size * 0.35f);
            AddEnemies(r, L, spawn, lim, enemyMin, def.patrollers, EnemyType.Patroller);
            AddEnemies(r, L, spawn, lim, enemyMin, def.chasers, EnemyType.Chaser);

            // guards: homes on coins far from spawn, spread apart
            if (def.guards > 0)
            {
                var sorted = new List<Vector3>(L.coinPoints);
                sorted.Sort((a, b) => Flat(b, spawn).CompareTo(Flat(a, spawn)));
                var homes = new List<Vector3>();
                for (int i = 0; i < sorted.Count && homes.Count < def.guards; i++)
                    if (MinDist(sorted[i], homes) >= 8f) homes.Add(sorted[i]);
                for (int i = 0; i < sorted.Count && homes.Count < def.guards; i++)
                    if (!homes.Contains(sorted[i])) homes.Add(sorted[i]);
                while (homes.Count < def.guards)
                {
                    Vector3 hp;
                    if (!Pick(r, lim, spawn, enemyMin * 0.6f, homes, 4f, null, 0f, 6, out hp)) hp = Fallback(r, spawn);
                    homes.Add(hp);
                }

                for (int g = 0; g < def.guards; g++)
                {
                    Vector3 home = homes[g];
                    Vector3 gp = home;
                    for (int a = 0; a < 25; a++)
                    {
                        float ang = r.Range(0f, Mathf.PI * 2f), d = r.Range(1.5f, 2.8f);
                        var q = home + new Vector3(Mathf.Cos(ang) * d, 0f, Mathf.Sin(ang) * d);
                        if (!NavMesh.SamplePosition(q, out var hit, 0.8f, NavMesh.AllAreas)) continue;
                        if (Flat(hit.position, home) > 3.2f) continue;
                        if (Flat(hit.position, spawn) < 6f) continue;
                        if (!Reachable(spawn, hit.position)) continue;
                        gp = hit.position;
                        break;
                    }
                    L.enemyPoints.Add(gp);
                    L.enemyTypes.Add(EnemyType.Guard);
                    L.guardHomes.Add(home);
                }
            }

            // power-ups
            for (int i = 0; i < def.powerUps; i++)
            {
                bool ok = false;
                for (int k = 0; k < relax.Length && !ok; k++)
                {
                    float f = relax[k];
                    if (Pick(r, lim, spawn, f > 0f ? 6f : 4f, L.powerUpPoints, 7f * f, L.coinPoints, f > 0f ? 3f : 1.5f, 6, out var p))
                    {
                        L.powerUpPoints.Add(p);
                        ok = true;
                    }
                }
                if (!ok) L.powerUpPoints.Add(Fallback(r, spawn));
            }
        }

        static void AddEnemies(Rng r, LevelLayout L, Vector3 spawn, float lim, float enemyMin, int count, EnemyType type)
        {
            float[] spacing = { 6f, 4f, 2f, 0f };
            float[] spawnF = { 1f, 0.85f, 0.7f, 0.55f };
            for (int i = 0; i < count; i++)
            {
                bool ok = false;
                for (int k = 0; k < spacing.Length && !ok; k++)
                {
                    if (Pick(r, lim, spawn, enemyMin * spawnF[k], L.enemyPoints, spacing[k], null, 0f, 6, out var p))
                    {
                        L.enemyPoints.Add(p);
                        L.enemyTypes.Add(type);
                        ok = true;
                    }
                }
                if (!ok)
                {
                    // farthest reachable fallback: opposite end of the arena
                    Vector3 p = new Vector3(r.Range(-lim, lim) * 0.5f, 0f, lim);
                    if (NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) p = hit.position;
                    L.enemyPoints.Add(p);
                    L.enemyTypes.Add(type);
                }
            }
        }

        // ================================================================ menu

        public static Transform BuildMenuDiorama()
        {
            Props.ApplyTints(null); // drop leftovers of an interrupted build
            var r = new Rng(777);
            var root = new GameObject("MenuDiorama").transform;
            var floor = Child("Floor", root);
            var walls = Child("Walls", root);
            var obst = Child("Obstacles", root);
            var lamps = Child("Lamps", root);
            var decor = Child("Decor", root);
            var scenery = Child("Scenery", root);
            var dyn = Child("Dynamic", root);

            Props.GridPlatform(floor, 24f, 0.6f);
            Props.OuterGround(scenery, 360f, -0.6f);
            var col = new GameObject("GroundCollider");
            col.transform.SetParent(floor, false);
            col.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            col.AddComponent<BoxCollider>().size = new Vector3(24f, 1f, 24f);

            var cyan = Props.Glow(Props.GlowCyan, 2f);
            var orange = Props.Glow(Props.GlowOrange, 2f);

            // floor glow: border, hero stage, lane to the back
            const float e = 11.55f;
            Props.FloorStrip(decor, new Vector3(-e, 0f, -e), new Vector3(e, 0f, -e), 0.14f, cyan);
            Props.FloorStrip(decor, new Vector3(-e, 0f, e), new Vector3(e, 0f, e), 0.14f, cyan);
            Props.FloorStrip(decor, new Vector3(-e, 0f, -e), new Vector3(-e, 0f, e), 0.14f, cyan);
            Props.FloorStrip(decor, new Vector3(e, 0f, -e), new Vector3(e, 0f, e), 0.14f, cyan);
            Props.FloorPad(decor, Vector3.zero, 6f, cyan);
            Props.FloorStrip(decor, new Vector3(0f, 0f, 3.4f), new Vector3(0f, 0f, 9.2f), 0.14f, cyan);
            Props.FloorPad(decor, new Vector3(-7f, 0f, -3f), 2f, orange);

            // back walls
            Props.Wall(walls, new Vector3(-10.5f, 0f, 11f), new Vector3(-1.8f, 0f, 11f), 2.4f, 1f);
            Props.Wall(walls, new Vector3(2.2f, 0f, 11.5f), new Vector3(10.5f, 0f, 11.5f), 3.2f, 1f);
            Props.AccentBlock(walls, new Vector3(-1.8f, 0f, 11f), new Vector3(1.4f, 2.9f, 1.4f), Accent.Teal);
            Props.Pillar(walls, new Vector3(2.2f, 0f, 11.5f), 3.8f, 1.3f);
            // side wall (left, L-shape with the back wall)
            Props.Wall(walls, new Vector3(-10.5f, 0f, 10.4f), new Vector3(-10.5f, 0f, 4.5f), 2.4f, 1f);

            // blocks
            Props.Block(obst, new Vector3(-8.6f, 0f, 8f), new Vector3(2.5f, 3f, 2.5f));
            Props.Block(obst, new Vector3(9.2f, 0f, 9f), new Vector3(2f, 2f, 3f));
            Props.AccentBlock(obst, new Vector3(-7.6f, 0f, 2.2f), new Vector3(1.6f, 1.6f, 1.6f), Accent.Red);
            Props.Block(obst, new Vector3(7.4f, 0f, 2.4f), new Vector3(2f, 1.4f, 2f));
            Props.AccentBlock(obst, new Vector3(7.6f, 1.4f, 2.2f), new Vector3(1.1f, 1.1f, 1.1f), Accent.Orange);
            Props.AccentBlock(obst, new Vector3(-2.4f, 0f, 8.6f), new Vector3(1.2f, 1f, 1.2f), Accent.Teal);
            Props.Block(obst, new Vector3(10f, 0f, -1.5f), new Vector3(1.5f, 2.5f, 3f));
            Props.Pillar(obst, new Vector3(-5.8f, 0f, 9.3f), 3.2f);
            Props.Pillar(obst, new Vector3(8.8f, 0f, 5.6f), 3f, 1f, Accent.Teal);
            Props.Barrier(obst, new Vector3(0f, 0f, 9.9f), 3f, 0f, Accent.Orange);
            Props.Terminal(obst, new Vector3(1.7f, 0f, 8.2f), 0f, Props.GlowCyan);
            Props.Terminal(obst, new Vector3(-9.3f, 0f, -0.6f), 90f, Props.GlowOrange);

            // lamps (night only lights)
            Props.LampPost(lamps, new Vector3(-4.6f, 0f, 6.4f), 2.2f, false);
            Props.LampPost(lamps, new Vector3(4.6f, 0f, 6.2f), 2.2f, false);
            Props.LampPost(lamps, new Vector3(-9.6f, 0f, 3.2f), 2f, false);

            Coin.Create(dyn, new Vector3(-5.2f, 0f, 4.2f));
            Coin.Create(dyn, new Vector3(-6.2f, 0f, 5.0f));

            // background: low rim + towers
            for (int i = 0; i < 12; i++)
            {
                float a = (i * 30f + r.Range(-8f, 8f)) * Mathf.Deg2Rad, d = r.Range(15f, 19f);
                var p = new Vector3(Mathf.Cos(a) * d, -0.6f, Mathf.Sin(a) * d);
                Props.RimBlock(scenery, p, new Vector3(r.Range(3f, 7f), r.Range(0.6f, 1.8f), r.Range(1.5f, 3f)), -a * Mathf.Rad2Deg + 90f);
            }
            for (int i = 0; i < 16; i++)
            {
                float a = (20f + i * (140f / 15f) + r.Range(-4f, 4f)) * Mathf.Deg2Rad; // arc in front of the camera (+Z)
                float d = r.Range(26f, 55f);
                var p = new Vector3(Mathf.Cos(a) * d, -0.6f, Mathf.Sin(a) * d);
                Props.Tower(scenery, p, new Vector3(r.Range(4f, 11f), r.Range(6f, 24f), r.Range(4f, 11f)), r.Chance(0.75f) ? 0f : 45f, r);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = (200f + i * 20f + r.Range(-5f, 5f)) * Mathf.Deg2Rad; // a few behind
                float d = r.Range(30f, 55f);
                var p = new Vector3(Mathf.Cos(a) * d, -0.6f, Mathf.Sin(a) * d);
                Props.Tower(scenery, p, new Vector3(r.Range(5f, 12f), r.Range(6f, 20f), r.Range(5f, 12f)), 0f, r);
            }

            MakeStatic(floor);
            MakeStatic(walls);
            MakeStatic(obst);
            MakeStatic(lamps);
            MakeStatic(decor);
            MakeStatic(scenery);
            Props.ApplyTints(root.gameObject.AddComponent<DayNightTint>());
            return root;
        }
    }
}
