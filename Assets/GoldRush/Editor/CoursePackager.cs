using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Exports the course packages to GoldRush_Export/Packages/.
    /// The starter package (Phase 0) holds only assets, never scripts: art, sounds, icons, fonts, UI shapes,
    /// the prefab kit and the materials it uses. Import settings are saved into the .meta files first, so the
    /// files import the same way in a brand-new student project (where our import scripts don't exist).
    /// </summary>
    public static class CoursePackager
    {
        static string PackageDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../GoldRush_Export/Packages"));

        [MenuItem("Gold Rush/Course/Export Starter Package", priority = 90)]
        public static void ExportStarter()
        {
            BakeImportSettings();
            var paths = new HashSet<string>();
            void AddDir(string dir, string pattern = "*")
            {
                if (!Directory.Exists(dir)) return;
                foreach (var f in Directory.GetFiles(dir, pattern, SearchOption.AllDirectories))
                    if (!f.EndsWith(".meta") && !f.EndsWith(".cs")) paths.Add(f.Replace('\\', '/'));
            }
            AddDir("Assets/GoldRush/Art/Characters/Hero");
            AddDir("Assets/GoldRush/Art/Characters/Monsters");
            AddDir("Assets/GoldRush/Art/Characters/Licenses");
            AddDir("Assets/GoldRush/Art/Audio");
            AddDir("Assets/GoldRush/Resources/Icons");
            AddDir("Assets/GoldRush/Resources/Fonts");
            AddDir("Assets/GoldRush/Resources/UISprites");
            AddDir("Assets/GoldRush/Materials", "GR_*.mat");
            paths.Add("Assets/GoldRush/Art/Textures/Grid.png");

            // the kit prefabs + everything they use (materials, textures, fonts), but no scripts
            var kit = Directory.GetFiles("Assets/GoldRush/Prefabs/Level", "*.prefab")
                .Concat(Directory.GetFiles("Assets/GoldRush/Prefabs/Gameplay", "*.prefab")).Select(p => p.Replace('\\', '/')).ToArray();
            foreach (var dep in AssetDatabase.GetDependencies(kit, true))
                if (!dep.EndsWith(".cs") && dep.StartsWith("Assets/")) paths.Add(dep);
            foreach (var p in kit)
                if (File.ReadAllText(p).Contains("m_Script:")) Debug.LogError("[GoldRush] Kit prefab has a script: " + p);

            Directory.CreateDirectory(PackageDir);
            string outPath = Path.Combine(PackageDir, "GoldRush_Phase00_Project.unitypackage");
            AssetDatabase.ExportPackage(paths.OrderBy(p => p).ToArray(), outPath, ExportPackageOptions.Default);
            File.WriteAllLines(Path.Combine(PackageDir, "GoldRush_Phase00_Project.txt"), paths.OrderBy(p => p));
            Debug.Log($"[GoldRush] Starter package: {paths.Count} files → {outPath}");
        }

        /// <summary>Scenes + everything they use, but no scripts (students have their own), plus the link fixer.</summary>
        static void ExportAssetsWithoutScripts(string[] roots, string name)
        {
            var paths = new HashSet<string>();
            foreach (var dep in AssetDatabase.GetDependencies(roots, true))
                if (!dep.EndsWith(".cs") && dep.StartsWith("Assets/")) paths.Add(dep);
            paths.Add("Assets/GoldRush/Editor/FixScriptLinks.cs");
            paths.Add("Assets/GoldRush/Editor/ScriptLinks.json");
            Directory.CreateDirectory(PackageDir);
            string outPath = Path.Combine(PackageDir, name + ".unitypackage");
            AssetDatabase.ExportPackage(paths.OrderBy(p => p).ToArray(), outPath, ExportPackageOptions.Default);
            File.WriteAllLines(Path.Combine(PackageDir, name + ".txt"), paths.OrderBy(p => p));
            Debug.Log($"[GoldRush] {name}: {paths.Count} files");
        }

        [MenuItem("Gold Rush/Course/Export Levels 2-4 Package", priority = 91)]
        public static void ExportLevels2to4()
        {
            ExportAssetsWithoutScripts(new[] { "Assets/GoldRush/Scenes/Level2.unity", "Assets/GoldRush/Scenes/Level3.unity",
                "Assets/GoldRush/Scenes/Level4.unity", "Assets/GoldRush/Scenes/Game.unity" }, "GoldRush_Levels2to4");
        }

        /// <summary>Saves the three dialogs of Level1's GameUI as prefabs and exports them (phase 9 shortcut).</summary>
        [MenuItem("Gold Rush/Course/Export Game UI Dialogs Package", priority = 92)]
        public static void ExportDialogs()
        {
            const string dir = "Assets/GoldRush/Prefabs/UI/";
            Directory.CreateDirectory(dir);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/GoldRush/Scenes/Level1.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            var ui = scene.GetRootGameObjects().Select(g => g.GetComponent<GameUI>()).FirstOrDefault(g => g);
            var made = new List<string>();
            if (ui)
            {
                var modal = ui.transform.Find("Modal");
                foreach (var (child, prefab) in new[] { ("Pause", "Dialog_Pause"), ("Win", "Dialog_Win"), ("Lose", "Dialog_Lose") })
                {
                    var t = modal ? modal.Find(child) : null;
                    if (!t) { Debug.LogWarning("[GoldRush] dialog not found: " + child); continue; }
                    string path = dir + prefab + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(t.gameObject, path); // a copy: the scene keeps its own dialog
                    made.Add(path);
                }
            }
            AssetDatabase.SaveAssets();
            ExportAssetsWithoutScripts(made.ToArray(), "GoldRush_GameUIDialogs");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GoldRushSetup.MenuScene, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        /// <summary>Writes the import settings our AssetPostprocessors apply into the .meta files.</summary>
        static void BakeImportSettings()
        {
            foreach (var dir in new[] { "Assets/GoldRush/Resources/Icons", "Assets/GoldRush/Resources/UISprites" })
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                    if (ti == null) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = true;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.filterMode = FilterMode.Bilinear;
                    EditorUtility.SetDirty(ti);
                    ti.SaveAndReimport();
                }
            foreach (var f in Directory.GetFiles("Assets/GoldRush/Art/Characters", "*.fbx", SearchOption.AllDirectories))
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                if (mi == null) continue;
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.importAnimation = true;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.importCameras = false;
                mi.importLights = false;
                if (mi.clipAnimations == null || mi.clipAnimations.Length == 0) mi.clipAnimations = mi.defaultClipAnimations;
                EditorUtility.SetDirty(mi);
                mi.SaveAndReimport();
            }
        }
    }
}
