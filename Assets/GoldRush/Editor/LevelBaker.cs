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
    /// Turns the procedural level generator into real, hand-editable scenes:
    /// Assets/GoldRush/Scenes/Level1..4.unity. Each scene contains a GameManager plus a level made of
    /// normal GameObjects (walls, blocks, lamps, gate) and gameplay markers (PlayerSpawn, CoinSpot,
    /// MonsterSpawn, PowerUpSpot). After baking, design the levels by hand in the Scene view.
    /// </summary>
    public static class LevelBaker
    {
        public const string SceneDir = "Assets/GoldRush/Scenes/";
        public const string MaterialDir = "Assets/GoldRush/Materials/Level/";
        public const string PrefabDir = "Assets/GoldRush/Prefabs/Level/";
        const string PalettePath = "Assets/GoldRush/Resources/DayNightPalette.asset";

        // day/night colours of every material used by the levels + prefabs (written to the palette asset)
        static readonly List<DayNightTint.Entry> collected = new List<DayNightTint.Entry>();

        public static string ScenePath(int index) => $"{SceneDir}Level{index + 1}.unity";

        [MenuItem("Gold Rush/Level Design/Rebuild Level Scenes (overwrites!)", priority = 60)]
        static void BakeAllMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild level scenes",
                    "This regenerates Level1–Level4.unity from the level generator and OVERWRITES any changes you made to them.",
                    "Rebuild", "Cancel")) return;
            BakeAll(false);
        }

        /// <param name="onlyMissing">true = only create scenes that don't exist yet (never overwrites).</param>
        public static void BakeAll(bool onlyMissing)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(MaterialDir);
            collected.Clear();
            int baked = 0;
            for (int i = 0; i < Levels.All.Length; i++)
            {
                if (onlyMissing && File.Exists(ScenePath(i))) continue;
                Bake(i);
                baked++;
            }
            // never overwrite a student's edited prefabs during the automatic setup
            if (onlyMissing && baked == 0 && Directory.Exists(PrefabDir)) return;
            BuildPrefabs();
            WritePalette();
            AssetDatabase.SaveAssets();
            Debug.Log("[GoldRush] Level scenes ready in " + SceneDir);
        }

        static void Bake(int index)
        {
            var def = Levels.All[index];
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                new GameObject("GameManager").AddComponent<GameManager>();

                var layout = LevelBuilder.Build(def);
                var root = layout.root;
                root.name = $"Level {index + 1} - {def.title}";
                root.gameObject.AddComponent<LevelScene>().arenaHalf = layout.half;

                var gameplay = Group(root, "Gameplay");
                var spawn = new GameObject("PlayerSpawn").AddComponent<PlayerSpawn>();
                spawn.transform.SetParent(gameplay, false);
                spawn.transform.SetPositionAndRotation(layout.spawn, Quaternion.Euler(0, layout.spawnYaw, 0));

                var coins = Group(gameplay, "Coins");
                for (int i = 0; i < layout.coinPoints.Count; i++)
                    Marker<CoinSpot>(coins, "CoinSpot " + (i + 1), layout.coinPoints[i]);

                var monsters = Group(gameplay, "Monsters");
                int guard = 0;
                for (int i = 0; i < layout.enemyPoints.Count; i++)
                {
                    var type = layout.enemyTypes[i];
                    string label = type == EnemyType.Patroller ? "Slime" : type == EnemyType.Chaser ? "Ghost" : "MushroomKing";
                    var m = Marker<MonsterSpawn>(monsters, $"{label} {i + 1}", layout.enemyPoints[i]);
                    m.type = type;
                    if (type == EnemyType.Guard && guard < layout.guardHomes.Count)
                    {
                        var home = new GameObject("Home").transform;
                        home.SetParent(m.transform, false);
                        home.position = layout.guardHomes[guard++];
                        m.home = home;
                    }
                }

                var powers = Group(gameplay, "PowerUps");
                for (int i = 0; i < layout.powerUpPoints.Count; i++)
                {
                    var type = (PowerType)((i + Mathf.Abs(def.seed)) % 3);
                    Marker<PowerUpSpot>(powers, "PowerUp " + type, layout.powerUpPoints[i]).type = type;
                }

                if (layout.dynamicRoot) Object.DestroyImmediate(layout.dynamicRoot.gameObject);
                foreach (var s in root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>())
                {
                    s.RemoveData();
                    s.navMeshData = null; // rebuilt when the level starts
                }
                MarkStatic(root, layout.gate ? layout.gate.transform : null);
                PersistAssets(scene);
                foreach (var t in root.GetComponentsInChildren<DayNightTint>()) collected.AddRange(t.entries);

                EditorSceneManager.SaveScene(scene, ScenePath(index));
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ------------------------------------------------------------------ prefab palette

        [MenuItem("Gold Rush/Level Design/Rebuild Prefab Palette", priority = 61)]
        public static void BuildPrefabsMenu()
        {
            collected.Clear();
            BuildPrefabs();
            WritePalette();
            AssetDatabase.SaveAssets();
            Debug.Log("[GoldRush] Prefab palette ready in " + PrefabDir);
        }

        /// <summary>Drag-and-drop building pieces for designing levels by hand.</summary>
        static void BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabDir + "Blocks");
            Directory.CreateDirectory(PrefabDir + "Gameplay");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var holder = new GameObject("Holder").transform;
                var pieces = new List<(string path, GameObject go)>
                {
                    ("Blocks/Block_1x1x1", Props.Block(holder, Vector3.zero, new Vector3(1, 1, 1))),
                    ("Blocks/Block_2x2x2", Props.Block(holder, Vector3.zero, new Vector3(2, 2, 2))),
                    ("Blocks/Block_4x1x2", Props.Block(holder, Vector3.zero, new Vector3(4, 1, 2))),
                    ("Blocks/Wall_8m", Props.Wall(holder, new Vector3(-4, 0, 0), new Vector3(4, 0, 0), 2.3f, 1f)),
                    ("Blocks/Wall_4m", Props.Wall(holder, new Vector3(-2, 0, 0), new Vector3(2, 0, 0), 2.3f, 1f)),
                    ("Blocks/Pillar", Props.Pillar(holder, Vector3.zero, 3f)),
                    ("Blocks/Barrier_Orange", Props.Barrier(holder, Vector3.zero, 3f, 0f, Accent.Orange)),
                    ("Blocks/AccentBlock_Red", Props.AccentBlock(holder, Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f), Accent.Red)),
                    ("Blocks/AccentBlock_Orange", Props.AccentBlock(holder, Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f), Accent.Orange)),
                    ("Blocks/AccentBlock_Teal", Props.AccentBlock(holder, Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f), Accent.Teal)),
                    ("Blocks/LampPost", TopUnder(holder, Props.LampPost(holder, Vector3.zero).transform)),
                    ("Blocks/Terminal", Props.Terminal(holder, Vector3.zero, 0f, Props.GlowCyan)),
                    ("Blocks/FloorPad_Cyan", Props.FloorPad(holder, Vector3.zero, 3f, Props.Glow(Props.GlowCyan))),
                    ("Gameplay/ExitGate", ExitGate.Create(holder, Vector3.zero).gameObject),
                    ("Gameplay/PlayerSpawn", new GameObject("PlayerSpawn", typeof(PlayerSpawn))),
                    ("Gameplay/CoinSpot", new GameObject("CoinSpot", typeof(CoinSpot))),
                    ("Gameplay/Monster_Slime", MonsterMarker("Slime", EnemyType.Patroller)),
                    ("Gameplay/Monster_Ghost", MonsterMarker("Ghost", EnemyType.Chaser)),
                    ("Gameplay/Monster_MushroomKing", MonsterMarker("MushroomKing", EnemyType.Guard)),
                    ("Gameplay/PowerUp_Speed", PowerMarker(PowerType.Speed)),
                    ("Gameplay/PowerUp_Ghost", PowerMarker(PowerType.Ghost)),
                    ("Gameplay/PowerUp_Magnet", PowerMarker(PowerType.Magnet)),
                };
                var tint = holder.gameObject.AddComponent<DayNightTint>();
                Props.ApplyTints(tint);
                PersistAssets(scene);
                collected.AddRange(tint.entries);
                foreach (var (path, go) in pieces)
                {
                    go.transform.SetParent(null, true);
                    go.transform.position = Vector3.zero;
                    go.name = Path.GetFileName(path);
                    PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + path + ".prefab");
                }
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static GameObject TopUnder(Transform holder, Transform t)
        {
            while (t.parent && t.parent != holder) t = t.parent;
            return t.gameObject;
        }

        static GameObject MonsterMarker(string name, EnemyType type)
        {
            var go = new GameObject(name, typeof(MonsterSpawn));
            go.GetComponent<MonsterSpawn>().type = type;
            return go;
        }

        static GameObject PowerMarker(PowerType type)
        {
            var go = new GameObject("PowerUp " + type, typeof(PowerUpSpot));
            go.GetComponent<PowerUpSpot>().type = type;
            return go;
        }

        static void WritePalette()
        {
            var pal = AssetDatabase.LoadAssetAtPath<DayNightPalette>(PalettePath);
            if (!pal)
            {
                pal = ScriptableObject.CreateInstance<DayNightPalette>();
                AssetDatabase.CreateAsset(pal, PalettePath);
            }
            foreach (var e in collected)
                if (e.material && EditorUtility.IsPersistent(e.material)) pal.Add(e.material, e.day, e.night);
            EditorUtility.SetDirty(pal);
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static T Marker<T>(Transform parent, string name, Vector3 pos) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go.AddComponent<T>();
        }

        /// <summary>Static geometry → batching static (Unity batches it in builds / play mode).</summary>
        static void MarkStatic(Transform root, Transform skip)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (skip && r.transform.IsChildOf(skip)) continue;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        /// <summary>Saves every runtime-created material/texture used in the scene as a project asset,
        /// so the scene is self-contained and students can edit the materials.</summary>
        public static void PersistAssets(Scene scene)
        {
            Directory.CreateDirectory(MaterialDir);
            var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true));
            var extra = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DayNightTint>(true))
                .SelectMany(t => t.entries).Select(e => e.material);
            var seen = new HashSet<Material>();
            foreach (var r in renderers)
                foreach (var m in r.sharedMaterials)
                    if (m && seen.Add(m)) Persist(m, r.gameObject.name);
            foreach (var m in extra)
                if (m && seen.Add(m)) Persist(m, m.name);
        }

        static void Persist(Material m, string hint)
        {
            if (EditorUtility.IsPersistent(m)) return;
            foreach (var prop in m.GetTexturePropertyNames())
            {
                var tex = m.GetTexture(prop);
                if (tex && !EditorUtility.IsPersistent(tex))
                    AssetDatabase.CreateAsset(tex, AssetDatabase.GenerateUniqueAssetPath(MaterialDir + Clean(tex.name) + ".asset"));
            }
            string name = Clean(m.name.StartsWith("Lit_") || m.name.StartsWith("Emi_") || m.name.StartsWith("Tr_") ? hint : m.name);
            m.name = name;
            AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(MaterialDir + name + ".mat"));
        }

        static string Clean(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            s = s.Replace(" (Instance)", "").Trim();
            return string.IsNullOrEmpty(s) ? "Material" : s;
        }
    }
}
