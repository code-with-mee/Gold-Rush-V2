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
