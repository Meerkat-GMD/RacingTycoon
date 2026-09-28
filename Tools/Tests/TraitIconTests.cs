using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CottonCircuit;

public static class TraitIconTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex.Message); failed++; }
    }

    // Spec "특성 아이콘" table, one pair per trait (31 traits -> 23 icon files).
    static readonly string[,] SpecTable =
    {
        { "hours", "Trait_Hours" }, { "patience", "Trait_Patience" }, { "ads", "Trait_Ads" },
        { "repeat_ads", "Trait_RepeatAds" }, { "shelf", "Trait_Shelf" }, { "sales", "Trait_Sales" },
        { "flavor_price", "Trait_PriceTag" }, { "location_price", "Trait_PriceTag" },
        { "engine", "Trait_Engine" }, { "handling", "Trait_Handling" }, { "coupe", "Trait_Kart" },
        { "stick_speed", "Trait_StickSpeed" }, { "stick_saving", "Trait_Spoon" }, { "sugar_saving", "Trait_Spoon" },
        { "stick_quality", "Trait_Ribbon" }, { "quality_focus", "Trait_Ribbon" },
        { "sugar_2", "Trait_Sugar2" }, { "sugar_3", "Trait_Sugar3" },
        { "machine_2", "Trait_Machine" }, { "machine_3", "Trait_Machine" },
        { "flavor_vanilla", "Trait_FlavorVanilla" },
        { "worker_1", "Trait_Worker" }, { "worker_2", "Trait_Worker" },
        { "worker_grade_2", "Trait_GradCap" }, { "worker_grade_3", "Trait_GradCap" },
        { "worker_speed", "Trait_Glove" },
        { "location_1", "Trait_MapPin" }, { "location_2", "Trait_MapPin" }, { "location_3", "Trait_MapPin" },
        { "group_visit", "Trait_Group" },
    };

    static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }

    public static int Main()
    {
        Test("every progression node maps to a listed icon", () => {
            var all = new HashSet<string>(TraitIcons.All);
            foreach (var node in Progression.Nodes)
                Check(all.Contains(TraitIcons.For(node.Id)), node.Id + " -> " + TraitIcons.For(node.Id) + " not in All");
        });
        Test("All lists 22 unique icon ids", () => {
            Check(TraitIcons.All.Length == 22, "expected 22 icons, got " + TraitIcons.All.Length);
            Check(new HashSet<string>(TraitIcons.All).Count == 22, "duplicate icon id in All");
            foreach (string id in TraitIcons.All) Check(!string.IsNullOrEmpty(id) && id.StartsWith("Trait_"), "bad icon id '" + id + "'");
        });
        Test("every icon in All is used by at least one node", () => {
            var used = new HashSet<string>();
            foreach (var node in Progression.Nodes) used.Add(TraitIcons.For(node.Id));
            foreach (string id in TraitIcons.All) Check(used.Contains(id), "unused icon " + id);
        });
        Test("mapping equals the spec table for all 30 traits", () => {
            Check(SpecTable.GetLength(0) == 30, "spec table has 30 rows");
            Check(Progression.Nodes.Length == 30, "progression has 30 nodes, got " + Progression.Nodes.Length);
            var specIds = new HashSet<string>();
            for (int i = 0; i < SpecTable.GetLength(0); i++)
            {
                string node = SpecTable[i, 0], icon = SpecTable[i, 1];
                Check(specIds.Add(node), "duplicate spec row " + node);
                Check(Progression.Find(node) != null, "spec trait " + node + " is not a progression node");
                Check(TraitIcons.For(node) == icon, node + " -> " + TraitIcons.For(node) + ", spec says " + icon);
            }
            foreach (var node in Progression.Nodes) Check(specIds.Contains(node.Id), "node " + node.Id + " missing from spec table");
        });
        Test("unknown or null node ids throw KeyNotFoundException", () => {
            Check(Throws<KeyNotFoundException>(() => TraitIcons.For("missing")), "unknown id did not throw");
            Check(Throws<KeyNotFoundException>(() => TraitIcons.For("")), "empty id did not throw");
            Check(Throws<KeyNotFoundException>(() => TraitIcons.For(null)), "null id did not throw");
            Check(Throws<KeyNotFoundException>(() => TraitIcons.For("Hours")), "lookup must be exact, not case-insensitive");
        });
        Test("All has the same order as ui_sprite_spec.TRAIT_ICONS", () => {
            // The test exe lives in Logs/, so the project root is its parent directory.
            string root = Path.GetDirectoryName(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/')));
            string spec = File.ReadAllText(Path.Combine(root, "Art", "Blender", "ui_sprite_spec.py"));
            var block = Regex.Match(spec, @"TRAIT_ICONS\s*=\s*\[(.*?)\]", RegexOptions.Singleline);
            Check(block.Success, "TRAIT_ICONS block not found in ui_sprite_spec.py");
            var python = new List<string>();
            foreach (Match m in Regex.Matches(block.Groups[1].Value, @"'([^']+)'")) python.Add(m.Groups[1].Value);
            Check(python.Count == TraitIcons.All.Length, "python lists " + python.Count + " icons, C# " + TraitIcons.All.Length);
            for (int i = 0; i < python.Count; i++)
                Check(python[i] == TraitIcons.All[i], "index " + i + ": python " + python[i] + ", C# " + TraitIcons.All[i]);
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
