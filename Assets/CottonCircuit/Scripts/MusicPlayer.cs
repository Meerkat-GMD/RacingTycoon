using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    // Two decks crossfade between cues. Machine songs resume where they stopped; songs with an
    // authored ending wrap from LoopEnd to LoopStart by crossfading into the other deck.
    public sealed class MusicPlayer
    {
        const float SwitchFade = .6f, WrapFade = 1f, PitchRate = .1f;
        readonly AudioSource[] decks = new AudioSource[2];
        readonly MusicEntry[] entries = new MusicEntry[2];
        readonly bool[] loaded = new bool[2];
        readonly float[] levels = new float[2], fades = { SwitchFade, SwitchFade };
        readonly Dictionary<MusicCue, float> resume = new Dictionary<MusicCue, float>();
        int active;

        public MusicCue Current { get; private set; }

        public MusicPlayer(GameObject host)
        {
            for (int i = 0; i < decks.Length; i++)
            {
                decks[i] = host.AddComponent<AudioSource>();
                decks[i].playOnAwake = false; decks[i].volume = 0;
            }
        }

        public void Update(SoundBank bank, MusicCue cue, float level, float pitch, float deltaTime)
        {
            if (cue != Current) Switch(bank, cue);
            var deck = decks[active];
            if (loaded[active] && deck.isPlaying && MusicChoice.WrapDue(deck.time, entries[active].LoopEnd))
                Begin(entries[active], entries[active].LoopStart, WrapFade);
            for (int i = 0; i < decks.Length; i++)
            {
                bool audible = i == active && loaded[i];
                levels[i] = Mathf.MoveTowards(levels[i], audible ? 1 : 0, deltaTime / fades[i]);
                decks[i].volume = levels[i] * entries[i].Volume * level;
                decks[i].pitch = Mathf.MoveTowards(decks[i].pitch, pitch, deltaTime * PitchRate);
                if (!audible && levels[i] <= 0 && decks[i].isPlaying) decks[i].Stop();
            }
        }

        void Switch(SoundBank bank, MusicCue cue)
        {
            if (MusicChoice.Remembers(Current) && loaded[active]) resume[Current] = decks[active].time;
            Current = cue;
            if (!bank.TryGet(cue, out var next)) { loaded[active] = false; fades[active] = SwitchFade; return; }
            float at = MusicChoice.Remembers(cue) && resume.TryGetValue(cue, out float stopped) ? stopped : next.LoopStart;
            Begin(next, at, SwitchFade);
        }

        void Begin(MusicEntry next, float at, float crossfade)
        {
            fades[active] = crossfade;
            int incoming = 1 - active;
            var deck = decks[incoming];
            deck.Stop();
            deck.clip = next.Clip;
            deck.loop = next.LoopEnd <= 0;
            deck.pitch = decks[active].pitch;
            deck.Play();
            deck.time = Mathf.Clamp(at, 0, next.Clip.length - .05f);
            entries[incoming] = next; loaded[incoming] = true;
            levels[incoming] = 0; fades[incoming] = crossfade;
            active = incoming;
        }
    }
}
