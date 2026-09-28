using System;
using UnityEngine;

namespace CottonCircuit
{
    // Loads the string table before the first scene and picks the language:
    // --language=ko|en, then the saved choice, then the OS (Korean OS → Korean, otherwise English).
    public static class Localization
    {
        public static event Action Changed;
        public static string CommandLine { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Strings.Load(Resources.Load<TextAsset>("Localization/strings").text);
            foreach (var argument in Environment.GetCommandLineArgs())
                if (argument.StartsWith("--language=")) CommandLine = argument.Substring("--language=".Length);
            SettingsStore.Use(Application.persistentDataPath);
            Resolve(true);
        }

        /// <summary>Called with the save directory; the smoke runner passes an isolated folder so it never writes the user's settings.</summary>
        public static void UseDirectory(string directory)
        {
            if (directory == SettingsStore.DirectoryPath) return;
            SettingsStore.Use(directory);
            Resolve(false);
        }

        public static void Choose(Language language)
        {
            SettingsStore.Apply(settings => settings.Language = Strings.Code(language));
            if (language == Strings.Current) return;
            Strings.Set(language);
            Changed?.Invoke();
        }

        static void Resolve(bool force)
        {
            var language = Strings.Resolve(CommandLine, SettingsStore.Current.Language, Application.systemLanguage == SystemLanguage.Korean);
            if (!force && language == Strings.Current) return;
            Strings.Set(language);
            Changed?.Invoke();
        }
    }
}
