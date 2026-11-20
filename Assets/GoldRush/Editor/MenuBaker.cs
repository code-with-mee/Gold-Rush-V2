using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Saves the main menu as real GameObjects in Assets/GoldRush/Scenes/MainMenu.unity:
    /// the 3D stage (diorama, hero, monsters, camera, lantern) and a Canvas with the Title, Level Select, Shop
    /// and Settings screens. After this, move / resize / recolour anything in the Scene view and Inspector.
    /// GoldRushMenu only connects the buttons (MenuAction) and fills in the data when the game runs.
    /// </summary>
    public static class MenuBaker
    {
        const string SpriteDir = "Assets/GoldRush/Resources/UISprites/";
        const string PalettePath = "Assets/GoldRush/Resources/DayNightPalette.asset";

        [MenuItem("Gold Rush/Level Design/Rebuild Menu Scene (overwrites!)", priority = 62)]
        static void BakeMenuItem()
        {
            if (!EditorUtility.DisplayDialog("Rebuild menu scene",
                    "This regenerates MainMenu.unity and OVERWRITES any changes you made to it.", "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Bake();
        }

        public static void Bake()
        {
            ExportUISprites();
            Directory.CreateDirectory(LevelBaker.MaterialDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var menu = new GameObject("GoldRushMenu").AddComponent<GoldRushMenu>();
            menu.BuildForEditor();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // the diorama's colours change between day and night: remember them in the palette
            var tints = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DayNightTint>(true)).ToList();
            LevelBaker.PersistAssets(scene);
            var pal = AssetDatabase.LoadAssetAtPath<DayNightPalette>(PalettePath);
            if (pal)
            {
                foreach (var t in tints)
                    foreach (var e in t.entries)
                        if (e.material && EditorUtility.IsPersistent(e.material)) pal.Add(e.material, e.day, e.night);
                EditorUtility.SetDirty(pal);
            }

            EditorSceneManager.SaveScene(scene, GoldRushSetup.MenuScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[GoldRush] Menu scene saved: " + GoldRushSetup.MenuScene);
        }

        /// <summary>
        /// Saves every procedural UI shape (Sprites.RoundRect, Pill, Circle, ...) as a PNG sprite in
        /// Resources/UISprites, so UI Images in scenes reference real files. Existing PNGs are kept.
        /// </summary>
        [MenuItem("Gold Rush/Level Design/Export UI Sprites", priority = 63)]
        public static void ExportUISprites()
        {
            Directory.CreateDirectory(SpriteDir);
            Sprites.ClearCache();
            var props = typeof(Sprites).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Sprite) && p.GetIndexParameters().Length == 0);
            int made = 0;
            foreach (var p in props)
            {
                var sp = (Sprite)p.GetValue(null);
                if (!sp) continue;
                string path = SpriteDir + sp.name + ".png";
                if (File.Exists(path)) continue;
                File.WriteAllBytes(path, sp.texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100;
                ti.spriteBorder = sp.border;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
                made++;
            }
            Sprites.ClearCache(); // from now on Sprites.* loads the PNG files
            if (made > 0) Debug.Log($"[GoldRush] Saved {made} UI sprites to {SpriteDir}");
        }
    }
}
