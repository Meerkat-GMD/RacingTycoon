using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Music stays compressed in memory so resume positions and scheduled loop restarts seek exactly;
    // short effects and loops decompress once on load.
    public sealed class AudioImportRules : AssetPostprocessor
    {
        public const string Root = "Assets/CottonCircuit/Audio/";
        public override uint GetVersion() => 2;
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            bool music = assetPath.StartsWith(Root + "Music/");
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? .7f : .8f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
        }
    }
}
