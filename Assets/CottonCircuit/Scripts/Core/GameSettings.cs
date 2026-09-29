using System;
using System.Collections.Generic;

namespace CottonCircuit
{
    /// <summary>Player preferences saved as settings.json next to the game save. Display mode and size are remembered by Unity itself.</summary>
    [Serializable] public class GameSettings
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public string Language = "";   // "" until the player picks one: follow the OS.
        public float MusicVolume = 1, EffectsVolume = 1;
        public bool Muted;

        public void Sanitize()
        {
            Version = CurrentVersion;
            if (Strings.Parse(Language) == null) Language = "";
            MusicVolume = Clamp(MusicVolume);
            EffectsVolume = Clamp(EffectsVolume);
        }

        // An unreadable volume (NaN) falls back to the default 100%; anything else, infinities too, is clamped to 0..1.
        static float Clamp(float value)
        {
            if (float.IsNaN(value)) return 1;
            return Math.Max(0, Math.Min(1, value));
        }
    }

    public struct ScreenSize : IEquatable<ScreenSize>
    {
        public readonly int Width, Height;
        public ScreenSize(int width, int height) { Width = width; Height = height; }
        public bool Equals(ScreenSize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is ScreenSize other && Equals(other);
        public override int GetHashCode() => Width * 100003 + Height;
        public override string ToString() => Width + " × " + Height;
    }

    public static class ScreenSizes
    {
        /// <summary>Distinct sizes that fit the monitor, plus the current window size, smallest first.</summary>
        public static List<ScreenSize> Choices(IEnumerable<ScreenSize> available, ScreenSize monitor, ScreenSize current)
        {
            var list = new List<ScreenSize>();
            foreach (var size in available)
                if (size.Width > 0 && size.Height > 0 && size.Width <= monitor.Width && size.Height <= monitor.Height && !list.Contains(size)) list.Add(size);
            if (current.Width > 0 && current.Height > 0 && !list.Contains(current)) list.Add(current);
            list.Sort((a, b) => a.Width != b.Width ? a.Width.CompareTo(b.Width) : a.Height.CompareTo(b.Height));
            return list;
        }
    }
}
