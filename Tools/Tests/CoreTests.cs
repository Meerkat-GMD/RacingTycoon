using System;
using CottonCircuit;

public static class CoreTests
{
    static int failures;
    static int passed;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + e.Message); failures++; }
    }
    static Product MakeProduct() { var p = new Production(220); p.Advance(Math.PI * 2, 10, 1); return p.Finish(); }
    public static int Main()
    {
        Test("stationary driving produces nothing", () => { var p = new Production(220); p.Advance(0, 10, 0); Check(p.Grams == 0 && p.Finish() == null, "empty run created candy"); });
        Test("one revolution winds twenty samples and forty grams", () => { var p = new Production(220); p.Advance(2 * Math.PI, 10, 1); Check(p.Samples.Count == 20 && p.Grams == 40, "incorrect winding yield"); });
        Test("lane radius and flavor remain in product", () => { var p = new Production(220); p.Advance(Math.PI, 7.5, 0); p.Advance(Math.PI, 12.5, 2); var product = p.Finish(); Check(product.Samples[0].Flavor == 0 && product.Samples[19].Flavor == 2 && product.Samples[19].Radius == 12.5, "lane history lost"); });
        Test("timestep does not change production", () => { var a = new Production(220); var b = new Production(220); a.Advance(4 * Math.PI, 10, 1); for (int i = 0; i < 800; i++) b.Advance(Math.PI / 200, 10, 1); Check(a.Grams == b.Grams && a.Samples.Count == b.Samples.Count, "frame-dependent production"); });
        Test("production stops at capacity", () => { var p = new Production(60); p.Advance(200, 10, 1); Check(p.Grams == 60 && p.Samples.Count == 30 && p.IsFull, "capacity exceeded"); });
        Test("invalid movement cannot generate candy", () => { var p = new Production(220); p.Advance(-1, 10, 0); p.Advance(double.NaN, 10, 0); p.Advance(double.PositiveInfinity, 10, 0); Check(p.Grams == 0, "invalid movement accepted"); });
        Test("finished run cannot produce or finish twice", () => { var p = new Production(220); p.Advance(Math.PI, 10, 1); var first = p.Finish(); p.Advance(Math.PI, 10, 1); Check(first.Grams == 20 && p.Grams == 20 && p.Finish() == null, "run was reused"); });
        Test("sale removes exactly one product and pays once", () => { var e = new Economy(); var product = MakeProduct(); Check(e.CompleteRun(product), "product refused"); int price = e.Price(product); int before = e.Coins; Check(e.SellNext() == price && e.Coins == before + price && e.Inventory.Count == 0 && e.SellNext() == 0, "invalid sale accounting"); });
        Test("same product cannot be stocked twice", () => { var e = new Economy(); var p = MakeProduct(); Check(e.CompleteRun(p) && !e.CompleteRun(p) && e.Inventory.Count == 1, "duplicate product stocked"); });
        Test("insufficient funds do not change level or balance", () => { var e = new Economy(); int before = e.Coins; Check(!e.BuyUpgrade(0) && e.Levels[0] == 0 && e.Coins == before, "unaffordable upgrade purchased"); });
        Test("upgrade debits full price and improves relevant statistic", () => { var e = new Economy(); e.Coins = 1000; int capacity = e.Capacity; int cost = e.UpgradeCost(1); Check(e.BuyUpgrade(1) && e.Coins == 1000 - cost && e.Capacity > capacity && e.Levels[0] == 0, "upgrade accounting wrong"); });
        Test("upgrade caps cannot consume currency", () => { var e = new Economy(); e.Coins = 10000; for (int i = 0; i < 3; i++) Check(e.BuyUpgrade(2), "early cap"); int before = e.Coins; Check(!e.BuyUpgrade(2) && e.Levels[2] == 3 && e.Coins == before, "cap charged coins"); });
        Test("inventory cap rejects without changing day", () => { var e = new Economy(); for (int i = 0; i < Economy.InventoryLimit; i++) Check(e.CompleteRun(MakeProduct()), "early inventory limit"); int day = e.Day; Check(!e.CompleteRun(MakeProduct()) && e.Inventory.Count == Economy.InventoryLimit && e.Day == day, "inventory cap broken"); });
        Test("null product cannot enter inventory", () => { var e = new Economy(); Check(!e.CompleteRun(null) && e.Inventory.Count == 0, "empty candy stocked"); });
        Test("flavor variety earns a higher price", () => { var a = new Production(220); var b = new Production(220); a.Advance(Math.PI * 2, 10, 1); b.Advance(Math.PI, 10, 0); b.Advance(Math.PI, 10, 2); var e = new Economy(); Check(e.Price(b.Finish()) > e.Price(a.Finish()), "mixed flavors receive no premium"); });
        Test("full cycle stocks only once and returns to shop", () => { var s = new GameSession(new Economy()); Check(s.StartRun(), "run refused"); s.Tick(10, Math.PI * 2, 10, 1); s.FinishRun(); s.FinishRun(); Check(s.Mode == GameMode.Results && s.Economy.Inventory.Count == 1, "double stock or wrong mode"); s.ReturnToShop(); Check(s.Mode == GameMode.Shop, "cannot return to shop"); });
        Test("pause freezes time and production", () => { var s = new GameSession(new Economy()); s.StartRun(); s.Paused = true; s.Tick(10, Math.PI, 10, 1); Check(s.Remaining == 60 && s.Production.Grams == 0, "pause advances gameplay"); });
        Test("timeout finalizes empty run without inventing candy", () => { var s = new GameSession(new Economy()); s.StartRun(); s.Tick(61, 0, 10, 1); Check(s.Mode == GameMode.Results && s.Remaining == 0 && s.Result == null && s.Economy.Inventory.Count == 0, "timeout broken"); });
        Test("time beyond deadline does not generate extra candy", () => { var s = new GameSession(new Economy()); s.StartRun(); s.Tick(59, 0, 10, 1); s.Tick(2, 2 * Math.PI, 10, 1); Check(s.Result != null && s.Result.Grams == 20, "production went beyond deadline"); });
        Test("full production automatically completes", () => { var s = new GameSession(new Economy()); s.StartRun(); s.Tick(40, 100, 10, 1); Check(s.Mode == GameMode.Results && s.Result.Grams == 220, "full machine not completed"); });
        Test("full shop cannot launch a run", () => { var e = new Economy(); for (int i = 0; i < Economy.InventoryLimit; i++) e.CompleteRun(MakeProduct()); var s = new GameSession(e); Check(!s.StartRun() && s.Mode == GameMode.Shop, "started with no inventory space"); });
        Test("cannot restart or return to shop during a run", () => { var s = new GameSession(new Economy()); s.StartRun(); s.Tick(5, Math.PI, 10, 0); s.ReturnToShop(); Check(!s.StartRun() && s.Mode == GameMode.Racing && s.Remaining == 55, "state transition erased active run"); });
        Test("title and story music come before any session", () => { Check(MusicChoice.Cue(new MusicScene { Title = true }) == MusicCue.Title, "title"); Check(MusicChoice.Cue(new MusicScene { Title = true, Story = true }) == MusicCue.Story, "story"); Check(MusicChoice.Cue(new MusicScene()) == MusicCue.None, "no session is silent"); });
        Test("tutorial music overrides the business song", () => { var s = new MusicScene { InGame = true, Tutorial = true, Progression = true, ShiftExists = true, ShiftOpen = true, Phase = BusinessPhase.Operating, Machine = 2 }; Check(MusicChoice.Cue(s) == MusicCue.Tutorial, "tutorial"); });
        Test("each machine has its own business song", () => { var s = new MusicScene { InGame = true, Progression = true, ShiftExists = true, ShiftOpen = true, Phase = BusinessPhase.Operating }; for (int m = 0; m < 3; m++) { s.Machine = m; Check(MusicChoice.Cue(s) == (MusicCue)((int)MusicCue.Machine1 + m), "machine " + m); } });
        Test("preparation plays its theme and settlement is quiet", () => { var s = new MusicScene { InGame = true, Progression = true, ShiftExists = true, Phase = BusinessPhase.Preparation }; Check(MusicChoice.Cue(s) == MusicCue.Preparation, "preparation"); s.Phase = BusinessPhase.Results; Check(MusicChoice.Cue(s) == MusicCue.None, "settlement"); s.Phase = BusinessPhase.Operating; s.ShiftOpen = false; Check(MusicChoice.Cue(s) == MusicCue.None, "closed day"); });
        Test("legacy modes map to preparation and the first machine song", () => { var s = new MusicScene { InGame = true, Mode = GameMode.Shop }; Check(MusicChoice.Cue(s) == MusicCue.Preparation, "shop"); s.Mode = GameMode.Racing; Check(MusicChoice.Cue(s) == MusicCue.Machine1, "racing"); s.Mode = GameMode.Results; Check(MusicChoice.Cue(s) == MusicCue.None, "results"); s = new MusicScene { InGame = true, ShiftExists = true, ShiftOpen = true }; Check(MusicChoice.Cue(s) == MusicCue.Machine1, "day without progression"); });
        Test("only the last thirty open seconds hurry", () => { var s = new MusicScene { InGame = true, ShiftOpen = true, RemainingSeconds = 31 }; Check(!MusicChoice.Hurry(s), "too early"); s.RemainingSeconds = 30; Check(MusicChoice.Hurry(s), "thirty seconds"); s.Tutorial = true; Check(!MusicChoice.Hurry(s), "tutorial never hurries"); s.Tutorial = false; s.ShiftOpen = false; Check(!MusicChoice.Hurry(s), "closed day"); });
        Test("authored endings wrap at their loop end and loops never wrap", () => { Check(!MusicChoice.WrapDue(111.4, 111.47) && MusicChoice.WrapDue(111.47, 111.47), "wrap point"); Check(!MusicChoice.WrapDue(500, 0), "whole-clip loop"); Check(MusicChoice.Remembers(MusicCue.Machine2) && !MusicChoice.Remembers(MusicCue.Preparation), "only machine songs resume"); });
        Test("pause does not end the business day", () => { var e = new Economy(); var shift = new ShopShift(e); Check(shift.IsOpen && shift.DayRunning, "open day"); shift.Paused = true; Check(!shift.IsOpen && shift.DayRunning, "paused day still running"); });
        Console.WriteLine("RESULT: " + passed + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }
}
