using System;
using System.Collections.Generic;
using System.IO;
using CottonCircuit;

public static class LocalizationTests
{
    static int failures, passed;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + e.Message); failures++; }
    }
    static void Throws(Action action, string message)
    {
        try { action(); } catch (FormatException) { return; }
        throw new Exception(message);
    }
    const string Sample = "key\tko\ten\ncommon.hello\t안녕\\n하세요\tHello\\nthere\ncommon.count\t{0}개\t{0} pcs\ncommon.path\ta\\\\b\ta\\tb\n";

    public static int Main()
    {
        Test("korean OS resolves to Korean", () => Check(Strings.Resolve(null, "", true) == Language.Korean, "ko"));
        Test("any other OS resolves to English", () => Check(Strings.Resolve(null, "", false) == Language.English, "en"));
        Test("a saved choice beats the OS", () => Check(Strings.Resolve(null, "en", true) == Language.English && Strings.Resolve(null, "ko", false) == Language.Korean, "saved"));
        Test("the command line beats the saved choice", () => Check(Strings.Resolve("ko", "en", false) == Language.Korean, "command line"));
        Test("unknown codes are ignored", () => Check(Strings.Resolve("jp", "xx", false) == Language.English && Strings.Parse("KO") == null, "unknown"));
        Test("codes round trip", () => Check(Strings.Parse(Strings.Code(Language.Korean)) == Language.Korean && Strings.Code(Language.English) == "en", "codes"));
        Test("load reads both columns and escapes", () =>
        {
            Strings.Load(Sample); Strings.Set(Language.Korean);
            Check(Strings.Get("common.hello") == "안녕\n하세요", "ko newline");
            Check(Strings.Get("common.hello", Language.English) == "Hello\nthere", "en newline");
            Check(Strings.Get("common.path") == "a\\b" && Strings.Get("common.path", Language.English) == "a\tb", "escapes");
        });
        Test("format fills placeholders in the current language", () =>
        {
            Strings.Load(Sample); Strings.Set(Language.English);
            Check(Strings.Format("common.count", 3) == "3 pcs", "format");
        });
        Test("set bumps the version", () => { int v = Strings.Version; Strings.Set(Language.Korean); Check(Strings.Version == v + 1, "version"); });
        Test("missing keys return the key and are recorded", () =>
        {
            Strings.Load(Sample);
            Check(Strings.Get("nope.key") == "nope.key" && Strings.Missing.Contains("nope.key"), "missing");
            Strings.Load(Sample);
            Check(Strings.Missing.Count == 0, "load clears missing");
        });
        Test("header, cell count and duplicates are enforced", () =>
        {
            Throws(() => Strings.Load("id\tko\ten\n"), "bad header accepted");
            Throws(() => Strings.Load("key\tko\ten\na\tb\n"), "two cells accepted");
            Throws(() => Strings.Load("key\tko\ten\na\tb\tc\na\td\te\n"), "duplicate accepted");
        });
        Test("validate reports empty cells and placeholder mismatches", () =>
        {
            Strings.Load("key\tko\ten\na.b\t\tx\nc.d\t{0}개\tpcs\ne.f\t{0}\t{0}\n");
            var problems = Strings.Validate();
            Check(problems.Count == 2, "expected 2 problems, got " + problems.Count + ": " + string.Join(" | ", problems));
        });
        Test("validate reports values that string.Format rejects", () =>
        {
            Strings.Load("key\tko\ten\na.b\t{0}개\t{0} pcs {\nc.d\t{{0}} {0}\t{0} {{x}}\n");
            var problems = Strings.Validate();
            Check(problems.Count == 1 && problems[0] == "a.b is not a valid format string in en", "expected only the stray brace, got " + string.Join(" | ", problems));
        });
        Test("a missing key is announced once", () =>
        {
            Strings.Load(Sample);
            var announced = new List<string>();
            Action<string> listener = announced.Add;
            Strings.MissingKey += listener;
            try { Strings.Get("nope.key"); Strings.Get("nope.key"); Strings.Format("other.key"); }
            finally { Strings.MissingKey -= listener; }
            Check(announced.Count == 2 && announced[0] == "nope.key" && announced[1] == "other.key", "announced " + string.Join(", ", announced));
        });
        Test("the shipped table loads and validates", () =>
        {
            Strings.Load(File.ReadAllText(Strings.TablePath(AppDomain.CurrentDomain.BaseDirectory)));
            var problems = Strings.Validate();
            Check(problems.Count == 0, string.Join(" | ", problems));
            Check(Strings.Get("language.self", Language.Korean) == "한국어" && Strings.Get("language.self", Language.English) == "English", "language names");
        });
        Test("settings default to full volume, unmuted, automatic language", () =>
        {
            var s = new GameSettings();
            Check(s.MusicVolume == 1 && s.EffectsVolume == 1 && !s.Muted && s.Language == "" && s.Version == GameSettings.CurrentVersion, "defaults");
        });
        Test("sanitize clamps volumes and clears unknown languages", () =>
        {
            var s = new GameSettings { MusicVolume = 3, EffectsVolume = float.NaN, Language = "jp", Version = 0 };
            s.Sanitize();
            Check(s.MusicVolume == 1 && s.EffectsVolume == 1 && s.Language == "" && s.Version == GameSettings.CurrentVersion, "sanitize");
            s.MusicVolume = -2; s.Language = "ko"; s.Sanitize();
            Check(s.MusicVolume == 0 && s.Language == "ko", "keeps valid");
            s.MusicVolume = float.PositiveInfinity; s.EffectsVolume = float.NegativeInfinity; s.Sanitize();
            Check(s.MusicVolume == 1 && s.EffectsVolume == 0, "infinities clamp to the ends");
        });
        Test("screen sizes are distinct, fit the monitor, include the current size and sort", () =>
        {
            var available = new[] { new ScreenSize(1920, 1080), new ScreenSize(1280, 720), new ScreenSize(1280, 720), new ScreenSize(2560, 1440), new ScreenSize(0, 0) };
            var list = ScreenSizes.Choices(available, new ScreenSize(1920, 1080), new ScreenSize(1600, 900));
            Check(list.Count == 3 && list[0].Equals(new ScreenSize(1280, 720)) && list[1].Equals(new ScreenSize(1600, 900)) && list[2].Equals(new ScreenSize(1920, 1080)), "choices " + string.Join(",", list));
            Check(new ScreenSize(1600, 900).ToString() == "1600 × 900", "label");
        });
        Test("every perk, tab, machine, location, course, flavor and size is translated", () =>
        {
            Strings.Load(File.ReadAllText(Strings.TablePath(AppDomain.CurrentDomain.BaseDirectory)));
            var keys = new List<string>();
            foreach (var node in Progression.Nodes) { keys.Add("trait." + node.Id + ".name"); keys.Add("trait." + node.Id + ".desc"); }
            foreach (var tab in UpgradeTreeLayout.Tabs) keys.Add("tab." + tab.Id);
            for (int i = 0; i < 3; i++) { keys.Add("machine." + i); keys.Add("flavor." + i); keys.Add("size." + i); }
            for (int i = 0; i < 4; i++) { keys.Add("location." + i + ".name"); keys.Add("location." + i + ".desc"); }
            // RaceRecipe.ValidateMap only names maps 0-1; RaceCourse.MapCount's third map has no recipe name.
            for (int map = 0; map < 2; map++) keys.Add("course." + map);
            var absent = keys.FindAll(k => !Strings.Has(k));
            Check(absent.Count == 0, "missing: " + string.Join(", ", absent));
        });
        Test("core names follow the current language", () =>
        {
            Strings.Load(File.ReadAllText(Strings.TablePath(AppDomain.CurrentDomain.BaseDirectory)));
            Strings.Set(Language.Korean);
            Check(Progression.Find("hours").Name == "영업시간" && Progression.MachineName(1) == "고급 기계" && RaceRecipe.Name(0) == "1번 · 슈가웨이", "korean");
            Strings.Set(Language.English);
            Check(Progression.Find("hours").Name == "Business Hours" && Progression.MachineName(1) == "Advanced Machine" && RaceRecipe.Name(0) == "No.1 · Sugarway", "english");
        });
        Console.WriteLine(passed + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }
}
