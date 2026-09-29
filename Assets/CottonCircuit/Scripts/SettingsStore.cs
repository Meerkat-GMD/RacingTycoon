using System;
using System.IO;
using UnityEngine;

namespace CottonCircuit
{
    // settings.json lives next to cotton-circuit.json; New game archives only the save, so settings survive.
    public static class SettingsStore
    {
        const string FileName = "settings.json";
        public static GameSettings Current { get; private set; } = new GameSettings();
        public static string DirectoryPath { get; private set; }
        public static event Action Changed;
        static bool unsaved;

        public static void Use(string directory)
        {
            DirectoryPath = directory;
            Current = Read(Path.Combine(directory, FileName));
            unsaved = false;
            Changed?.Invoke();
        }

        /// <summary>Changes the settings and applies them at once. A slider drag passes persist: false for every step and calls Save when it ends.</summary>
        public static void Apply(Action<GameSettings> change, bool persist = true)
        {
            change(Current);
            Current.Sanitize();
            unsaved = true;
            if (persist) Save();
            Changed?.Invoke();
        }

        /// <summary>Writes settings.json when an applied change has not been written yet.</summary>
        public static void Save()
        {
            if (!unsaved) return;
            unsaved = false;
            Write();
        }

        static GameSettings Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return new GameSettings();
                var settings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(path)) ?? new GameSettings();
                settings.Sanitize();
                return settings;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Debug.LogWarning("Settings could not be read, using defaults: " + e.Message);
                return new GameSettings();
            }
        }

        static void Write()
        {
            if (string.IsNullOrEmpty(DirectoryPath)) return;
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string path = Path.Combine(DirectoryPath, FileName), temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Current, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Settings could not be saved; they apply to this session only: " + e.Message);
            }
        }
    }
}
