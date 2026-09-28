using System;
using System.Collections.Generic;
using CottonCircuit;

public static class ProgressionTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex.Message); failed++; }
    }
    static Economy Fresh()
    {
        var economy = new Economy { Coins = 1000000 };
        Progression.Enable(economy);
        return economy;
    }
    static void BuyPath(Economy economy, string id)
    {
        UpgradeNode node = Progression.Find(id);
        Check(node != null, "missing node " + id);
        foreach (string parent in node.Parents)
            if (Progression.Level(economy, parent) == 0) BuyPath(economy, parent);
        if (Progression.Level(economy, id) == 0)
            Check(Progression.Buy(economy, id), "could not buy " + id);
    }
    static Product Candy(int flavor)
    {
        return new Product { Id = Guid.NewGuid().ToString("N"), Grams = 50,
            DistanceBased = true, DistanceMeters = 1000, FlavorIndex = flavor, Quality = 50 };
    }
    public static int Main()
    {
        Strings.Load(System.IO.File.ReadAllText(Strings.TablePath(AppDomain.CurrentDomain.BaseDirectory)));
        Strings.Set(Language.Korean);
        Test("fresh progression starts with the downhill car without a paid unlock", () => {
            var e = Fresh();
            Check(e.Progression.CartStyle == 1 && Progression.Level(e, "coupe") == 0,
                "starter should use the free downhill style");
        });
        Test("downhill is available from the start and the optional classic kart requires its trait", () => {
            var e = Fresh();
            Check(Progression.HasCartStyle(e, 1) && !Progression.HasCartStyle(e, 0), "starting vehicle availability incorrect");
            Check(!Progression.HasCartStyle(e, -1) && !Progression.HasCartStyle(e, 2), "invalid driving style accepted");
            BuyPath(e, "coupe");
            Check(Progression.HasCartStyle(e, 0) && Progression.HasCartStyle(e, 1), "optional kart unlock failed");
            Check(e.Progression.CartStyle == 1, "purchasing a vehicle trait changed the selected driving style");
        });
        Test("legacy economy remains unchanged without progression", () => {
            var e = new Economy();
            Check(e.Progression == null && e.StockCapacity == 6 && e.MaxSpeed == 26f, "legacy defaults changed");
            int oldPrice = e.Price(Candy(0));
            e.Coins = 1000; Check(e.BuyShelf() && e.StockCapacity == 9, "legacy shelf changed");
            Check(e.BuyUpgrade(0) && e.MaxSpeed == 28.5f && e.Price(Candy(0)) == oldPrice, "legacy upgrade changed");
        });
        Test("enable gives a conservative starter without losing money", () => {
            var e = new Economy { Coins = 47 };
            Progression.Enable(e);
            Check(e.Coins == 47 && e.Progression.Phase == BusinessPhase.Preparation &&
                e.Progression.SelectedLocation == 0 && e.Progression.SelectedMachine == 0 &&
                Progression.DaySeconds(e) == 180 && Progression.Capacity(e) == 6 &&
                Progression.MaxSugarGrade(e) == 1 && Progression.OwnedMachines(e) == 1 &&
                Progression.WorkerCount(e) == 0 && Progression.HasFlavor(e, 0) && Progression.HasFlavor(e, 1) &&
                !Progression.HasFlavor(e, 2) && !Progression.HasLocation(e, 1), "starter incorrect");
            Progression.Enable(e);
            Check(e.Progression.Purchases.Count == 0, "enable was not idempotent");
        });
        Test("legacy permanent upgrades migrate to matching progression levels", () => {
            var e = new Economy { Coins = 39, ShelfLevel = 2, Levels = new[] { 2, 2, 1 } };
            Progression.Enable(e);
            Check(e.Coins == 39 && Progression.Level(e, "engine") == 2 &&
                Progression.Level(e, "shelf") == 2 && Progression.Level(e, "sales") == 1 &&
                Progression.MaxSugarGrade(e) == 3 && Progression.Level(e, "sugar_2") == 1,
                "legacy grants missing");
        });
        Test("all prerequisite nodes must be bought at rank one", () => {
            var e = Fresh();
            Check(!Progression.CanBuy(e, "machine_3") && !Progression.Buy(e, "machine_3"), "locked child purchased");
            BuyPath(e, "machine_2");
            Check(!Progression.CanBuy(e, "machine_3"), "one parent incorrectly sufficed");
            BuyPath(e, "stick_speed");
            Check(Progression.CanBuy(e, "machine_3"), "all parents did not unlock child");
        });
        Test("purchase deducts exactly once and caps feature and stat nodes", () => {
            var e = Fresh();
            int cost = Progression.Cost(e, "hours");
            Check(cost > 0 && Progression.Buy(e, "hours") && e.Coins == 1000000 - cost, "first debit wrong");
            Check(Progression.DaySeconds(e) == 220, "hours grant missing");
            Check(Progression.Buy(e, "hours") && Progression.Buy(e, "hours") &&
                Progression.DaySeconds(e) == 300, "rank growth wrong");
            int balance = e.Coins;
            Check(!Progression.Buy(e, "hours") && Progression.Cost(e, "hours") == 0 &&
                e.Coins == balance, "max level charged");
            BuyPath(e, "machine_2"); balance = e.Coins;
            Check(!Progression.Buy(e, "machine_2") && e.Coins == balance &&
                Progression.OwnedMachines(e) == 2, "feature repurchased");
        });
        Test("duration purchase updates prepared business timer for save validation", () => {
            var e = Fresh(); new ShopShift(e);
            Check(e.Business.RemainingSeconds == 180 && Progression.Buy(e, "hours") &&
                e.Business.RemainingSeconds == 220, "prepared timer retained previous duration");
        });
        Test("insufficient coins and business phase prevent purchases", () => {
            var e = Fresh(); e.Coins = Progression.Cost(e, "hours") - 1;
            Check(!Progression.Buy(e, "hours") && Progression.Level(e, "hours") == 0, "short funds accepted");
            e.Coins = 1000000; e.Progression.Phase = BusinessPhase.Operating;
            Check(!Progression.CanBuy(e, "hours") && !Progression.Buy(e, "hours") && e.Coins == 1000000, "operating purchase accepted");
            e.Progression.Phase = BusinessPhase.Results;
            Check(!Progression.Buy(e, "hours"), "results purchase accepted");
        });
        Test("capabilities follow purchased feature nodes", () => {
            var e = Fresh();
            BuyPath(e, "worker_grade_3"); BuyPath(e, "location_3");
            Check(Progression.HasFlavor(e, 1) && Progression.HasFlavor(e, 2) &&
                Progression.MaxSugarGrade(e) == 3 && Progression.WorkerCount(e) >= 1 &&
                Progression.WorkerGrade(e) == 3 && Progression.OwnedMachines(e) == 3 &&
                Progression.HasLocation(e, 3), "capability grant missing");
            Check(Progression.FlavorMachineTier(0) == 1 &&
                Progression.FlavorMachineTier(1) == 1 && Progression.FlavorMachineTier(2) == 3 &&
                Progression.MachineTier(2) == 3 && Progression.MachineMap(2) == 2,
                "machine requirement wrong");
        });
        Test("stat purchases change production and customer economics", () => {
            var e = Fresh();
            double patience = Progression.PatienceSeconds(e), arrival = Progression.ArrivalSeconds(e);
            double growth = Progression.GrowthMultiplier(e), sugar = Progression.SugarMultiplier(e);
            double quality = Progression.StarBonus(e), speed = Progression.SpeedMultiplier(e);
            double steering = Progression.SteeringMultiplier(e), worker = Progression.WorkerMetersPerSecond(e);
            BuyPath(e, "patience"); BuyPath(e, "ads"); BuyPath(e, "stick_speed");
            BuyPath(e, "stick_saving"); BuyPath(e, "stick_quality"); BuyPath(e, "handling");
            BuyPath(e, "worker_speed"); BuyPath(e, "engine");
            Check(Progression.PatienceSeconds(e) > patience && Progression.ArrivalSeconds(e) < arrival &&
                Progression.GrowthMultiplier(e) > growth && Progression.SugarMultiplier(e) < sugar &&
                Progression.StarBonus(e) > quality && Progression.SpeedMultiplier(e) > speed &&
                Progression.SteeringMultiplier(e) > steering &&
                Progression.WorkerMetersPerSecond(e) > worker && e.MaxSpeed > 26f,
                "stat effect absent");
        });
        Test("base worker can complete an eligible small recipe during one day", () => {
            var e = Fresh(); BuyPath(e, "worker_1");
            Check(Progression.WorkerMetersPerSecond(e) * Progression.DaySeconds(e) >= ShopShift.MetersForSize(0),
                "worker cannot finish even one small candy per day");
        });
        Test("shelf and sales nodes affect real Economy values", () => {
            var e = Fresh(); var product = Candy(0);
            int original = e.Price(product);
            BuyPath(e, "shelf"); BuyPath(e, "sales");
            Check(e.StockCapacity == 9 && e.Price(product) > original, "economy effect absent");
            BuyPath(e, "location_1"); e.Progression.SelectedLocation = 1;
            Check(e.Price(product) > original, "location price absent");
        });
        Test("legacy purchase commands cannot spend coins in progression mode", () => {
            var e = Fresh(); int balance = e.Coins;
            Check(!e.BuyShelf() && !e.BuyUpgrade(0) && e.ShelfCost == 0 &&
                e.UpgradeCost(0) == 0 && e.Coins == balance, "hidden legacy purchase spent coins");
        });
        Test("hover summary reports current and next effect", () => {
            var e = Fresh();
            Check(Progression.EffectSummary(e, "engine").Contains("26.0") &&
                Progression.EffectSummary(e, "engine").Contains("28.1") &&
                Progression.EffectSummary(e, "stick_speed").Contains("100%") &&
                Progression.EffectSummary(e, "stick_speed").Contains("112%"),
                "numeric hover effect missing");
        });
        Test("soda is a starting flavor with no trait left to buy", () => {
            Check(Progression.Find("flavor_soda") == null, "soda trait still listed");
            foreach (UpgradeNode node in Progression.Nodes)
                Check(Array.IndexOf(node.Parents, "flavor_soda") < 0, node.Id + " still requires the soda trait");
            Check(string.Join(",", Progression.Find("flavor_price").Parents) == "sales" &&
                string.Join(",", Progression.Find("quality_focus").Parents) == "stick_quality",
                "soda children did not keep their remaining prerequisites");
            var shift = new ShopShift(Fresh());
            Check(shift.CanMakeFlavor(0, 0) && shift.CanMakeFlavor(0, 1) && !shift.CanMakeFlavor(0, 2),
                "the basic machine must make strawberry and soda but not vanilla");
        });
        Test("catalog grants are reachable and graph has no cycles", () => {
            var e = Fresh(); var ids = new HashSet<string>();
            Check(Progression.Nodes.Length >= 25 && Progression.Nodes.Length <= 35, "catalog size out of range");
            foreach (UpgradeNode node in Progression.Nodes)
            {
                Check(ids.Add(node.Id), "duplicate id " + node.Id);
                Check(node.MaxLevel > 0 && node.BaseCost > 0 && node.Parents != null, "invalid node " + node.Id);
                BuyPath(e, node.Id);
                Check(Progression.Level(e, node.Id) >= 1, "unreachable node " + node.Id);
            }
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
