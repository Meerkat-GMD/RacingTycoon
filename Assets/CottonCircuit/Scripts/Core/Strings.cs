using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CottonCircuit
{
    public enum Language { Korean, English }

    /// <summary>
    /// The translation table (Resources/Localization/strings.tsv: key, ko, en) and the current language.
    /// Pure C# so Core rules and the mono tests translate exactly like the game.
    /// </summary>
    public static class Strings
    {
        const string Header = "key\tko\ten";
        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        static readonly HashSet<string> missing = new HashSet<string>();
        static readonly Regex Placeholder = new Regex(@"\{(\d+)(?:,[^}]*)?(?::[^}]*)?\}");

        public static Language Current { get; private set; } = Language.English;
        public static int Version { get; private set; }
        public static ICollection<string> Missing => missing;
        /// <summary>Raised the first time a key is missing; the game logs it in development builds.</summary>
        public static event Action<string> MissingKey;

        public static string TablePath(string baseDirectory)
        {
            return Path.GetFullPath(Path.Combine(baseDirectory, "..", "Assets", "Resources", "Localization", "strings.tsv"));
        }

        public static void Load(string tsv)
        {
            var lines = (tsv ?? "").Replace("\r\n", "\n").Split('\n');
            if (lines[0].TrimStart('﻿') != Header) throw new FormatException("strings.tsv must start with the header 'key<TAB>ko<TAB>en'");
            var loaded = new Dictionary<string, string[]>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                var cells = lines[i].Split('\t');
                if (cells.Length != 3) throw new FormatException("strings.tsv line " + (i + 1) + " needs exactly 3 cells");
                if (loaded.ContainsKey(cells[0])) throw new FormatException("strings.tsv line " + (i + 1) + " repeats the key " + cells[0]);
                loaded.Add(cells[0], new[] { Unescape(cells[1]), Unescape(cells[2]) });
            }
            table.Clear(); missing.Clear();
            foreach (var pair in loaded) table.Add(pair.Key, pair.Value);
            Version++;
        }

        public static void Set(Language language) { Current = language; Version++; }
        public static bool Has(string key) => key != null && table.ContainsKey(key);
        public static string Get(string key) => Get(key, Current);

        public static string Get(string key, Language language)
        {
            if (key != null && table.TryGetValue(key, out var values)) return values[(int)language];
            if (key != null && missing.Add(key)) MissingKey?.Invoke(key);
            return key ?? "";
        }

        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);

        public static string Code(Language language) => language == Language.Korean ? "ko" : "en";
        public static Language? Parse(string code) => code == "ko" ? Language.Korean : code == "en" ? Language.English : (Language?)null;

        /// <summary>Command line, then the player's saved choice, then the OS: Korean OS → Korean, anything else → English.</summary>
        public static Language Resolve(string commandLine, string saved, bool systemKorean)
        {
            return Parse(commandLine) ?? Parse(saved) ?? (systemKorean ? Language.Korean : Language.English);
        }

        public static List<string> Validate()
        {
            var problems = new List<string>();
            foreach (var pair in table)
            {
                if (pair.Value[0].Trim().Length == 0 || pair.Value[1].Trim().Length == 0) problems.Add(pair.Key + " has an empty translation");
                else if (Placeholders(pair.Value[0]) != Placeholders(pair.Value[1])) problems.Add(pair.Key + " uses different placeholders in ko and en");
                for (int i = 0; i < pair.Value.Length; i++)
                    if (!Formats(pair.Value[i])) problems.Add(pair.Key + " is not a valid format string in " + Code((Language)i));
            }
            return problems;
        }

        // A stray brace makes string.Format throw, so try it with as many arguments as the highest placeholder needs.
        static bool Formats(string text)
        {
            int count = 0;
            foreach (Match match in Placeholder.Matches(text)) count = Math.Max(count, int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) + 1);
            try { string.Format(CultureInfo.InvariantCulture, text, new object[count]); return true; }
            catch (FormatException) { return false; }
        }

        static string Placeholders(string text)
        {
            var found = new SortedSet<int>();
            foreach (Match match in Placeholder.Matches(text)) found.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            return string.Join(",", found);
        }

        static string Unescape(string cell)
        {
            if (cell.IndexOf('\\') < 0) return cell;
            var text = new StringBuilder(cell.Length);
            for (int i = 0; i < cell.Length; i++)
            {
                char c = cell[i];
                if (c != '\\' || i + 1 == cell.Length) { text.Append(c); continue; }
                char next = cell[++i];
                text.Append(next == 'n' ? '\n' : next == 't' ? '\t' : next);
            }
            return text.ToString();
        }
    }
}
