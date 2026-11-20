using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GoldRush.EditorTools
{
    /// <summary>One-click project setup: base materials, GameAssets, MainMenu + Game scenes, build settings.
    /// Runs automatically once after the scripts compile (if anything is missing).</summary>
    [InitializeOnLoad]
    public static class GoldRushSetup
    {
        const string Root = "Assets/GoldRush";
        const string GenDir = Root + "/Materials";
        const string ResDir = Root + "/Resources";
        const string SceneDir = Root + "/Scenes";
        const string AssetsPath = ResDir + "/GoldRushAssets.asset";
        public const string MenuScene = SceneDir + "/MainMenu.unity";
        public const string GameScene = SceneDir + "/Game.unity";

        static string SmokeFlag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_smoke.flag"));
        static string SmokeRun => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_smoke.run"));
        static string TrailerFlag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_trailer.flag"));
        static string TrailerRun => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_trailer.run"));

        static GoldRushSetup()
        {
            EditorApplication.delayCall += AutoSetup;
            EditorApplication.update += PollSmokeFlag;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredEditMode) EditorSceneManager.playModeStartScene = null;
            };
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(AssetsPath) || !File.Exists(MenuScene) || !File.Exists(GameScene) || !File.Exists(LevelBaker.ScenePath(0))) Setup();
        }

        [MenuItem("Gold Rush/Setup Project", priority = 0)]
        public static void Setup()
        {
            Directory.CreateDirectory(GenDir);
            Directory.CreateDirectory(ResDir);
            Directory.CreateDirectory(SceneDir);
            AssetDatabase.Refresh();

            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, AssetsPath);
            }

            CharacterSetup.Build(ga); // models + animator controllers from Assets/GoldRush/Art/Characters
            EnsureIconSprites();

            ga.lit = LitMaterial("GR_Lit", false, false);
            ga.litEmissive = LitMaterial("GR_LitEmissive", true, false);
            ga.litTransparent = LitMaterial("GR_LitTransparent", false, true);
            ga.particle = ParticleMaterial();
            ga.skybox = SkyboxMaterial();
            EditorUtility.SetDirty(ga);
            AssetDatabase.SaveAssets();

            bool madeScenes = CreateScene(MenuScene, "GoldRushMenu", typeof(GoldRushMenu))
                            & CreateScene(GameScene, "GameManager", typeof(GameManager));

            // hand-made level scenes (generated once, then edited by hand — never overwritten here)
            if (madeScenes) LevelBaker.BakeAll(onlyMissing: true);

            // build settings: menu, the 4 level scenes, and the generated "Game" scene (daily challenge)
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScene, true) };
            for (int i = 0; i < Levels.All.Length; i++)
                if (File.Exists(LevelBaker.ScenePath(i))) list.Add(new EditorBuildSettingsScene(LevelBaker.ScenePath(i), true));
            list.Add(new EditorBuildSettingsScene(GameScene, true));
            EditorBuildSettings.scenes = list.ToArray();

            // landscape mobile game
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[GoldRush] Setup complete" + (madeScenes ? "" : " (scenes could not be created — save your open scene and run Gold Rush > Setup Project)") +
                      ". Open Assets/GoldRush/Scenes/MainMenu.unity and press Play, or use Gold Rush > Play.");

            var active = SceneManager.GetActiveScene();
            if (madeScenes && !active.isDirty && active.path != MenuScene && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        }

        static Material LitMaterial(string name, bool emissive, bool transparent)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.3f);
            m.SetFloat("_Metallic", 0f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (transparent) MakeTransparent(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
        }

        static Material ParticleMaterial()
        {
            string path = $"{GenDir}/GR_Particle.mat";
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            MakeTransparent(m);
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material SkyboxMaterial()
        {
            string path = $"{GenDir}/GR_SunsetSky.mat";
            var shader = Shader.Find("Skybox/Procedural");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetFloat("_SunDisk", 2f);
            m.SetFloat("_SunSize", 0.06f);
            m.SetFloat("_SunSizeConvergence", 3f);
            m.SetFloat("_AtmosphereThickness", 1.35f);
            m.SetColor("_SkyTint", new Color(1f, 0.62f, 0.45f));
            m.SetColor("_GroundColor", new Color(0.62f, 0.45f, 0.33f));
            m.SetFloat("_Exposure", 1.2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static bool CreateScene(string path, string rootName, System.Type component)
        {
            if (File.Exists(path)) return true;
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path))
            {
                Debug.LogWarning("[GoldRush] The open scene is untitled; cannot create " + path + " additively.");
                return false;
            }
            Scene scene;
            try { scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); }
            catch (System.Exception e) { Debug.LogWarning("[GoldRush] " + e.Message); return false; }
            var go = new GameObject(rootName);
            go.AddComponent(component);
            SceneManager.MoveGameObjectToScene(go, scene);
            bool ok = EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        [MenuItem("Gold Rush/Play", priority = 20)]
        public static void PlayFromMenu()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene);
            if (scene == null) { Setup(); scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene); }
            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Gold Rush/Run Smoke Test", priority = 21)]
        public static void RunSmokeTest()
        {
            File.WriteAllText(SmokeRun, "run");
            PlayFromMenu();
        }

        [MenuItem("Gold Rush/Record Trailer Clips", priority = 22)]
        public static void RecordTrailer()
        {
            File.WriteAllText(TrailerRun, "run");
            PlayFromMenu();
        }

        [MenuItem("Gold Rush/Reset Save Data", priority = 40)]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteKey("goldrush_save_v1");
            PlayerPrefs.Save();
            Debug.Log("[GoldRush] Save data cleared.");
        }

        [MenuItem("Gold Rush/Give 500 Gold (debug)", priority = 41)]
        public static void GiveGold()
        {
            Save.Load();
            Save.Data.gold += 500;
            Save.Persist();
            Debug.Log("[GoldRush] +500 gold. Total: " + Save.Data.gold);
        }

        // Automation hooks (used by tooling outside the editor): create Temp/gr_smoke.flag to run the
        // smoke test, or Temp/gr_preview.flag to render the character preview.
        static string JobFlag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_job.flag"));
        static string BakeMenuFlag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_bakemenu.flag"));
        static string PreviewFlag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/gr_preview.flag"));

        static void PollSmokeFlag()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (HandleFlag(SmokeFlag))
            {
                EnsureIconSprites();
                if (!File.Exists(AssetsPath) || !File.Exists(MenuScene) || !File.Exists(LevelBaker.ScenePath(0))) Setup();
                RunSmokeTest();
            }
            else if (HandleFlag(TrailerFlag))
            {
                RecordTrailer();
            }
            else if (HandleFlag(JobFlag))
            {
                // Temp/gr_job.txt holds "Namespace.Class.Method" lines: static methods to run, in order
                var log = new System.Text.StringBuilder();
                foreach (var line in File.ReadAllLines(JobFlag + ".txt"))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int dot = line.LastIndexOf('.');
                    try
                    {
                        System.Type type = null;
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                            if ((type = asm.GetType(line.Substring(0, dot))) != null) break;
                        type.GetMethod(line.Substring(dot + 1), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            .Invoke(null, null);
                        log.AppendLine("ok " + line);
                    }
                    catch (System.Exception e) { log.AppendLine("FAIL " + line + ": " + (e.InnerException ?? e)); Debug.LogException(e); }
                }
                File.WriteAllText(JobFlag + ".done", log.ToString());
            }
            else if (HandleFlag(BakeMenuFlag))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                MenuBaker.Bake();
                File.WriteAllText(BakeMenuFlag + ".done", System.DateTime.Now.ToString("O"));
            }
            else if (HandleFlag(PreviewFlag))
            {
                AssetDatabase.ImportAsset("Assets/GoldRush/Art/Characters", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                Setup(); // rebuild animator controllers + asset references
                CharacterPreview.Render();
            }
        }

        static readonly System.DateTime loadedAt = System.DateTime.Now;

        /// <summary>True when the loaded scripts are newer than every .cs file (no pending recompile).</summary>
        static bool ScriptsUpToDate()
        {
            var newest = System.DateTime.MinValue;
            foreach (var f in Directory.GetFiles("Assets/GoldRush", "*.cs", SearchOption.AllDirectories))
            {
                var t = File.GetLastWriteTime(f);
                if (t > newest) newest = t;
            }
            return loadedAt > newest; // this static field resets on every domain reload
        }

        /// <summary>Re-imports the UI icons as sprites if they were imported before the import rule existed.</summary>
        public static void EnsureIconSprites()
        {
            var ti = AssetImporter.GetAtPath("Assets/GoldRush/Resources/Icons/ic_home.png") as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
                AssetDatabase.ImportAsset("Assets/GoldRush/Resources/Icons", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        /// <summary>Returns true when the flag's action should run now. The first time a flag is seen it
        /// refreshes the AssetDatabase (works even with Auto Refresh off) and re-arms itself, so the action
        /// runs after any resulting recompile.</summary>
        static bool HandleFlag(string flag)
        {
            if (!File.Exists(flag)) return false;
            if (File.Exists(flag + ".refreshed"))
            {
                if (!ScriptsUpToDate()) return false; // wait until the new code is compiled AND loaded
                File.Delete(flag);
                File.Delete(flag + ".refreshed");
                return true;
            }
            File.Delete(flag);
            File.WriteAllText(flag + ".refreshed", "1");
            File.WriteAllText(flag, "1");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return false;
        }
    }
}
