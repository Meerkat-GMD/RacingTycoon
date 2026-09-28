using System;
using System.Collections.Generic;
using System.Globalization;

namespace CottonCircuit
{
    public enum BusinessPhase { Preparation, Operating, Results }

    [Serializable] public class NodePurchase { public string Id; public int Level; }

    [Serializable] public class ProgressionState
    {
        public BusinessPhase Phase;
        public List<NodePurchase> Purchases = new List<NodePurchase>();
        public int SelectedMachine, SelectedLocation;
        public int CartStyle = 1;
    }

    [Serializable] public class UpgradeNode
    {
        public string Id, Category, Icon;
        public string Name => Strings.Get("trait." + Id + ".name");
        public string Description => Strings.Get("trait." + Id + ".desc");
        public string[] Parents;
        public int MaxLevel, BaseCost;
        public double CostGrowth;
        public float X, Y;
    }

    /// <summary>Permanent, coin-funded capabilities and their balancing rules.</summary>
    public static class Progression
    {
        static UpgradeNode N(string id, string category, string icon,
            int max, int cost, double growth, float x, float y, params string[] parents)
        {
            return new UpgradeNode { Id = id, Category = category, Icon = icon, MaxLevel = max, BaseCost = cost,
                CostGrowth = growth, X = x, Y = y, Parents = parents };
        }

        // Neighboring prerequisites share a compact topology layout; detached roots and the cart chain sit to the left.
        public static readonly UpgradeNode[] Nodes = {
            N("hours", "business", "시", 3, 110, 2.0, 70, 70),
            N("patience", "business", "참", 3, 90, 1.9, 215, 70),
            N("ads", "business", "광", 3, 120, 2.1, 320, 296),
            N("shelf", "business", "칸", 2, 150, 2.0, 140, 205),
            N("sales", "sales", "값", 3, 160, 2.0, 418, 60),
            N("engine", "equipment", "속", 3, 130, 2.1, 70, 365),
            N("handling", "equipment", "향", 3, 125, 2.0, 190, 440, "engine"),
            N("coupe", "equipment", "차", 1, 480, 1, 120, 565, "handling"),
            N("stick_speed", "production", "성", 3, 110, 2.0, 820, 343),
            N("stick_saving", "production", "절", 3, 140, 2.0, 960, 315, "stick_speed"),
            N("stick_quality", "production", "품", 3, 135, 2.0, 890, 219, "stick_speed"),
            N("sugar_2", "production", "설", 1, 210, 1, 800, 451),
            N("sugar_3", "production", "특", 1, 690, 1, 891, 561, "sugar_2"),
            N("machine_2", "equipment", "기", 1, 360, 1, 647, 342, "sugar_2"),
            N("machine_3", "equipment", "기", 1, 1100, 1, 691, 444, "machine_2", "stick_speed"),
            N("flavor_vanilla", "production", "바", 1, 980, 1, 749, 574, "machine_3", "sugar_3"),
            N("worker_1", "staff", "알", 1, 420, 1, 577, 243, "machine_2"),
            N("worker_2", "staff", "알", 1, 930, 1, 734, 275, "worker_1", "machine_3"),
            N("worker_grade_2", "staff", "교", 1, 360, 1, 545, 384, "worker_1", "machine_2"),
            N("worker_grade_3", "staff", "교", 1, 880, 1, 577, 490, "worker_grade_2", "machine_3"),
            N("worker_speed", "staff", "손", 3, 220, 2.0, 473, 155, "worker_1"),
            N("flavor_price", "sales", "맛", 3, 230, 2.0, 561, 89, "sales"),
            N("location_1", "location", "장", 1, 620, 1, 462, 313, "ads", "machine_2"),
            N("location_2", "location", "축", 1, 1300, 1, 467, 487, "location_1", "worker_grade_2"),
            N("location_3", "location", "별", 1, 2700, 1, 609, 595, "location_2", "machine_3", "flavor_vanilla"),
            N("location_price", "sales", "단", 3, 280, 2.0, 372, 199, "location_1", "sales"),
            N("sugar_saving", "production", "계", 2, 320, 2.1, 942, 456, "stick_saving", "sugar_2"),
            N("quality_focus", "production", "꽃", 2, 340, 2.1, 794, 115, "stick_quality"),
            N("repeat_ads", "business", "입", 2, 390, 2.1, 375, 426, "ads", "location_1"),
            N("group_visit", "business", "무", 3, 180, 2.0, 250, 380, "ads")
        };

