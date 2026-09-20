using System;
using System.Collections.Generic;
using CottonCircuit;

public static class OrderTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Near(double actual, double expected, string message)
    { Check(Math.Abs(actual - expected) < 0.00001, message + " (got " + actual + ", wanted " + expected + ")"); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static Product Candy(string id, int grams, params int[] flavors)
    {
        var product = new Product { Id = id, Grams = grams, Samples = new List<WindingSample>() };
        foreach (int flavor in flavors) product.Samples.Add(new WindingSample { Flavor = flavor });
        return product;
    }
    static Product Strawberry(string id) { return Candy(id, 60, 0); }

    public static int Main()
    {
        Test("recipe chooses dominant flavor and breaks ties by earliest sample", () => {
            Check(CandyRecipe.FlavorOf(Candy("mixed", 60, 2, 0, 0, 2)) == 2, "tie ignored sample order");
            Check(CandyRecipe.FlavorOf(Candy("dominant", 60, 1, 2, 2)) == 2, "wrong dominant flavor");
            Check(CandyRecipe.FlavorOf(Candy("bad", 60, 3)) == -1 && CandyRecipe.FlavorOf(null) == -1 &&
                CandyRecipe.FlavorOf(Candy("empty", 0, 0)) == -1,
                "invalid sample accepted");
        });
        Test("recipe size boundaries reject partial candy", () => {
            Check(CandyRecipe.SizeOf(Candy("partial", 49, 0)) == -1, "partial stock has a size");
            Check(CandyRecipe.SizeOf(Candy("small", 50, 0)) == 0, "50 grams is not small");
            Check(CandyRecipe.SizeOf(Candy("small", 99, 0)) == 0, "99 grams is not small");
            Check(CandyRecipe.SizeOf(Candy("large", 100, 0)) == 1, "100 grams is not large");
            Check(CandyRecipe.TargetGrams(0) == 60 && CandyRecipe.TargetGrams(1) == 120 &&
                CandyRecipe.TargetGrams(-1) == 0, "target sizes invalid");
            Check(CandyRecipe.Matches(Candy("mixed", 60, 1, 0, 1),
                new CustomerOrder { Flavor = 1, Size = 0 }), "mixed candy did not match dominant flavor");
            Check(!CandyRecipe.Matches(Candy("partial", 49, 1),
                new CustomerOrder { Flavor = 1, Size = 0 }), "partial candy matched order");
        });
        Test("fresh shop receives an immediate strawberry small order", () => {
            var economy = new Economy();
            var manager = new OrderManager(economy);
            Check(manager.Orders.Count == 1 && manager.Orders[0].Id == "order-1" &&
                manager.Orders[0].Flavor == 0 && manager.Orders[0].Size == 0 &&
                economy.OrderSerial == 1 && manager.Revision == 1, "initial order missing or incorrect");
            Near(manager.Orders[0].Remaining, 120, "initial patience wrong");
        });
        Test("initial order can request valid saved stock", () => {
            var economy = new Economy();
            var stock = Candy("saved", 120, 2, 2, 1);
            Check(economy.CompleteRun(stock), "saved stock rejected");
            var manager = new OrderManager(economy);
            Check(manager.Orders.Count == 1 && manager.Orders[0].Flavor == 2 &&
                manager.Orders[0].Size == 1 && CandyRecipe.Matches(stock, manager.Orders[0]),
                "initial order cannot use saved stock");
        });
        Test("reload keeps waiting orders, countdown and next serial", () => {
            var economy = new Economy();
            economy.Orders.Add(new CustomerOrder { Id = "order-7", Flavor = 1, Size = 1, Remaining = 42 });
            economy.OrderSerial = 7;
            economy.NextCustomerIn = 4;
            var manager = new OrderManager(economy);
            Check(manager.Orders.Count == 1 && manager.Orders[0].Id == "order-7" &&
                manager.Orders[0].Remaining == 42 && economy.NextCustomerIn == 4 && manager.Revision == 0,
                "constructor reset loaded queue");
            manager.Tick(4);
            Check(manager.Orders.Count == 2 && manager.Orders[1].Id == "order-8" &&
                economy.OrderSerial == 8, "arrival reused an ID after reload");
        });
        Test("queue caps at two and expiry never removes coins or stock", () => {
            var economy = new Economy();
            economy.CompleteRun(Strawberry("kept"));
            var manager = new OrderManager(economy);
            manager.Tick(15);
            Check(manager.Orders.Count == 2 && manager.Orders[1].Id == "order-2" &&
                manager.Orders[0].Remaining == 105, "second arrival timing wrong");
            manager.Tick(105);
            Check(manager.Orders.Count == 1 && manager.Orders[0].Id == "order-2" &&
                economy.MissedOrders == 1 && economy.Inventory.Count == 1 && economy.Coins == 80,
                "expiry changed stock/coins or queue");
            manager.Tick(15);
            Check(manager.Orders.Count <= 2 && economy.OrderSerial <= 3, "full queue accumulated arrivals");
        });
        Test("full queue holds the next arrival until fifteen seconds after a slot opens", () => {
            var economy = new Economy();
            economy.CompleteRun(Strawberry("ready"));
            var manager = new OrderManager(economy);
            manager.Tick(15);
            manager.Tick(40);
            Check(economy.OrderSerial == 2 && economy.Coins == 80 && economy.Inventory.Count == 1,
                "waiting customers bought stock or queued hidden arrivals");
            Check(manager.Serve("order-1", "ready") != null, "first order could not be served");
            manager.Tick(14);
            Check(manager.Orders.Count == 1 && economy.OrderSerial == 2, "arrival occurred before fifteen seconds");
            manager.Tick(1);
            Check(manager.Orders.Count == 2 && manager.Orders[1].Id == "order-3" &&
                economy.OrderSerial == 3, "slot did not refill after fifteen seconds");
        });
        Test("invalid and huge time inputs cannot corrupt or spin the queue", () => {
            var economy = new Economy();
            var manager = new OrderManager(economy);
            int revision = manager.Revision;
            manager.Tick(-1); manager.Tick(double.NaN); manager.Tick(double.PositiveInfinity);
            Check(manager.Revision == revision && manager.Orders[0].Remaining == 120,
                "invalid time changed queue");
            manager.Tick(1e12);
            Check(manager.Orders.Count <= 2 && economy.MissedOrders > 0 && economy.MissedOrders < 1000,
                "huge time generated unbounded arrivals");
        });
        Test("wrong and repeated handover never pay or consume stock", () => {
            var economy = new Economy();
            var good = Strawberry("good");
            var wrong = Candy("wrong", 60, 1);
            economy.CompleteRun(good); economy.CompleteRun(wrong);
            var manager = new OrderManager(economy);
            string orderId = manager.Orders[0].Id;
            Check(manager.Serve("missing", good.Id) == null &&
                manager.Serve(orderId, wrong.Id) == null &&
                manager.Serve(orderId, "missing") == null, "invalid handover succeeded");
            Check(economy.Inventory.Count == 2 && economy.Orders.Count == 1 && economy.Coins == 80,
                "invalid handover mutated state");
            var receipt = manager.Serve(orderId, good.Id);
            Check(receipt != null && receipt.Price == 73 && receipt.Tip == 18 &&
                receipt.Satisfaction == 1, "valid receipt wrong");
            Check(economy.Coins == 171 && economy.TotalTips == 18 &&
                economy.LifetimeRevenue == 91 && economy.TotalSold == 1 &&
                economy.OrdersServed == 1 && economy.Inventory.Count == 1 && economy.Orders.Count == 0,
                "handover accounting or removal wrong");
            Check(manager.Serve(orderId, good.Id) == null && economy.Coins == 171,
                "same order paid twice");
        });
        Test("tip and satisfaction follow remaining patience", () => {
            var economy = new Economy();
            var stock = Strawberry("timed");
            economy.CompleteRun(stock);
            var manager = new OrderManager(economy);
            manager.Tick(60);
            var receipt = manager.Serve("order-1", stock.Id);
            Check(receipt != null && receipt.Price == 73 && receipt.Tip == 9,
                "half-patience tip wrong");
            Near(receipt.Satisfaction, .5, "half-patience satisfaction wrong");
            Near(economy.SatisfactionTotal, .5, "aggregate satisfaction wrong");
        });
        Test("expired order cannot be served with stale ID", () => {
            var economy = new Economy();
            var stock = Strawberry("kept");
            economy.CompleteRun(stock);
            var manager = new OrderManager(economy);
            manager.Tick(120);
            Check(manager.Serve("order-1", stock.Id) == null && economy.Inventory.Count == 1 &&
                economy.TotalSold == 0 && economy.Coins == 80, "expired handover paid");
        });
        Test("shelf upgrades raise capacity and discard frees one slot", () => {
            var economy = new Economy();
            for (int i = 0; i < 6; i++) Check(economy.CompleteRun(Strawberry("stock-" + i)), "early stock refusal");
            Check(!economy.CompleteRun(Strawberry("overflow")) && !economy.BuyShelf() &&
                economy.ShelfCost == 160, "base shelf guard wrong");
            economy.Coins = 500;
            Check(economy.BuyShelf() && economy.ShelfLevel == 1 && economy.StockCapacity == 9 &&
                economy.Coins == 340 && economy.ShelfCost == 300, "first shelf upgrade wrong");
            for (int i = 6; i < 9; i++) Check(economy.CompleteRun(Strawberry("stock-" + i)), "upgraded slot unavailable");
            Check(!economy.CompleteRun(Strawberry("overflow")), "ninth slot cap wrong");
            Check(economy.Discard("stock-0") && economy.Inventory.Count == 8 &&
                !economy.Discard("stock-0") && economy.CompleteRun(Strawberry("replacement")),
                "discard did not free exactly one slot");
            Check(economy.BuyShelf() && economy.StockCapacity == 12 && economy.ShelfLevel == 2 &&
                economy.Coins == 40 && economy.ShelfCost == 0 && !economy.BuyShelf(),
                "second shelf cap or cost wrong");
        });
        Test("targeted run validates inputs and auto-finishes at machine-capped target", () => {
            var economy = new Economy();
            var session = new GameSession(economy);
            Check(!session.StartRun(0, 30) && !session.StartRun(60, double.NaN) &&
                !session.StartRun(60, double.PositiveInfinity) && !session.StartRun(60, 0) &&
                session.Mode == GameMode.Shop, "invalid run launched");
            Check(session.StartRun(60, 30) && session.Remaining == 30, "small run refused");
            session.Tick(1, 100, 10, 0);
            Check(session.Mode == GameMode.Results && session.Result != null &&
                session.Result.Grams == 60 && economy.Inventory.Count == 1,
                "small target did not auto-finish");
            session.ReturnToShop();
            Check(session.StartRun(1000, 30), "large requested run refused");
            session.Tick(1, 1000, 10, 0);
            Check(session.Mode == GameMode.Results && session.Result.Grams == economy.Capacity,
                "run did not cap at machine capacity");
        });
        Test("run stock guard uses expanded shelf capacity", () => {
            var economy = new Economy { Coins = 1000 };
            Check(economy.BuyShelf(), "shelf purchase failed");
            for (int i = 0; i < 6; i++) economy.CompleteRun(Strawberry("s" + i));
            var session = new GameSession(economy);
            Check(session.StartRun(60, 30), "six stock blocked expanded shelf");
            session.FinishRun(); session.ReturnToShop();
            for (int i = 6; i < 9; i++) economy.CompleteRun(Strawberry("s" + i));
            Check(!session.StartRun(60, 30) && !session.StartRun(), "full expanded shelf launched run");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
