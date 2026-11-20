using UnityEditor;

namespace GoldRush.EditorTools
{
    /// <summary>Icons in Assets/GoldRush/Resources/Icons import as crisp UI sprites (white, tinted in code).</summary>
    public class UIImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/GoldRush/Resources/Icons/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
        }
    }
}