        public static UpgradeNode Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var node in Nodes) if (node.Id == id) return node;
            return null;
        }

        public static int Level(Economy e, string id)
        {
            if (e == null || e.Progression == null || e.Progression.Purchases == null || Find(id) == null) return 0;
            int result = 0;
            foreach (var purchase in e.Progression.Purchases)
                if (purchase != null && purchase.Id == id) result = Math.Max(result, purchase.Level);
            return Math.Min(Find(id).MaxLevel, result);
        }

        public static int Cost(Economy e, string id)
        {
            var node = Find(id);
            if (e == null || e.Progression == null || node == null || Level(e, id) >= node.MaxLevel) return 0;
            return (int)Math.Ceiling(node.BaseCost * Math.Pow(node.CostGrowth, Level(e, id)));
        }

        public static bool CanBuy(Economy e, string id)
        {
            var node = Find(id);
            if (node == null || e == null || e.Progression == null ||
                e.Progression.Phase != BusinessPhase.Preparation || Cost(e, id) <= 0 || e.Coins < Cost(e, id)) return false;
            foreach (string parent in node.Parents) if (Level(e, parent) < 1) return false;
            return true;
        }

        public static bool Buy(Economy e, string id)
        {
            if (!CanBuy(e, id)) return false;
            int cost = Cost(e, id);
            if (e.Progression.Purchases == null) e.Progression.Purchases = new List<NodePurchase>();
            NodePurchase purchase = e.Progression.Purchases.Find(p => p != null && p.Id == id);
            if (purchase == null) { purchase = new NodePurchase { Id = id }; e.Progression.Purchases.Add(purchase); }
            e.Coins -= cost;
            purchase.Level++;
            if (id == "hours" && e.Business != null)
                e.Business.RemainingSeconds = DaySeconds(e);
            return true;
        }

        static void Grant(ProgressionState state, string id, int level)
        {
            var node = Find(id);
            if (node == null || level <= 0) return;
            state.Purchases.Add(new NodePurchase { Id = id, Level = Math.Min(node.MaxLevel, level) });
        }

        public static void Enable(Economy e)
        {
            if (e == null || e.Progression != null) return;
            var state = new ProgressionState { Phase = BusinessPhase.Preparation };
            e.Progression = state;
            if (e.Levels != null)
            {
                if (e.Levels.Length > 0) Grant(state, "engine", e.Levels[0]);
                if (e.Levels.Length > 1)
                {
                    if (e.Levels[1] >= 1) Grant(state, "sugar_2", 1);
                    if (e.Levels[1] >= 2) Grant(state, "sugar_3", 1);
                }
                if (e.Levels.Length > 2) Grant(state, "sales", e.Levels[2]);
            }
            Grant(state, "shelf", e.ShelfLevel);
        }

        public static double DaySeconds(Economy e) { return 180 + 40 * Level(e, "hours"); }
        public static double PatienceSeconds(Economy e) { return 90 + 12 * Level(e, "patience"); }
        public static double ArrivalSeconds(Economy e) { return ArrivalSeconds(e, Level(e, "ads"), Level(e, "repeat_ads")); }
        static double ArrivalSeconds(Economy e, int ads, int repeatAds)
        {
            int location = e != null && e.Progression != null ? Math.Max(0, Math.Min(3, e.Progression.SelectedLocation)) : 0;
            return 26.0 / (1 + .3 * ads + .2 * repeatAds) * new[] { 1.0, .92, .84, .76 }[location];
        }
        public static double GroupChance(Economy e) { return GroupChance(Level(e, "group_visit")); }
        static double GroupChance(int level) { return .12 + .16 * level; }
        /// <summary>Visitors in one arrival. Deterministic per order serial and day, so saves and tests replay.</summary>
        public static int GroupSize(Economy e, int serial)
        {
            int level = Level(e, "group_visit");
            if (Roll(serial, e.Day, 0) >= GroupChance(level)) return 1;
            return level >= 2 && Roll(serial, e.Day, 1) < .5 ? 3 : 2;
        }
        static double Roll(int serial, int day, int salt)
        {
            unchecked
            {
                uint hash = (uint)(serial * 73856093 ^ day * 19349663 ^ salt * 83492791);
                hash ^= hash >> 13; hash *= 0x5bd1e995; hash ^= hash >> 15;
                return hash % 10000 / 10000.0;
            }
        }
        public static int Capacity(Economy e) { return 6 + 3 * Level(e, "shelf"); }
        public static int MaxSugarGrade(Economy e) { return Level(e, "sugar_3") > 0 ? 3 : Level(e, "sugar_2") > 0 ? 2 : 1; }
        public static int OwnedMachines(Economy e) { return 1 + Level(e, "machine_2") + Level(e, "machine_3"); }
        public static int WorkerCount(Economy e) { return Math.Min(2, Level(e, "worker_1") + Level(e, "worker_2")); }
        public static int WorkerGrade(Economy e) { return 1 + Level(e, "worker_grade_2") + Level(e, "worker_grade_3"); }
        public static double GrowthMultiplier(Economy e) { return 1 + .12 * Level(e, "stick_speed"); }
        public static double SugarMultiplier(Economy e) { return Math.Max(.5, 1 - .08 * Level(e, "stick_saving") - .07 * Level(e, "sugar_saving")); }
        public static double StarBonus(Economy e) { return .05 + .006 * Level(e, "stick_quality") + .005 * Level(e, "quality_focus"); }
        public static double SalesMultiplier(Economy e)
        {
            int location = e != null && e.Progression != null ? Math.Max(0, Math.Min(3, e.Progression.SelectedLocation)) : 0;
            return (1 + .1 * Level(e, "sales")) * (1 + .12 * location + .06 * location * Level(e, "location_price"));
        }
        public static double FlavorPriceMultiplier(Economy e, int flavor)
        {
            return flavor <= 0 ? 1 : (1 + .07 * flavor + .06 * Level(e, "flavor_price"));
        }
        public static double SpeedMultiplier(Economy e) { return 1 + .08 * Level(e, "engine"); }
        public static double SteeringMultiplier(Economy e) { return 1 + .08 * Level(e, "handling"); }
        public static double WorkerMetersPerSecond(Economy e) { return ShopShift.LapMeters / 60.0 * (1 + .15 * Level(e, "worker_speed")); }
        // Strawberry and soda are starting flavors; vanilla is the one flavor unlocked by a trait.
        public static bool HasFlavor(Economy e, int flavor)
        {
            return flavor == 0 || flavor == 1 || flavor == 2 && Level(e, "flavor_vanilla") > 0;
        }
        public static bool HasLocation(Economy e, int location)
        {
            return location == 0 || location >= 1 && location <= 3 && Level(e, "location_" + location) > 0;
        }
        public static bool HasCartStyle(Economy e, int style)
        {
            return e != null && e.Progression != null &&
                (style == 1 || style == 0 && Level(e, "coupe") > 0);
        }
        public static int MachineTier(int index) { return index >= 0 && index < 3 ? index + 1 : 0; }
        public static int MachineMap(int index) { return index >= 0 && index < 3 ? index : -1; }
        public static string MachineName(int index) { return Strings.Get("machine." + Math.Max(0, Math.Min(2, index))); }
        public static string LocationName(int index) { return Strings.Get("location." + Math.Max(0, Math.Min(3, index)) + ".name"); }
        public static string LocationDescription(int index)
        {
            return Strings.Get("location." + Math.Max(0, Math.Min(3, index)) + ".desc");
        }
        public static string SizeName(int size) => Strings.Get("size." + Math.Max(0, Math.Min(2, size)));
        public static int FlavorMachineTier(int flavor) { return flavor == 0 || flavor == 1 ? 1 : flavor == 2 ? 3 : 0; }

        public static string EffectSummary(Economy e, string id)
        {
            var node = Find(id);
            if (node == null) return "";
            int level = Level(e, id);
            if (level >= node.MaxLevel) return Strings.Get("effect.max");
            if (node.MaxLevel == 1) return node.Description;
            if (id == "hours") return Values("effect.hours", DaySeconds(e), DaySeconds(e) + 40, "0");
            if (id == "patience") return Values("effect.patience", PatienceSeconds(e), PatienceSeconds(e) + 12, "0");
            if (id == "shelf") return Values("effect.shelf", Capacity(e), Capacity(e) + 3, "0");
            if (id == "ads" || id == "repeat_ads")
            {
                double next = ArrivalSeconds(e, Level(e, "ads") + (id == "ads" ? 1 : 0), Level(e, "repeat_ads") + (id == "repeat_ads" ? 1 : 0));
                return Values("effect.arrival", ArrivalSeconds(e), next, "0.0");
            }
            if (id == "group_visit") return Values("effect.group", GroupChance(level) * 100, GroupChance(level + 1) * 100, "0");
            if (id == "engine") return Values("effect.speed", 26 * SpeedMultiplier(e), 26 * (SpeedMultiplier(e) + .08), "0.0");
            if (id == "handling") return Values("effect.steering", SteeringMultiplier(e) * 100, (SteeringMultiplier(e) + .08) * 100, "0");
            if (id == "stick_speed") return Values("effect.growth", GrowthMultiplier(e) * 100, (GrowthMultiplier(e) + .12) * 100, "0");
            if (id == "stick_saving" || id == "sugar_saving")
                return Values("effect.sugar", SugarMultiplier(e) * 100,
                    Math.Max(.5, SugarMultiplier(e) - (id == "stick_saving" ? .08 : .07)) * 100, "0");
            if (id == "stick_quality" || id == "quality_focus")
                return Values("effect.star", StarBonus(e) * 100, (StarBonus(e) + (id == "stick_quality" ? .006 : .005)) * 100, "0.0");
            if (id == "sales") return Values("effect.price", (1 + .1 * level) * 100, (1 + .1 * (level + 1)) * 100, "0");
            if (id == "flavor_price") return Values("effect.flavor", (1.07 + .06 * level) * 100, (1.07 + .06 * (level + 1)) * 100, "0");
            if (id == "location_price") return Values("effect.location", .06 * level * 100, .06 * (level + 1) * 100, "0");
            if (id == "worker_speed") return Values("effect.helper", WorkerMetersPerSecond(e),
                ShopShift.LapMeters / 60.0 * (1 + .15 * (level + 1)), "0.0");
            return node.Description;
        }

        static string Values(string key, double current, double next, string format)
        {
            return Strings.Format(key, current.ToString(format, CultureInfo.InvariantCulture), next.ToString(format, CultureInfo.InvariantCulture));
        }
    }
}
