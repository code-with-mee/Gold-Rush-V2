using UnityEditor;

namespace GoldRush.EditorTools
{
    /// <summary>Import rules for everything in Assets/GoldRush/Art/Characters:
    /// generic rig, animations imported, and locomotion clips set to loop.</summary>
    public class CharacterImportRules : AssetPostprocessor
    {
        const string Folder = "Assets/GoldRush/Art/Characters/";

        static readonly string[] LoopKeywords = { "idle", "walk", "run", "flying", "dance", "wave", "yes" };

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.importAnimation = true;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.importCameras = false;
            mi.importLights = false;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (var c in clips)
            {
                string n = c.name.ToLowerInvariant();
                bool loop = false;
                foreach (var k in LoopKeywords) if (n.Contains(k)) { loop = true; break; }
                if (n.Contains("jump")) loop = n.Contains("jump_idle");
                c.loopTime = loop;
                c.loopPose = loop;
                // "CharacterArmature|Idle" -> "Idle"
                int bar = c.name.LastIndexOf('|');
                if (bar >= 0) c.name = c.name.Substring(bar + 1);
            }
            mi.clipAnimations = clips;
        }
    }
}
