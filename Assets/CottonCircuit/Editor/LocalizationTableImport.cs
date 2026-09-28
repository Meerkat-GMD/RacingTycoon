using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // .tsv has no built-in Unity importer; without this, strings.tsv imports as a generic
    // DefaultImporter asset and Resources.Load<TextAsset> returns null at runtime.
    [ScriptedImporter(1, "tsv")]
    public sealed class LocalizationTableImport : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("text", asset);
            ctx.SetMainObject(asset);
        }
    }
}
