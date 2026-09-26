using System;
using CottonCircuit;

public static class FeelTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Near(double actual, double expected, double tolerance, string message)
    { Check(Math.Abs(actual - expected) <= tolerance, message + " (got " + actual + ", expected " + expected + ")"); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static double Angle(double angle)
    { while (angle > Math.PI) angle -= 2 * Math.PI; while (angle < -Math.PI) angle += 2 * Math.PI; return angle; }
    static double Distance(RoadPoint a, RoadPoint b)
    { double x = a.X - b.X, z = a.Z - b.Z; return Math.Sqrt(x * x + z * z); }
    static ArcadeDrive ReadyDrive()
    {
        var drive = new ArcadeDrive { MaximumSpeed = 12 };
        drive.Step(1, 0, false, false, 1);
        return drive;
    }
    static ArcadeDrive DownhillDrive(double speed)
    {
        var drive = new ArcadeDrive { MaximumSpeed = speed, Style = DrivingStyle.Downhill };
        drive.Step(1, 0, false, false, 1);
        return drive;
    }
    static void Charge(ArcadeDrive drive, double target)
    {
        // Low-speed alternating turns keep this physics fixture within the start
        // straight on either map without teleporting or modifying drive state.
        for (int i = 0; i < 200 && drive.DriftCharge < target; i++)
            drive.Step(1, i % 40 < 20 ? .65 : -.65, false, true, .02);
        Check(drive.WallHits == 0 && drive.DriftCharge >= target, "charge fixture hit a wall or failed to charge");
    }
    static void Follow(ArcadeDrive drive, double dt)
    {
        var target = drive.Course.Sample(drive.Sample.Progress + 10).Position;
        double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
        drive.Step(1, Math.Max(-1, Math.Min(1, Angle(desired - drive.Heading) * 2.2)), false, false, dt);
    }
    static void ClearState(ArcadeDrive drive)
    {
        Check(drive.BoostRemaining == 0 && drive.DriftCharge == 0 && !drive.IsDrifting,
            "cancelled drive retained boost or drift");
        Check(drive.BoostTier == 0 && drive.BoostDuration == 0,
            "cancelled drive retained presentation boost state");
    }
    public static int Main()
    {
        Test("base kart reaches ninety percent of 26m/s within 0.8 seconds", () => {
            var drive = new ArcadeDrive();
            var start = drive.Position;
            drive.Step(1, 0, false, false, .8);
            Check(drive.Speed >= 23.4 && drive.Speed <= 26, "launch remained slow: " + drive.Speed);
            Check(Distance(start, drive.Position) >= 7, "velocity response did not deliver launch acceleration");
            Near(drive.MaximumSpeed, 26, .001, "default speed does not match new kart");
        });
        Test("economy supplies faster base kart and keeps upgrade increment", () => {
            var economy = new Economy();
            var drive = new ArcadeDrive { MaximumSpeed = economy.MaxSpeed };
            drive.Step(1, 0, false, false, 1);
            Near(drive.Speed, 26, .001, "economy still supplies old top speed");
            economy.Levels[0] = 1;
            drive.MaximumSpeed = economy.MaxSpeed;
            drive.Step(1, 0, false, false, .2);
            Near(drive.Speed, 28.5, .001, "upgrade no longer adds 2.5m/s");
        });
        Test("keyboard steering ramps and reverses without a heading snap", () => {
            var drive = ReadyDrive();
            double before = drive.Heading;
            drive.Step(1, 1, false, false, .02);
            double firstTurn = Angle(drive.Heading - before);
            Check(firstTurn > 0 && firstTurn < .012, "first keypress snapped heading: " + firstTurn);
            Check(drive.SteeringInput > 0 && drive.SteeringInput < .4,
                "presentation steering did not expose ramp");
            drive.Step(1, 1, false, false, .12);
            Check(Angle(drive.Heading - before) > .10, "steering response is sluggish");
            before = drive.Heading;
            drive.Step(1, -1, false, false, .02);
            Check(Angle(drive.Heading - before) >= 0, "one reverse frame instantly flipped wheel direction");
            drive.Step(1, -1, false, false, .16);
            Check(drive.SteeringInput < -.7, "reversal took too long to respond");
        });
        Test("short charged drift gives immediate weak boost and one trigger", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            double before = drive.Speed;
            drive.Step(1, 0, false, false, .02);
            Check(drive.Speed >= before + 4.9, "weak boost lacks immediate 5m/s kick");
            Near(drive.BoostRemaining, 1.08, .005, "weak boost duration wrong");
            Check(drive.BoostTier == 1, "weak boost tier wrong");
            Near(drive.BoostDuration, 1.1, .001, "weak presentation duration wrong");
            Follow(drive, .02);
            Check(drive.BoostCount == 1 && drive.DriftCharge == 0, "release retriggered boost");
        });
        Test("fully charged drift gives stronger kick and longer boost", () => {
            var drive = ReadyDrive();
            Charge(drive, .8);
            double before = drive.Speed;
            drive.Step(1, 0, false, false, .02);
            Check(drive.Speed >= before + 7.9, "strong boost lacks immediate 8m/s kick");
            Near(drive.BoostRemaining, 1.68, .005, "strong boost duration wrong");
            Check(drive.BoostTier == 2, "strong boost tier wrong");
            Near(drive.BoostDuration, 1.7, .001, "strong presentation duration wrong");
        });
        Test("boost expiry carries speed then sheds excess gradually", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            drive.Step(1, 0, false, false, .02);
            double peak = drive.Speed;
            while (drive.BoostRemaining > 0) {
                Follow(drive, .02);
                peak = Math.Max(peak, drive.Speed);
            }
            Check(drive.WallHits == 0, "expiry fixture hit wall");
            Near(peak, 21, .01, "weak boost cap did not deliver extra 9m/s");
            double before = drive.Speed;
            Follow(drive, .02);
            Check(drive.Speed > drive.MaximumSpeed + 2, "boost expiry snapped down to base speed");
            Check(before - drive.Speed > 0 && before - drive.Speed < .5,
                "boost expiry deceleration was abrupt");
            Check(drive.BoostTier == 0 && drive.BoostDuration == 0,
                "expired boost retained HUD state");
        });
        Test("strong boost raises reachable speed above weak boost", () => {
            var drive = ReadyDrive();
            Charge(drive, .8);
            drive.Step(1, 0, false, false, .02);
            for (int i = 0; i < 18 && drive.Speed < 26; i++) Follow(drive, .02);
            Check(drive.WallHits == 0, "strong boost fixture hit wall");
            Near(drive.Speed, 26, .01, "strong boost cap did not deliver extra 14m/s");
        });
        Test("releasing another drift refreshes boost without stacking speed past its cap", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            drive.Step(1, 0, false, false, .02);
            for (int i = 0; i < 100 && drive.DriftCharge < .4; i++)
                drive.Step(1, -.65, false, true, .02);
            Check(drive.WallHits == 0 && drive.DriftCharge >= .4 && drive.BoostRemaining > 0,
                "repeat-boost setup did not remain on the road with active boost");
            drive.Step(1, 0, false, false, .02);
            Check(drive.BoostCount == 2, "second completed drift did not refresh boost");
            Check(drive.Speed <= 21, "repeated impulse escaped boost cap: " + drive.Speed);
        });
        Test("stationary and straight drift cannot farm boost", () => {
            var idle = new ArcadeDrive();
            idle.Step(0, 1, false, true, 2);
            idle.Step(0, 0, false, false, .02);
            Check(idle.BoostCount == 0 && idle.DriftCharge == 0 && idle.TotalProgress == 0,
                "stationary drift farmed rewards");
            var straight = ReadyDrive();
            straight.Step(1, 0, false, true, .8);
            straight.Step(1, 0, false, false, .02);
            Check(straight.BoostCount == 0 && straight.DriftCharge == 0, "straight drift farmed boost");
            var tap = ReadyDrive();
            tap.Step(1, .65, false, true, .12);
            tap.Step(1, 0, false, false, .02);
            Check(tap.BoostCount == 0, "tiny drift tap bypassed charge threshold");
        });
        Test("braking cancels active boost and boosted production immediately", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            drive.Step(1, 0, false, false, .02);
            Check(drive.BoostRemaining > 0 && drive.LastBoostedRewardDistance > 0, "setup did not boost");
            double speed = drive.Speed;
            drive.Step(1, 0, true, false, .02);
            Check(drive.Speed < speed && drive.LastBoostedRewardDistance == 0, "brake retained acceleration or bonus");
            ClearState(drive);
        });
        Test("recovery and stop clear boost steering and reward pulse", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            drive.Step(1, 0, false, false, .02);
            double progress = drive.TotalProgress;
            drive.Recover();
            ClearState(drive);
            Check(drive.SteeringInput == 0 && drive.Speed == 0 &&
                drive.LastRewardDistance == 0 && drive.TotalProgress == progress,
                "recovery retained input or changed earned progress");
            drive.Step(1, .5, false, false, .2);
            drive.Stop();
            Check(drive.SteeringInput == 0 && drive.LastRewardDistance == 0,
                "stop retained wheel input or reward pulse");
        });
        Test("braking a charged drift cannot release boost", () => {
            var drive = ReadyDrive();
            Charge(drive, .4);
            drive.Step(1, 0, true, false, .02);
            Check(drive.BoostCount == 0, "brake release fired boost");
            ClearState(drive);
        });
        Test("large caller frames preserve steering and boost integration", () => {
            var a = ReadyDrive(); var b = ReadyDrive();
            a.Step(1, .65, false, true, .6);
            for (int i = 0; i < 30; i++) b.Step(1, .65, false, true, .02);
            a.Step(1, 0, false, false, .2);
            for (int i = 0; i < 10; i++) b.Step(1, 0, false, false, .02);
            Near(Distance(a.Position, b.Position), 0, .00001, "frame partition changed position");
            Near(a.Speed, b.Speed, .00001, "frame partition changed boost speed");
            Near(a.BoostRemaining, b.BoostRemaining, .00001, "frame partition changed boost duration");
            Near(a.TotalProgress, b.TotalProgress, .00001, "frame partition changed reward");
        });
        Test("ten and twenty millisecond frames keep launch and steering close", () => {
            var a = new ArcadeDrive(); var b = new ArcadeDrive();
            for (int i = 0; i < 50; i++) a.Step(1, .15, false, false, .02);
            for (int i = 0; i < 100; i++) b.Step(1, .15, false, false, .01);
            Near(a.Speed, b.Speed, .2, "launch changes with frame rate");
            Near(Angle(a.Heading - b.Heading), 0, .015, "steering changes with frame rate");
            Near(Distance(a.Position, b.Position), 0, .4, "position changes excessively with frame rate");
        });
        Test("downhill steering and velocity carry more inertia at similar launch speed", () => {
            var kart = new ArcadeDrive(); kart.Step(1, 0, false, false, 1);
            var downhill = DownhillDrive(26);
            Near(downhill.Speed, kart.Speed, .3, "comparison launch speeds are too far apart");
            kart.Step(1, .7, false, false, .12);
            downhill.Step(1, .7, false, false, .12);
            Check(downhill.SteeringInput < kart.SteeringInput - .07,
                "downhill steering has no additional weight");
            Check(Math.Abs(Angle(kart.Heading - downhill.Heading)) > .01,
                "styles have indistinguishable turning response");
            // Observe velocity once the slower wheel response has developed;
            // the first few frames mainly measure steering lag, not grip.
            kart.Step(1, .7, false, false, .12);
            downhill.Step(1, .7, false, false, .12);
            double kartSlip = Math.Abs(Angle(kart.Heading - Math.Atan2(kart.Velocity.X, kart.Velocity.Z)));
            double downhillSlip = Math.Abs(Angle(downhill.Heading - Math.Atan2(downhill.Velocity.X, downhill.Velocity.Z)));
            Check(downhillSlip > kartSlip, "downhill velocity has no additional inertia");
        });
        Test("downhill slide completion counts skill without generating a boost", () => {
            var drive = DownhillDrive(12);
            Charge(drive, .4);
            double before = drive.Speed;
            drive.Step(1, 0, false, false, .02);
            Check(drive.DriftCount == 1 && drive.SkillCount == 1,
                "controlled slide did not count once");
            Check(drive.BoostCount == 0 && drive.BoostRemaining == 0 && drive.Speed <= before + 1,
                "downhill release generated a kart boost");
            Check(drive.LastBoostedRewardDistance == 0, "downhill received boosted production");
            Follow(drive, .1);
            Check(drive.DriftCount == 1, "slide completion repeated every frame");
        });
        Test("downhill Space overrides full throttle and slows actual movement", () => {
            var drift = DownhillDrive(26); var normal = DownhillDrive(26);
            double speed = drift.Speed;
            double velocity = Distance(drift.Velocity, new RoadPoint(0, 0));
            RoadPoint start = drift.Position;
            double progress = drift.TotalProgress;
            drift.Step(1, 0, false, true, .02);
            Check(drift.Speed < speed && Distance(drift.Velocity, new RoadPoint(0, 0)) < velocity,
                "Space did not immediately reduce both displayed speed and road velocity");
            drift.Step(1, 0, false, true, .48);
            normal.Step(1, 0, false, false, .5);
            Check(drift.WallHits == 0 && normal.WallHits == 0, "slowdown comparison hit a wall");
            Check(drift.Speed < speed - 3 && drift.Speed > speed - 6,
                "half-second drift did not scrub speed smoothly: " + speed + " -> " + drift.Speed);
            Check(Distance(start, drift.Position) < Distance(start, normal.Position) - 1,
                "Space changed the speed display without reducing travel distance");
            Check(drift.TotalProgress - progress < normal.TotalProgress - progress - 1,
                "Space slowdown did not reduce distance-based production");
            Console.WriteLine("  DOWNHILL half-second Space m/s: " + speed.ToString("F3") + " -> " +
                drift.Speed.ToString("F3") + "; full throttle: " + normal.Speed.ToString("F3"));
        });
        Test("downhill Space slows below the slide threshold and can stop", () => {
            var drive = new ArcadeDrive { Style = DrivingStyle.Downhill };
            drive.Step(1, 0, false, false, .12);
            Check(drive.Speed > 3 && drive.Speed < 6, "low-speed fixture missed the slide threshold");
            double before = drive.Speed;
            drive.Step(1, 0, false, true, .1);
            Check(drive.Speed < before, "Space requires active slide charge to slow the car");
            drive.Step(1, 0, false, true, 2);
            Near(drive.Speed, 0, 1e-12, "holding Space leaves a hidden minimum speed");
            Near(Distance(drive.Velocity, new RoadPoint(0, 0)), 0, 1e-12,
                "road motion continued after Space stopped the car");
            Check(drive.DriftCount == 0 && drive.BoostCount == 0, "low-speed Space farmed skills or boosts");
        });
        Test("downhill releasing Space resumes normal acceleration without a boost", () => {
            var drive = DownhillDrive(26);
            double entry = drive.Speed;
            drive.Step(1, .3, false, true, .3);
            double exit = drive.Speed;
            Check(exit < entry, "Space did not slow before release");
            drive.Step(1, 0, false, false, .02);
            Check(drive.Speed > exit && drive.Speed < exit + 1, "release did not return to normal engine pull");
            Check(drive.BoostCount == 0 && drive.BoostRemaining == 0 && drive.LastBoostedRewardDistance == 0,
                "downhill Space release generated a kart boost");
        });
        Test("downhill service brake stays stronger and takes priority over Space", () => {
            var drift = DownhillDrive(26); var brake = DownhillDrive(26); var both = DownhillDrive(26);
            double entry = drift.Speed;
            drift.Step(1, 0, false, true, .5);
            brake.Step(1, 0, true, false, .5);
            both.Step(1, 0, true, true, .5);
            Check(drift.Speed < entry && drift.Speed > brake.Speed + 5, "Space slowdown is absent or exceeds braking");
            Near(both.Speed, brake.Speed, 1e-10, "Space stacked with service braking");
        });
        Test("downhill full throttle with Space held at rest cannot launch or charge", () => {
            var drive = new ArcadeDrive { Style = DrivingStyle.Downhill };
            RoadPoint start = drive.Position;
            drive.Step(1, 1, false, true, 1);
            Near(drive.Speed, 0, 0, "throttle overrode Space at rest");
            Near(Distance(start, drive.Position), 0, 0, "Space at rest moved the car");
            Check(drive.DriftCharge == 0 && drive.DriftCount == 0 && drive.TotalProgress == 0,
                "stationary Space farmed skills or production");
        });
        Test("downhill Space preserves slip while slowing the car", () => {
            var drive = DownhillDrive(26);
            double entry = drive.Speed;
            drive.Step(1, .65, false, true, .3);
            double slip = Math.Abs(Angle(drive.Heading - Math.Atan2(drive.Velocity.X, drive.Velocity.Z)));
            Check(drive.WallHits == 0 && drive.Speed < entry, "drift fixture did not slow cleanly");
            Check(slip > .08 && drive.IsDrifting, "Space slowdown snapped velocity onto the car heading");
        });
        Test("downhill Space slowdown is stable across caller frame sizes", () => {
            var large = DownhillDrive(26); var small = DownhillDrive(26); var fine = DownhillDrive(26);
            large.Step(1, .3, false, true, .5);
            for (int i = 0; i < 25; i++) small.Step(1, .3, false, true, .02);
            for (int i = 0; i < 50; i++) fine.Step(1, .3, false, true, .01);
            Check(large.Speed < 24, "Space slowdown was missing at every frame size");
            Near(large.Speed, small.Speed, 1e-10, "large caller frame changed drift speed");
            Near(Distance(large.Position, small.Position), 0, 1e-10, "large caller frame changed drift motion");
            Near(large.Speed, fine.Speed, .05, "drift speed depends on frame rate");
            Near(Distance(large.Position, fine.Position), 0, .15, "drift motion depends on frame rate");
        });
        Test("downhill braking into a turn scrubs speed and initiates a controlled slide", () => {
            var drive = DownhillDrive(26);
            double before = drive.Speed;
            drive.Step(1, .8, true, false, .5);
            Check(drive.Speed < before - 5 && drive.Speed > 6, "corner braking did not scrub controlled speed");
            Check(drive.IsDrifting && drive.DriftCharge >= .32 && drive.WallHits == 0,
                "brake turn did not charge a clean slide");
            drive.Step(1, 0, false, false, .02);
            Check(drive.DriftCount == 1 && drive.BoostCount == 0,
                "brake slide exit did not complete downhill skill");
        });
        Test("downhill cannot farm slide skills while stationary or driving straight", () => {
            var drive = DownhillDrive(12);
            drive.Stop();
            drive.Step(0, 1, false, true, 1);
            drive.Step(0, 0, false, false, .02);
            Check(drive.DriftCount == 0, "stationary slide awarded skill");
            drive.Step(1, 0, false, true, 1);
            drive.Step(1, 0, false, false, .02);
            Check(drive.DriftCount == 0 && drive.BoostCount == 0,
                "straight slide awarded skill");
        });
        Test("downhill recovery cancels pending skill and reset preserves selection", () => {
            var drive = DownhillDrive(12);
            Charge(drive, .4);
            drive.Recover();
            drive.Step(1, 0, false, false, .02);
            Check(drive.DriftCount == 0, "recovery completed cancelled slide");
            drive.Step(1, 0, false, false, 1);
            Charge(drive, .4);
            drive.Step(1, 0, false, false, .02);
            Check(drive.DriftCount == 1, "setup did not complete slide");
            drive.Reset();
            Check(drive.DriftCount == 0 && drive.SkillCount == 0,
                "reset retained prior run skill");
            Check(drive.Style == DrivingStyle.Downhill,
                "reset changed style selection");
            ClearState(drive);
        });
        Test("wall impacts cancel pending skill and boost in both styles", () => {
            // A downhill handbrake can now stop the car; enter at normal driving
            // speed so the charged slide still reaches the wall before stopping.
            foreach (var drive in new[] { ReadyDrive(), DownhillDrive(26) }) {
                Charge(drive, .4);
                int skills = (int)drive.SkillCount;
                for (int i = 0; i < 300 && drive.WallHits == 0; i++)
                    drive.Step(1, 1, false, true, .02);
                Check(drive.WallHits > 0, "collision setup did not hit wall");
                ClearState(drive);
                drive.Step(1, 0, false, false, .02);
                Check(drive.SkillCount == skills && drive.LastBoostedRewardDistance == 0,
                    "collision awarded cancelled slide or boost");
            }
            var boosted = ReadyDrive();
            Charge(boosted, .4);
            boosted.Step(1, 0, false, false, .02);
            for (int i = 0; i < 100 && boosted.WallHits == 0; i++)
                boosted.Step(1, 1, false, false, .02);
            Check(boosted.WallHits > 0, "boost collision setup missed wall");
            ClearState(boosted);
            Check(boosted.LastBoostedRewardDistance == 0, "wall impact retained boost production");
        });
        Test("downhill slide released into a wall does not award a clean-slide skill", () => {
            var probe = DownhillDrive(26);
            Charge(probe, .4);
            int steps = 0;
            while (probe.WallHits == 0 && steps < 200) {
                probe.Step(1, 1, false, true, .02);
                steps++;
            }
            Check(probe.WallHits > 0, "release collision setup missed wall");
            var drive = DownhillDrive(26);
            Charge(drive, .4);
            for (int i = 1; i < steps; i++) drive.Step(1, 1, false, true, .02);
            Check(drive.WallHits == 0 && drive.DriftCharge >= .32, "release fixture lost clean charged approach");
            drive.Step(1, 1, false, false, .02);
            Check(drive.WallHits > 0, "drift release did not reach the wall");
            Check(drive.DriftCount == 0, "release into a wall awarded a clean-slide skill");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
