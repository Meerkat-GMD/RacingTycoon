#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CottonCircuit.Tests
{
    public static class ShopShiftSaveChecks
    {
        public static int Run(string tempDir)
        {
            if (string.IsNullOrEmpty(tempDir)) throw new ArgumentException("A test output directory is required.", "tempDir");
            int checks = CheckValidation();
            string root = Path.Combine(tempDir, "shift-save-" + Guid.NewGuid().ToString("N"));
            var state = MidBatch();
            state.Inventory.Add(DistanceProduct("stocked-a", 1250, 2));
            var store = new SaveStore(Path.Combine(root, "mid-batch"));
            Check(ref checks, store.Save(state), "mid-batch state saves");
            string path = Path.Combine(store.DirectoryPath, "cotton-circuit.json");
            var envelope = JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(path));
            Check(ref checks, envelope.Version == 8, "new saves use V8 for driving selection, resumed candy and reactions");
            var restoredStore = new SaveStore(store.DirectoryPath);
            var restored = restoredStore.Load();
            Check(ref checks, restoredStore.CanSave && restoredStore.Error == null && restored.Business != null,
                "V6 reload accepts the persisted business");
            Check(ref checks, restored.Business.RemainingSeconds == 432.125 && restored.Business.SugarGrams == 37.5 &&
                restored.Business.SugarFlavor == 1 && restored.Business.BatchMeters == 2345.625 &&
                restored.Business.BatchFlavor == 1 && restored.Business.BatchQuality == 80,
                "reload preserves exact fractional time, loaded sugar, and unfinished batch");
            Check(ref checks, restored.Inventory.Count == 1 && restored.Inventory[0].DistanceBased &&
                restored.Inventory[0].DistanceMeters == 1250 && restored.Inventory[0].FlavorIndex == 2,
                "reload preserves stocked distance and flavor independently of preview grams");
            Check(ref checks, restored.Business.Customers[0].Id == "shop-1" && restored.Business.Customers[0].Size == 2 &&
                restored.Business.Customers[0].Flavor == 2 && restored.Business.Customers[0].Slot == 0 && restored.Business.NextCustomerIn == 2,
                "reload retains the same customer rather than issuing a new order");
            var resumed = new ShopShift(restored);
            Check(ref checks, resumed.State.RemainingSeconds == 432.125 && resumed.State.BatchMeters == 2345.625 &&
                resumed.CustomerAt(0).Id == "shop-1" && restored.OrderSerial == 1,
                "constructing the resumed shift does not reset production or duplicate customers");

            state.Coins = 155;
            state.LifetimeRevenue = state.Business.DayRevenue = 75;
            state.TotalSold = state.OrdersServed = state.Business.DaySold = 1;
            state.SatisfactionTotal = 1;
            state.OrderSerial = 2;
            state.Business.Customers[0].Id = "shop-2";
            state.Business.Customers[0].Angry = true;
            state.Business.Customers[0].ReactionRemaining = .75;
            state.Business.DayWrong = 1;
            Check(ref checks, store.Save(state), "angry customer state saves");
            restored = new SaveStore(store.DirectoryPath).Load();
            Check(ref checks, restored.Business.Customers[0].Angry && restored.Business.Customers[0].ReactionRemaining == .75 &&
                restored.Business.DayWrong == 1 && restored.Business.DayRevenue == 75 && restored.Business.DaySold == 1 &&
                restored.Coins == 155, "angry reaction resumes with its remaining delay and unchanged daily accounting");

            state.Business.Customers.Clear();
            state.Business.NextCustomerIn = 1.25;
            Check(ref checks, store.Save(state), "between-customer state saves without inventing a customer");
            restored = new SaveStore(store.DirectoryPath).Load();
            Check(ref checks, restored.Business != null && restored.Business.Customers.Count == 0 &&
                restored.Business.NextCustomerIn == 1.25 && restored.Business.RemainingSeconds == 432.125,
                "JsonUtility round-trip preserves a null customer and the remaining arrival delay");

            state.Business.Closed = true;
            state.Business.RemainingSeconds = 0;
            state.Business.Customers.Clear();
            state.Business.NextCustomerIn = 0;
            state.Business.DayTrashed = 2;
            Check(ref checks, store.Save(state), "closed day saves");
            restored = new SaveStore(store.DirectoryPath).Load();
            resumed = new ShopShift(restored);
            Check(ref checks, resumed.State.Closed && resumed.State.RemainingSeconds == 0 && resumed.CustomerAt(0) == null &&
                resumed.State.DayTrashed == 2 && resumed.State.DayRevenue == 75 && resumed.State.DaySold == 1 &&
                resumed.State.BatchMeters == 2345.625 && resumed.State.SugarGrams == 37.5 && restored.Coins == 155,
                "reload keeps the day closed and preserves carryover instead of reopening or paying again");

            Check(ref checks, resumed.NextDay(), "closed saved day starts a fresh business day");
            var freshDayStore = new SaveStore(Path.Combine(root, "fresh-day"));
            Check(ref checks, freshDayStore.Save(restored), "fresh day reset saves");
            var freshDay = new SaveStore(freshDayStore.DirectoryPath).Load();
            Check(ref checks, freshDay.Day == restored.Day && freshDay.Coins == 155 && freshDay.LifetimeRevenue == 75 &&
                freshDay.Inventory.Count == 0 && freshDay.CompletedIds.Count == 0 && freshDay.Business.SugarGrams == 0 &&
                freshDay.Business.SugarFlavor == -1 && freshDay.Business.BatchMeters == 0 && freshDay.Business.BatchFlavor == -1 &&
                string.IsNullOrEmpty(freshDay.Business.BatchProductId) && freshDay.Business.BatchQuality == 50 &&
                freshDay.Business.RemainingSeconds == 600 && !freshDay.Business.Closed && freshDay.Business.DayRevenue == 0 &&
                freshDay.Business.DaySold == 0 && freshDay.Business.DayWrong == 0 && freshDay.Business.DayTrashed == 0,
                "reload preserves empty new-day production and stock with lifetime progress intact");
            string freshCustomerId = freshDay.Business.Customers[0].Id;
            var freshShift = new ShopShift(freshDay);
            Check(ref checks, freshShift.State.Customers.Count == 1 && freshShift.CustomerAt(0).Id == freshCustomerId &&
                freshShift.CustomerAt(0).PatienceRemaining == 90 && !freshShift.CustomerAt(0).Angry &&
                freshShift.State.NextCustomerIn == 60, "new-day reload keeps the fresh customer without duplicating arrivals");

            string original = File.ReadAllText(path);
            state.Business.SugarGrams = 101;
            Check(ref checks, !store.Save(state) && File.ReadAllText(path) == original,
                "invalid runtime state cannot overwrite the last valid save");

            string corruptDir = Path.Combine(root, "corrupt");
            Directory.CreateDirectory(corruptDir);
            string corruptPath = Path.Combine(corruptDir, "cotton-circuit.json");
            string corrupt = JsonUtility.ToJson(new SaveStore.Envelope { Version = 5, State = state }, true);
            File.WriteAllText(corruptPath, corrupt);
            var corruptStore = new SaveStore(corruptDir);
            corruptStore.Load();
            Check(ref checks, !corruptStore.CanSave && corruptStore.Error != null,
                "invalid persisted business blocks writes and reports the load failure");
            Check(ref checks, !corruptStore.Save(new Economy()) && File.ReadAllText(corruptPath) == corrupt,
                "a corrupt original is preserved when a fallback economy tries to save");

            string absentBusinessDir = Path.Combine(root, "old-schema-without-business");
            Directory.CreateDirectory(absentBusinessDir);
            File.WriteAllText(Path.Combine(absentBusinessDir, "cotton-circuit.json"),
                "{\"Version\":3,\"State\":{\"Coins\":517,\"Day\":9,\"Levels\":[2,1,3],\"Inventory\":[]," +
                "\"CompletedIds\":[],\"ShelfLevel\":0,\"Orders\":[],\"OrderSerial\":0,\"TotalTips\":0," +
                "\"MissedOrders\":0,\"NextCustomerIn\":15,\"SatisfactionTotal\":0,\"OrdersServed\":0," +
                "\"TotalSold\":0,\"LifetimeRevenue\":0}}");
            var absentBusinessStore = new SaveStore(absentBusinessDir);
            var absentBusiness = absentBusinessStore.Load();
            Check(ref checks, absentBusinessStore.CanSave && absentBusiness.Business == null && absentBusiness.Coins == 517,
                "a genuine old schema with no Business field loads without a phantom closed shift");
            bool nullBusinessSaved = absentBusinessStore.Save(absentBusiness);
            var nullBusinessRestoreStore = new SaveStore(absentBusinessDir);
            var nullBusinessRestored = nullBusinessRestoreStore.Load();
            Check(ref checks, nullBusinessSaved && nullBusinessRestoreStore.CanSave &&
                nullBusinessRestored.Business == null && nullBusinessRestored.Coins == 517 && nullBusinessRestored.Day == 9,
                "JsonUtility keeps an intentionally absent business null across a V6 legacy-mode save");

            var markerConflict = JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(path));
            markerConflict.HasBusiness = false;
            File.WriteAllText(corruptPath, JsonUtility.ToJson(markerConflict, true));
            var conflictingStore = new SaveStore(corruptDir);
            conflictingStore.Load();
            Check(ref checks, !conflictingStore.CanSave,
                "a missing-business marker cannot silently erase a nonempty business payload");
            markerConflict.HasBusiness = true;
            markerConflict.HasCustomer = false;
            markerConflict.State.Business.Closed = false;
            markerConflict.State.Business.RemainingSeconds = 432.125;
            markerConflict.State.Business.Customer = new ShopCustomer { Id = "shop-2", Flavor = 1, Size = 1 };
            File.WriteAllText(corruptPath, JsonUtility.ToJson(markerConflict, true));
            conflictingStore = new SaveStore(corruptDir);
            conflictingStore.Load();
            Check(ref checks, !conflictingStore.CanSave,
                "a missing-customer marker cannot silently erase an actual customer payload");

            string v4Dir = Path.Combine(root, "single-customer-v4");
            Directory.CreateDirectory(v4Dir);
            var v4State = MidBatch();
            v4State.Business.Customer = v4State.Business.Customers[0];
            v4State.Business.Customers.Clear();
            v4State.Business.NextCustomerIn = 0;
            File.WriteAllText(Path.Combine(v4Dir, "cotton-circuit.json"), JsonUtility.ToJson(
                new SaveStore.Envelope { Version = 4, State = v4State, HasBusiness = true, HasCustomer = true }, true));
            var v4Store = new SaveStore(v4Dir);
            var v4Restored = v4Store.Load();
            Check(ref checks, v4Store.CanSave && v4Restored.Business.Customers.Count == 1 &&
                v4Restored.Business.Customers[0].Id == "shop-1" && v4Restored.Business.Customers[0].Slot == 0 &&
                v4Restored.Business.NextCustomerIn == ShopShift.ArrivalDelay && v4Restored.Business.BatchMeters == 2345.625 &&
                v4Restored.Business.SugarGrams == 37.5 && v4Restored.Business.RemainingSeconds == 432.125,
                "V4 single customer and active production migrate without losing values");
            Check(ref checks, v4Store.Save(v4Restored) &&
                new SaveStore(v4Dir).Load().Business.Customers[0].Id == "shop-1", "migrated V4 customer survives a V6 round trip");

            var multi = MidBatch();
            multi.OrderSerial = 3;
            multi.Business.Customers.Add(new ShopCustomer { Id = "shop-2", Slot = 1, Flavor = 1, Size = 1 });
            multi.Business.Customers.Add(new ShopCustomer { Id = "shop-3", Slot = 2, Flavor = 0, Size = 0, Angry = true,
                ReactionRemaining = .625 });
            multi.Business.NextCustomerIn = 0;
            var multiStore = new SaveStore(Path.Combine(root, "three-customers"));
            Check(ref checks, multiStore.Save(multi), "three active customers save together");
            var multiRestored = new SaveStore(multiStore.DirectoryPath).Load();
            Check(ref checks, multiRestored.Business.Customers.Count == 3 &&
                multiRestored.Business.Customers[0].Id == "shop-1" && multiRestored.Business.Customers[0].Slot == 0 &&
                multiRestored.Business.Customers[1].Id == "shop-2" && multiRestored.Business.Customers[1].Slot == 1 &&
                multiRestored.Business.Customers[2].Id == "shop-3" && multiRestored.Business.Customers[2].Slot == 2 &&
                multiRestored.Business.Customers[2].Angry && multiRestored.Business.Customers[2].ReactionRemaining == .625,
                "V6 round trip retains all customer identities, positions, and independent reaction");

            var swapState = MidBatch();
            var swapShift = new ShopShift(swapState);
            var selected = DistanceProduct("resume-id", 87.625, 2);
            swapState.Inventory.Add(selected);
            Check(ref checks, swapShift.ResumeProduct(selected.Id), "undersize candy resumes");
            var resumeStore = new SaveStore(Path.Combine(root, "resumed-candy"));
            Check(ref checks, resumeStore.Save(swapState), "mismatched sugar and resumed identity save");
            var resumedState = new SaveStore(resumeStore.DirectoryPath).Load();
            Check(ref checks, resumedState.Business.BatchProductId == selected.Id && resumedState.Business.BatchMeters == 87.625 &&
                resumedState.Business.BatchQuality == 75 && resumedState.Business.BatchFlavor == 2 && resumedState.Business.SugarFlavor == 1 &&
                resumedState.Business.SugarGrams == 37.5 && resumedState.Inventory.Count == 1,
                "resume round trip keeps identity quality distance stock and mismatched fuel");
            var afterReload = new ShopShift(resumedState); afterReload.Advance(1, 100);
            Check(ref checks, afterReload.State.BatchMeters == 87.625 && afterReload.State.SugarGrams == 37.5,
                "reloaded wrong sugar cannot grow candy");
            Check(ref checks, afterReload.Extract().Id == selected.Id, "reloaded extraction retains resumed identity");

            foreach (bool timeout in new[] { false, true })
            {
                var reactionState = new Economy(); var reactionShift = new ShopShift(reactionState);
                var customer = reactionShift.CustomerAt(0);
                if (timeout) reactionShift.Advance(90, 0);
                else
                {
                    var matching = ShopShift.Preview(ShopShift.MetersForSize(customer.Size), customer.Flavor);
                    matching.Id = "happy-stock"; reactionState.Inventory.Add(matching);
                    reactionShift.Deliver(matching.Id, customer.Id);
                }
                reactionShift.Advance(.25, 0);
                var reactionStore = new SaveStore(Path.Combine(root, timeout ? "timeout" : "happy"));
                Check(ref checks, reactionStore.Save(reactionState), "accounted reacting customer saves");
                var loadedReaction = new SaveStore(reactionStore.DirectoryPath).Load();
                var reaction = loadedReaction.Business.Customers.Find(c => c.Id == customer.Id);
                Check(ref checks, reaction != null && reaction.Happy == !timeout && reaction.TimedOut == timeout &&
                    reaction.Angry == timeout && reaction.ReactionRemaining == 1.25 && loadedReaction.Coins == reactionState.Coins &&
                    loadedReaction.MissedOrders == reactionState.MissedOrders, "reaction resumes without duplicate accounting");
                new ShopShift(loadedReaction).Advance(1.25, 0);
                Check(ref checks, loadedReaction.Business.Customers.Find(c => c.Id == customer.Id) == null &&
                    loadedReaction.TotalSold == reactionState.TotalSold && loadedReaction.MissedOrders == reactionState.MissedOrders,
                    "restored reaction departs exactly once");
            }

            string v5Dir = Path.Combine(root, "v5-pacing"); Directory.CreateDirectory(v5Dir);
            var old = MidBatch(); old.Business.NextCustomerIn = .5;
            old.Business.Customers[0].Angry = true; old.Business.Customers[0].ReactionRemaining = .625;
            old.Inventory.Add(DistanceProduct("legacy-stock", 300, 0));
            string v5Path = Path.Combine(v5Dir, "cotton-circuit.json");
            File.WriteAllText(v5Path, JsonUtility.ToJson(new SaveStore.Envelope { Version = 5, State = old, HasBusiness = true }));
            var v5Store = new SaveStore(v5Dir); var migrated = v5Store.Load();
            Check(ref checks, v5Store.CanSave && migrated.Business.NextCustomerIn == ShopShift.ArrivalDelay / 4 &&
                migrated.Business.Customers[0].Angry && migrated.Business.Customers[0].ReactionRemaining == .625 &&
                migrated.Business.Customers[0].PatienceRemaining == 90 && migrated.Coins == old.Coins &&
                migrated.Inventory[0].Id == "legacy-stock", "V5 scales pending fraction and preserves angry stock and money");
            old.Business.NextCustomerIn = 2.01;
            File.WriteAllText(v5Path, JsonUtility.ToJson(new SaveStore.Envelope { Version = 5, State = old, HasBusiness = true }));
            var invalidOld = new SaveStore(v5Dir); invalidOld.Load();
            Check(ref checks, !invalidOld.CanSave, "V5 invalid timer is rejected before migration");

            CheckLegacyRoundTrip(root, 1, ref checks);
            CheckLegacyRoundTrip(root, 2, ref checks);
            CheckLegacyRoundTrip(root, 3, ref checks);
            return checks;
        }

        // This subset also runs under Mono; disk/JsonUtility checks above run in the development player.
        static int CheckValidation()
        {
            int checks = 0;
            Check(ref checks, SaveStore.Valid(MidBatch()), "a coherent active business is accepted");
            Check(ref checks, SaveStore.Valid(new Economy()), "legacy economies without a business remain supported");
            Invalid(e => e.Business.RemainingSeconds = double.NaN, "nonfinite business time rejected", ref checks);
            Invalid(e => e.Business.RemainingSeconds = 601, "business time above the day duration rejected", ref checks);
            Invalid(e => e.Business.RemainingSeconds = -1, "negative business time rejected", ref checks);
            Invalid(e => e.Business.RemainingSeconds = 0, "zero time cannot remain open", ref checks);
            Invalid(e => e.Business.Closed = true, "positive time cannot be marked closed", ref checks);
            Invalid(e => e.Business.SugarGrams = double.PositiveInfinity, "nonfinite sugar rejected", ref checks);
            Invalid(e => e.Business.SugarGrams = -.01, "negative sugar rejected", ref checks);
            Invalid(e => e.Business.SugarGrams = 100.01, "overfilled sugar rejected", ref checks);
            Invalid(e => e.Business.SugarFlavor = -1, "loaded sugar requires a flavor", ref checks);
            var mismatched = MidBatch(); mismatched.Business.SugarFlavor = 2;
            Check(ref checks, SaveStore.Valid(mismatched), "resumed candy may wait for matching sugar");
            Invalid(e => e.Business.BatchMeters = double.NaN, "nonfinite batch distance rejected", ref checks);
            Invalid(e => e.Business.BatchMeters = -.01, "negative batch distance rejected", ref checks);
            Invalid(e => e.Business.BatchFlavor = -1, "positive batch requires a flavor", ref checks);
            Invalid(e => e.Business.BatchQuality = 101, "out of range batch quality rejected", ref checks);
            Invalid(e => e.Business.DayRevenue = -1, "negative daily revenue rejected", ref checks);
            Invalid(e => e.Business.DayRevenue = 1, "daily revenue cannot exceed lifetime revenue", ref checks);
            Invalid(e => e.Business.DaySold = 1, "daily sales cannot exceed lifetime sales", ref checks);
            Invalid(e => e.Business.DayWrong = -1, "negative wrong-delivery count rejected", ref checks);
            Invalid(e => e.Business.DayTrashed = -1, "negative disposal count rejected", ref checks);
            Invalid(e => e.Business.NextCustomerIn = ShopShift.ArrivalDelay + .01, "arrival timer above interval rejected", ref checks);
            Invalid(e => e.Business.Customers[0].Id = "shop-2", "customer ID cannot exceed issued serial", ref checks);
            Invalid(e => e.Business.Customers[0].Id = "shop-01", "customer identity must be canonical", ref checks);
            Invalid(e => e.Business.Customers[0].Slot = 3, "out of range customer slot rejected", ref checks);
            Invalid(e => { e.OrderSerial = 2; e.Business.Customers.Add(new ShopCustomer { Id = "shop-1", Slot = 1 }); },
                "duplicate customer identity rejected", ref checks);
            Invalid(e => { e.OrderSerial = 2; e.Business.Customers.Add(new ShopCustomer { Id = "shop-2", Slot = 0 }); },
                "duplicate customer slot rejected", ref checks);
            var three = MidBatch();
            three.OrderSerial = 3;
            three.Business.Customers.Add(new ShopCustomer { Id = "shop-2", Slot = 1, Flavor = 1, Size = 1 });
            three.Business.Customers.Add(new ShopCustomer { Id = "shop-3", Slot = 2, Flavor = 0, Size = 0 });
            three.Business.NextCustomerIn = 0;
            Check(ref checks, SaveStore.Valid(three), "three distinct stable slots can be saved");
            Invalid(e => e.Business.Customers[0].Flavor = 3, "invalid customer flavor rejected", ref checks);
            Invalid(e => e.Business.Customers[0].Size = 3, "invalid customer size rejected", ref checks);
            Invalid(e => e.Business.Customers[0].ReactionRemaining = .1, "calm customer cannot have an angry timer", ref checks);
            Invalid(e => e.Business.Customers[0].Angry = true, "angry customer requires a positive reaction timer", ref checks);
            Invalid(e => { e.Business.Customers[0].Angry = true; e.Business.Customers[0].ReactionRemaining = 1.51; },
                "angry reaction above its duration rejected", ref checks);
            Invalid(e => { e.Business.Customers.Clear(); e.Business.NextCustomerIn = ShopShift.ArrivalDelay + .01; },
                "waiting customer timer above the arrival delay rejected", ref checks);
            Invalid(e => { e.Business.Customers.Clear(); e.Business.NextCustomerIn = double.NaN; },
                "nonfinite arrival delay rejected", ref checks);
            Invalid(e => { e.Business.Closed = true; e.Business.RemainingSeconds = 0; },
                "closed business cannot retain a customer", ref checks);

            Invalid(e => e.Business.DayMissed = 1, "daily missed count cannot exceed lifetime count", ref checks);
            Invalid(e => e.Business.BatchProductId = "unknown", "resumed identity requires completion history", ref checks);
            Invalid(e => { var p = DistanceProduct("duplicate-active", 100, 1); e.Inventory.Add(p);
                e.Business.BatchProductId = p.Id; }, "active candy cannot also occupy shelf", ref checks);
            Invalid(e => e.Business.Customers[0].PatienceRemaining = double.NaN, "nonfinite patience rejected", ref checks);
            Invalid(e => e.Business.Customers[0].PatienceRemaining = 0, "waiting customer requires positive patience", ref checks);
            Invalid(e => e.Business.Customers[0].PatienceRemaining = 90.01, "excessive patience rejected", ref checks);
            Invalid(e => e.Business.Customers[0].TimedOut = true, "timeout requires angry reaction", ref checks);
            Invalid(e => { var c = e.Business.Customers[0]; c.Angry = c.Happy = true; c.ReactionRemaining = 1; },
                "contradictory emotions rejected", ref checks);

            Invalid(e => { var c = e.Business.Customers[0]; c.Happy = true; c.ReactionRemaining = 1; },
                "happy customer requires a recorded sale", ref checks);
            Invalid(e => { var c = e.Business.Customers[0]; c.Angry = c.TimedOut = true;
                c.PatienceRemaining = 0; c.ReactionRemaining = 1; }, "timeout requires a recorded miss", ref checks);
            var waiting = MidBatch();
            waiting.Business.Customers.Clear();
            waiting.Business.NextCustomerIn = 2;
            Check(ref checks, SaveStore.Valid(waiting), "between-customer state is accepted");
            var empty = MidBatch();
            empty.Business.SugarGrams = 0;
            empty.Business.SugarFlavor = -1;
            Check(ref checks, SaveStore.Valid(empty), "exhausted sugar may leave an unfinished flavored batch");
            empty.Business.BatchMeters = 0;
            empty.Business.BatchFlavor = -1;
            Check(ref checks, SaveStore.Valid(empty), "empty sugar and empty batch are accepted");

            var tiny = MidBatch();
            tiny.Inventory.Add(DistanceProduct("tiny", .000001, 0));
            Check(ref checks, SaveStore.Valid(tiny), "tiny extracted products survive saves for disposal");
            var large = MidBatch();
            large.Inventory.Add(DistanceProduct("large", 100000, 2));
            Check(ref checks, SaveStore.Valid(large), "large distance products use bounded visual samples");
            InvalidProduct(p => p.DistanceMeters = 0, "zero-distance finished product rejected", ref checks);
            InvalidProduct(p => p.DistanceMeters = double.PositiveInfinity, "nonfinite product distance rejected", ref checks);
            InvalidProduct(p => p.FlavorIndex = 3, "invalid explicit product flavor rejected", ref checks);
            InvalidProduct(p => p.Samples[0].Flavor = 2, "preview samples cannot contradict explicit product flavor", ref checks);
            InvalidProduct(p => p.Samples[0].Radius = double.NaN, "nonfinite preview coordinate rejected", ref checks);
            InvalidProduct(p => p.Grams = 60, "preview grams must match the serialized samples", ref checks);
            InvalidProduct(p => {
                p.Samples = LegacyProduct("preview", 362, 1).Samples;
                p.Grams = 362;
            }, "distance previews above 180 samples rejected", ref checks);
            return checks;
        }

        static void CheckLegacyRoundTrip(string root, int version, ref int checks)
        {
            string directory = Path.Combine(root, "legacy-v" + version);
            Directory.CreateDirectory(directory);
            var legacy = new Economy { Coins = 517, Day = 9, Levels = new[] { 2, 1, 3 } };
            legacy.Inventory.Add(LegacyProduct("old-small", 60, 1));
            legacy.Inventory.Add(LegacyProduct("old-large", 120, 2));
            string path = Path.Combine(directory, "cotton-circuit.json");
            File.WriteAllText(path, JsonUtility.ToJson(new SaveStore.Envelope { Version = version, State = legacy }, true));
            var store = new SaveStore(directory);
            var restored = store.Load();
            Check(ref checks, store.CanSave && restored.Coins == 517 && restored.Day == 9 &&
                restored.Levels[0] == 2 && restored.Levels[1] == 1 && restored.Levels[2] == 3 && restored.Inventory.Count == 2,
                "V" + version + " load preserves stock, coins, day, and upgrades");
            var shift = new ShopShift(restored);
            Check(ref checks, shift.State != null && restored.Inventory[0].DistanceBased &&
                restored.Inventory[0].DistanceMeters == RaceCourse.Shared.Length && ShopShift.SizeOf(restored.Inventory[0]) == 0 &&
                restored.Inventory[0].FlavorIndex == 1 && restored.Inventory[1].DistanceMeters == RaceCourse.Shared.Length * 1.5 &&
                ShopShift.SizeOf(restored.Inventory[1]) == 1 &&
                restored.Inventory[1].FlavorIndex == 2 && restored.Coins == 517 && restored.Day == 9,
                "V" + version + " stock enters the new shift at one/one-and-a-half laps as A/B without payment or loss");
            Check(ref checks, store.Save(restored), "V" + version + " migrated state can be written as V6");
            var again = new SaveStore(directory).Load();
            Check(ref checks, again.Business != null && again.Inventory.Count == 2 && again.Coins == 517 &&
                again.Inventory[0].DistanceMeters == RaceCourse.Shared.Length && ShopShift.SizeOf(again.Inventory[0]) == 0 &&
                again.Inventory[1].DistanceMeters == RaceCourse.Shared.Length * 1.5 && ShopShift.SizeOf(again.Inventory[1]) == 1 &&
                JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(path)).Version == 8,
                "V" + version + " migration remains intact after a second reload");
        }

        static Economy MidBatch()
        {
            return new Economy {
                OrderSerial = 1,
                Business = new BusinessState {
                    RemainingSeconds = 432.125, SugarGrams = 37.5, SugarFlavor = 1,
                    BatchMeters = 2345.625, BatchFlavor = 1, BatchQuality = 80,
                    Customers = new List<ShopCustomer> { new ShopCustomer { Id = "shop-1", Flavor = 2, Size = 2, Slot = 0 } },
                    NextCustomerIn = 2
                }
            };
        }

        static Product DistanceProduct(string id, double distance, int flavor)
        {
            var product = LegacyProduct(id, 4, flavor);
            product.DistanceBased = true;
            product.DistanceMeters = distance;
            product.FlavorIndex = flavor;
            return product;
        }

        static Product LegacyProduct(string id, int grams, int flavor)
        {
            var product = new Product { Id = id, Grams = grams, Quality = 75, Samples = new List<WindingSample>() };
            for (int i = 0; i < grams / 2; i++)
                product.Samples.Add(new WindingSample { Angle = (i + 1) * .3141592653589793, Radius = 10, Flavor = flavor });
            return product;
        }

        static void Invalid(Action<Economy> mutate, string message, ref int checks)
        {
            var state = MidBatch();
            mutate(state);
            Check(ref checks, !SaveStore.Valid(state), message);
        }

        static void InvalidProduct(Action<Product> mutate, string message, ref int checks)
        {
            var state = MidBatch();
            var product = DistanceProduct("bad", 1250, 1);
            state.Inventory.Add(product);
            mutate(product);
            Check(ref checks, !SaveStore.Valid(state), message);
        }

        static void Check(ref int checks, bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException("Shop shift save check failed: " + message);
            checks++;
        }
    }
}
#endif
