using System;
using CottonCircuit;

public static class BoosterTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static ArcadeDrive Ready()
    {
        var drive = new ArcadeDrive { MaximumSpeed = 12 };
        drive.Step(1, 0, false, false, .5);
        return drive;
    }
    static void Charge(ArcadeDrive drive)
    {
        for (int i = 0; i < 160 && drive.DriftCharge < .4; i++)
            drive.Step(1, i % 40 < 20 ? .65 : -.65, false, true, .02);
        Check(drive.WallHits == 0 && drive.DriftCharge >= .4, "drift fixture left road");
    }
    static void Follow(ArcadeDrive drive)
    {
        var target = drive.Course.Sample(drive.Sample.Progress + 12).Position;
        double angle = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z) - drive.Heading;
        while (angle > Math.PI) angle -= 2 * Math.PI;
        while (angle < -Math.PI) angle += 2 * Math.PI;
        drive.Step(1, Math.Max(-1, Math.Min(1, angle * 1.8)), false, false, .02);
    }
    public static int Main()
    {
        Test("starting booster gives an immediate kick and consumes exactly one stock", () => {
            var drive = Ready(); double speed = drive.Speed;
            drive.Step(1, 0, false, false, .02, true);
            Check(drive.ManualBoostActive && drive.BoostRemaining > 2.4 && drive.Speed > speed + 7,
                "stored booster failed to accelerate");
            Check(drive.StoredBoosts == 0 && drive.BoostCount == 1, "activation consumed wrong stock");
            for (int i = 0; i < 5; i++) drive.Step(1, 0, false, false, .02, true);
            Check(drive.BoostCount == 1 && drive.Speed <= 26, "repeated input stacked boost");
        });
        Test("one large input frame activates once and cannot spend a second stock", () => {
            var drive = Ready(); Charge(drive); drive.Step(1, 0, false, false, .02);
            drive.Step(0, 0, true, false, .02);
            Check(drive.StoredBoosts == 2, "charged drift did not refill stock");
            int before = drive.BoostCount;
            drive.Step(1, 0, false, false, .3, true);
            Check(drive.StoredBoosts == 1 && drive.BoostCount == before + 1,
                "substeps spent repeated boosters");
        });
        Test("brake no throttle and active boost reject consumption", () => {
            var drive = Ready();
            drive.Step(1, 0, true, false, .02, true);
            drive.Step(0, 0, false, false, .02, true);
            Check(drive.StoredBoosts == 1 && drive.BoostCount == 0, "invalid input spent stock");
            Charge(drive); drive.Step(1, 0, false, false, .02);
            drive.Step(1, 0, false, false, .02, true);
            Check(drive.StoredBoosts == 2 && !drive.ManualBoostActive, "active mini boost consumed stock");
        });
        Test("clean drift refills once and inventory stays within two slots", () => {
            var drive = Ready(); Charge(drive); drive.Step(1, 0, false, false, .02);
            Check(drive.StoredBoosts == 2, "clean drift did not add one booster");
            for (int i = 0; i < 20; i++) Follow(drive);
            Check(drive.StoredBoosts == 2, "released input repeated refill");
            drive.Recover(); drive.Step(1, 0, false, false, .5);
            Charge(drive); drive.Step(1, 0, false, false, .02);
            Check(drive.StoredBoosts == 2, "stock exceeded capacity");
        });
        Test("stationary straight and interrupted slides cannot refill stock", () => {
            var drive = new ArcadeDrive();
            drive.Step(0, 1, false, true, 1); drive.Step(0, 0, false, false, .02);
            drive.Step(1, 0, false, true, .5); drive.Step(1, 0, false, false, .02);
            Check(drive.StoredBoosts == 1, "stationary or straight slide refilled");
            drive = Ready(); Charge(drive); drive.Recover(); drive.Step(1, 0, false, false, .02);
            Check(drive.StoredBoosts == 1, "recovery completed a cancelled slide");
        });
        Test("brake recovery stop and wall cancel a stored boost without refunding it", () => {
            for (int mode = 0; mode < 4; mode++) {
                var drive = Ready(); drive.Step(1, 0, false, false, .02, true);
                if (mode == 0) drive.Step(1, 0, true, false, .02);
                if (mode == 1) drive.Recover();
                if (mode == 2) drive.Stop();
                if (mode == 3) {
                    for (int i = 0; i < 100 && drive.WallHits == 0; i++) drive.Step(1, 1, false, false, .02);
                    Check(drive.WallHits > 0, "collision fixture missed wall");
                }
                Check(drive.BoostRemaining == 0 && !drive.ManualBoostActive && drive.StoredBoosts == 0,
                    "cancellation retained effect or refunded consumed stock: " + mode);
                drive.Step(1, 0, false, false, .02, true);
                Check(drive.BoostCount == 1, "empty stock reactivated");
            }
        });
        Test("boost expiry is smooth and cannot retrigger with empty stock", () => {
            var drive = Ready(); drive.Step(1, 0, false, false, .02, true);
            while (drive.BoostRemaining > 0) Follow(drive);
            double before = drive.Speed; Follow(drive);
            Check(drive.WallHits == 0 && before > 12 && before - drive.Speed < .3,
                "expiry snapped speed or failed to stay on course");
            drive.Step(1, 0, false, false, .02, true);
            Check(!drive.ManualBoostActive && drive.BoostCount == 1, "expiry granted free booster");
        });
        Test("drift release during stored boost refills without shortening its remaining time", () => {
            var drive = Ready(); Charge(drive);
            drive.Step(1, .3, false, true, .02, true);
            double remaining = drive.BoostRemaining;
            drive.Step(1, 0, false, false, .02);
            Check(drive.WallHits == 0 && drive.StoredBoosts == 1, "clean boost slide did not refill");
            Check(drive.ManualBoostActive && drive.BoostRemaining > remaining - .03,
                "mini boost shortened stored boost");
            Check(drive.Speed <= 26, "drift release stacked impulses past cap");
        });
        Test("a charged drift released into a wall cannot refill booster stock", () => {
            var probe = Ready(); Charge(probe); int steps = 0;
            while (probe.WallHits == 0 && steps < 200) { probe.Step(1, 1, false, true, .02); steps++; }
            Check(probe.WallHits > 0, "wall fixture did not collide");
            var drive = Ready(); Charge(drive);
            for (int i = 1; i < steps; i++) drive.Step(1, 1, false, true, .02);
            drive.Step(1, 1, false, false, .02);
            Check(drive.WallHits > 0 && drive.StoredBoosts == 1, "wall release awarded a booster");
        });
        Test("reset supplies a fresh race but downhill cannot use stored boost", () => {
            var drive = Ready(); drive.Step(1, 0, false, false, .02, true); drive.Reset();
            Check(drive.StoredBoosts == 1 && drive.BoostCount == 0 && !drive.ManualBoostActive,
                "new race did not reset booster state");
            drive.Style = DrivingStyle.Downhill; drive.Reset();
            drive.Step(1, 0, false, false, .3, true);
            Check(drive.StoredBoosts == 0 && drive.BoostCount == 0 && drive.BoostRemaining == 0,
                "downhill received kart booster");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
