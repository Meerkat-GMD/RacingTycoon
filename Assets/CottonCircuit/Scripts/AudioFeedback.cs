using UnityEngine;
namespace CottonCircuit
{
    public class AudioFeedback : MonoBehaviour
    {
        AudioSource source, engine, skid;
        AudioClip purchase, complete, select, boost, engineClip, skidClip;
        public bool Muted { get; private set; }
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .18f;
            select = Tone(520, .08f); purchase = Tone(880, .18f); complete = Tone(660, .35f); boost = Tone(220, .5f, true);
            engineClip = Motor(false); skidClip = Motor(true);
            engine = Loop(engineClip); skid = Loop(skidClip);
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
            bool audible = active && !Muted;
            engine.volume = audible ? Mathf.Lerp(.035f, .15f, Mathf.Clamp01(kart.Speed / 26)) : 0;
            engine.pitch = .65f + kart.Speed / 19;
            skid.volume = audible && kart.Drifting ? .13f : 0;
            skid.pitch = 1 + kart.Speed / 40;
        }
        public void Play(int kind) { if (!Muted && source) source.PlayOneShot(kind == 0 ? select : kind == 1 ? purchase : kind == 3 ? boost : complete); }
        public void Toggle() { Muted = !Muted; if (Muted) { source.Stop(); engine.volume = 0; skid.volume = 0; } }
        void OnDestroy() { foreach (var clip in new[] { purchase, complete, select, boost, engineClip, skidClip }) if (clip) Destroy(clip); }
    }
}
