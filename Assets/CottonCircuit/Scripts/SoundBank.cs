using System;
using UnityEngine;
namespace CottonCircuit
{
    public enum Sound
    {
        UiClick, TutorialPopup, SugarShake, CandyDrop, CandyExtract, DeliverSuccess, StarBonus, Coins,
        DeliverFail, Trash, Purchase, ClosingBell, ClosingJingle, EngineStart, EngineStop, WallHit
    }

    [Serializable] public struct SoundEntry
    {
        public Sound Id;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume;
        public float MinInterval;   // a repeat sooner than this is skipped
        public float PitchJitter;   // random +/- pitch for sounds that repeat quickly
    }

    [Serializable] public struct MusicEntry
    {
        public MusicCue Cue;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume;
        // LoopEnd 0 loops the whole clip. Otherwise playback returns to LoopStart at LoopEnd,
        // a whole number of bars before the authored ending; the file itself is never edited.
        public float LoopStart, LoopEnd;
    }

    // Filled by the editor SoundCatalog from Assets/CottonCircuit/Audio.
    public class SoundBank : ScriptableObject
    {
        public SoundEntry[] Sounds = new SoundEntry[0];
        public MusicEntry[] Music = new MusicEntry[0];
        public AudioClip EngineLoop, MachineHum;
        [Range(0, 1)] public float EngineVolume = .3f, MachineHumVolume = .18f;

        public bool TryGet(Sound id, out SoundEntry entry)
        {
            foreach (var candidate in Sounds) if (candidate.Id == id && candidate.Clip) { entry = candidate; return true; }
            entry = default; return false;
        }

        public bool TryGet(MusicCue cue, out MusicEntry entry)
        {
            foreach (var candidate in Music) if (candidate.Cue == cue && candidate.Clip) { entry = candidate; return true; }
            entry = default; return false;
        }
    }
}
