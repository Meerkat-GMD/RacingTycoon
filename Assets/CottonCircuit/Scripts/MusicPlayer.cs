using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    // Three decks crossfade between cues, so a quick change never cuts a song that is still fading.
    // Machine songs resume where they stopped. A song with an authored ending restarts from LoopStart:
    // the restart is scheduled on the audio clock at LoopEnd and the old deck fades out briefly.
    public sealed class MusicPlayer
    {
        const float SwitchFade = .6f, WrapTail = .15f, PitchRate = .1f, Lookahead = .5f, StallGrace = 1f;
        readonly AudioSource[] decks = new AudioSource[3];
        readonly MusicEntry[] entries = new MusicEntry[3];
        readonly bool[] loaded = new bool[3];
        readonly float[] levels = new float[3], fades = { SwitchFade, SwitchFade, SwitchFade }, started = new float[3];
        readonly Dictionary<MusicCue, float> resume = new Dictionary<MusicCue, float>();
        int active, scheduled = -1;
        double wrapAt;

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
            if (scheduled >= 0 && AudioSettings.dspTime >= wrapAt) CompleteWrap();
            else if (scheduled < 0 && loaded[active] && entries[active].LoopEnd > 0) PrepareWrap();
            for (int i = 0; i < decks.Length; i++)
            {
                bool audible = i == active && loaded[i] || i == scheduled;
                if (i != scheduled) levels[i] = Mathf.MoveTowards(levels[i], audible ? 1 : 0, deltaTime / fades[i]);
                decks[i].volume = levels[i] * entries[i].Volume * level;
                decks[i].pitch = Mathf.MoveTowards(decks[i].pitch, pitch, deltaTime * PitchRate);
                if (!audible && levels[i] <= 0 && decks[i].isPlaying) decks[i].Stop();
            }
        }

        void Switch(SoundBank bank, MusicCue cue)
        {
            CancelWrap();
            if (MusicChoice.Remembers(Current) && loaded[active]) resume[Current] = (float)Position(decks[active]);
            Current = cue;
            if (!bank.TryGet(cue, out var next)) { loaded[active] = false; fades[active] = SwitchFade; return; }
            if (Revive(next.Clip)) return;
            float at = MusicChoice.Remembers(cue) && resume.TryGetValue(cue, out float stopped) ? stopped : next.LoopStart;
            Begin(next, at, SwitchFade);
        }

        // A song that is still fading out fades back in from where it is instead of restarting.
        bool Revive(AudioClip clip)
        {
            int found = -1;
            for (int i = 0; i < decks.Length; i++)
                if (decks[i].clip == clip && decks[i].isPlaying && levels[i] > 0 && (found < 0 || levels[i] > levels[found])) found = i;
            if (found < 0) return false;
            fades[active] = SwitchFade;
            active = found; loaded[found] = true; fades[found] = SwitchFade;
            return true;
        }

        void PrepareWrap()
        {
            var deck = decks[active];
            var entry = entries[active];
            double left = (entry.LoopEnd - Position(deck)) / Mathf.Max(.1f, deck.pitch);
            bool stopped = !deck.isPlaying && Time.unscaledTime - started[active] > StallGrace;
            // After a long hitch the loop end has already passed: restart at once.
            if (left <= 0 || stopped) { Begin(entry, entry.LoopStart, WrapTail); return; }
            if (left > Lookahead) return;
            int incoming = Quietest();
            var next = decks[incoming];
            next.Stop();
            next.clip = entry.Clip; next.loop = false; next.pitch = deck.pitch;
            next.timeSamples = Samples(entry.LoopStart, entry.Clip);
            wrapAt = AudioSettings.dspTime + left;
            next.PlayScheduled(wrapAt);
            entries[incoming] = entry; loaded[incoming] = false; levels[incoming] = 1;
            scheduled = incoming;
        }

        void CompleteWrap()
        {
            fades[active] = WrapTail;
            loaded[scheduled] = true; fades[scheduled] = WrapTail; started[scheduled] = Time.unscaledTime;
            active = scheduled; scheduled = -1;
        }

        void CancelWrap()
        {
            if (scheduled < 0) return;
            decks[scheduled].Stop(); levels[scheduled] = 0; scheduled = -1;
        }

        void Begin(MusicEntry next, float at, float crossfade)
        {
            CancelWrap();
            fades[active] = crossfade;
            int incoming = Quietest();
            var deck = decks[incoming];
            deck.Stop();
            deck.clip = next.Clip;
            deck.loop = next.LoopEnd <= 0;
            deck.pitch = decks[active].pitch;
            deck.timeSamples = Samples(at, next.Clip);
            deck.Play();
            entries[incoming] = next; loaded[incoming] = true;
            levels[incoming] = 0; fades[incoming] = crossfade; started[incoming] = Time.unscaledTime;
            active = incoming;
        }

        int Quietest()
        {
            int best = -1;
            for (int i = 0; i < decks.Length; i++)
                if (i != active && i != scheduled && (best < 0 || levels[i] < levels[best])) best = i;
            return best;
        }

        static double Position(AudioSource deck) => deck.clip ? deck.timeSamples / (double)deck.clip.frequency : 0;
        static int Samples(float seconds, AudioClip clip) => Mathf.Clamp((int)(seconds * clip.frequency), 0, clip.samples - 1);
    }
}
