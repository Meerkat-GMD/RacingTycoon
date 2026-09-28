using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CottonCircuit;

public static class ProgressionSaveTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static bool Upgrade(SaveStore.Envelope envelope)
    {
        return (bool)typeof(SaveStore).GetMethod("Upgrade", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { envelope });
    }
    static Economy Fresh()
    {
        var economy = new Economy();
        economy.Progression = new ProgressionState { Phase = BusinessPhase.Preparation, CartStyle = 1 };
        return economy;
    }
    static void Buy(Economy economy, string id, int level = 1)
    {
        economy.Progression.Purchases.Add(new NodePurchase { Id = id, Level = level });
    }
    static Economy Operating()
    {
        var economy = Fresh();
        economy.Progression.Phase = BusinessPhase.Operating;
        economy.Business = new BusinessState { RemainingSeconds = 180, NextCustomerIn = 26 };
        for (int i = 0; i < 3; i++) economy.Business.Machines.Add(new MachineProduction());
        return economy;
    }
    public static int Main()
    {
        Test("V8 fresh downhill progression is accepted", () => {
            var e = Fresh();
            Check(SaveStore.Valid(e), "fresh preparation rejected");
            Check(Upgrade(new SaveStore.Envelope { Version = 8, State = e }), "V8 envelope rejected");
        });
        Test("unknown, duplicate, and excessive purchases are rejected", () => {
            var e = Fresh(); Buy(e, "missing"); Check(!SaveStore.Valid(e), "unknown node accepted");
            e.Progression.Purchases.Clear(); Buy(e, "engine", 4); Check(!SaveStore.Valid(e), "excess rank accepted");
            e.Progression.Purchases.Clear(); Buy(e, "engine"); Buy(e, "engine"); Check(!SaveStore.Valid(e), "duplicate accepted");
        });
        Test("purchased children require every parent", () => {
            var e = Fresh(); Buy(e, "location_1"); Check(!SaveStore.Valid(e), "orphan accepted");
            Buy(e, "ads"); Check(!SaveStore.Valid(e), "partial parent closure accepted");
            Buy(e, "sugar_2"); Buy(e, "machine_2");
            Check(SaveStore.Valid(e), "complete parent closure rejected");
        });
        Test("selections require owned machine, location, and cart", () => {
            var e = Fresh(); e.Progression.SelectedMachine = 1;
            Check(!SaveStore.Valid(e), "unowned machine selected");
            e.Progression.SelectedMachine = 0; e.Progression.SelectedLocation = 1;
            Check(!SaveStore.Valid(e), "locked location selected");
            e.Progression.SelectedLocation = 0; e.Progression.CartStyle = 0;
            Check(!SaveStore.Valid(e), "locked cart selected");
        });
        Test("V7 default kart migrates once without granting or removing purchases", () => {
            var e = Fresh(); e.Progression.CartStyle = 0; e.Coins = 412; e.Day = 4;
            Buy(e, "engine", 2); Buy(e, "hours");
            var envelope = new SaveStore.Envelope { Version = 7, State = e };
            Check(Upgrade(envelope), "valid V7 default rejected");
            Check(e.Progression.CartStyle == 1 && envelope.Version == 9, "default vehicle did not migrate");
            Check(e.Coins == 412 && e.Day == 4 && e.Progression.Phase == BusinessPhase.Preparation &&
                e.Progression.Purchases.Count == 2 && Progression.Level(e, "engine") == 2 &&
                Progression.Level(e, "hours") == 1 && Progression.Level(e, "coupe") == 0,
                "migration changed unrelated progress");
            Check(Upgrade(envelope) && e.Progression.CartStyle == 1 && e.Coins == 412,
                "repeated migration changed state");
        });
        Test("V7 unlocked vehicle choices are preserved", () => {
            for (int style = 0; style <= 1; style++)
            {
                var e = Fresh(); Buy(e, "engine"); Buy(e, "handling"); Buy(e, "coupe");
                e.Progression.CartStyle = style;
                Check(Upgrade(new SaveStore.Envelope { Version = 7, State = e }), "owned V7 style rejected");
                Check(e.Progression.CartStyle == style && e.Progression.Purchases.Count == 3,
                    "owned vehicle choice or purchases changed");
            }
        });
        Test("V7 corruption is checked before changing the default vehicle", () => {
            var e = Fresh();
            Check(!Upgrade(new SaveStore.Envelope { Version = 7, State = e }),
                "V7 downhill without the old unlock was accepted");
            e.Progression.CartStyle = 0; Buy(e, "coupe");
            Check(!Upgrade(new SaveStore.Envelope { Version = 7, State = e }), "orphan old unlock accepted");
            Check(e.Progression.CartStyle == 0, "corrupt state was normalized before rejection");
            e.Progression.Purchases.Clear(); e.Coins = -1;
            Check(!Upgrade(new SaveStore.Envelope { Version = 7, State = e }), "negative old balance accepted");
            Check(e.Progression.CartStyle == 0, "corrupt balance was normalized before rejection");
            e.Coins = 80; e.Progression.CartStyle = 2;
            Check(!Upgrade(new SaveStore.Envelope { Version = 7, State = e }), "invalid old style accepted");
        });
        Test("V8 soda trait owners get the price back and keep every other purchase", () => {
            var e = Fresh(); e.Coins = 500;
            Buy(e, "sugar_2"); Buy(e, "machine_2"); Buy(e, "flavor_soda"); Buy(e, "sales"); Buy(e, "flavor_price", 2);
            var envelope = new SaveStore.Envelope { Version = 8, State = e };
            Check(Upgrade(envelope) && envelope.Version == 9, "valid V8 soda owner rejected");
            Check(e.Coins == 810 && !e.Progression.Purchases.Exists(p => p.Id == "flavor_soda") &&
                e.Progression.Purchases.Count == 4 && Progression.Level(e, "flavor_price") == 2 &&
                Progression.Level(e, "machine_2") == 1, "soda refund or remaining purchases wrong");
            Check(Upgrade(envelope) && e.Coins == 810, "reloading a V9 file refunded soda again");
        });
        Test("V8 files that broke the old soda rules are rejected before any refund", () => {
            var promotion = Fresh(); Buy(promotion, "sales"); Buy(promotion, "flavor_price");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = promotion }), "V8 flavor promotion without soda accepted");
            var decoration = Fresh(); Buy(decoration, "stick_speed"); Buy(decoration, "stick_quality"); Buy(decoration, "quality_focus");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = decoration }), "V8 decoration without soda accepted");
            var noMachine = Fresh(); Buy(noMachine, "flavor_soda");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = noMachine }), "V8 soda without the second machine accepted");
            Check(noMachine.Coins == 80 && noMachine.Progression.Purchases.Count == 1, "corrupt soda file was refunded before rejection");
            var doubled = Fresh(); Buy(doubled, "sugar_2"); Buy(doubled, "machine_2"); Buy(doubled, "flavor_soda", 2);
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = doubled }), "V8 soda above its single rank accepted");
        });
        Test("the soda refund lands only after the old balance passes validation", () => {
            var e = Fresh(); e.Coins = -300;
            Buy(e, "sugar_2"); Buy(e, "machine_2"); Buy(e, "flavor_soda");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = e }), "negative V8 balance accepted through the soda refund");
            var v7 = Fresh(); v7.Coins = -300; v7.Progression.CartStyle = 0;
            Buy(v7, "engine"); Buy(v7, "handling"); Buy(v7, "coupe"); Buy(v7, "sugar_2"); Buy(v7, "machine_2"); Buy(v7, "flavor_soda");
            Check(!Upgrade(new SaveStore.Envelope { Version = 7, State = v7 }), "negative V7 balance accepted through the soda refund");
            var rich = Fresh(); rich.Coins = 999999900;
            Buy(rich, "sugar_2"); Buy(rich, "machine_2"); Buy(rich, "flavor_soda");
            Check(Upgrade(new SaveStore.Envelope { Version = 8, State = rich }) && rich.Coins == 1000000000,
                "a refund near the coin cap must clamp instead of breaking the save");
        });
        Test("V8 files that used soda against the old rules are rejected", () => {
            var recipe = Operating(); recipe.Business.Machines[0].RecipeFlavor = 1;
            Check(SaveStore.Valid(recipe), "starting soda recipe must be valid under V9 rules");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = recipe }), "V8 soda recipe without the trait accepted");
            var owner = Operating(); Buy(owner, "sugar_2"); Buy(owner, "machine_2"); Buy(owner, "flavor_soda");
            owner.Business.Machines[0].RecipeFlavor = 1;
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = owner }), "V8 soda on the basic machine accepted");
            var stock = Fresh();
            var soda = ShopShift.Preview(ShopShift.MetersForSize(0), 1);
            soda.Id = "old-soda"; soda.SugarGrade = 1;
            stock.Inventory.Add(soda); stock.CompletedIds.Add(soda.Id);
            Check(SaveStore.Valid(stock), "starting soda stock must be valid under V9 rules");
            Check(!Upgrade(new SaveStore.Envelope { Version = 8, State = stock }), "V8 soda stock without the trait accepted");
        });
        Test("V9 files cannot hold the removed soda trait", () => {
            var e = Fresh(); Buy(e, "sugar_2"); Buy(e, "machine_2"); Buy(e, "flavor_soda");
            Check(!Upgrade(new SaveStore.Envelope { Version = 9, State = e }), "V9 soda trait accepted");
        });
        Test("invalid phase is rejected", () => {
            var e = Fresh(); e.Progression.Phase = (BusinessPhase)99;
            Check(!SaveStore.Valid(e), "invalid enum accepted");
        });
        Test("operating state validates material cost and every machine", () => {
            var e = Operating(); Check(SaveStore.Valid(e), "fresh shift rejected");
            e.Business.DayMaterialCost = -1; Check(!SaveStore.Valid(e), "negative cost accepted");
            e.Business.DayMaterialCost = 0;
            e.Business.Machines[2].SugarGrade = 4; Check(!SaveStore.Valid(e), "invalid third machine grade accepted");
            e.Business.Machines[2].SugarGrade = 1;
            e.Business.Machines[1].WorkerAssigned = true; Check(!SaveStore.Valid(e), "unowned worker accepted");
        });
        Test("phase and business state agree", () => {
            var e = Operating(); e.Progression.Phase = BusinessPhase.Results;
            Check(!SaveStore.Valid(e), "open business accepted as results");
            e.Progression.Phase = BusinessPhase.Preparation; e.Business.NextCustomerIn = 0;
            Check(SaveStore.Valid(e), "fresh preparation rejected");
            e.Business.Closed = true;
            Check(SaveStore.Valid(e), "preparation after a prior day rejected");
            e.Business.RemainingSeconds = 0;
            Check(!SaveStore.Valid(e), "spent day accepted as preparation");
        });
        Test("V6 stays legacy and does not synthesize progression", () => {
            var e = new Economy();
            Check(Upgrade(new SaveStore.Envelope { Version = 6, State = e }), "legacy save rejected");
            Check(e.Progression == null, "migration enabled progression early");
            Check(SaveStore.Valid(e), "legacy economy rejected");
        });
        Test("V6 product without sugar grade gains basic grade on migration", () => {
            var e = new Economy();
            var product = ShopShift.Preview(ShopShift.MetersForSize(0), 0);
            product.Id = "legacy-candy"; product.SugarGrade = 0;
            e.Inventory.Add(product); e.CompletedIds.Add(product.Id);
            Check(Upgrade(new SaveStore.Envelope { Version = 6, State = e }), "old product rejected");
            Check(product.SugarGrade == 1, "old product grade was not normalized");
        });
        Test("real shift saves in preparation, operating, and results", () => {
            var e = Fresh(); var shift = new ShopShift(e);
            Check(SaveStore.Valid(e), "first preparation rejected");
            Check(shift.BeginBusiness() && SaveStore.Valid(e), "operating rejected");
            shift.Advance(180, 0);
            Check(e.Progression.Phase == BusinessPhase.Results && SaveStore.Valid(e), "results rejected");
            Check(shift.ReturnToPreparation() && SaveStore.Valid(e), "next preparation rejected");
        });
        Test("overflow winding saves only with a batch and matching active copy", () => {
            var e = Operating(); var m = e.Business.Machines[0];
            m.BatchMeters = ShopShift.LapMeters; m.BatchFlavor = 0; m.BatchOverflowMeters = 30;
            e.Business.BatchMeters = ShopShift.LapMeters; e.Business.BatchFlavor = 0; e.Business.BatchOverflowMeters = 30;
            Check(SaveStore.Valid(e), "capped batch with overflow rejected");
            e.Business.BatchOverflowMeters = 31; Check(!SaveStore.Valid(e), "active overflow mismatch accepted");
            e.Business.BatchOverflowMeters = 30; m.BatchOverflowMeters = e.Business.BatchOverflowMeters = -1;
            Check(!SaveStore.Valid(e), "negative overflow accepted");
            m.BatchOverflowMeters = e.Business.BatchOverflowMeters = double.NaN;
            Check(!SaveStore.Valid(e), "non-finite overflow accepted");
            m.BatchMeters = e.Business.BatchMeters = 0; m.BatchFlavor = e.Business.BatchFlavor = -1;
            m.BatchOverflowMeters = e.Business.BatchOverflowMeters = 5;
            Check(!SaveStore.Valid(e), "overflow without a batch accepted");
        });
        Test("active batch grade cannot exceed selected machine grade", () => {
            var e = Operating(); Buy(e, "sugar_2");
            var m = e.Business.Machines[0]; m.BatchMeters = 1; m.BatchFlavor = 0; m.BatchSugarGrade = 2;
            e.Business.BatchMeters = 1; e.Business.BatchFlavor = 0; e.Business.BatchSugarGrade = 2;
            Check(!SaveStore.Valid(e), "higher-grade batch accepted on grade-one machine");
        });
        Test("stock must use an unlocked flavor and an achievable size", () => {
            var e = Fresh();
            var product = ShopShift.Preview(ShopShift.MetersForSize(1), 0);
            product.Id = "oversize"; product.SugarGrade = 1;
            e.Inventory.Add(product); e.CompletedIds.Add(product.Id);
            Check(!SaveStore.Valid(e), "grade-one stock at medium size accepted");
            e.Inventory.Clear(); e.CompletedIds.Clear();
            var soda = ShopShift.Preview(ShopShift.MetersForSize(0), 1);
            soda.Id = "starting-soda"; soda.SugarGrade = 1;
            e.Inventory.Add(soda); e.CompletedIds.Add(soda.Id);
            Check(SaveStore.Valid(e), "starting soda stock rejected");
            e.Inventory.Clear(); e.CompletedIds.Clear();
            var vanilla = ShopShift.Preview(ShopShift.MetersForSize(0), 2);
            vanilla.Id = "locked-flavor"; vanilla.SugarGrade = 1;
            e.Inventory.Add(vanilla); e.CompletedIds.Add(vanilla.Id);
            Check(!SaveStore.Valid(e), "locked vanilla stock accepted");
        });
        Test("paid premium production survives an actual save and load", () => {
            string dir = Path.Combine(Path.GetTempPath(), "cc-save-" + Guid.NewGuid().ToString("N"));
            try
            {
                var e = Fresh(); e.Coins = 1000;
                Buy(e, "sugar_2"); Buy(e, "machine_2");
                var shift = new ShopShift(e);
                shift.Machine(1).SugarGrade = 2;
                Check(shift.SelectMachine(1) && shift.BeginBusiness(), "premium setup failed");
                Check(shift.Pour(1), "premium pour rejected");
                shift.Advance(1, 10);
                shift.SyncActive();
                Check(e.Coins == 997 && e.Business.DayMaterialCost == 3, "material charge incorrect");
                Check(new SaveStore(dir).Save(e), "paid shift failed to save");
                var restored = new SaveStore(dir).Load();
                Check(restored.Coins == 997 && restored.Business.DayMaterialCost == 3 &&
                    restored.Business.Machines[1].SugarGrade == 2 &&
                    restored.Business.Machines[1].BatchMeters > 0 &&
                    restored.Progression.SelectedMachine == 1, "premium state lost on load");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Test("V7 operating file resumes paid batches, stock, and remaining time on downhill with the soda refund", () => {
            string dir = Path.Combine(Path.GetTempPath(), "cc-v7-migration-" + Guid.NewGuid().ToString("N"));
            try
            {
                var e = Fresh(); e.Coins = 1000; e.Day = 6;
                Buy(e, "sugar_2"); Buy(e, "machine_2");
                var shift = new ShopShift(e);
                shift.Machine(1).SugarGrade = 2;
                Check(shift.SelectMachine(1) && shift.BeginBusiness() && shift.Pour(1), "old paid setup failed");
                shift.Advance(6, 10); shift.SyncActive();
                var product = ShopShift.Preview(10, 0);
                product.Id = "v7-stock"; product.SugarGrade = 1;
                e.Inventory.Add(product); e.CompletedIds.Add(product.Id);
                // The old file owned the soda trait; migration removes it and refunds its 310 coin price.
                e.Progression.CartStyle = 1; e.Coins += 310;
                string expectedAfterMigration = UnityEngine.JsonUtility.ToJson(e);
                e.Progression.CartStyle = 0; e.Coins -= 310; Buy(e, "flavor_soda");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), UnityEngine.JsonUtility.ToJson(
                    new SaveStore.Envelope { Version = 7, State = e, HasProgression = true, HasBusiness = true }));
                var store = new SaveStore(dir);
                var restored = store.Load();
                Check(store.CanSave && store.Error == null, "valid V7 operating file rejected");
                Check(restored.Progression.CartStyle == 1 && restored.Progression.Phase == BusinessPhase.Operating &&
                    restored.Business.RemainingSeconds == 174 && restored.Coins == 1307 &&
                    restored.Business.DayMaterialCost == 3 && restored.Inventory.Count == 1 &&
                    restored.Inventory[0].Id == "v7-stock", "operating progress reset during migration");
                Check(UnityEngine.JsonUtility.ToJson(restored) == expectedAfterMigration,
                    "migration modified state beyond the vehicle style and the soda refund");
                Check(store.Save(restored), "migrated state failed to save as V9");
                var stored = UnityEngine.JsonUtility.FromJson<SaveStore.Envelope>(
                    File.ReadAllText(Path.Combine(dir, "cotton-circuit.json")));
                Check(stored.Version == 9, "migrated file was not upgraded to V9");
                Check(UnityEngine.JsonUtility.ToJson(new SaveStore(dir).Load()) == expectedAfterMigration,
                    "V9 second load changed resumed state");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Test("V8 default downhill and unlocked kart both roundtrip through files", () => {
            string dir = Path.Combine(Path.GetTempPath(), "cc-v8-vehicles-" + Guid.NewGuid().ToString("N"));
            try
            {
                var e = Fresh(); var store = new SaveStore(dir);
                Check(store.Save(e), "free downhill failed to save");
                Check(store.Load().Progression.CartStyle == 1, "default downhill lost on load");
                Buy(e, "engine"); Buy(e, "handling"); Buy(e, "coupe"); e.Progression.CartStyle = 0;
                Check(store.Save(e), "unlocked kart failed to save");
                Check(store.Load().Progression.CartStyle == 0, "explicit kart lost on load");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Test("absent progression accepts empty Unity placeholders but rejects hidden payloads", () => {
            string dir = Path.Combine(Path.GetTempPath(), "cc-presence-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                for (int style = 0; style <= 1; style++)
                {
                    var e = Fresh(); e.Progression.CartStyle = style;
                    File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), UnityEngine.JsonUtility.ToJson(
                        new SaveStore.Envelope { Version = 8, State = e, HasProgression = false }));
                    var store = new SaveStore(dir);
                    Check(store.Load().Progression == null && store.CanSave, "empty Unity placeholder rejected");
                }
                var corrupt = Fresh(); Buy(corrupt, "engine");
                string payload = UnityEngine.JsonUtility.ToJson(
                    new SaveStore.Envelope { Version = 8, State = corrupt, HasProgression = false });
                File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), payload);
                var blocked = new SaveStore(dir); blocked.Load();
                Check(!blocked.CanSave && File.ReadAllText(Path.Combine(dir, "cotton-circuit.json")) == payload,
                    "meaningful hidden progression was discarded or overwritten");
                corrupt.Progression.Purchases.Clear(); corrupt.Progression.CartStyle = 2;
                File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), UnityEngine.JsonUtility.ToJson(
                    new SaveStore.Envelope { Version = 8, State = corrupt, HasProgression = false }));
                blocked = new SaveStore(dir); blocked.Load();
                Check(!blocked.CanSave, "corrupt placeholder vehicle enum accepted");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Test("selected worker ownership and automatic production survive a V8 file reload", () => {
            string dir = Path.Combine(Path.GetTempPath(), "cc-selected-worker-" + Guid.NewGuid().ToString("N"));
            try
            {
                var e = Fresh(); e.Coins = 1000;
                Buy(e, "sugar_2"); Buy(e, "machine_2"); Buy(e, "worker_1");
                var shift = new ShopShift(e); shift.Machine(0).WorkerAssigned = true;
                Check(shift.BeginBusiness(), "worker business setup failed");
                shift.Advance(25, 0); double partial = shift.State.BatchMeters;
                Check(partial > 0 && e.Inventory.Count == 0, "selected worker did not begin the saved batch");
                var store = new SaveStore(dir); Check(store.Save(e), "selected worker state was not saveable");
                var restored = store.Load(); var resumed = new ShopShift(restored);
                Check(store.CanSave && resumed.SelectedMachine == 0 && resumed.HasWorker(0) && resumed.WorkerCanOperate(0) &&
                    resumed.State.BatchMeters == partial, "worker assignment or batch changed on reload");
                resumed.Advance(40, 0);
                Check(restored.Inventory.Count == 1 && restored.Business.RemainingSeconds == 115 && restored.Coins == 1000,
                    "resumed selected worker lost automatic time production or changed basic material economics");
                Check(store.Save(restored), "worker automatic extraction left mismatched active save state");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Console.WriteLine("Progression save checks: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}

// Mono-only boundary stub: these tests exercise SaveStore's JSON file path without
// requiring the native Unity player. Unity Editor integration checks use real JsonUtility.
namespace UnityEngine
{
    public static class JsonUtility
    {
        static System.Web.Script.Serialization.JavaScriptSerializer Serializer()
        {
            return new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
        }
        public static string ToJson(object value, bool prettyPrint = false) { return Serializer().Serialize(value); }
        public static T FromJson<T>(string json) { return Serializer().Deserialize<T>(json); }
    }
}
