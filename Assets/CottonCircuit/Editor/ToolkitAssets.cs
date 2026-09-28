using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace CottonCircuit.Editor
{
    public static class ToolkitAssets
    {
        const string Root = "Assets/Resources/UI/";
        const string SourceFont = "Assets/CottonCircuit/UI/Fonts/NotoSansKR.ttf";
        const string GeneratedFont = Root + "NotoSansKR.asset";

        public static bool IsRegularFace(FontAsset font) => font &&
            string.Equals(font.faceInfo.styleName, "Regular", StringComparison.OrdinalIgnoreCase);

        public static void Ensure()
        {
            // One fan stand per shelf capacity: 12 clips is the source, 6 and 9 are derived by Tools/fan-stand-variants.py.
            foreach (string clips in new[] { "", "6", "9" })
            {
                string rackPath = "Assets/CottonCircuit/UI/Art/CottonCandyFanStand" + clips + ".png";
                AssetDatabase.ImportAsset(rackPath, ImportAssetOptions.ForceSynchronousImport);
                var rackImporter = AssetImporter.GetAtPath(rackPath) as TextureImporter;
                if (rackImporter != null && (rackImporter.mipmapEnabled || !rackImporter.alphaIsTransparency ||
                    rackImporter.textureCompression != TextureImporterCompression.Uncompressed))
                {
                    rackImporter.mipmapEnabled = false;
                    rackImporter.alphaIsTransparency = true;
                    rackImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    rackImporter.filterMode = FilterMode.Bilinear;
                    rackImporter.maxTextureSize = 2048;
                    rackImporter.SaveAndReimport();
                }
            }
            // This source is a static wght=400 instance. Import it before inspecting
            // generated data so a former variable-font Thin face cannot be reused.
            AssetDatabase.ImportAsset(SourceFont, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFont);
            if (!source) throw new InvalidOperationException("Missing bundled Noto Sans KR Regular font.");
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(GeneratedFont);
            if (font && !IsRegularFace(font))
            {
                if (!AssetDatabase.DeleteAsset(GeneratedFont))
                    throw new InvalidOperationException("Could not replace the outdated UI font asset.");
                font = null;
            }
            if (!font)
            {
                font = FontAsset.CreateFontAsset(source, 40, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (!IsRegularFace(font))
                    throw new InvalidOperationException("UI font source must contain a genuine Regular face; got " + (font ? font.faceInfo.styleName : "no face") + ".");
                font.name = "Noto Sans KR Regular UI";
                AssetDatabase.CreateAsset(font, GeneratedFont);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            }
            bool regularFace = IsRegularFace(font);
            if (!regularFace) throw new InvalidOperationException("Generated UI font is not Regular.");
            Debug.Log("COTTON_UI_FONT_REGULAR=" + regularFace + " family=" + font.faceInfo.familyName + " style=" + font.faceInfo.styleName);
            var text = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(Root + "PanelTextSettings.asset");
            if (!text)
            {
                text = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(text, Root + "PanelTextSettings.asset");
            }
            text.defaultFontAsset = font;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Root + "PanelSettings.asset");
            if (!panel)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, Root + "PanelSettings.asset");
            }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1600, 900);
            panel.screenMatchMode = PanelScreenMatchMode.Expand;
            panel.textSettings = text;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Root + "CottonTheme.tss");
            EditorUtility.SetDirty(font); EditorUtility.SetDirty(text); EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            foreach (string path in Directory.GetFiles(Root, "*.uss")) AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Root, "*.uxml"))
                if (!AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path.Replace('\\', '/')))
                    throw new InvalidOperationException("Invalid UI document: " + path);
        }
    }
}
