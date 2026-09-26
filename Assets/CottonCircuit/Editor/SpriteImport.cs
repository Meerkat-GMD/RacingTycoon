using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Every PNG under Assets/CottonCircuit/Sprites/ imports as a single UI sprite
    // (spec import rule: Sprite, Single, no mipmaps, alpha is transparency, high quality compression).
    public sealed class SpriteImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/CottonCircuit/Sprites/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.sRGBTexture = true;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.maxTextureSize = 2048;
        }
    }
}
