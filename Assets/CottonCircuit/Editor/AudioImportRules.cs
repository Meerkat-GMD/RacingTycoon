using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Music streams from disk; short effects and loops decompress once on load.
    public sealed class AudioImportRules : AssetPostprocessor
    {
        public const string Root = "Assets/CottonCircuit/Audio/";
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            bool music = assetPath.StartsWith(Root + "Music/");
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? .7f : .8f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
        }
    }
}
