using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Builds the student "prefab kit": clean, script-free prefabs with well-named materials (M_*).
    ///   Prefabs/Level/     floor, walls, blocks, pillar, lamp, terminal, pads ...
    ///   Prefabs/Gameplay/  the 3D models of the coin, exit gate and power-ups (students add the scripts)
    ///   Materials/M_*.mat  one material per colour, named after what it is
    /// The pieces come from the same code that builds the levels (Props, Coin, ExitGate, PowerUp),
    /// so the kit looks exactly like the game.
    /// </summary>
    public static class KitBuilder
    {
        const string LevelDir = "Assets/GoldRush/Prefabs/Level/";
        const string GameplayDir = "Assets/GoldRush/Prefabs/Gameplay/";
        const string MatDir = "Assets/GoldRush/Materials/";

        [MenuItem("Gold Rush/Course/Build Prefab Kit", priority = 82)]
        public static void Build()
        {
            if (!File.Exists(CourseExporter.TextureDir + "Grid.png")) CourseExporter.ExportGrid();
            Directory.CreateDirectory(LevelDir);
            Directory.CreateDirectory(GameplayDir);
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(CourseExporter.TextureDir);

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Props.ApplyTints(null);
                var h = new GameObject("Holder").transform;
                var pieces = new List<(string path, GameObject go)>
                {
                    (LevelDir + "Floor_40m", Floor(h)),
                    (LevelDir + "OuterGround", Wrap("OuterGround", h, Props.OuterGround(h, 480f, -0.6f))),
                    (LevelDir + "Block_1x1x1", Props.Block(h, Vector3.zero, new Vector3(1, 1, 1))),
                    (LevelDir + "Block_2x2x2", Props.Block(h, Vector3.zero, new Vector3(2, 2, 2))),
                    (LevelDir + "Block_4x1x2", Props.Block(h, Vector3.zero, new Vector3(4, 1, 2))),
                    (LevelDir + "AccentBlock_Red", Props.AccentBlock(h, Vector3.zero, Vector3.one * 1.5f, Accent.Red)),
                    (LevelDir + "AccentBlock_Orange", Props.AccentBlock(h, Vector3.zero, Vector3.one * 1.5f, Accent.Orange)),
                    (LevelDir + "AccentBlock_Teal", Props.AccentBlock(h, Vector3.zero, Vector3.one * 1.5f, Accent.Teal)),
                    (LevelDir + "Wall_4m", Props.Wall(h, new Vector3(-2, 0, 0), new Vector3(2, 0, 0), 2.3f, 1f)),
                    (LevelDir + "Wall_8m", Props.Wall(h, new Vector3(-4, 0, 0), new Vector3(4, 0, 0), 2.3f, 1f)),
                    (LevelDir + "Pillar", Props.Pillar(h, Vector3.zero, 3f)),
                    (LevelDir + "Barrier_Orange", Props.Barrier(h, Vector3.zero, 3f, 0f, Accent.Orange)),
                    (LevelDir + "LampPost", Top(h, Props.LampPost(h, Vector3.zero).transform)),
                    (LevelDir + "Terminal", Props.Terminal(h, Vector3.zero, 0f, Props.GlowCyan)),
                    (LevelDir + "FloorPad_Cyan", Props.FloorPad(h, Vector3.zero, 3f, Props.Glow(Props.GlowCyan))),
                    (GameplayDir + "Coin", CoinModel(h)),
                    (GameplayDir + "ExitGate", GateModel(h)),
                    (GameplayDir + "PowerUp_Speed", PowerUp.Create(h, Vector3.zero, PowerType.Speed).gameObject),
                    (GameplayDir + "PowerUp_Ghost", PowerUp.Create(h, Vector3.zero, PowerType.Ghost).gameObject),
                    (GameplayDir + "PowerUp_Magnet", PowerUp.Create(h, Vector3.zero, PowerType.Magnet).gameObject),
                };
                var tints = new Dictionary<Material, (Color day, Color night)>();
                var tint = h.gameObject.AddComponent<DayNightTint>();
                Props.ApplyTints(tint);
                foreach (var e in tint.entries) if (e.material) tints[e.material] = (e.day, e.night);

                var made = new Dictionary<string, Material>();
                foreach (var (path, go) in pieces)
                {
                    StripScripts(go);
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i]) mats[i] = Clean(mats[i], r.gameObject.name, Path.GetFileName(path), made);
                        r.sharedMaterials = mats;
                    }
                    go.transform.SetParent(null, true);
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    go.name = Path.GetFileName(path);
                    PrefabUtility.SaveAsPrefabAsset(go, path + ".prefab");
                }
                WriteNightColours(made, tints);
                AssetDatabase.SaveAssets();
                Debug.Log($"[GoldRush] Prefab kit: {pieces.Count} prefabs, {made.Count} materials");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Renders a thumbnail of every kit prefab to GoldRush_Export/Docs/images/kit/ (for the course).</summary>
        [MenuItem("Gold Rush/Course/Render Kit Thumbnails", priority = 83)]
        public static void RenderThumbnails()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../GoldRush_Export/Docs/images/kit"));
            Directory.CreateDirectory(outDir);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var offLights = new List<Light>();
            var oldAmbient = RenderSettings.ambientLight; var oldMode = RenderSettings.ambientMode; var oldFog = RenderSettings.fog;
            try
            {
                RenderSettings.fog = false;
                // switch off the lights of the scene that is open in the editor while we take the pictures
                foreach (var l in Object.FindObjectsByType<Light>()) if (l.enabled && l.gameObject.scene != scene) { l.enabled = false; offLights.Add(l); }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.3f; sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
                const int Layer = 31; // render only the kit piece, not the scene that is open in the editor
                sun.cullingMask = 1 << Layer;
                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.93f, 0.95f, 0.98f);
                cam.fieldOfView = 30f;
                cam.cullingMask = 1 << Layer;
                var rt = new RenderTexture(512, 384, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var paths = Directory.GetFiles(LevelDir, "*.prefab").Concat(Directory.GetFiles(GameplayDir, "*.prefab"));
                foreach (var path in paths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
                    foreach (var l in go.GetComponentsInChildren<Light>(true)) l.cullingMask = 1 << Layer;
                    var rs = go.GetComponentsInChildren<Renderer>();
                    if (rs.Length == 0) { Object.DestroyImmediate(go); continue; }
                    Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    if (prefab.name == "OuterGround") b = new Bounds(Vector3.zero, new Vector3(12, 0.1f, 12));
                    float size = b.extents.magnitude;
                    Vector3 dir = Quaternion.Euler(25f, 35f, 0f) * Vector3.forward; // from the front (-Z side)
                    cam.transform.position = b.center - dir * (size / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.95f);
                    cam.transform.rotation = Quaternion.LookRotation(dir);
                    cam.nearClipPlane = 0.05f; cam.farClipPlane = size * 10f + 50f;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, prefab.name + ".png"), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Object.DestroyImmediate(go);
                }
                rt.Release();
                Debug.Log("[GoldRush] Kit thumbnails saved to " + outDir);
            }
            finally
            {
                RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientMode = oldMode; RenderSettings.fog = oldFog;
                foreach (var l in offLights) if (l) l.enabled = true;
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ------------------------------------------------------------------ pieces

        static GameObject Floor(Transform h)
        {
            var root = new GameObject("Floor_40m");
            root.transform.SetParent(h, false);
            Props.GridPlatform(root.transform, 40f, 0.6f);
            var col = root.AddComponent<BoxCollider>(); // one solid ground under the whole floor
            col.center = new Vector3(0, -0.5f, 0);
            col.size = new Vector3(40f, 1f, 40f);
            return root;
        }

        static GameObject Wrap(string name, Transform h, GameObject inner)
        {
            var root = new GameObject(name);
            root.transform.SetParent(h, false);
            inner.transform.SetParent(root.transform, true);
            return root;
        }

        static GameObject Top(Transform holder, Transform t)
        {
            while (t.parent && t.parent != holder) t = t.parent;
            return t.gameObject;
        }

        static GameObject CoinModel(Transform h)
        {
            var c = Coin.Create(h, Vector3.down); // Create lifts it by 1 m: this puts the root at 0
            var vis = c.transform.Find("Visual");
            string[] names = { "Rim", "Face", "Star" };
            for (int i = 0; i < vis.childCount && i < names.Length; i++) vis.GetChild(i).name = names[i];
            return c.gameObject;
        }

        /// <summary>The gate model with clear names on the parts the ExitGate script needs (see phase 6).</summary>
        static GameObject GateModel(Transform h)
        {
            var gate = ExitGate.Create(h, Vector3.zero);
            var so = new SerializedObject(gate);
            Transform T(string f) => so.FindProperty(f).objectReferenceValue as Transform;
            if (T("door")) { T("door").name = "Door"; if (T("door").childCount > 0) T("door").GetChild(0).name = "Panel"; }
            if (T("glow")) T("glow").name = "Glow";
            if (T("glowRing")) T("glowRing").name = "Ring";
            if (T("markerAnchor")) T("markerAnchor").name = "MarkerAnchor";
            var gl = so.FindProperty("glowLight").objectReferenceValue as Light; if (gl) gl.name = "GlowLight";
            var tx = so.FindProperty("text").objectReferenceValue as TextMesh; if (tx) tx.name = "Text";
            var strips = so.FindProperty("strips");
            string[] sn = { "StripTop", "StripLeft", "StripRight" };
            for (int i = 0; i < strips.arraySize; i++)
                if (strips.GetArrayElementAtIndex(i).objectReferenceValue is Renderer r) r.name = i < sn.Length ? sn[i] : "Strip" + (i + 1);
            // the two posts and the lintel are the "Block" children, in the order the code makes them
            string[] bn = { "PostLeft", "PostRight", "Lintel" };
            int b = 0;
            foreach (Transform c in gate.transform)
                if (c.name == "Block" && b < bn.Length) c.name = bn[b++];
            return gate.gameObject;
        }

        /// <summary>The kit is imported before any script exists: keep only built-in components.</summary>
        static void StripScripts(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
        }

        // ------------------------------------------------------------------ materials

        static readonly (Color c, string name)[] Tinted =
        {
            (Props.FloorDay, "M_Floor"), (Props.OuterDay, "M_OuterGround"), (Props.PadDay, "M_Pad"),
            (Props.WallDay, "M_Block"), (Props.WallTopDay, "M_BlockTop"), (Props.TrimDay, "M_Trim"),
            (Props.TowerDay, "M_Tower"), (Props.TowerTopDay, "M_TowerTop"),
            (Props.RedDay, "M_Red"), (Props.RedTopDay, "M_RedTop"), (Props.OrangeDay, "M_Orange"),
            (Props.OrangeTopDay, "M_OrangeTop"), (Props.TealDay, "M_Teal"), (Props.TealTopDay, "M_TealTop"),
        };

        static readonly (Color c, string name)[] Glows =
        {
            (Props.GlowCyan, "M_GlowCyan"), (Props.GlowOrange, "M_GlowOrange"), (Props.GlowRed, "M_GlowRed"),
            (Props.GlowGreen, "M_GlowGreen"), (Props.LampColor, "M_GlowLamp"),
        };

        static float Dist(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        /// <summary>Gives a material made in code a clear name and saves it once (same colour = same asset).</summary>
        static Material Clean(Material m, string objectName, string prefabName, Dictionary<string, Material> made)
        {
            if (EditorUtility.IsPersistent(m)) return m;
            Color baseC = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            bool emissive = m.IsKeywordEnabled("_EMISSION");
            bool transparent = m.renderQueue >= 3000;
            string name = null;

            if (transparent) name = "M_" + prefabName + "_" + Pretty(objectName) + "_Glass";
            else if (emissive)
            {
                Color e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : baseC;
                float max = Mathf.Max(e.r, e.g, e.b, 0.0001f);
                Color hue = new Color(e.r / max, e.g / max, e.b / max);
                var best = Glows.OrderBy(g => Dist(hue, new Color(g.c.r / Mathf.Max(g.c.r, g.c.g, g.c.b), g.c.g / Mathf.Max(g.c.r, g.c.g, g.c.b), g.c.b / Mathf.Max(g.c.r, g.c.g, g.c.b)))).First();
                name = Dist(hue, best.c / Mathf.Max(best.c.r, best.c.g, best.c.b)) < 0.25f ? best.name : "M_" + prefabName + "_" + Pretty(objectName);
            }
            else
            {
                var best = Tinted.OrderBy(t => Dist(baseC, t.c)).First();
                name = Dist(baseC, best.c) < 0.03f ? best.name : "M_" + prefabName + "_" + Pretty(objectName);
            }
            if (prefabName.StartsWith("Coin")) name = baseC.g > 0.6f ? "M_Gold" : "M_GoldDark";
            if (prefabName.StartsWith("PowerUp_")) name = "M_Power" + prefabName.Substring(8) + (transparent ? "_Shell" : "_Core");
            if (prefabName == "ExitGate" && transparent) name = "M_GateGlow";

            if (made.TryGetValue(name, out var existing)) return existing;

            foreach (var prop in m.GetTexturePropertyNames())
            {
                var tex = m.GetTexture(prop);
                if (!tex || EditorUtility.IsPersistent(tex)) continue;
                if (tex.name.Contains("grid")) m.SetTexture(prop, AssetDatabase.LoadAssetAtPath<Texture2D>(CourseExporter.TextureDir + "Grid.png"));
                else if (tex is Texture2D t2)
                {
                    string tp = CourseExporter.TextureDir + name.Replace("M_", "") + ".png";
                    File.WriteAllBytes(tp, t2.EncodeToPNG());
                    AssetDatabase.ImportAsset(tp, ImportAssetOptions.ForceSynchronousImport);
                    m.SetTexture(prop, AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
                }
            }
            m.name = name;
            string path = MatDir + name + ".mat";
            var onDisk = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (onDisk) { EditorUtility.CopySerialized(m, onDisk); onDisk.name = name; m = onDisk; }
            else AssetDatabase.CreateAsset(m, path);
            made[name] = m;
            return m;
        }

        /// <summary>Kit name for a material by its colour (tinted colours and glows), or null.</summary>
        static string ColourName(Material m)
        {
            Color baseC = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            if (m.renderQueue >= 3000) return null;
            if (m.IsKeywordEnabled("_EMISSION"))
            {
                Color e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : baseC;
                float max = Mathf.Max(e.r, e.g, e.b, 0.0001f);
                Color hue = new Color(e.r / max, e.g / max, e.b / max);
                foreach (var g in Glows)
                {
                    float gm = Mathf.Max(g.c.r, g.c.g, g.c.b);
                    if (Dist(hue, new Color(g.c.r / gm, g.c.g / gm, g.c.b / gm)) < 0.25f) return g.name;
                }
                return null;
            }
            foreach (var t in Tinted) if (Dist(baseC, t.c) < 0.03f) return t.name;
            return null;
        }

        static readonly string[] SceneNames = { "MainMenu", "Level1", "Level2", "Level3", "Level4", "Game" };

        /// <summary>
        /// Points every scene at the clean M_* materials instead of the old generated ones (Materials/Level/"Cube 12" ...).
        /// Same colour (and same grid tiling) = the kit material; anything else is renamed after what uses it.
        /// </summary>
        [MenuItem("Gold Rush/Course/Clean Up Level Materials", priority = 84)]
        public static void CleanLevelMaterials()
        {
            const string OldDir = "Assets/GoldRush/Materials/Level/";
            if (!Directory.Exists(OldDir)) return;
            var kit = Directory.GetFiles(MatDir, "M_*.mat").Select(AssetDatabase.LoadAssetAtPath<Material>).Where(x => x)
                .ToDictionary(x => x.name);
            var olds = Directory.GetFiles(OldDir, "*.mat").Select(AssetDatabase.LoadAssetAtPath<Material>).Where(x => x).ToList();

            // 1. who uses each old material (for a readable name)
            var hint = new Dictionary<Material, string>();
            foreach (var sn in SceneNames)
            {
                string sp = "Assets/GoldRush/Scenes/" + sn + ".unity";
                if (!File.Exists(sp)) continue;
                var sc = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
                foreach (var r in sc.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    foreach (var m in r.sharedMaterials)
                        if (m && olds.Contains(m) && !hint.ContainsKey(m))
                            hint[m] = (r.transform.parent ? Pretty(r.transform.parent.name) + "_" : "") + Pretty(r.gameObject.name);
            }

            // 2. merge into a kit material, or rename
            var map = new Dictionary<Material, Material>();
            var used = new HashSet<string>(kit.Keys);
            foreach (var m in olds)
            {
                string cn = ColourName(m);
                if (cn != null && kit.TryGetValue(cn, out var k) && SameTiling(m, k)) { map[m] = k; continue; }
                string baseName = cn != null ? cn + "_Tile" + Mathf.RoundToInt(m.GetTextureScale("_BaseMap").x)
                                             : "M_" + (hint.TryGetValue(m, out var h) ? h : Pretty(m.name));
                string n = baseName; int i = 2;
                while (used.Contains(n)) n = baseName + "_" + i++;
                used.Add(n);
                string from = AssetDatabase.GetAssetPath(m);
                string err = AssetDatabase.MoveAsset(from, MatDir + n + ".mat"); // keeps the GUID: scenes stay linked
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[GoldRush] " + err);
            }

            // 3. swap merged materials in every scene (renderers + day/night tint lists)
            foreach (var sn in SceneNames)
            {
                string sp = "Assets/GoldRush/Scenes/" + sn + ".unity";
                if (!File.Exists(sp)) continue;
                var sc = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
                bool dirty = false;
                foreach (var r in sc.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                {
                    var mats = r.sharedMaterials; bool ch = false;
                    for (int j = 0; j < mats.Length; j++) if (mats[j] && map.TryGetValue(mats[j], out var to)) { mats[j] = to; ch = true; }
                    if (ch) { r.sharedMaterials = mats; dirty = true; }
                }
                foreach (var t in sc.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DayNightTint>(true)))
                {
                    foreach (var e in t.entries) if (e.material && map.TryGetValue(e.material, out var to)) { e.material = to; dirty = true; }
                    t.entries = t.entries.GroupBy(e => e.material).Select(g => g.First()).ToList();
                    EditorUtility.SetDirty(t);
                }
                if (dirty) EditorSceneManager.SaveScene(sc);
            }
            var pal = AssetDatabase.LoadAssetAtPath<DayNightPalette>("Assets/GoldRush/Resources/DayNightPalette.asset");
            if (pal)
            {
                var so = new SerializedObject(pal);
                var list = so.FindProperty("entries");
                if (list != null)
                    for (int j = 0; j < list.arraySize; j++)
                    {
                        var mp = list.GetArrayElementAtIndex(j).FindPropertyRelative("material");
                        if (mp != null && mp.objectReferenceValue is Material om && map.TryGetValue(om, out var to)) mp.objectReferenceValue = to;
                    }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 4. the merged originals are no longer used
            foreach (var m in map.Keys) AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m));
            foreach (var f in Directory.GetFiles(OldDir, "*.asset")) // textures saved next to the old materials
                AssetDatabase.MoveAsset(f.Replace('\\', '/'), CourseExporter.TextureDir + Path.GetFileNameWithoutExtension(f) + ".asset");
            if (Directory.GetFiles(OldDir).Where(f => !f.EndsWith(".meta")).Count() == 0) AssetDatabase.DeleteAsset(OldDir.TrimEnd('/'));
            AssetDatabase.SaveAssets();
            Debug.Log($"[GoldRush] Level materials: {map.Count} merged into the kit, {olds.Count - map.Count} renamed");
        }

        static bool SameTiling(Material a, Material b)
        {
            if (!a.HasProperty("_BaseMap") || !b.HasProperty("_BaseMap")) return true;
            bool ta = a.GetTexture("_BaseMap"), tb = b.GetTexture("_BaseMap");
            if (ta != tb) return false;
            return !ta || Vector2.Distance(a.GetTextureScale("_BaseMap"), b.GetTextureScale("_BaseMap")) < 0.01f;
        }

        static string Pretty(string s)
        {
            s = s.Replace(" (Instance)", "").Replace(" ", "");
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        /// <summary>Remember the night colour of each kit material (used by the day/night system).</summary>
        static void WriteNightColours(Dictionary<string, Material> made, Dictionary<Material, (Color day, Color night)> tints)
        {
            var pal = AssetDatabase.LoadAssetAtPath<DayNightPalette>("Assets/GoldRush/Resources/DayNightPalette.asset");
            if (!pal) return;
            foreach (var t in Tinted)
            {
                if (!made.TryGetValue(t.name, out var m)) continue;
                var src = tints.FirstOrDefault(kv => Dist(kv.Value.day, t.c) < 0.03f);
                if (src.Key) pal.Add(m, src.Value.day, src.Value.night);
            }
            EditorUtility.SetDirty(pal);
        }
    }
}
