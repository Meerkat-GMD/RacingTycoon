using System;
using CottonCircuit;
class ProgressionShiftTests
{
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static Economy Fresh() { var e = new Economy(); Progression.Enable(e); return e; }
    static void All(Economy e) { foreach (var n in Progression.Nodes) e.Progression.Purchases.Add(new NodePurchase { Id = n.Id, Level = n.MaxLevel }); }
    static int Main()
    {
        try {
            var proportionalEconomy = Fresh(); var proportionalShift = new ShopShift(proportionalEconomy);
            proportionalShift.BeginBusiness(); int startingCoins = proportionalEconomy.Coins;
            Check(proportionalShift.Pour(0, 1) && Math.Abs(proportionalShift.State.SugarGrams - 1) < 1e-9,
                "gentle basic pour inserts only one gram");
            Check(proportionalShift.Pour(0, 4) && Math.Abs(proportionalShift.State.SugarGrams - 5) < 1e-9 && proportionalEconomy.Coins == startingCoins,
                "stronger basic pour inserts its measured amount for free");
            Check(!proportionalShift.Pour(0, 0) && !proportionalShift.Pour(0, -1) && !proportionalShift.Pour(0, double.NaN) && !proportionalShift.Pour(0, double.PositiveInfinity),
                "invalid pour quantities cannot create sugar");
            proportionalShift.EmptySugar();
            for (int i=0;i<9;i++) proportionalShift.Pour(0);
            proportionalShift.Pour(0, 9);
            Check(proportionalShift.Pour(0, 4) && proportionalShift.State.SugarGrams == 100,
                "variable pour accepts only the remaining tank capacity");
            proportionalEconomy = Fresh(); All(proportionalEconomy); proportionalEconomy.Coins = 100;
            proportionalShift = new ShopShift(proportionalEconomy); proportionalShift.SelectMachine(1);
            proportionalShift.Machine(1).SugarGrade = 2; proportionalShift.BeginBusiness();
            int measuredCost = proportionalShift.PourCost(1, 1, 1);
            Check(proportionalShift.Pour(1, 1) && proportionalShift.State.SugarGrams == 1 &&
                proportionalEconomy.Coins == 100 - measuredCost && proportionalShift.State.DayMaterialCost == measuredCost,
                "premium material charges only the accepted measured grams");
            proportionalShift.EmptySugar(); proportionalEconomy.Coins = 0;
            Check(!proportionalShift.Pour(1, 4) && proportionalShift.State.SugarGrams == 0,
                "unaffordable measured pour adds no material");
            var e = Fresh(); var s = new ShopShift(e);
            s.Advance(30, 100); Check(!s.IsOpen && s.State.Customers.Count == 0, "preparation does not tick or spawn");
            Check(s.BeginBusiness() && s.State.RemainingSeconds == 180, "explicit first 3 minute opening");
            Check(!s.BeginBusiness(), "cannot reopen active shift");
            Check(s.State.Customers[0].Flavor == 0 && s.State.Customers[0].Size == 0, "initial order strawberry standard");
            int coins = e.Coins; Check(s.Pour(0) && e.Coins == coins, "basic strawberry pour is free");
            Check(!s.Pour(1), "locked flavor rejected");
            for (int i=0;i<10;i++) s.Pour(0);
            s.Advance(1, ShopShift.LapMeters * 3);
            Check(Math.Abs(s.State.BatchMeters - ShopShift.MetersForSize(0)) < .001, "grade one caps at standard");
            double sugar = s.State.SugarGrams; s.Advance(1, 100);
            Check(s.State.SugarGrams == sugar, "capped candy does not consume more sugar");
            var p = s.Extract(); Check(p != null && ShopShift.SizeOf(p) == 0, "extract standard candy");
            var customer = s.State.Customers[0]; Check(s.Deliver(p.Id, customer.Id) == DeliveryResult.Sold, "initial sale succeeds");
            int earned = e.Coins; Check(s.Deliver(p.Id, customer.Id) == DeliveryResult.Rejected && e.Coins == earned, "sale pays once");
            s.Advance(200,0); Check(e.Progression.Phase == BusinessPhase.Results && !s.IsOpen, "closing enters results");
            Check(s.ReturnToPreparation() && !s.IsOpen, "results return without starting clock");
            Check(s.BeginBusiness() && e.Day == 2 && e.Inventory.Count == 0 && s.State.BatchMeters == 0, "new day clears transient stock");

            e = Fresh(); All(e); e.Coins = 10000; s = new ShopShift(e);
            s.Machine(1).SugarGrade = 2; s.Machine(1).RecipeFlavor = 1; s.Machine(1).RecipeSize = 1;
            Check(s.SelectMachine(1) && s.BeginBusiness(), "second machine can be selected");
            coins = e.Coins; Check(s.Pour(1) && e.Coins < coins && s.State.DayMaterialCost > 0, "premium sugar charges wallet");
            s.Advance(1, 5); double progress = s.State.BatchMeters;
            Check(s.SelectMachine(0) && s.State.BatchMeters == 0, "switch loads separate batch");
            Check(s.SelectMachine(1) && Math.Abs(s.State.BatchMeters-progress)<.001, "switch restores previous production");
            s.EmptySugar(); e.Coins = 0; Check(!s.Pour(1) && s.State.SugarGrams == 0, "insufficient funds cannot create sugar");
            Check(s.SelectMachine(0) && s.Pour(0), "free recipe remains available without coins");
            s.Machine(1).WorkerAssigned=true; s.Advance(10,0);
            Check(Math.Abs(s.Machine(1).BatchMeters-progress)<.001, "unfunded worker pauses");
            e.Coins=10000; s.Advance(180,0);
            Check(e.Inventory.Count > 0 && e.Inventory.Count <= e.StockCapacity, "worker repeats selected recipe to shelf");
            while(e.Inventory.Count < e.StockCapacity) e.Inventory.Add(ShopShift.Preview(ShopShift.LapMeters,0));
            coins=e.Coins; s.Advance(2,0); Check(e.Coins==coins, "full shelf stops worker material spending");
            e=Fresh(); All(e); e.Coins=10000; s=new ShopShift(e);
            s.Machine(1).WorkerAssigned=true; s.Machine(1).SugarGrade=2;
            s.Machine(1).RecipeFlavor=1; s.Machine(1).RecipeSize=1;
            s.SelectMachine(1); s.BeginBusiness(); s.Pour(0); s.Advance(1,1); s.SelectMachine(0);
            s.Advance(180,0);
            Check(e.Inventory.Exists(product=>product.FlavorIndex==0) && e.Inventory.Exists(product=>product.FlavorIndex==1),
                "worker finishes inherited flavor then resumes its assigned recipe");
            e=Fresh(); All(e); e.Coins=10000; s=new ShopShift(e);
            s.Machine(1).WorkerAssigned=true; s.Machine(1).SugarGrade=2; s.Machine(1).RecipeSize=1;
            s.SelectMachine(1); s.BeginBusiness();
            p=ShopShift.Preview(ShopShift.LapMeters*.1,0); p.Id="low-grade"; p.SugarGrade=1; e.Inventory.Add(p);
            Check(s.ResumeProduct(p.Id),"higher machine accepts unfinished basic candy");
            s.SelectMachine(0); s.Advance(80,0);
            Check(e.Inventory.Exists(product=>product.Id=="low-grade" && ShopShift.SizeOf(product)==0),
                "worker honors inherited material cap instead of waiting forever");
            s.SelectMachine(1); s.Extract(); s.EmptySugar();
            Check(s.ResumeProduct("low-grade"),"completed basic candy can be inspected on upgraded machine");
            coins=e.Coins; Check(!s.Pour(0) && e.Coins==coins,"old material cap rejects paid pour on completed candy");
            p=ShopShift.Preview(ShopShift.LapMeters*.1,0); p.Id="premium"; p.SugarGrade=3; e.Inventory.Add(p);
            s.SelectMachine(0); Check(!s.ResumeProduct("premium") && e.Inventory.Exists(product=>product.Id=="premium"),
                "free-grade basic machine cannot resume premium product and bypass material costs");
            e=Fresh(); s=new ShopShift(e); s.BeginBusiness();
            for (int i=0;i<10;i++) s.Pour(0);
            s.Advance(1, ShopShift.LapMeters*3); p=s.Extract();
            s.Pour(0); s.Advance(1, ShopShift.LapMeters*.3);
            Check(p!=null && e.Inventory.Count==1 && s.State.BatchMeters>0 && s.State.DayTrashed==0, "closing fixture holds stock and an unfinished batch");
            s.Advance(500,0);
            Check(e.Progression.Phase==BusinessPhase.Results && e.Inventory.Count==0 && s.State.BatchMeters==0 &&
                s.Machine(0).BatchMeters==0 && s.State.DayTrashed==2, "closing discards displayed and unfinished candy as disposals");

            e=Fresh(); double baseArrival=Progression.ArrivalSeconds(e);
            e.Progression.Purchases.Add(new NodePurchase { Id="ads", Level=2 });
            Check(Progression.ArrivalSeconds(e) <= baseArrival*.65, "two ad levels shorten the visit interval noticeably");
            e=Fresh(); int single=0;
            for (int serial=0;serial<400;serial++) { int size=Progression.GroupSize(e,serial); Check(size==1||size==2, "base groups hold at most two"); if (size==1) single++; }
            Check(single>300 && single<400, "base arrivals are mostly single with occasional pairs");
            e=Fresh(); e.Progression.Purchases.Add(new NodePurchase { Id="ads", Level=1 }); e.Progression.Purchases.Add(new NodePurchase { Id="group_visit", Level=3 });
            int trio=-1, groups=0;
            for (int serial=0;serial<400;serial++) { int size=Progression.GroupSize(e,serial); if (size>1) groups++; if (size==3 && trio<0) trio=serial; }
            Check(trio>=0 && groups>160, "group upgrade raises the group rate and allows three visitors");
            e.OrderSerial=trio; s=new ShopShift(e); s.BeginBusiness();
            Check(s.State.Customers.Count==3 && s.CustomerAt(0)!=null && s.CustomerAt(1)!=null && s.CustomerAt(2)!=null &&
                s.State.Customers[0].Id!=s.State.Customers[2].Id && e.OrderSerial==trio+3, "a group arrival fills each free slot with its own order");
            e = Fresh(); s = new ShopShift(e);
            Check(s.SugarGrade(0) == 1 && s.MaxSize(0) == 0 && s.SizeLimitNote(0) == "",
                "before sugar upgrades every machine uses free grade one and sizes stay hidden");
            e.Progression.Purchases.Add(new NodePurchase { Id = "sugar_2", Level = 1 });
            e.Progression.Purchases.Add(new NodePurchase { Id = "machine_2", Level = 1 });
            e.Progression.Purchases.Add(new NodePurchase { Id = "machine_3", Level = 1 });
            Check(s.SugarGrade(0) == 1 && s.SugarGrade(1) == 2 && s.SugarGrade(2) == 2,
                "unlocked grade applies automatically up to each machine's tier");
            Check(s.Machine(0).SugarGrade == 1 && s.Machine(1).SugarGrade == 2 && s.Machine(2).SugarGrade == 2,
                "stored machine grades follow the automatic grade");
            Check(s.SizeLimitNote(0) == "기본 기계는 소까지" && s.SizeLimitNote(1) == "소다 기계는 중까지" &&
                s.SizeLimitNote(2) == "중까지 · 특급 설탕이면 대", "size notes name the machine or sugar limit");
            s.Machine(1).SugarGrade = 1;
            Check(s.SugarGrade(1) == 2 && s.PourCost(1, 0) > 0 && s.MaxSize(1) == 1,
                "a hand-edited low grade cannot buy cheaper sugar on an upgraded machine");
            e.Progression.Purchases.Add(new NodePurchase { Id = "sugar_3", Level = 1 });
            Check(s.SugarGrade(2) == 3 && s.MaxSize(2) == 2 && s.SizeLimitNote(2) == "특급 기계는 대까지",
                "top sugar unlocks the top size on the top machine");
            Console.WriteLine(passed+" passed, 0 failed"); return 0;
        } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
