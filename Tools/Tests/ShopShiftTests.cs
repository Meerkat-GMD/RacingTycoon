using System;
using CottonCircuit;

public static class ShopShiftTests
{
    static int passed, failed;
    static readonly double LapMeters = RaceCourse.Shared.Length;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static bool Near(double actual, double expected) { return Math.Abs(actual - expected) < .000001; }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static Product Make(ShopShift shift, int flavor, double meters)
    {
        for (int i = 0; i < 10; i++) shift.Pour(flavor);
        shift.Advance(1, meters);
        return shift.Extract();
    }
    public static int Main()
    {
        Test("new pacing leaves customer waiting for production", () => {
            var shift = new ShopShift(new Economy()); shift.Advance(ShopShift.ArrivalDelay - .01, 0);
            Check(shift.State.Customers.Count == 1, "second customer must wait sixty seconds");
        });
        Test("resume supports stocked undersize candy", () => {
            var e = new Economy(); var shift = new ShopShift(e); var p = Make(shift, 1, 100);
            var method = typeof(ShopShift).GetMethod("ResumeProduct");
            Check(method != null, "resume API missing");
            Check((bool)method.Invoke(shift, new object[] { p.Id }) && e.Inventory.Count == 0 && shift.State.BatchMeters == 100,
                "resume must move existing candy into production");
        });
        Test("correct customer stays briefly to show happiness", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0);
            var p = Make(shift, c.Flavor, ShopShift.MetersForSize(c.Size)); shift.Deliver(p.Id, c.Id);
            Check(shift.CustomerAt(0) == c && Near(c.ReactionRemaining, 1.5), "happy reaction missing");
        });
        Test("unserved customer times out after ninety seconds", () => {
            var shift = new ShopShift(new Economy()); var c = shift.CustomerAt(0); shift.Advance(90, 0);
            Check(c.Angry && Near(c.ReactionRemaining, 1.5), "patience expiry must start angry reaction");
        });
        Test("arrivals use sixty seconds and stable independent patience", () => {
            var shift = new ShopShift(new Economy()); var first = shift.CustomerAt(0);
            shift.Advance(ShopShift.ArrivalDelay - .01, 0);
            Check(shift.State.Customers.Count == 1, "second arrival early");
            shift.Advance(.01, 0); var second = shift.CustomerAt(1);
            Check(second != null && shift.CustomerAt(0) == first && Near(second.PatienceRemaining, 90), "arrival disturbed slots");
            shift.Advance(30, 0);
            Check(first.TimedOut && !second.Angry && Near(second.PatienceRemaining, 60), "patience was not independent");
        });
        Test("an angry departure at the arrival deadline preserves the scheduled customer", () => {
            foreach (double advance in new[] { 1.5, 61.5 })
            {
                var economy = new Economy();
                var shift = new ShopShift(economy);
                var candy = ShopShift.Preview(LapMeters, 1);
                candy.Id = "wrong-stock";
                economy.Inventory.Add(candy);
                var original = shift.CustomerAt(0);
                shift.Advance(ShopShift.ArrivalDelay - 1.5, 0);
                Check(shift.Deliver(candy.Id, original.Id) == DeliveryResult.Wrong, "setup did not anger first customer");
                shift.Advance(advance, 0);
                Check(shift.CustomerAt(0) != null && shift.CustomerAt(0).Id != original.Id &&
                    shift.State.Customers.Count == (advance == 1.5 ? 1 : 2),
                    "departure canceled the independent arrival at the same deadline");
            }
        });
        Test("serving the exact customer leaves neighbors and reuses the gap", () => {
            var e = new Economy(); var shift = new ShopShift(e);
            e.OrderSerial = 3;
            shift.State.Customers.Add(new ShopCustomer { Id = "shop-2", Slot = 1, Flavor = 1 });
            shift.State.Customers.Add(new ShopCustomer { Id = "shop-3", Slot = 2, Flavor = 2 });
            var first = shift.CustomerAt(0); var middle = shift.CustomerAt(1); var last = shift.CustomerAt(2);
            var candy = Make(shift, middle.Flavor, ShopShift.MetersForSize(middle.Size));
            Check(shift.Deliver(candy.Id, first.Id) == DeliveryResult.Wrong, "wrong target was silently routed to matching neighbor");
            Check(first.Angry && !middle.Angry && shift.CustomerAt(2) == last, "wrong target affected neighbors");
            var secondCandy = Make(shift, middle.Flavor, ShopShift.MetersForSize(middle.Size));
            Check(shift.Deliver(secondCandy.Id, middle.Id) == DeliveryResult.Sold && shift.CustomerAt(1) == middle && middle.Happy &&
                shift.CustomerAt(2) == last && e.TotalSold == 1, "exact target did not leave a stable gap");
            shift.Advance(1.5, 0);
            Check(shift.CustomerAt(0) == null && shift.CustomerAt(2) == last, "angry reaction affected other slots");
            shift.Advance(ShopShift.ArrivalDelay - 1, 0);
            Check(shift.CustomerAt(0) != null && shift.CustomerAt(0).Id != first.Id && shift.CustomerAt(1) == null,
                "replacement did not take first available stable slot");
        });
        Test("one pour runs out after one fifth of a lap", () => {
            var e = new Economy(); var shift = new ShopShift(e);
            shift.Advance(10, LapMeters);
            Check(shift.State.BatchMeters == 0, "empty sugar grew candy");
            Check(shift.Pour(1), "pour refused"); shift.Advance(10, LapMeters);
            Check(Near(shift.State.BatchMeters, LapMeters * .2) && shift.State.SugarGrams == 0,
                "one pour did not exhaust after 0.2 lap");
            var candy = shift.Extract();
            Check(candy != null && ShopShift.SizeOf(candy) == -1 && candy.DistanceBased && candy.FlavorIndex == 1,
                "undersize extraction lost explicit product state");
            Check(e.Day == 1 && e.Coins == 80 && e.Inventory.Count == 1, "extraction awarded lap income or incremented day");
            Check(shift.Extract() == null && e.Inventory.Count == 1, "repeated extraction duplicated stock");
        });
        Test("a full sugar tank produces exactly two laps and size C", () => {
            var shift = new ShopShift(new Economy());
            for (int i = 0; i < 10; i++) Check(shift.Pour(2), "full tank setup failed");
            shift.Advance(1, LapMeters);
            Check(Near(shift.State.SugarGrams, 50) && Near(shift.State.BatchMeters, LapMeters),
                "one lap did not consume half a tank");
            shift.Advance(1, LapMeters * 2);
            Check(Near(shift.State.BatchMeters, LapMeters * 2) && shift.State.SugarGrams == 0,
                "a full tank did not exhaust after two laps");
            var candy = shift.Extract();
            Check(candy != null && ShopShift.SizeOf(candy) == 2, "full tank could not reach size C");
        });
        Test("full tank exhaustion across small frames still reaches size C", () => {
            foreach (int frames in new[] { 120, 1000, 5000, 12345 })
            {
                var shift = new ShopShift(new Economy());
                for (int i = 0; i < 10; i++) shift.Pour(0);
                for (int i = 0; i <= frames; i++) shift.Advance(1d / frames, LapMeters * 2 / frames);
                Check(Near(shift.State.BatchMeters, LapMeters * 2) && shift.State.SugarGrams == 0,
                    "small frames changed the full tank driving range at " + frames + " frames");
                double missingMeters = LapMeters * 2 - shift.State.BatchMeters;
                Check(ShopShift.SizeOf(shift.Extract()) == 2,
                    "floating point accumulation prevented size C at " + frames + " frames; short by " + missingMeters.ToString("R") + " meters");
            }
        });
        Test("small frame size targets stay exact without promoting genuinely short batches", () => {
            double[] laps = { 1, 1.5, 2 };
            for (int size = 0; size < laps.Length; size++)
            {
                var shift = new ShopShift(new Economy());
                for (int i = 0; i < 10; i++) shift.Pour(0);
                double target = LapMeters * laps[size];
                shift.Advance(1, target * .999999);
                Check(shift.State.BatchMeters < target && ShopShift.SizeForDistance(shift.State.BatchMeters) == size - 1,
                    "a genuinely short batch was promoted to size " + size);
                for (int i = 0; i < 120; i++) shift.Advance(.001, target * .000001 / 120);
                Check(shift.State.BatchMeters == target && ShopShift.SizeForDistance(shift.State.BatchMeters) == size,
                    "tiny final frames did not reach exact size " + size);
            }
        });
        Test("only positive finite forward distance grows a fueled batch", () => {
            var shift = new ShopShift(new Economy()); shift.Pour(0);
            foreach (double distance in new[] { 0d, -100d, double.NaN, double.PositiveInfinity }) shift.Advance(1, distance, 3, 1);
            Check(shift.State.BatchMeters == 0 && shift.State.SugarGrams == 10 && shift.State.BatchQuality == 50,
                "idle, reverse, or invalid distance changed the batch");
            double remaining = shift.State.RemainingSeconds;
            foreach (double seconds in new[] { 0d, -1d, double.NaN, double.PositiveInfinity }) shift.Advance(seconds, 100);
            Check(shift.State.RemainingSeconds == remaining && shift.State.BatchMeters == 0, "invalid time advanced gameplay");
        });
        Test("distance thresholds assign exact sizes and reject invalid values", () => {
            double[] meters = { -1, 0, .001, LapMeters - .001, LapMeters, LapMeters * 1.5 - .001,
                LapMeters * 1.5, LapMeters * 2 - .001, LapMeters * 2, 1000000, double.NaN, double.PositiveInfinity };
            int[] sizes = { -1, -1, -1, -1, 0, 0, 1, 1, 2, 2, -1, -1 };
            for (int i = 0; i < meters.Length; i++) Check(ShopShift.SizeForDistance(meters[i]) == sizes[i], "wrong tier at " + meters[i]);
            Check(ShopShift.SizeOf(null) == -1, "null has a size");
        });
        Test("size distances map A B C to one one-and-a-half and two laps", () => {
            double[] laps = { 1, 1.5, 2 };
            for (int size = 0; size < laps.Length; size++)
            {
                double meters = ShopShift.MetersForSize(size);
                Check(Near(meters / LapMeters, laps[size]) && ShopShift.SizeForDistance(meters) == size,
                    "size helper returned the wrong lap target");
            }
            foreach (int invalid in new[] { -1, 3, int.MinValue, int.MaxValue })
            {
                bool rejected = false;
                try { ShopShift.MetersForSize(invalid); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "size helper accepted invalid size " + invalid);
            }
        });
        Test("sugar caps at capacity and changing flavor requires extracting the batch", () => {
            var shift = new ShopShift(new Economy());
            Check(!shift.Pour(-1) && !shift.Pour(3), "invalid flavor accepted");
            for (int i = 0; i < 10; i++) Check(shift.Pour(1), "valid refill refused");
            Check(shift.State.SugarGrams == 100 && !shift.Pour(1), "capacity exceeded");
            shift.Advance(1, 100); Check(!shift.Pour(2), "mixed an active batch");
            Check(shift.Pour(1) && shift.State.SugarGrams == 100, "same flavor cannot top up");
            shift.Extract(); Check(shift.Pour(2), "flavor replacement refused after extraction");
            Check(shift.State.SugarGrams == 10 && shift.State.SugarFlavor == 2, "flavor replacement retained previous sugar");
            shift.Advance(1, 500); Check(!shift.Pour(0), "exhausted batch lost its flavor lock");
        });
        Test("preview renders tiny and huge products with bounded valid samples", () => {
            Check(ShopShift.Preview(0, 0) == null && ShopShift.Preview(double.NaN, 0) == null &&
                ShopShift.Preview(1000, 3) == null, "invalid preview produced candy");
            foreach (double meters in new[] { double.Epsilon, .0001, 1000, 3000, double.MaxValue })
            {
                var p = ShopShift.Preview(meters, 2);
                Check(p != null && p.DistanceMeters == meters && p.DistanceBased && p.FlavorIndex == 2 &&
                    p.Samples.Count >= 1 && p.Samples.Count <= 180 && p.Grams == p.Samples.Count * 2, "unbounded or empty preview");
                double previous = 0;
                foreach (var sample in p.Samples)
                {
                    Check(sample.Flavor == 2 && sample.Angle > previous && sample.Angle <= 100 && sample.Radius >= 7 && sample.Radius <= 13,
                        "preview violates saved/rendered sample limits");
                    previous = sample.Angle;
                }
            }
        });
        Test("preview cache buckets grow at sample boundaries and stop allocating at their cap", () => {
            double[] meters = { -1, 0, double.NaN, double.PositiveInfinity, double.Epsilon,
                LapMeters / 30 - .001, LapMeters / 30 + .001, LapMeters, LapMeters + .001,
                LapMeters * 1.5, LapMeters * 2, LapMeters * 6 - .001, LapMeters * 6, double.MaxValue };
            int[] samples = { 0, 0, 0, 0, 1, 1, 2, 30, 31, 45, 60, 180, 180, 180 };
            for (int i = 0; i < meters.Length; i++)
                Check(ShopShift.PreviewSampleCount(meters[i]) == samples[i], "wrong preview bucket at " + meters[i]);
        });
        Test("full shelf refuses extraction without losing batch or sugar", () => {
            var e = new Economy(); var shift = new ShopShift(e);
            for (int i = 0; i < e.StockCapacity; i++) Check(Make(shift, 0, 100) != null, "failed shelf setup");
            shift.Pour(0); shift.Advance(1, 200);
            double sugar = shift.State.SugarGrams;
            Check(shift.Extract() == null && shift.State.BatchMeters == 200 && shift.State.SugarGrams == sugar,
                "full shelf discarded the current batch");
            Check(shift.Discard(e.Inventory[0].Id) && shift.Extract() != null && e.Inventory.Count == 6,
                "batch was unavailable after making shelf room");
        });
        Test("correct delivery pays once and requires the current customer identity", () => {
            var e = new Economy(); var shift = new ShopShift(e); var customer = shift.CustomerAt(0);
            var candy = Make(shift, customer.Flavor, LapMeters * (1 + customer.Size * .5)); candy.Quality = 0;
            Check(shift.Deliver(candy.Id, "stale-customer") == DeliveryResult.Rejected && e.Inventory.Count == 1,
                "stale customer accepted the product");
            Check(shift.Deliver(candy.Id, customer.Id) == DeliveryResult.Sold, "matching delivery refused");
            int paid = 30 + customer.Size * 30;
            Check(e.Coins == 80 + paid && e.TotalSold == 1 && e.LifetimeRevenue == paid && e.TotalTips == 0 &&
                shift.State.DayRevenue == paid && shift.State.DaySold == 1 && shift.CustomerAt(0) == customer && customer.Happy, "sale accounting is incorrect");
            Check(shift.Deliver(candy.Id, customer.Id) == DeliveryResult.Rejected && e.Coins == 80 + paid,
                "duplicate drop paid twice");
            shift.Advance(ShopShift.ArrivalDelay - 1.01, 0); Check(shift.CustomerAt(0) == null, "customer arrived early");
            shift.Advance(.01, 0); Check(shift.CustomerAt(0) != null && shift.CustomerAt(0).Id != customer.Id, "next customer did not arrive");
        });
        Test("undersize customer drop returns stock and trash consumes it once", () => {
            var e = new Economy(); var shift = new ShopShift(e); var p = Make(shift, 0, LapMeters * .5);
            Check(shift.Deliver(p.Id, shift.CustomerAt(0).Id) == DeliveryResult.Rejected && e.Inventory.Count == 1 &&
                !shift.CustomerAt(0).Angry && e.Price(p) == 0, "undersize product could be delivered");
            Check(shift.Discard(p.Id) && !shift.Discard(p.Id) && shift.State.DayTrashed == 1 && e.Inventory.Count == 0 && e.Coins == 80,
                "discard was not one-time or paid money");
        });
        Test("wrong flavor consumes candy and preserves the angry reaction before departure", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0);
            var p = Make(shift, (c.Flavor + 1) % 3, LapMeters * (1 + c.Size * .5));
            Check(shift.Deliver(p.Id, c.Id) == DeliveryResult.Wrong && e.Inventory.Count == 0 && e.Coins == 80 &&
                c.Angry && Near(c.ReactionRemaining, 1.5) && shift.State.DayWrong == 1, "wrong delivery did not consume without paying");
            var second = Make(shift, c.Flavor, LapMeters * (1 + c.Size * .5));
            Check(shift.Deliver(second.Id, c.Id) == DeliveryResult.Rejected && e.Inventory.Count == 1, "angry customer accepted another delivery");
            shift.Advance(.49, 0); Check(shift.CustomerAt(0) == c, "angry customer left before 1.5 seconds");
            shift.Advance(.01, 0); Check(shift.CustomerAt(0) == null, "angry customer did not leave at 1.5 seconds");
            shift.Advance(ShopShift.ArrivalDelay - 2.51, 0); Check(shift.CustomerAt(0) == null, "replacement arrived before the next interval");
            shift.Advance(.01, 0); Check(shift.CustomerAt(0) != null && shift.CustomerAt(0).Id != c.Id, "replacement did not arrive");
        });
        Test("larger candy does not substitute for the requested size", () => {
            var shift = new ShopShift(new Economy()); var c = shift.CustomerAt(0); c.Size = 0;
            var p = Make(shift, c.Flavor, LapMeters * 1.5);
            Check(shift.Deliver(p.Id, c.Id) == DeliveryResult.Wrong && shift.State.DaySold == 0, "larger tier substituted for A");
        });
        Test("one long tick accounts for angry reaction and next arrival in order", () => {
            var shift = new ShopShift(new Economy()); var c = shift.CustomerAt(0);
            var p = Make(shift, (c.Flavor + 1) % 3, LapMeters * (1 + c.Size * .5));
            shift.Deliver(p.Id, c.Id); shift.Advance(ShopShift.ArrivalDelay + .5, 0);
            Check(shift.CustomerAt(0) != null && shift.CustomerAt(0).Id != c.Id && !shift.CustomerAt(0).Angry,
                "long tick skipped customer phase transition");
        });
        Test("pause freezes all timers and blocks production and inventory actions", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0);
            var p = Make(shift, (c.Flavor + 1) % 3, LapMeters * (1 + c.Size * .5)); shift.Deliver(p.Id, c.Id);
            var kept = Make(shift, c.Flavor, 500); shift.Pour(c.Flavor); shift.Advance(.1, 10);
            double remaining = shift.State.RemainingSeconds, reaction = c.ReactionRemaining, sugar = shift.State.SugarGrams;
            shift.Paused = true; shift.Advance(100, 1000);
            Check(shift.State.RemainingSeconds == remaining && c.ReactionRemaining == reaction && shift.State.BatchMeters == 10 &&
                shift.State.SugarGrams == sugar, "paused timers or growth changed");
            Check(!shift.Pour(c.Flavor) && shift.Extract() == null && !shift.Discard(kept.Id) &&
                shift.Deliver(kept.Id, c.Id) == DeliveryResult.Rejected && !shift.NextDay(), "paused action mutated state");
        });
        Test("closing clips the final frame and next day starts with empty stock and production", () => {
            var e = new Economy(); var shift = new ShopShift(e); var p = Make(shift, 0, LapMeters * .5);
            shift.Pour(0);
            shift.Advance(1, 100, 1, 0);
            shift.State.BatchProductId = "active-candy";
            var oldCustomer = shift.CustomerAt(0);
            shift.Advance(597, 0); shift.Advance(2, 1000);
            Check(shift.State.Closed && !shift.IsOpen && shift.State.RemainingSeconds == 0 && shift.State.BatchMeters == 0 &&
                shift.State.BatchProductId == null && e.Inventory.Count == 0 && shift.State.DayTrashed == 2,
                "closing did not discard the stock and unfinished batch as disposals");
            double sugar = shift.State.SugarGrams;
            Check(shift.CustomerAt(0) == null && !shift.Pour(0) && shift.Extract() == null && !shift.Discard(p.Id) &&
                shift.Deliver(p.Id, "stale") == DeliveryResult.Rejected && e.Day == 1, "closed day accepted actions or incremented day");
            shift.Advance(10, 1000); Check(shift.State.SugarGrams == sugar && shift.State.BatchMeters == 0, "closed simulation changed stock");
            Check(shift.NextDay() && e.Day == 2 && shift.State.RemainingSeconds == 600 && !shift.State.Closed &&
                shift.State.SugarGrams == 0 && shift.State.SugarFlavor == -1 && shift.State.BatchMeters == 0 &&
                shift.State.BatchFlavor == -1 && shift.State.BatchQuality == 50 && shift.State.BatchProductId == null &&
                e.Inventory.Count == 0 && e.CompletedIds.Count == 0, "next day retained stock or production");
            var fresh = shift.CustomerAt(0);
            Check(shift.State.Customers.Count == 1 && fresh != null && fresh != oldCustomer && fresh.Id != oldCustomer.Id &&
                !fresh.Angry && !fresh.Happy && !fresh.TimedOut && Near(fresh.PatienceRemaining, 90) &&
                Near(shift.State.NextCustomerIn, 60), "next day did not create the normal fresh customer");
            Check(!shift.NextDay() && e.Day == 2, "next day started twice");
        });
        Test("empty sugar preserves the active candy stock customer and clock", () => {
            var e = new Economy(); var shift = new ShopShift(e); var stocked = Make(shift, 1, 100);
            shift.Pour(2); shift.Advance(1, 100, 2, 0);
            shift.State.BatchProductId = "active-candy";
            var customer = shift.CustomerAt(0);
            double remaining = shift.State.RemainingSeconds, nextArrival = shift.State.NextCustomerIn;
            double meters = shift.State.BatchMeters;
            int quality = shift.State.BatchQuality;
            Check(shift.EmptySugar(), "empty sugar action rejected loaded sugar");
            Check(shift.State.SugarGrams == 0 && shift.State.SugarFlavor == -1 &&
                shift.State.BatchMeters == meters && shift.State.BatchFlavor == 2 && shift.State.BatchQuality == quality &&
                shift.State.BatchProductId == "active-candy" && e.Inventory.Count == 1 && e.Inventory[0] == stocked &&
                shift.CustomerAt(0) == customer && shift.State.RemainingSeconds == remaining &&
                shift.State.NextCustomerIn == nextArrival, "empty sugar changed other business state");
            Check(!shift.EmptySugar() && shift.State.BatchProductId == "active-candy",
                "repeated empty action mutated the active candy");
        });
        Test("empty sugar rejects empty paused and closed shifts", () => {
            var shift = new ShopShift(new Economy());
            Check(!shift.EmptySugar() && shift.State.SugarFlavor == -1, "empty sugar succeeded with an empty tank");
            shift.Pour(1); shift.Paused = true;
            Check(!shift.EmptySugar() && shift.State.SugarGrams == 10 && shift.State.SugarFlavor == 1,
                "paused shift emptied sugar");
            shift.Paused = false; shift.Advance(600, 0);
            Check(!shift.EmptySugar() && shift.State.SugarGrams == 10 && shift.State.SugarFlavor == 1,
                "closed shift emptied sugar");
        });
        Test("daily accounting resets while lifetime sale records survive", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0);
            var p = Make(shift, c.Flavor, LapMeters * (1 + c.Size * .5)); shift.Deliver(p.Id, c.Id);
            var junk = Make(shift, 0, 10); shift.Discard(junk.Id);
            e.Levels[0] = 1; e.Levels[1] = 2; e.Levels[2] = 3; e.ShelfLevel = 1;
            int revenue = e.LifetimeRevenue, coins = e.Coins;
            shift.Advance(600, 0);
            int missed = e.MissedOrders, capacity = e.StockCapacity, orderSerial = e.OrderSerial;
            Check(missed > 0 && shift.State.DayMissed > 0, "setup did not record missed customers");
            shift.NextDay();
            Check(shift.State.DayRevenue == 0 && shift.State.DaySold == 0 && shift.State.DayWrong == 0 &&
                shift.State.DayMissed == 0 && shift.State.DayTrashed == 0 && e.TotalSold == 1 &&
                e.LifetimeRevenue == revenue && e.Coins == coins && e.MissedOrders == missed &&
                e.OrderSerial == orderSerial + 1 && e.Levels[0] == 1 && e.Levels[1] == 2 &&
                e.Levels[2] == 3 && e.ShelfLevel == 1 && e.StockCapacity == capacity,
                "next day corrupted daily or lifetime progress");
        });
        Test("distance prices use A B C bases with quality and shop level modifiers", () => {
            var e = new Economy();
            int[] prices = { 30, 60, 90 };
            for (int i = 0; i < 3; i++)
            {
                var p = ShopShift.Preview(LapMeters * (1 + i * .5), 0); p.Quality = 0;
                Check(e.Price(p) == prices[i], "distance tier has wrong base price");
            }
            var candy = ShopShift.Preview(LapMeters * 1.5, 2); candy.Quality = 100; e.Levels[2] = 2;
            Check(e.Price(candy) == 117, "quality and shop multiplier did not apply to tier base");
            var legacy = new Production(60); legacy.Advance(100, 10, 0); var old = legacy.Finish();
            e.Levels[2] = 0; Check(e.Price(old) == 73, "legacy product pricing changed");
        });
        Test("legacy stock migrates by tier and preserves identity upgrades and revenue", () => {
            var e = new Economy(); var a = new Production(60); a.Advance(100, 10, 1); var p = a.Finish(); p.Quality = 65;
            var b = new Production(120); b.Advance(100, 10, 2); var q = b.Finish();
            e.CompleteRun(p); e.CompleteRun(q); e.Levels[2] = 2; new OrderManager(e); int day = e.Day;
            var shift = new ShopShift(e);
            Check(e.Inventory[0].Id == p.Id && e.Inventory[1].Id == q.Id && p.DistanceBased && q.DistanceBased &&
                p.DistanceMeters == LapMeters && q.DistanceMeters == LapMeters * 1.5 && p.FlavorIndex == 1 && q.FlavorIndex == 2 && p.Quality == 65,
                "legacy stock migration lost products");
            Check(e.Day == day && e.Coins == 80 && e.Levels[2] == 2 && e.Orders.Count == 0 && shift.CustomerAt(0) != null,
                "migration reset economy or left old customers");
            var same = new ShopShift(e); Check(same.CustomerAt(0).Id == shift.CustomerAt(0).Id && e.Inventory.Count == 2,
                "reload migrated stock or customer twice");
        });
        Test("existing distance products retain their meters and use lap based tiers", () => {
            var e = new Economy();
            double[] meters = { LapMeters - .001, LapMeters, LapMeters * 1.5, LapMeters * 2 };
            int[] sizes = { -1, 0, 1, 2 };
            for (int i = 0; i < meters.Length; i++)
                e.Inventory.Add(new Product { Id = "saved-" + i, DistanceBased = true, DistanceMeters = meters[i], FlavorIndex = 2 });
            new ShopShift(e);
            for (int i = 0; i < meters.Length; i++)
                Check(e.Inventory[i].DistanceMeters == meters[i] && ShopShift.SizeOf(e.Inventory[i]) == sizes[i],
                    "saved distance product was rescaled or classified with old thresholds");
        });
        Test("reconstructing a shift keeps pending arrivals and angry state", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0);
            var p = Make(shift, (c.Flavor + 1) % 3, LapMeters * (1 + c.Size * .5)); shift.Deliver(p.Id, c.Id); shift.Advance(.4, 0);
            var resumed = new ShopShift(e);
            Check(resumed.CustomerAt(0) == c && Near(c.ReactionRemaining, 1.1), "reload reset angry reaction");
            resumed.Advance(1.1, 0); resumed.Advance(.5, 0); var waiting = new ShopShift(e);
            Check(waiting.CustomerAt(0) == null && Near(waiting.State.NextCustomerIn, ShopShift.ArrivalDelay - 3), "reload skipped arrival wait");
        });
        Test("eligible driving changes quality while extracting resets the next batch", () => {
            var shift = new ShopShift(new Economy()); shift.Pour(0); shift.Advance(1, LapMeters * .1, 2, 1);
            Check(shift.Extract().Quality == 50, "driving deltas did not set batch quality");
            shift.Advance(1, LapMeters * .1, 1, 0); Check(shift.Extract().Quality == 55, "batch quality leaked or skill did not apply");
        });
        Test("vertical round trips pour once while jitter horizontal and outside motion do not", () => {
            var shake = new SugarShake(); Check(!shake.Move(0, 0, true), "pickup poured");
            Check(!shake.Move(100, 0, true) && !shake.Move(200, 0, true), "horizontal movement poured");
            for (int i = 0; i < 10; i++) Check(!shake.Move(200, i % 2 == 0 ? 10 : 0, true), "jitter poured");
            Check(!shake.Move(200, 24, true) && shake.Move(200, 0, true), "24 pixel round trip did not pour");
            Check(!shake.Move(200, -24, true) && shake.Move(200, 0, true), "second full round trip did not pour");
            Check(!shake.Move(0, 100, false) && !shake.Move(0, 0, true), "crossing boundary counted an outside leg");
            shake.Reset(); Check(!shake.Move(0, 100, true) && !shake.Move(0, 76, true) && shake.Move(0, 100, true),
                "downward first round trip failed");
            shake.Reset(); Check(!shake.Move(double.NaN, 0, true) && !shake.Move(0, 0, true), "nonfinite pointer made a shake");
        });
        Test("full shelf exchange preserves identity distance quality and fuel", () => {
            var e = new Economy(); var shift = new ShopShift(e);
            for (int i = 0; i < e.StockCapacity; i++) Make(shift, 1, 50 + i);
            var picked = e.Inventory[2]; picked.Quality = 73;
            shift.Pour(0); shift.Advance(1, 100);
            double sugar = shift.State.SugarGrams;
            Check(shift.ResumeProduct(picked.Id), "full shelf exchange rejected");
            var outgoing = e.Inventory[2];
            Check(e.Inventory.Count == e.StockCapacity && outgoing.DistanceMeters == 100 && outgoing.FlavorIndex == 0 &&
                shift.State.BatchProductId == picked.Id && shift.State.BatchQuality == 73 && shift.State.BatchMeters == picked.DistanceMeters &&
                shift.State.BatchFlavor == 1 && shift.State.SugarGrams == sugar && shift.State.SugarFlavor == 0, "exchange lost attributes");
            Check(!shift.ResumeProduct(picked.Id), "repeated resume duplicated active candy");
            shift.Advance(1, 100, 5, 0);
            Check(shift.State.BatchMeters == picked.DistanceMeters && shift.State.SugarGrams == sugar && shift.State.BatchQuality == 73,
                "wrong sugar grew resumed candy");
            Check(shift.Pour(1), "matching replacement sugar refused");
            shift.Advance(1, 1);
            Check(shift.State.BatchMeters == picked.DistanceMeters + 1, "resumed growth missing");
            Check(shift.ResumeProduct(outgoing.Id) && e.Inventory[2].Id == picked.Id && e.Inventory[2].Quality == 73,
                "swap back changed original identity or quality");
            shift.Discard(e.Inventory[0].Id);
            Check(shift.Extract().Id == outgoing.Id && e.Coins == 80 && e.TotalSold == 0, "extract changed resumed identity or paid");
        });
        Test("paused and closed shifts reject resume without moving inventory", () => {
            var e = new Economy(); var shift = new ShopShift(e); var p = Make(shift, 0, 100);
            shift.Paused = true; Check(!shift.ResumeProduct(p.Id) && e.Inventory.Count == 1, "paused resume moved stock");
            shift.Paused = false; shift.Advance(600, 0);
            Check(!shift.ResumeProduct(p.Id) && e.Inventory.Count == 0 && shift.State.BatchMeters == 0 && shift.State.DayTrashed == 1,
                "closed resume moved stock or closing kept it");
        });
        Test("long and short customer ticks agree on deadlines and accounting", () => {
            var a = new Economy(); var b = new Economy(); var large = new ShopShift(a); var small = new ShopShift(b);
            large.Advance(250, 0); for (int i = 0; i < 2500; i++) small.Advance(.1, 0);
            Check(a.OrderSerial == b.OrderSerial && a.MissedOrders == b.MissedOrders && a.Business.DayMissed == b.Business.DayMissed &&
                a.Business.Customers.Count == b.Business.Customers.Count && Near(a.Business.NextCustomerIn, b.Business.NextCustomerIn),
                "chunking changed customer accounting");
            foreach (var c in a.Business.Customers) {
                var other = small.CustomerAt(c.Slot);
                Check(other != null && other.Id == c.Id && other.Angry == c.Angry && other.TimedOut == c.TimedOut &&
                    Near(other.PatienceRemaining, c.PatienceRemaining) && Near(other.ReactionRemaining, c.ReactionRemaining), "chunking changed deadlines");
            }
        });
        Test("timeout counts once keeps stock rejects delivery and freezes on pause", () => {
            var e = new Economy(); var shift = new ShopShift(e); var c = shift.CustomerAt(0); Make(shift, 0, 100);
            shift.Advance(89, 0);
            Check(c.TimedOut && c.Angry && !c.Happy && shift.State.DayMissed == 1 && e.MissedOrders == 1 && e.Inventory.Count == 1,
                "timeout accounting or inventory incorrect");
            shift.Paused = true; shift.Advance(10, 0);
            Check(Near(c.ReactionRemaining, 1.5), "paused timeout reaction advanced");
            shift.Paused = false; shift.Advance(1.5, 0);
            Check(shift.CustomerAt(0) == null && e.MissedOrders == 1 && shift.State.DayWrong == 0, "timeout departed or counted incorrectly");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
