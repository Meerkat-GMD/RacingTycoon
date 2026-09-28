using System;
using System.Reflection;
using CottonCircuit;

public static class RecipeTests
{
    static int passed, failed;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Near(double actual, double expected, string message)
    { Check(Math.Abs(actual - expected) < .00001, message + " (got " + actual + ")"); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }

    static bool Start(GameSession s, int map, int flavor)
    { return s.StartRecipe(map, flavor); }
    static void Tick(GameSession s, double dt, double radians, double radius, int laps, int boosts = 0, int hits = 0)
    { s.TickRecipe(dt, radians, radius, laps, boosts, hits); }
    static int Quality(Product p) { return p.Quality; }
    static void SetQuality(Product p, int quality) { p.Quality = quality; }
    static int Bonus(GameSession s) { return s.ResultBonus; }
    static bool Upgrade(SaveStore.Envelope envelope)
    {
        var method = typeof(SaveStore).GetMethod("Upgrade", BindingFlags.Static | BindingFlags.NonPublic);
        return (bool)method.Invoke(null, new object[] { envelope });
    }
    static Product Candy()
    {
        var production = new Production(60);
        production.Advance(100, 10, 0);
        return production.Finish();
    }

    public static int Main()
    {
        Test("filling target before a lap keeps race active", () => {
            var s = new GameSession(new Economy());
            Check(Start(s, 0, 2), "launch refused");
            Tick(s, 1, 100, 10, 0);
            Check(s.Production.Grams == 60 && s.Production.IsFull, "target not filled");
            Check(s.Mode == GameMode.Racing && s.Result == null && s.Economy.Inventory.Count == 0 &&
                s.Economy.Coins == 80, "full target ended race or awarded product");
        });
        Test("both maps finish every configured flavor at their fixed size", () => {
            for (int map = 0; map < 2; map++) for (int flavor = 0; flavor < 3; flavor++)
            {
                var economy = new Economy();
                economy.Levels[1] = 3;
                var s = new GameSession(economy);
                Check(Start(s, map, flavor), "launch refused");
                Tick(s, 10, Math.PI, 7.5, 0);
                Tick(s, 10, Math.PI, 12.5, 1, 2, 1);
                Check(s.Mode == GameMode.Results && s.Result != null && s.Result.Grams == (map == 0 ? 60 : 120),
                    "completed lap did not create map-sized candy");
                Check(s.Result.Samples.Count == (map == 0 ? 30 : 60), "sample count does not match size");
                foreach (var sample in s.Result.Samples) Check(sample.Flavor == flavor, "road changed configured flavor");
                Check(s.Result.Samples[0].Radius == 7.5 && s.Result.Samples[10].Radius == 12.5,
                    "driven winding radius was lost");
            }
        });
        Test("one lap fills missing winding samples and awards exactly once", () => {
            var s = new GameSession(new Economy());
            Start(s, 1, 1);
            Tick(s, RaceRecipe.ParSeconds(1), 0, 10, 1, 5, 0);
            Check(s.Mode == GameMode.Results && s.Result != null && s.Result.Grams == 120,
                "lap left missing samples or no candy");
            Check(s.Economy.Inventory.Count == 1 && s.Economy.Day == 2 && Bonus(s) == 40 &&
                s.Economy.Coins == 120, "completion bonus or inventory wrong");
            var result = s.Result;
            Tick(s, 1, 100, 10, 2, 100, 0);
            s.FinishRun();
            Check(s.Result == result && s.Economy.Inventory.Count == 1 && s.Economy.Coins == 120 &&
                s.Economy.Day == 2, "repeated completion rewarded twice");
        });
        Test("abort discards full candy without payment or day advance", () => {
            var s = new GameSession(new Economy());
            Start(s, 0, 1); Tick(s, 1, 100, 10, 0);
            s.FinishRun(); s.FinishRun();
            Check(s.Mode == GameMode.Results && s.Result == null && Bonus(s) == 0 &&
                s.Economy.Inventory.Count == 0 && s.Economy.Coins == 80 && s.Economy.Day == 1,
                "abort stocked candy or paid reward");
        });
        Test("timeouts discard partial candy and reject a late lap", () => {
            for (int map = 0; map < 2; map++)
            {
                var s = new GameSession(new Economy());
                Start(s, map, 2);
                Tick(s, 1, Math.PI, 10, 0);
                Tick(s, map == 0 ? 179 : 239, 0, 10, 1);
                Check(s.Mode == GameMode.Results && s.Result == null && s.Remaining == 0 &&
                    Bonus(s) == 0 && s.Economy.Inventory.Count == 0 && s.Economy.Coins == 80,
                    "timeout completed partial or late product");
            }
        });
        Test("negative lap count cannot complete the recipe", () => {
            var s = new GameSession(new Economy());
            Start(s, 0, 0); Tick(s, 1, 100, 10, -1);
            Check(s.Mode == GameMode.Racing && s.Economy.Inventory.Count == 0, "backward lap rewarded");
        });
        Test("recipe launch validates configuration and respects stock capacity", () => {
            var s = new GameSession(new Economy());
            Check(!Start(s, -1, 0) && !Start(s, 2, 0) && !Start(s, 0, -1) && !Start(s, 0, 3) &&
                s.Mode == GameMode.Shop && s.Production == null, "invalid recipe launched");
            for (int i = 0; i < 6; i++) s.Economy.CompleteRun(Candy());
            Check(!Start(s, 0, 0) && s.Mode == GameMode.Shop, "full shelf accepted recipe");
            s.Economy.Coins = 500;
            Check(s.Economy.BuyShelf() && Start(s, 1, 2), "expanded shelf did not allow recipe");
            Check(s.RecipeMode && s.RecipeMap == 1 && s.RecipeFlavor == 2 && s.Elapsed == 0 &&
                s.Remaining == 240 && !Start(s, 0, 0), "recipe settings or active run were overwritten");
        });
        Test("pause and invalid time cannot produce, complete or abort a recipe", () => {
            var s = new GameSession(new Economy()); Start(s, 0, 1);
            s.Paused = true;
            Tick(s, 10, 100, 10, 1, 10, 0); s.FinishRun();
            Check(s.Mode == GameMode.Racing && s.Elapsed == 0 && s.Remaining == 180 &&
                s.Production.Grams == 0, "paused recipe advanced or ended");
            s.Paused = false;
            Tick(s, double.NaN, 100, 10, 1); Tick(s, double.PositiveInfinity, 100, 10, 1);
            Tick(s, -1, 100, 10, 1); Tick(s, 0, 100, 10, 1);
            Check(s.Mode == GameMode.Racing && s.Elapsed == 0 && s.Production.Grams == 0,
                "invalid timestep corrupted recipe");
            Tick(s, 5, Math.PI, 10, 0);
            Near(s.Elapsed, 5, "elapsed time did not advance"); Near(s.Remaining, 175, "timeout did not count down");
        });
        Test("legacy ticks cannot finish recipes and new runs reset result accounting", () => {
            var s = new GameSession(new Economy()); Start(s, 0, 1);
            s.Tick(180, 100, 10, 2);
            Check(s.Mode == GameMode.Racing && s.Elapsed == 0 && s.Production.Grams == 0,
                "legacy lane tick changed recipe");
            Tick(s, RaceRecipe.ParSeconds(0), 100, 10, 1);
            s.ReturnToShop(); Check(Start(s, 1, 2), "second recipe refused");
            Check(s.Result == null && s.ResultBonus == 0 && s.Elapsed == 0 && s.Remaining == 240,
                "previous result leaked into next recipe");
            s.FinishRun(); s.ReturnToShop(); Check(s.StartRun(60, 30), "legacy run refused after recipe");
            Check(!s.RecipeMode && s.ResultBonus == 0, "legacy mode inherited recipe accounting");
            Tick(s, 30, 100, 10, 1);
            Check(s.Mode == GameMode.Racing && s.Remaining == 30, "recipe tick modified legacy run");
            s.Tick(1, 100, 10, 2);
            Check(s.Mode == GameMode.Results && s.Result.Grams == 60 && s.Result.Quality == 0 &&
                s.Economy.Coins == 100, "legacy auto-finish or reward behavior changed");
        });
        Test("quality rewards clean boosts and stays within price bounds", () => {
            var clean = new GameSession(new Economy());
            Start(clean, 0, 0); Tick(clean, 50, 100, 10, 1, 10, 0);
            var rough = new GameSession(new Economy());
            Start(rough, 0, 0); Tick(rough, 50, 100, 10, 1, 0, int.MaxValue);
            Check(Quality(clean.Result) == 100 && Quality(rough.Result) == 0, "quality not bounded by performance");
            var economy = new Economy(); var p = Candy();
            Check(economy.Price(p) == 73, "legacy quality-zero price changed");
            SetQuality(p, 100);
            Check(economy.Price(p) == 95, "quality premium is not thirty percent");
            SetQuality(p, int.MaxValue); Check(economy.Price(p) == 95, "invalid quality exceeds premium bound");
            SetQuality(p, -1); Check(economy.Price(p) == 73, "negative quality cuts legacy price");
        });
        Test("fast and slower finishes produce bounded time-based bonuses", () => {
            var fast = new GameSession(new Economy());
            Start(fast, 0, 0); Tick(fast, RaceRecipe.ParSeconds(0) * .5, 100, 10, 1);
            var slow = new GameSession(new Economy());
            Start(slow, 0, 0); Tick(slow, RaceRecipe.ParSeconds(0) * 2, 100, 10, 1);
            Check(Bonus(fast) == 20 && Bonus(slow) == 10 && fast.Economy.Coins == 100 &&
                slow.Economy.Coins == 90, "time-based bonus is missing or exceeds cap");
        });
        Test("tank quality contributes without invalid counters creating extra quality", () => {
            var baseRun = new GameSession(new Economy()); Start(baseRun, 0, 0); Tick(baseRun, 50, 100, 10, 1);
            var upgraded = new GameSession(new Economy()); upgraded.Economy.Levels[1] = 3;
            Start(upgraded, 0, 0); Tick(upgraded, 50, 100, 10, 1);
            Check(baseRun.Result.Quality == 50 && upgraded.Result.Quality == 65 && upgraded.Result.Grams == 60,
                "tank quality did not improve quality independently of map size");
            Check(RaceRecipe.Quality(-1, -1, -1) == 50 && RaceRecipe.Quality(int.MaxValue, 0, int.MaxValue) == 100 &&
                RaceRecipe.Quality(int.MaxValue, int.MaxValue, int.MaxValue) == 0,
                "invalid quality counters underflowed or overflowed");
        });
        Test("customers stay for a full long race and satisfaction uses five minutes", () => {
            var economy = new Economy(); var candy = Candy(); economy.CompleteRun(candy);
            var orders = new OrderManager(economy);
            orders.Tick(150);
            var receipt = orders.Serve("order-1", candy.Id);
            Check(receipt != null, "customer expired before long race could finish");
            Near(receipt.Satisfaction, .5, "satisfaction does not use five-minute patience");
            Check(receipt.Tip == 9, "half-patience tip changed");
        });
        Test("V3 save validation rejects out-of-range quality", () => {
            var economy = new Economy(); var candy = Candy(); economy.CompleteRun(candy);
            candy.Quality = 101;
            Check(!SaveStore.Valid(economy), "excess quality accepted into save");
            candy.Quality = -1;
            Check(!SaveStore.Valid(economy), "negative quality accepted into save");
            candy.Quality = 100;
            Check(SaveStore.Valid(economy), "maximum valid quality rejected");
        });
        Test("V2 waiting orders preserve satisfaction and all economy progress", () => {
            var economy = new Economy {
                Coins = 731, Day = 8, ShelfLevel = 1, Levels = new[] { 1, 2, 3 }, OrderSerial = 4,
                TotalTips = 12, TotalSold = 1, LifetimeRevenue = 90, OrdersServed = 1,
                SatisfactionTotal = .5, MissedOrders = 1, NextCustomerIn = 7
            };
            var candy = Candy(); candy.Quality = 50; economy.CompleteRun(candy);
            economy.Orders.Add(new CustomerOrder { Id = "order-3", Flavor = 1, Size = 1, Remaining = 60 });
            economy.Orders.Add(new CustomerOrder { Id = "order-4", Flavor = 2, Size = 0, Remaining = 120 });
            var envelope = new SaveStore.Envelope { Version = 2, State = economy };
            Check(Upgrade(envelope), "valid V2 rejected");
            Near(economy.Orders[0].Remaining, 150, "half patience ratio lost");
            Near(economy.Orders[1].Remaining, 300, "full patience ratio lost");
            Check(economy.Inventory.Count == 1 && candy.Quality == 0 && economy.Coins == 731 &&
                economy.Day == 9 && economy.Levels[1] == 2 && economy.ShelfLevel == 1 &&
                economy.TotalTips == 12 && economy.TotalSold == 1 && economy.OrdersServed == 1 &&
                economy.SatisfactionTotal == .5 && economy.MissedOrders == 1 && economy.NextCustomerIn == 7,
                "V2 migration lost saved progress or retroactively awarded quality");
            Check(envelope.Version == 9 && Upgrade(envelope) && economy.Orders[0].Remaining == 150,
                "migration scaled patience more than once");
        });
        Test("V1 stock migrates with original price and new orders remain empty", () => {
            var economy = new Economy { Coins = 421, Levels = new[] { 1, 0, 2 } };
            var candy = Candy(); candy.Quality = 100; economy.CompleteRun(candy);
            var envelope = new SaveStore.Envelope { Version = 1, State = economy };
            Check(Upgrade(envelope) && envelope.Version == 9 && economy.Coins == 421 &&
                economy.Inventory.Count == 1 && candy.Quality == 0 && economy.Price(candy) == 110 &&
                economy.CompletedIds.Contains(candy.Id), "V1 stock value or progress changed");
            Check(economy.Orders.Count == 0 && economy.OrderSerial == 0 && economy.NextCustomerIn == 15,
                "V1 migration invented waiting orders");
        });
        Test("invalid old patience and unknown versions remain rejected", () => {
            var economy = new Economy { OrderSerial = 1 };
            economy.Orders.Add(new CustomerOrder { Id = "order-1", Flavor = 0, Size = 0, Remaining = 121 });
            Check(!Upgrade(new SaveStore.Envelope { Version = 2, State = economy }), "invalid V2 patience accepted");
            Check(!Upgrade(new SaveStore.Envelope { Version = 10, State = new Economy() }), "future save accepted");
            Check(Upgrade(new SaveStore.Envelope { Version = 3, State = new Economy() }), "valid V3 rejected");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
