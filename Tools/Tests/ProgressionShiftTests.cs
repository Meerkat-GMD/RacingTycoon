using System;
using CottonCircuit;
class ProgressionShiftTests
{
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static Economy Fresh() { var e = new Economy(); Progression.Enable(e); return e; }
    static void All(Economy e) { foreach (var n in Progression.Nodes) e.Progression.Purchases.Add(new NodePurchase { Id = n.Id, Level = n.MaxLevel }); }
    static ShopShift WorkerFixture(out Economy economy, int selected, int worker = 0)
    {
        economy = Fresh(); All(economy); economy.Coins = 10000;
        var shift = new ShopShift(economy); shift.SelectMachine(selected);
        shift.Machine(worker).WorkerAssigned = true;
        shift.BeginBusiness(); return shift;
    }
    static void WorkerControlChecks()
    {
        Economy viewed, hidden, manual;
        var viewedShift = WorkerFixture(out viewed, 0);
        var hiddenShift = WorkerFixture(out hidden, 1);
        var manualShift = WorkerFixture(out manual, 0);
        viewedShift.Advance(20, 0); hiddenShift.Advance(20, 0); manualShift.Advance(20, ShopShift.LapMeters * 100, 100);
        var shown = viewedShift.Machine(0); var offscreen = hiddenShift.Machine(0); var ignoredInput = manualShift.Machine(0);
        Check(shown.BatchMeters > 0 && Math.Abs(shown.BatchMeters - offscreen.BatchMeters) < 1e-8 &&
            Math.Abs(shown.SugarGrams - offscreen.SugarGrams) < 1e-8 && viewed.Coins == hidden.Coins &&
            viewed.Business.DayMaterialCost == hidden.Business.DayMaterialCost,
            "viewing a worker preserves offscreen production rate and material spending");
        Check(Math.Abs(shown.BatchMeters - ignoredInput.BatchMeters) < 1e-8 && shown.BatchQuality == ignoredInput.BatchQuality &&
            Math.Abs(shown.SugarGrams - ignoredInput.SugarGrams) < 1e-8,
            "manual distance and wall hits cannot double grow or damage a selected worker batch");
        viewedShift.Advance(50, 0); hiddenShift.Advance(50, 0);
        Check(viewed.Inventory.Count > 0 && viewed.Inventory.Count == hidden.Inventory.Count &&
            Math.Abs(viewedShift.State.BatchMeters - hiddenShift.Machine(0).BatchMeters) < 1e-8 &&
            viewedShift.State.SugarGrams == viewedShift.Machine(0).SugarGrams,
            "selected workers repeatedly extract to inventory and keep selected state synchronized");

        Economy unstaffed = Fresh(); var idle = new ShopShift(unstaffed); idle.BeginBusiness(); idle.Pour(0);
        idle.Advance(10, 0);
        Check(idle.State.BatchMeters == 0 && idle.State.SugarGrams == 10 && !idle.HasWorker(0) && !idle.WorkerCanOperate(0),
            "an unstaffed machine with sugar cannot produce while the player is idle");
        idle.State.Machines[0].WorkerAssigned = true; idle.Advance(1, 0);
        Check(!idle.HasWorker(0) && idle.State.BatchMeters == 0,
            "assignment without hiring does not create a free automatic worker");
        unstaffed.Progression.Purchases.Add(new NodePurchase { Id = "worker_1", Level = 1 });
        Check(idle.HasWorker(0) && !idle.HasWorker(1) && !idle.HasWorker(-1) && !idle.HasWorker(3),
            "worker ownership requires a valid owned machine and an assigned hired worker");
        unstaffed.Progression.Purchases.Add(new NodePurchase { Id = "machine_2", Level = 1 });
        idle.State.Machines[0].WorkerAssigned = false; idle.State.Machines[1].WorkerAssigned = true;
        Check(!idle.HasWorker(1) && !idle.WorkerCanOperate(1), "worker education is required for a higher tier machine");
        unstaffed.Progression.Purchases.Add(new NodePurchase { Id = "worker_grade_2", Level = 1 });
        Check(idle.HasWorker(1), "trained hired worker can own an assigned higher tier machine");
        idle.State.Machines[0].WorkerAssigned = true;
        Check(idle.HasWorker(0) && !idle.HasWorker(1), "assignments cannot create more automatic workers than were hired");

        var owned = WorkerFixture(out viewed, 0); owned.Advance(.2, 0);
        double sugar = owned.State.SugarGrams, batch = owned.State.BatchMeters;
        var stock = ShopShift.Preview(ShopShift.LapMeters, 0); stock.Id = "worker-protected-stock";
        viewed.Inventory.Add(stock); viewed.CompletedIds.Add(stock.Id);
        Check(!owned.Pour(0) && !owned.EmptySugar() && owned.Extract() == null && !owned.ResumeProduct(stock.Id) &&
            owned.State.SugarGrams == sugar && owned.State.BatchMeters == batch && viewed.Inventory.Contains(stock),
            "manual pour empty extract and resume leave a selected worker batch intact");
        int coins = viewed.Coins, material = owned.State.DayMaterialCost;
        for (int i = 0; i < 5; i++) Check(owned.WorkerCanOperate(0), "working worker remains operable when queried");
        Check(viewed.Coins == coins && owned.State.DayMaterialCost == material && owned.State.SugarGrams == sugar &&
            owned.State.BatchMeters == batch && owned.Machine(0).SugarGrams == sugar && viewed.Inventory.Count == 1,
            "worker operation queries do not spend consume grow extract or synchronize state");
        owned.Paused = true;
        Check(owned.HasWorker(0) && !owned.WorkerCanOperate(0), "pause keeps ownership but blocks automatic operation");
        owned.Advance(10, 0); Check(owned.State.BatchMeters == batch, "paused worker does not grow"); owned.Paused = false;
        while (viewed.Inventory.Count < viewed.StockCapacity) viewed.Inventory.Add(ShopShift.Preview(ShopShift.LapMeters, 0));
        Check(!owned.WorkerCanOperate(0), "full shelf reports selected worker stopped"); owned.Advance(5, 0);
        Check(owned.State.BatchMeters == batch && owned.State.SugarGrams == sugar && viewed.Coins == coins,
            "full shelf stops selected worker growth and material spending");

        var premium = WorkerFixture(out viewed, 1, 1); premium.Machine(1).RecipeFlavor = 1; viewed.Coins = 0;
        Check(!premium.WorkerCanOperate(1), "unfunded premium worker reports stopped"); premium.Advance(2, 0);
        Check(premium.State.BatchMeters == 0 && premium.State.SugarGrams == 0 && premium.State.DayMaterialCost == 0,
            "unfunded selected worker cannot create paid ingredients");
        viewed.Coins = 2; Check(!premium.WorkerCanOperate(1), "partial refill money does not pay for a full worker pour");
        viewed.Coins = 3; Check(premium.WorkerCanOperate(1), "affordable premium worker can begin"); premium.Advance(.2, 0);
        Check(viewed.Coins == 0 && premium.State.DayMaterialCost == 3 && premium.WorkerCanOperate(1),
            "prepaid matching sugar keeps the worker running after money runs out");
        premium.Advance(60, 0);
        Check(premium.State.BatchMeters > 0 && premium.State.SugarGrams == 0 && !premium.WorkerCanOperate(1),
            "worker stops when paid sugar runs out and cannot be replaced");

        viewed = Fresh(); All(viewed); viewed.Coins = 10000; owned = new ShopShift(viewed);
        owned.SelectMachine(1); owned.BeginBusiness(); owned.Pour(0); owned.Advance(.2, 1);
        // A handover may already have a finished batch; it needs no refill to be put on the shelf.
        owned.State.BatchMeters = ShopShift.MetersForSize(0); owned.SyncActive();
        owned.Machine(1).WorkerAssigned = true; viewed.Coins = 0;
        Check(owned.WorkerCanOperate(1), "ready inherited candy can be finished without refill funds"); owned.Advance(.2, 0);
        Check(viewed.Inventory.Count == 1 && viewed.Coins == 0 && owned.State.BatchMeters == 0,
            "selected worker extracts ready inherited candy without a material charge");
        owned.Advance(1000, 0);
        Check(!owned.WorkerCanOperate(1) && owned.HasWorker(1), "closed day preserves worker assignment but stops operation");
        owned.ReturnToPreparation(); Check(!owned.WorkerCanOperate(1), "preparation does not operate hired workers");

        var tutorialEconomy = Fresh(); var tutorial = new ShopShift(tutorialEconomy); tutorial.BeginTutorial();
        All(tutorialEconomy); tutorial.State.Machines[0].WorkerAssigned = true;
        Check(tutorial.HasWorker(0) && !tutorial.WorkerCanOperate(0), "practice never starts automatic worker production");
    }
    static int Main()
    {
        try {
            Strings.Load(System.IO.File.ReadAllText(Strings.TablePath(AppDomain.CurrentDomain.BaseDirectory)));
            Strings.Set(Language.Korean);
            var viewedWorkerEconomy = Fresh(); All(viewedWorkerEconomy); viewedWorkerEconomy.Coins = 10000;
            var viewedWorkerShift = new ShopShift(viewedWorkerEconomy);
            viewedWorkerShift.Machine(0).WorkerAssigned = true;
            viewedWorkerShift.BeginBusiness(); viewedWorkerShift.Advance(1, 0);
            Check(viewedWorkerShift.State.BatchMeters > 0,
                "a hired assigned worker produces on the selected machine without manual driving");
            WorkerControlChecks();
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
            Check(!s.Pour(2), "locked vanilla rejected");
            for (int i=0;i<10;i++) s.Pour(0);
            s.Advance(1, ShopShift.LapMeters * 1.5);
            Check(Math.Abs(s.State.BatchMeters - ShopShift.MetersForSize(0)) < .001, "grade one caps at standard");
            Check(Math.Abs(s.State.BatchOverflowMeters - ShopShift.LapMeters * .5) < .001 &&
                Math.Abs(s.State.SugarGrams - 25) < .001, "winding past the cap keeps draining sugar and counting laps");
            s.Advance(1, ShopShift.LapMeters);
            Check(s.State.SugarGrams == 0 && Math.Abs(s.State.BatchMeters - ShopShift.MetersForSize(0)) < .001 &&
                Math.Abs(s.State.BatchOverflowMeters - ShopShift.LapMeters) < .001, "size stays at the cap until the sugar runs out");
            Check(s.Pour(0) && s.State.SugarGrams == 10, "sugar can still be poured into a capped batch");
            s.Advance(1, ShopShift.LapMeters * .1);
            Check(Math.Abs(s.State.BatchOverflowMeters - ShopShift.LapMeters * 1.1) < .001 &&
                ShopShift.SizeForDistance(s.State.BatchMeters) == 0, "poured sugar winds overflow without growing the size");
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
            s.Machine(1).SugarGrade=2;
            s.Machine(1).RecipeFlavor=1; s.Machine(1).RecipeSize=1;
            s.SelectMachine(1); s.BeginBusiness(); s.Pour(0); s.Advance(1,1); s.Machine(1).WorkerAssigned=true; s.SelectMachine(0);
            s.Advance(180,0);
            Check(e.Inventory.Exists(product=>product.FlavorIndex==0) && e.Inventory.Exists(product=>product.FlavorIndex==1),
                "worker finishes inherited flavor then resumes its assigned recipe");
            e=Fresh(); All(e); e.Coins=10000; s=new ShopShift(e);
            s.Machine(1).SugarGrade=2; s.Machine(1).RecipeSize=1;
            s.SelectMachine(1); s.BeginBusiness();
            p=ShopShift.Preview(ShopShift.LapMeters*.1,0); p.Id="low-grade"; p.SugarGrade=1; e.Inventory.Add(p);
            Check(s.ResumeProduct(p.Id),"higher machine accepts unfinished basic candy");
            s.Machine(1).WorkerAssigned=true; s.SelectMachine(0); s.Advance(80,0);
            Check(e.Inventory.Exists(product=>product.Id=="low-grade" && ShopShift.SizeOf(product)==0),
                "worker honors inherited material cap instead of waiting forever");
            s.Machine(1).WorkerAssigned=false; s.SelectMachine(1); s.Extract(); s.EmptySugar();
            Check(s.ResumeProduct("low-grade"),"completed basic candy can be inspected on upgraded machine");
            coins=e.Coins; Check(s.Pour(0) && e.Coins<coins,"completed candy still accepts paid sugar for overflow winding");
            s.Advance(1, ShopShift.LapMeters*.1);
            Check(ShopShift.SizeForDistance(s.State.BatchMeters)==0 && s.State.BatchOverflowMeters>0,
                "old material cap keeps the resumed candy at its original size");
            s.EmptySugar();
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
            Check(s.SizeLimitNote(0) == "기본 기계는 소까지" && s.SizeLimitNote(1) == "고급 기계는 중까지" &&
                s.SizeLimitNote(2) == "중까지 · 특급 설탕이면 대", "size notes name the machine or sugar limit");
            Strings.Set(Language.English);
            Check(s.SizeLimitNote(0) == "Basic Machine: up to S", "english size note");
            Strings.Set(Language.Korean);
            s.Machine(1).SugarGrade = 1;
            Check(s.SugarGrade(1) == 2 && s.PourCost(1, 0) > 0 && s.MaxSize(1) == 1,
                "a hand-edited low grade cannot buy cheaper sugar on an upgraded machine");
            e.Progression.Purchases.Add(new NodePurchase { Id = "sugar_3", Level = 1 });
            Check(s.SugarGrade(2) == 3 && s.MaxSize(2) == 2 && s.SizeLimitNote(2) == "특급 기계는 대까지",
                "top sugar unlocks the top size on the top machine");

            // Stars follow wall hits only; quality traits raise the sale bonus instead of the start.
            e = Fresh(); All(e); e.Coins = 10000; s = new ShopShift(e);
            s.Machine(1).WorkerAssigned = true; s.Machine(1).RecipeFlavor = 0; s.Machine(1).RecipeSize = 0;
            s.BeginBusiness(); s.Pour(0); s.Advance(1, 5);
            Check(ShopShift.Stars(s.State.BatchQuality) == 3 && s.State.BatchQuality == ShopShift.QualityForStars(3),
                "a clean progression batch starts at three stars even with every quality trait");
            s.Advance(1, 5, 1);
            Check(ShopShift.Stars(s.State.BatchQuality) == 2, "a wall hit removes one star from the driven machine");
            s.EmptySugar(); s.Advance(1, 5, 1);
            Check(ShopShift.Stars(s.State.BatchQuality) == 1, "a wall hit costs a star while the stalled candy stays on the stick");
            s.Advance(1, 5, 4);
            Check(s.State.BatchQuality == 0, "progression stars stop at zero");
            s.Advance(120, 0, 3);
            var worker = e.Inventory.Find(product => product.FlavorIndex == 0 && product.Id != s.State.BatchProductId);
            Check(worker != null && ShopShift.Stars(worker.Quality) == 3,
                "a worker's candy keeps three stars when the player hits walls on another machine");
            var fresh = Fresh();
            Check(Math.Abs(Progression.StarBonus(fresh) - .05) < 1e-9 && Math.Abs(Progression.StarBonus(e) - .078) < 1e-9,
                "star bonus starts at five percent and quality traits raise it to 7.8 percent");
            var starred = ShopShift.Preview(ShopShift.LapMeters, 0); starred.Quality = ShopShift.QualityForStars(3);
            var bare = ShopShift.Preview(ShopShift.LapMeters, 0); bare.Quality = 0;
            Check(Math.Abs(e.Price(starred) - e.Price(bare) * 1.234) <= 1,
                "three stars with every quality trait add 23.4 percent to the sale price");
            for (int size = 0; size < 3; size++)
            {
                var plain = ShopShift.Preview(ShopShift.MetersForSize(size), 0); plain.Quality = 0;
                Check(fresh.Price(plain) == 45 * (size + 1), "a starless strawberry candy sells for 45 coins per size tier");
            }
            Console.WriteLine(passed+" passed, 0 failed"); return 0;
        } catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
