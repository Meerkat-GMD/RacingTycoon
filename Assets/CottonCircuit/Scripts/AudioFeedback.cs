using UnityEngine;
namespace CottonCircuit
{
    public class AudioFeedback : MonoBehaviour
    {
        AudioSource source, engine, skid, wind;
        AudioClip purchase, complete, select, boost, superBoost, engineClip, skidClip, windClip;
        bool wasDriving;
        public bool Muted { get; private set; }
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .18f;
            select = Tone(520, .08f); purchase = Tone(880, .18f); complete = Tone(660, .35f);
            boost = BoostSound(false); superBoost = BoostSound(true);
            engineClip = Motor(false); skidClip = Motor(true); windClip = Motor(true);
            engine = Loop(engineClip); skid = Loop(skidClip); wind = Loop(windClip);
        }
        AudioSource Loop(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.clip = clip; s.loop = true; s.volume = 0; s.Play(); return s;
        }
        static AudioClip Motor(bool noise)
        {
            const int count = 22050; var data = new float[count]; var random = new System.Random(71); float low = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f;
                low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, .18f);
                data[i] = noise ? low * .65f : (Mathf.Sin(2 * Mathf.PI * 70 * t) * .4f + Mathf.Sin(2 * Mathf.PI * 140 * t) * .14f + Mathf.Sin(2 * Mathf.PI * 210 * t) * .06f);
            }
            var clip = AudioClip.Create(noise ? "Tire slide" : "Electric sugar motor", count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }
        static AudioClip Tone(float frequency, float seconds, bool sweep = false)
        {
            int count = (int)(22050 * seconds); var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f;
                data[i] = Mathf.Sin(2 * Mathf.PI * (frequency * t + (sweep ? 600 * t * t : 0))) * Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * (sweep ? 2 : 8));
            }
            var clip = AudioClip.Create("Cotton chime", count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }
        public void UpdateDriving(KartController kart, bool active)
        {
            if (!engine) return;
            if (wasDriving && !active) source.Stop();
            wasDriving = active;
            bool audible = active && !Muted;
            engine.volume = audible ? Mathf.Lerp(.035f, .17f, Mathf.Clamp01(kart.Speed / 32)) : 0;
            bool downhill = kart.DriveModel.Style == DrivingStyle.Downhill;
            engine.pitch = .6f + kart.Speed / (downhill ? 26 : 20);
            skid.volume = audible && kart.Drifting && kart.Speed > 5 ? .13f : 0;
            skid.pitch = 1 + kart.Speed / 40;
            wind.volume = audible ? Mathf.Clamp01((kart.Speed - 12) / (downhill ? 43 : 28)) * (kart.Boosting ? .18f : downhill ? .15f : .09f) : 0;
            wind.pitch = .7f + kart.Speed / 60;
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
        public void PlayBoost(int tier) { if (!Muted && source) source.PlayOneShot(tier == 2 ? superBoost : boost, 1.4f); }
        public void Play(int kind) { if (!Muted && source) source.PlayOneShot(kind == 0 ? select : kind == 1 ? purchase : kind == 3 ? boost : complete); }
        public void Toggle() { Muted = !Muted; if (Muted) { source.Stop(); engine.volume = 0; skid.volume = 0; wind.volume = 0; } }
        void OnDestroy() { foreach (var clip in new[] { purchase, complete, select, boost, superBoost, engineClip, skidClip, windClip }) if (clip) Destroy(clip); }
    }
}
