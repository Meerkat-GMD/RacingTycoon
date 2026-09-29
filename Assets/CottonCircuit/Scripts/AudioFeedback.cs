using System.Collections.Generic;
using UnityEngine;
namespace CottonCircuit
{
    public class AudioFeedback : MonoBehaviour
    {
        const float MusicLevel = .7f, PausedMusic = .4f, CoinDelay = .15f, JingleDelay = .8f, BoostVolume = .25f;
        public SoundBank Sounds;
        GameController game;
        readonly AudioSource[] pool = new AudioSource[8];
        int next;
        AudioSource engine, skid, wind, hum;
        AudioClip boost, superBoost, skidClip, windClip;
        MusicPlayer music;
        readonly Dictionary<Sound, float> lastTimes = new Dictionary<Sound, float>();
        readonly List<KeyValuePair<float, Sound>> queued = new List<KeyValuePair<float, Sound>>();
        readonly Dictionary<Sound, int> counts = new Dictionary<Sound, int>();
        public bool Muted => SettingsStore.Current.Muted;
        public float MusicVolume => SettingsStore.Current.MusicVolume;
        public float EffectsVolume => SettingsStore.Current.EffectsVolume;
        public MusicCue Music => music.Current;
        // How often a sound was actually heard; the development smoke check reads it.
        public int Played(Sound id) => counts.TryGetValue(id, out int count) ? count : 0;

        void Awake()
        {
            game = GetComponent<GameController>();
            for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
            boost = BoostSound(false); superBoost = BoostSound(true);
            skidClip = Noise("Tire slide"); windClip = Noise("Wind");
            engine = Loop(Sounds.EngineLoop); skid = Loop(skidClip); wind = Loop(windClip); hum = Loop(Sounds.MachineHum);
            music = new MusicPlayer(gameObject);
        }

        void OnEnable() { ToolkitUI.ButtonPressed += Click; SettingsStore.Changed += ApplySettings; }
        void OnDisable() { ToolkitUI.ButtonPressed -= Click; SettingsStore.Changed -= ApplySettings; }
        void Click() { Play(Sound.UiClick); }

        void ApplySettings()
        {
            if (!Muted || engine == null) return;
            foreach (var source in pool) source.Stop();
            queued.Clear();
            engine.volume = skid.volume = wind.volume = hum.volume = 0;
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;
            for (int i = queued.Count - 1; i >= 0; i--)
                if (queued[i].Key <= now) { var sound = queued[i].Value; queued.RemoveAt(i); Play(sound); }
            var scene = game.MusicScene;
            bool paused = game.Session != null && game.Session.Paused;
            float level = Muted ? 0 : MusicLevel * MusicVolume * (paused ? PausedMusic : 1);
            music.Update(Sounds, MusicChoice.Cue(scene), level, MusicChoice.Hurry(scene) ? MusicChoice.HurryPitch : 1, Time.unscaledDeltaTime);
        }

        public void Play(Sound id)
        {
            if (Muted || !Sounds.TryGet(id, out var entry)) return;
            float now = Time.unscaledTime;
            if (lastTimes.TryGetValue(id, out float last) && now - last < entry.MinInterval) return;
            lastTimes[id] = now;
            counts[id] = Played(id) + 1;
            Emit(entry.Clip, entry.Volume, 1 + Random.Range(-entry.PitchJitter, entry.PitchJitter));
        }

        void PlayAfter(Sound id, float delay) { queued.Add(new KeyValuePair<float, Sound>(Time.unscaledTime + delay, id)); }

        public void PlaySale(bool starBonus)
        {
            Play(starBonus ? Sound.StarBonus : Sound.DeliverSuccess);
            PlayAfter(Sound.Coins, CoinDelay);
        }

        public void PlayClosing(bool loss)
        {
            Play(Sound.ClosingBell); Play(Sound.EngineStop);
            PlayAfter(loss ? Sound.LossJingle : Sound.ProfitJingle, JingleDelay);
        }

        public void PlayBoost(int tier) { if (!Muted) Emit(tier == 2 ? superBoost : boost, BoostVolume, 1); }

        void Emit(AudioClip clip, float volume, float pitch)
        {
            var source = pool[next]; next = (next + 1) % pool.Length;
            source.clip = clip; source.volume = volume * EffectsVolume; source.pitch = pitch; source.Play();
        }

        public void UpdateDriving(KartController kart, bool active)
        {
            if (!engine) return;
            bool audible = active && !Muted;
            bool downhill = kart.DriveModel.Style == DrivingStyle.Downhill;
            float speed = Mathf.Clamp01(kart.Speed / (downhill ? 45 : 32));
            engine.volume = audible ? Mathf.Lerp(.25f, 1f, speed) * Sounds.EngineVolume * EffectsVolume : 0;
            engine.pitch = .8f + speed * .8f;
            hum.volume = audible ? Sounds.MachineHumVolume * EffectsVolume : 0;
            skid.volume = audible && kart.Drifting && kart.Speed > 5 ? .13f * EffectsVolume : 0;
            skid.pitch = 1 + kart.Speed / 40;
            wind.volume = audible ? Mathf.Clamp01((kart.Speed - 12) / (downhill ? 43 : 28)) * (kart.Boosting ? .18f : downhill ? .15f : .09f) * EffectsVolume : 0;
            wind.pitch = .7f + kart.Speed / 60;
        }

        AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.clip = clip; source.loop = true; source.volume = 0; source.Play();
            return source;
        }

        static AudioClip Noise(string name)
        {
            const int count = 22050; var data = new float[count]; var random = new System.Random(71); float low = 0;
            for (int i = 0; i < count; i++) { low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, .18f); data[i] = low * .65f; }
            var clip = AudioClip.Create(name, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        static AudioClip BoostSound(bool strong)
        {
            int count = strong ? 15435 : 9922; var data = new float[count];
            var random = new System.Random(strong ? 103 : 91); float noise = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f, p = (float)i / count;
                noise = Mathf.Lerp(noise, (float)random.NextDouble() * 2 - 1, .4f);
                float envelope = Mathf.Min(1, p * 18) * Mathf.Pow(1 - p, 1.6f);
                data[i] = (noise * .85f + Mathf.Sin(2 * Mathf.PI * (100 * t + 220 * t * t)) * .22f) * envelope;
            }
            var clip = AudioClip.Create(strong ? "Super sugar rush" : "Sugar rush", count, 1, 22050, false);
            clip.SetData(data, 0); return clip;
        }

        void OnDestroy() { foreach (var clip in new[] { boost, superBoost, skidClip, windClip }) if (clip) Destroy(clip); }
    }
}
