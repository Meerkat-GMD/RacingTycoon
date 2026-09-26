using System;
using CottonCircuit;

public static class ContinuousTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static void Start(GameSession session, int map = 0, int flavor = 0, int laps = 0, int skills = 0, int hits = 0)
    {
        Check(session.StartContinuousRecipe(map, flavor, laps, skills, hits), "continuous recipe refused");
    }
    static Product Candy()
    { var production = new Production(60); production.Advance(100, 10, 0); return production.Finish(); }
    static void Tick(GameSession session, int laps, double seconds = 24, int skills = 0, int hits = 0)
    { session.TickRecipe(seconds, 100, 10, laps, skills, hits); }
    static void DriveInput(ArcadeDrive drive, out double throttle, out double steering, out bool brake)
    {
        AutoDrive.Input(drive, out throttle, out steering, out brake);
    }
    public static int Main()
    {
        Test("each forward lap stocks once without interrupting the next recipe", () => {
            var session = new GameSession(new Economy()); Start(session);
            Tick(session, 0, 5); Check(session.Economy.Inventory.Count == 0, "partial lap awarded candy");
            Tick(session, 1, 19); var first = session.Result;
            Check(session.Mode == GameMode.Racing && first != null && first.Grams == 60 &&
                session.Economy.Inventory.Count == 1 && session.Economy.Coins == 100, "first lap did not stock and continue");
            Tick(session, 1, 1);
            Check(session.Economy.Inventory.Count == 1 && session.Economy.Coins == 100, "same lap rewarded twice");
            Tick(session, 2, 23);
            Check(session.Mode == GameMode.Racing && session.Economy.Inventory.Count == 2 &&
                session.Economy.Coins == 120 && session.Result.Id != first.Id, "second lap did not award once");
        });
        Test("a new recipe uses cumulative lap and performance baselines", () => {
            var session = new GameSession(new Economy()); Start(session, 1, 2, 7, 9, 5);
            Tick(session, 7, 1, 9, 5);
            Check(session.Economy.Inventory.Count == 0, "old laps minted a new product");
            Tick(session, 8, 29, 10, 5);
            Check(session.Result.Grams == 120 && session.Result.Quality == 55 && session.Economy.Coins == 120,
                "prior run counters changed recipe quality or bonus");
            foreach (var sample in session.Result.Samples) Check(sample.Flavor == 2, "recipe flavor changed");
            Tick(session, 9, 30, 10, 5);
            Check(session.Result.Quality == 50 && session.Economy.Inventory.Count == 2, "quality counters leaked between laps");
        });
        Test("full stock waits for a fresh whole lap after room becomes available", () => {
            var economy = new Economy(); for (int i = 0; i < 6; i++) economy.CompleteRun(Candy());
            var session = new GameSession(economy); Start(session);
            Tick(session, 3, 72);
            Check(session.Mode == GameMode.Racing && economy.Inventory.Count == 6 && economy.Coins == 80,
                "full shelf interrupted racing or paid a lap");
            economy.Discard(economy.Inventory[0].Id); Tick(session, 3, 3);
            Tick(session, 4, 10);
            Check(economy.Inventory.Count == 5 && economy.Coins == 80, "resuming paid for a partial lap");
            Tick(session, 5, 24);
            Check(economy.Inventory.Count == 6 && economy.Coins == 100, "full lap after space opened failed to stock");
            Tick(session, 6, 24);
            Check(economy.Inventory.Count == 6 && economy.Coins == 100, "full shelf dropped a paid product");
        });
        Test("sale during racing leaves recipe and completed inventory consistent", () => {
            var session = new GameSession(new Economy()); var orders = new OrderManager(session.Economy);
            Start(session); Tick(session, 1); var candy = session.Result;
            var receipt = orders.Serve(session.Economy.Orders[0].Id, candy.Id);
            Check(receipt != null && session.Mode == GameMode.Racing && session.Economy.Inventory.Count == 0,
                "continuous order could not be served");
            int coins = session.Economy.Coins; Tick(session, 1, 1);
            Check(session.Economy.Coins == coins && session.Economy.Inventory.Count == 0, "sale regenerated finished candy");
            Tick(session, 2, 23);
            Check(session.Economy.Inventory.Count == 1 && session.Economy.Coins == coins + 20,
                "sale interrupted the next lap");
        });
        Test("pause freezes continuous production and resume does not reuse expired laps", () => {
            var session = new GameSession(new Economy()); Start(session);
            session.Paused = true; Tick(session, 1); session.FinishRun();
            Check(session.Elapsed == 0 && session.Economy.Inventory.Count == 0 && session.Mode == GameMode.Racing,
                "paused continuous race changed state");
            session.Paused = false; Tick(session, 0, 180);
            Check(session.Mode == GameMode.Racing && session.Economy.Inventory.Count == 0, "timeout interrupted continuous mode");
            Tick(session, 1); Check(session.Economy.Inventory.Count == 0, "timeout paid an incomplete lap");
            Tick(session, 2); Check(session.Economy.Inventory.Count == 1, "fresh full lap did not resume production");
        });
        Test("automatic driver takes over a manual corner without resetting the car", () => {
            foreach (DrivingStyle style in new[] { DrivingStyle.Kart, DrivingStyle.Downhill })
            {
                var drive = new ArcadeDrive() { Style = style };
                for (int i = 0; i < 60; i++) drive.Step(1, i < 20 ? 0 : .8, false, false, .05);
                double progress = drive.TotalProgress;
                for (int i = 0; i < 2400 && drive.Laps == 0; i++)
                {
                    double throttle, steering; bool brake;
                    DriveInput(drive, out throttle, out steering, out brake);
                    drive.Step(throttle, steering, brake, false, .05);
                }
                Check(drive.Laps > 0 && drive.TotalProgress > progress,
                    "auto takeover remained stalled after manual input: " + style);
            }
        });
        foreach (int map in new[] { 0, 1 }) foreach (DrivingStyle style in new[] { DrivingStyle.Kart, DrivingStyle.Downhill })
            foreach (double dt in new[] { .02, .05 }) foreach (double motor in new[] { 26.0, 33.5 })
            {
                int selectedMap = map; var selectedStyle = style; double stepTime = dt, maximum = motor;
                Test("auto driver completes three clean laps: map " + map + " " + style + " dt " + dt + " speed " + motor, () => {
                    var drive = new ArcadeDrive(RaceCourse.ForMap(selectedMap)) { Style = selectedStyle, MaximumSpeed = maximum };
                    for (int i = 0; i < 120 / stepTime && drive.Laps < 3; i++)
                    {
                        double throttle, steering; bool brake;
                        DriveInput(drive, out throttle, out steering, out brake);
                        drive.Step(throttle, steering, brake, false, stepTime);
                    }
                    Check(drive.Laps >= 3 && drive.WallHits == 0 && drive.Speed > 5,
                        "automatic driver stalled or hit walls: laps " + drive.Laps + ", hits " + drive.WallHits);
                });
            }
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
