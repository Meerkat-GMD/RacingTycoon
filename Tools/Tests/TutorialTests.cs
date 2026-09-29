using System;
using System.Collections.Generic;
using System.IO;
using CottonCircuit;

class TutorialTests
{
    static int passed, failed;
    static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    static Economy Fresh() { var e = new Economy(); Progression.Enable(e); return e; }
    static ShopShift Training(out Economy e)
    {
        e = Fresh(); var shift = new ShopShift(e);
        Check(shift.BeginTutorial(), "new-game tutorial could not begin");
        return shift;
    }
    static void Fill(ShopShift shift) { for (int i = 0; i < 5; i++) Check(shift.Pour(0), "practice sugar rejected"); }
    static Product Make(ShopShift shift)
    { Fill(shift); shift.Advance(1, ShopShift.LapMeters); return shift.Extract(); }
    static Economy Reload(Economy e, ShopShift shift)
    {
        shift.SyncActive();
        string directory = Path.Combine(Path.GetTempPath(), "cc-tutorial-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore(directory);
            Check(store.Save(e), "tutorial state failed validation/save: " + store.Error);
            var loaded = new SaveStore(directory);
            var result = loaded.Load(); Check(loaded.CanSave, "tutorial save could not reload"); return result;
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    static bool FinishSale(Economy e, ShopShift shift)
    {
        var product = Make(shift); Check(product != null, "full practice candy was not extracted");
        Check(shift.Deliver(product.Id, shift.CustomerAt(0).Id) == DeliveryResult.Sold, "practice sale failed");
        return shift.CompleteTutorial();
    }
    static int Main()
    {
        Test("new games explicitly opt into a protected first customer", () => {
            var e = Fresh(); var shift = new ShopShift(e);
            Check(shift.BeginTutorial(), "new-game tutorial could not begin");
            Check(shift.IsOpen && shift.State.Customers.Count == 1, "tutorial must open with one customer");
            Check(shift.CustomerAt(0).Flavor == 0 && shift.CustomerAt(0).Size == 0, "first order must be strawberry standard");
            double timer = shift.State.RemainingSeconds, patience = shift.CustomerAt(0).PatienceRemaining;
            shift.Advance(300, 0);
            Check(shift.State.RemainingSeconds == timer && shift.CustomerAt(0).PatienceRemaining == patience,
                "tutorial clocks advanced while the player learned");
            Check(shift.State.Customers.Count == 1 && shift.State.DayMissed == 0, "extra arrivals or missed customers during tutorial");
            Check(SaveStore.Valid(e), "started tutorial not saveable");
        });
        Test("ordinary and existing games do not silently enable tutorials", () => {
            var e = Fresh(); var shift = new ShopShift(e);
            Check(!Tutorial.Active(e) && e.TutorialStep == TutorialStep.Disabled, "tutorial enabled implicitly");
            Check(shift.BeginBusiness(), "normal business could not begin"); shift.Advance(1, 0);
            Check(shift.State.RemainingSeconds == 179, "normal clock froze");
            Check(!shift.BeginTutorial(), "running business was replaced by tutorial");
            e.Day = 2; Check(!shift.BeginTutorial(), "older game was replaced by tutorial");
        });
        Test("one candy worth of sugar is needed before driving", () => {
            Economy e; var shift = Training(out e);
            Check(Tutorial.RequiredSugar(e) == 50 && Tutorial.SugarProgress(e) == 0, "incorrect initial sugar target");
            shift.Pour(0);
            Check(e.TutorialStep == TutorialStep.PourSugar && Math.Abs(Tutorial.SugarProgress(e) - .2) < .0001,
                "one small pour skipped the rest of shaking");
            shift.Advance(10, ShopShift.LapMeters);
            Check(shift.State.BatchMeters == 0 && shift.State.SugarGrams == 10, "production moved before sugar step finished");
            for (int i = 0; i < 4; i++) shift.Pour(0);
            Check(e.TutorialStep == TutorialStep.Drive && Tutorial.SugarProgress(e) == 1, "full sugar did not enable driving");
        });
        Test("early extraction preserves the growing candy and clocks", () => {
            Economy e; var shift = Training(out e); Fill(shift);
            shift.Advance(300, ShopShift.LapMeters / 2);
            double progress = shift.State.BatchMeters;
            Check(progress > 0 && e.TutorialStep == TutorialStep.Drive, "protected production did not advance");
            Check(shift.Extract() == null && shift.State.BatchMeters == progress && e.Inventory.Count == 0,
                "early extract lost practice candy");
            Check(shift.State.RemainingSeconds == 180 && shift.CustomerAt(0).PatienceRemaining == 60,
                "production advanced nonproduction clocks");
            shift.Advance(1, ShopShift.LapMeters / 2);
            Check(e.TutorialStep == TutorialStep.Extract && ShopShift.SizeForDistance(shift.State.BatchMeters) == 0,
                "full candy did not enable extraction");
            var candy = shift.Extract();
            Check(candy != null && e.TutorialStep == TutorialStep.Deliver && e.Inventory.Count == 1,
                "extract did not enable sale");
            Check(shift.Extract() == null, "practice candy extracted twice");
        });
        Test("other actions cannot consume or replace the tutorial ingredients", () => {
            Economy e; var shift = Training(out e); shift.Pour(0);
            Check(!shift.Pour(1) && !shift.EmptySugar() && shift.State.SugarGrams == 10, "practice sugar could be replaced or emptied");
            Check(!shift.SelectMachine(0), "machine switching was enabled during tutorial");
            for (int i = 0; i < 4; i++) shift.Pour(0);
            shift.Advance(1, ShopShift.LapMeters); var candy = shift.Extract();
            Check(!shift.Discard(candy.Id) && !e.Discard(candy.Id) && !shift.ResumeProduct(candy.Id),
                "practice candy could be discarded or resumed");
            Check(e.Inventory.Count == 1 && !shift.Pour(0), "delivery step allowed another batch");
        });
        Test("soda stays locked until the guided sale ends", () => {
            Economy e; var shift = Training(out e);
            Check(!shift.CanMakeFlavor(0, 1) && shift.MachineMakesFlavor(0, 1) && !shift.MachineMakesFlavor(0, 2),
                "the tutorial alone must lock soda, while the machine still cannot make vanilla");
            Check(FinishSale(e, shift) && shift.CanMakeFlavor(0, 1), "soda did not open after the guided sale");
            var skipped = Training(out e);
            Check(skipped.SkipTutorial() && skipped.CanMakeFlavor(0, 1) && skipped.Pour(1) && e.Business.DayMaterialCost == 2,
                "skipping the tutorial did not open paid soda sugar");
        });
        Test("the first ordinary day orders strawberry and soda but no vanilla", () => {
            var e = Fresh(); var shift = new ShopShift(e);
            Check(shift.BeginBusiness(), "ordinary first day could not open");
            var flavors = new HashSet<int>();
            for (int i = 0; i < 40 && shift.IsOpen; i++)
            {
                foreach (var customer in shift.State.Customers) flavors.Add(customer.Flavor);
                shift.Advance(5, 0);
            }
            Check(flavors.Contains(0) && flavors.Contains(1) && !flavors.Contains(2),
                "first-day orders used flavors " + string.Join(",", flavors));
        });
        Test("a wrong practice delivery leaves candy and customer intact", () => {
            Economy e; var shift = Training(out e); var candy = Make(shift);
            var customer = shift.CustomerAt(0); customer.Flavor = 1;
            Check(shift.Deliver(candy.Id, customer.Id) == DeliveryResult.Rejected && e.Inventory.Count == 1,
                "wrong practice delivery consumed candy");
            Check(!customer.Angry && shift.State.DayWrong == 0, "wrong practice delivery punished player");
            customer.Flavor = 0;
            Check(shift.Deliver(candy.Id, "missing-customer") == DeliveryResult.Rejected && e.Inventory.Count == 1,
                "missing recipient consumed candy");
            Check(shift.Deliver(candy.Id, customer.Id) == DeliveryResult.Sold && e.TutorialStep == TutorialStep.Success,
                "correct practice sale did not complete exercise");
            int coins = e.Coins;
            Check(shift.Deliver(candy.Id, customer.Id) == DeliveryResult.Rejected && e.Coins == coins, "practice sale paid twice");
            shift.Advance(20, 0); Check(shift.State.RemainingSeconds == 180, "success message did not hold timer");
            Check(shift.CompleteTutorial() && !shift.CompleteTutorial(), "completion was not one-shot");
            shift.Advance(1, 0);
            Check(!Tutorial.Active(e) && e.Coins == coins && shift.State.RemainingSeconds == 179,
                "completion did not preserve reward and start ordinary clock");
        });
        Test("tutorial saves resume every stage without replaying or resetting", () => {
            Economy e; var shift = Training(out e);
            for (int stage = 1; stage <= 6; stage++)
            {
                e = Reload(e, shift); shift = new ShopShift(e);
                Check((int)e.TutorialStep == stage, "stage " + stage + " was lost on reload");
                Check(!shift.BeginTutorial(), "reload allowed restarting tutorial");
                if (stage == 1) Fill(shift);
                else if (stage == 2) shift.Advance(1, ShopShift.LapMeters);
                else if (stage == 3) shift.Extract();
                else if (stage == 4) shift.Deliver(e.Inventory[0].Id, shift.CustomerAt(0).Id);
                else if (stage == 5) shift.CompleteTutorial();
            }
        });
        Test("skipping any unfinished stage opens a clean ordinary first day", () => {
            for (int stage = 1; stage <= 4; stage++)
            {
                Economy e; var shift = Training(out e);
                if (stage >= 2) Fill(shift);
                if (stage >= 3) shift.Advance(1, ShopShift.LapMeters);
                if (stage >= 4) shift.Extract();
                Check(shift.SkipTutorial() && !shift.SkipTutorial(), "skip must work exactly once at stage " + stage);
                Check(e.TutorialStep == TutorialStep.Complete && e.Day == 1 && e.Coins == 80 && e.Inventory.Count == 0,
                    "skipping changed starting economy or kept practice stock");
                Check(shift.IsOpen && shift.State.RemainingSeconds == 180 && shift.State.SugarGrams == 0 && shift.State.BatchMeters == 0 &&
                    shift.State.DaySold == 0 && shift.CustomerAt(0) != null, "skip did not produce a usable fresh shift");
                shift.SyncActive(); Check(SaveStore.Valid(e), "skip made an invalid save");
                shift.Advance(1, 0); Check(shift.State.RemainingSeconds == 179, "skipped tutorial still froze clock");
            }
        });
        Test("skipping the success card keeps the earned sale", () => {
            Economy e; var shift = Training(out e); var candy = Make(shift);
            shift.Deliver(candy.Id, shift.CustomerAt(0).Id); int coins = e.Coins;
            Check(coins > 80 && shift.SkipTutorial(), "success card skip failed");
            Check(e.Coins == coins && e.TotalSold == 1 && shift.State.DaySold == 1, "success skip erased earned sale");
            shift.SyncActive(); Check(SaveStore.Valid(e), "success skip made invalid save");
        });
        Test("growth tip is shown once after the first results are closed", () => {
            Economy e; var shift = Training(out e); Check(FinishSale(e, shift), "tutorial did not finish");
            Check(!Tutorial.TryConsumeGrowthHint(e), "growth hint appeared before first settlement");
            shift.Advance(180, 0);
            Check(e.Progression.Phase == BusinessPhase.Results && !Tutorial.TryConsumeGrowthHint(e),
                "growth hint appeared over first results");
            Check(shift.ReturnToPreparation() && Tutorial.TryConsumeGrowthHint(e), "growth hint missing after first results closed");
            Check(!Tutorial.TryConsumeGrowthHint(e), "growth hint repeated");
            e = Reload(e, shift); shift = new ShopShift(e);
            Check(e.GrowthHintShown && !Tutorial.TryConsumeGrowthHint(e), "growth hint replayed after reload");
            shift.BeginBusiness(); shift.Advance(180, 0); shift.ReturnToPreparation();
            Check(e.Day == 2 && !Tutorial.TryConsumeGrowthHint(e), "growth hint repeated on another day");
        });
        Test("skipping still allows one growth hint but old games do not", () => {
            Economy e; var shift = Training(out e); shift.SkipTutorial(); shift.Advance(180, 0); shift.ReturnToPreparation();
            Check(Tutorial.TryConsumeGrowthHint(e), "skipped tutorial lost first-day hint");
            e = Fresh(); shift = new ShopShift(e); shift.BeginBusiness(); shift.Advance(180, 0); shift.ReturnToPreparation();
            Check(!Tutorial.TryConsumeGrowthHint(e), "old or direct game was forced into new onboarding");
        });
        Test("V8 files without tutorial fields remain ordinary games", () => {
            string directory = Path.Combine(Path.GetTempPath(), "cc-pre-tutorial-" + Guid.NewGuid().ToString("N"));
            try
            {
                var e = Fresh(); var store = new SaveStore(directory); Check(store.Save(e), "legacy fixture save failed");
                string path = Path.Combine(directory, "cotton-circuit.json");
                string json = File.ReadAllText(path).Replace("\"TutorialStep\":0,", "").Replace("\"GrowthHintShown\":false,", "");
                File.WriteAllText(path, json); e = store.Load();
                Check(store.CanSave && e.TutorialStep == TutorialStep.Disabled && !e.GrowthHintShown, "old V8 changed onboarding state");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        });
        Test("invalid tutorial stages and impossible active phases cannot be saved", () => {
            var e = Fresh(); e.TutorialStep = (TutorialStep)99;
            Check(!SaveStore.Valid(e), "unknown tutorial stage accepted");
            e.TutorialStep = TutorialStep.Drive; Check(!SaveStore.Valid(e), "tutorial active outside business accepted");
            e.TutorialStep = TutorialStep.Disabled; e.GrowthHintShown = true;
            Check(!SaveStore.Valid(e), "growth hint was consumed without new-game onboarding");
        });
        Console.WriteLine("Tutorial checks: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}

// Mono boundary for exercising the real SaveStore file path outside the Unity player.
namespace UnityEngine
{
    public static class JsonUtility
    {
        static System.Web.Script.Serialization.JavaScriptSerializer Serializer()
        { return new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }; }
        public static string ToJson(object value, bool prettyPrint = false) { return Serializer().Serialize(value); }
        public static T FromJson<T>(string json) { return Serializer().Deserialize<T>(json); }
    }
}
