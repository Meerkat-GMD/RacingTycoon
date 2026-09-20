using UnityEngine;
namespace CottonCircuit
{
    public class AudioFeedback : MonoBehaviour
    {
        AudioSource source;
        AudioClip purchase, complete, select;
        public bool Muted { get; private set; }
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.volume = .18f;
            select = Tone(520, .08f); purchase = Tone(880, .18f); complete = Tone(660, .35f);
        }
        static AudioClip Tone(float frequency, float seconds)
        {
            int count = (int)(22050 * seconds);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f;
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 8);
            }
            var clip = AudioClip.Create("Cotton chime", count, 1, 22050, false);
            clip.SetData(data, 0); return clip;
        }
        public void Play(int kind) { if (!Muted && source) source.PlayOneShot(kind == 0 ? select : kind == 1 ? purchase : complete); }
        public void Toggle() { Muted = !Muted; }
        void OnDestroy() { if (purchase) Destroy(purchase); if (complete) Destroy(complete); if (select) Destroy(select); }
    }
}
