using System.IO;
using UnityEditor;
using UnityEngine;
namespace CottonCircuit.Editor
{
    // Fills the SoundBank from Assets/CottonCircuit/Audio. Sources are listed in Audio/THIRD-PARTY.md.
    // Song volumes pull the measured loudness toward -20 LUFS. The two songs with authored endings
    // loop over whole bars from the beat grid (103.35 BPM x 48 bars, 90 BPM x 44 bars).
    public static class SoundCatalog
    {
        const string Root = "Assets/CottonCircuit/Audio/";
        public const string BankPath = "Assets/CottonCircuit/Data/SoundBank.asset";

        static AudioClip Load(string file)
        {
            string path = Root + file;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new FileNotFoundException("Missing audio " + path, path);
            return clip;
        }

        static SoundEntry Effect(Sound id, string file, float volume, float minInterval = .04f, float jitter = 0) =>
            new SoundEntry { Id = id, Clip = Load("Sfx/" + file), Volume = volume, MinInterval = minInterval, PitchJitter = jitter };

        static MusicEntry Song(MusicCue cue, string file, float volume, float loopStart = 0, float loopEnd = 0) =>
            new MusicEntry { Cue = cue, Clip = Load("Music/" + file), Volume = volume, LoopStart = loopStart, LoopEnd = loopEnd };

        public static SoundBank Build()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (!bank) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
            bank.Sounds = new[] {
                Effect(Sound.UiClick, "UiClick.ogg", .5f, .05f),
                Effect(Sound.TutorialPopup, "TutorialPopup.wav", .6f, .3f),
                Effect(Sound.SugarShake, "SugarShake.ogg", .5f, .35f, .08f),
                Effect(Sound.CandyDrop, "CandyDrop.ogg", .7f, .08f),
                Effect(Sound.CandyExtract, "CandyExtract.ogg", .6f),
                Effect(Sound.DeliverSuccess, "DeliverSuccess.ogg", .7f),
                Effect(Sound.StarBonus, "StarBonus.ogg", .6f),
                Effect(Sound.Coins, "Coins.ogg", .5f),
                Effect(Sound.DeliverFail, "DeliverFail.wav", .6f, .2f),
                Effect(Sound.Trash, "Trash.ogg", .7f),
                Effect(Sound.Purchase, "Purchase.wav", .7f),
                Effect(Sound.ClosingBell, "ClosingBell.ogg", .7f),
                Effect(Sound.ClosingJingle, "ClosingJingle.ogg", .8f),
                Effect(Sound.EngineStart, "EngineStart.ogg", .35f, .5f),
                Effect(Sound.EngineStop, "EngineStop.ogg", .5f, .5f),
                Effect(Sound.WallHit, "WallHit.ogg", .6f, .25f, .06f),
            };
            bank.Music = new[] {
                Song(MusicCue.Title, "Title_CasualTheme1.ogg", .75f),
                Song(MusicCue.Story, "Story_CasualTheme2Acoustic.ogg", 1f),
                Song(MusicCue.Tutorial, "Tutorial_EasyPeasy.ogg", .49f),
                Song(MusicCue.Preparation, "Preparation_CasualTheme3Acoustic.ogg", .8f),
                Song(MusicCue.Machine1, "Machine1_FloweySpeedway.ogg", .66f),
                Song(MusicCue.Machine2, "Machine2_SecretStage.ogg", .35f, 0f, 111.47f),
                Song(MusicCue.Machine3, "Machine3_TimeTrial.ogg", .31f, .6f, 117.93f),
            };
            bank.EngineLoop = Load("Sfx/EngineLoop.wav");
            bank.MachineHum = Load("Sfx/MachineHum.wav");
            bank.EngineVolume = .3f; bank.MachineHumVolume = .18f;
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }
    }
}
