using System;
using CottonCircuit;

public static class DrivingTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Near(double actual, double expected, double tolerance, string message)
    { Check(Math.Abs(actual - expected) <= tolerance, message + " (got " + actual + ", wanted " + expected + ")"); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static double Distance(RoadPoint a, RoadPoint b)
    { double x = a.X - b.X, z = a.Z - b.Z; return Math.Sqrt(x * x + z * z); }
    static double TurnAngle(double angle)
    { while (angle > Math.PI) angle -= 2 * Math.PI; while (angle < -Math.PI) angle += 2 * Math.PI; return angle; }
    static void Follow(ArcadeDrive drive, double seconds)
    {
        int steps = (int)(seconds / .02);
        for (int i = 0; i < steps; i++)
        {
            var target = drive.Course.Sample(drive.Sample.Progress + 10).Position;
            double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
            double steering = Math.Max(-1, Math.Min(1, TurnAngle(desired - drive.Heading) * 1.8));
            drive.Step(1, steering, false, false, .02);
        }
    }
    static void SteerToward(ArcadeDrive drive, RoadPoint target, double desiredSpeed)
    {
        double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
        double steering = Math.Max(-1, Math.Min(1, TurnAngle(desired - drive.Heading) * 2.2));
        drive.Step(drive.Speed < desiredSpeed ? .75 : 0, steering,
            drive.Speed > desiredSpeed + .8, false, .02);
    }

    public static int Main()
    {
        Test("course samples close smoothly at a finite length", () => {
            var c = RaceCourse.Shared;
            Check(c.Length >= 520 && c.Length <= 620, "implausible starter course length");
            var start = c.Sample(0);
            var finish = c.Sample(c.Length);
            Near(Distance(start.Position, finish.Position), 0, 0.001, "loop is open");
            Near(Distance(start.Tangent, finish.Tangent), 0, 0.001, "seam turns sharply");
            for (int i = 0; i < 100; i++) {
                var s = c.Sample(c.Length * i / 100);
                Check(Math.Sqrt(s.Position.X * s.Position.X + s.Position.Z * s.Position.Z) + s.HalfWidth <= 130,
                    "road escapes machine rim");
                Near(Math.Sqrt(s.Tangent.X * s.Tangent.X + s.Tangent.Z * s.Tangent.Z), 1, 0.001, "tangent not unit length");
            }
        });
        Test("projection preserves road side and shortcut mapping", () => {
            var c = RaceCourse.Shared;
            var s = c.Sample(c.Length * 0.16);
            var right = new RoadPoint(s.Tangent.Z, -s.Tangent.X);
            var p = c.Project(new RoadPoint(s.Position.X + right.X * 2, s.Position.Z + right.Z * 2));
            Check(!p.IsShortcut && p.Lateral > 1.8 && p.Lateral < 2.2, "road side lost");
            Check(Math.Abs(p.Progress - s.Progress) < 1.5, "nearby point projected far around loop");
            Check(c.ShortcutPoints.Length >= 3, "shortcut missing");
            var shortcut = c.Project(c.ShortcutPoints[c.ShortcutPoints.Length / 2]);
            Check(shortcut.IsShortcut && shortcut.Progress > 0 && shortcut.Progress < c.Length,
                "shortcut is not connected to main progress");
        });
        Test("kart clearance selects the wider valid ribbon at a shortcut overlap", () => {
            // Independently measured segment distances: main 5.026m, shortcut 2.942m.
            // The closer shortcut fits a point, but only the main fits the .8m kart.
            var p = new RoadPoint(79.4, 25.0);
            var withoutClearance = RaceCourse.Shared.Project(p);
            Check(withoutClearance.IsShortcut && Math.Abs(withoutClearance.Lateral) > RaceCourse.ShortcutHalfWidth - .8,
                "fixture no longer overlaps a narrow shortcut edge");
            var s = RaceCourse.Shared.Project(p, .8);
            Check(!s.IsShortcut && Math.Abs(s.Lateral) < s.HalfWidth - .8,
                "narrow shortcut displaced a valid main-road position");
        });
        foreach (int map in new[] { 0, 1 })
        foreach (DrivingStyle style in new[] { DrivingStyle.Kart, DrivingStyle.Downhill })
        foreach (int side in new[] { -1, 1 })
        {
            int selectedMap = map, selectedSide = side;
            DrivingStyle selectedStyle = style;
            Test("map " + (map + 1) + " " + style + " drives the expanded main lane on side " + side, () => {
                var drive = new ArcadeDrive(RaceCourse.ForMap(selectedMap)) { Style = selectedStyle };
                double distanceInNewLane = 0;
                for (int step = 0; step < 1600 && drive.Sample.Progress < 65; step++)
                {
                    var ahead = drive.Course.Sample(drive.Sample.Progress + 8);
                    var right = new RoadPoint(ahead.Tangent.Z, -ahead.Tangent.X);
                    var before = drive.Position;
                    SteerToward(drive, ahead.Position + right * (selectedSide * 5.5), 8);
                    Check(drive.WallHits == 0, "expanded main lane hit a wall at lateral " + drive.Sample.Lateral);
                    if (drive.Sample.Lateral * selectedSide > 5)
                        distanceInNewLane += Distance(before, drive.Position);
                }
                Check(distanceInNewLane > 20, "kart did not traverse the newly opened main lane");
            });
            Test("map " + (map + 1) + " " + style + " still collides beyond the expanded main edge on side " + side, () => {
                var drive = new ArcadeDrive(RaceCourse.ForMap(selectedMap)) { Style = selectedStyle };
                for (int step = 0; step < 700 && drive.WallHits == 0; step++)
                {
                    var ahead = drive.Course.Sample(drive.Sample.Progress + 8);
                    var right = new RoadPoint(ahead.Tangent.Z, -ahead.Tangent.X);
                    SteerToward(drive, ahead.Position + right * (selectedSide * 8.5), 8);
                }
                Check(drive.WallHits > 0, "kart escaped the expanded road without a wall collision");
                Near(drive.Sample.Lateral * selectedSide, 6.4, .04,
                    "wall did not keep the kart's .8m half-width inside the 7.2m road edge");
            });
        }
        Test("throttle moves freely and right steering turns clockwise", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            var start = d.Position;
            d.Step(1, 0, false, false, 1);
            Check(Distance(start, d.Position) > 2 && d.Speed > 4, "throttle did not accelerate");
            double heading = d.Heading;
            d.Step(1, 0.6, false, false, .3);
            Check(TurnAngle(d.Heading - heading) > .08, "right steer did not turn right");
            Check(Distance(start, d.Position) > 4, "steering did not move kart freely");
            d.Step(0, 0, true, false, .5);
            Check(d.Speed < 8, "brake did not slow kart");
        });
        Test("sustained drift releases one boost and braking cancels it", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            d.Step(1, .55, false, true, .5);
            Check(d.IsDrifting && d.DriftCharge > .25, "turning drift did not charge");
            d.Step(1, 0, false, false, .02);
            Check(!d.IsDrifting && d.BoostCount == 1 && d.BoostRemaining > 0,
                "drift release did not start boost");
            d.Step(1, 0, false, false, .1);
            Check(d.BoostCount == 1, "held release retriggered boost");
            d.Step(1, 0, true, false, .02);
            Check(d.BoostRemaining == 0, "braking kept boost active");
        });
        Test("boosted progress is reported separately for exact production bonus", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            d.Step(1, .55, false, true, .5);
            d.Step(1, 0, false, false, .02);
            Check(d.LastBoostedRewardDistance > 0 &&
                d.LastBoostedRewardDistance <= d.LastRewardDistance,
                "boosted distance was not reported");
            d.Step(1, 0, true, false, .02);
            Check(d.LastBoostedRewardDistance == 0, "braking reported boosted production");
        });
        Test("walls slide and recovery keeps earned progress without paying again", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            double rewarded = d.TotalProgress;
            for (int i = 0; i < 120; i++) d.Step(1, 1, false, false, .02);
            Check(d.WallHits > 0, "wall never contacted");
            var wall = d.Course.Project(d.Position);
            Check(Math.Abs(wall.Lateral) <= wall.HalfWidth + .05, "kart escaped road");
            int hits = d.WallHits;
            d.Recover();
            Check(d.WallHits == hits && d.TotalProgress >= rewarded && d.LastRewardDistance == 0,
                "recovery lost statistics or paid reward");
            Check(Math.Abs(d.Sample.Lateral) < .1 && d.Speed == 0 && d.BoostRemaining == 0,
                "recovery did not place a stopped kart on centerline");
        });
        Test("wall impact keeps kart clearance and does not auto-align steering", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            double before = d.Heading;
            for (int i = 0; i < 150 && d.WallHits == 0; i++) {
                before = d.Heading;
                d.Step(1, 1, false, false, .02);
            }
            Check(d.WallHits > 0, "setup missed wall");
            Check(Math.Abs(d.Sample.Lateral) <= d.Sample.HalfWidth - .75,
                "kart center did not leave body clearance at barrier");
            Check(Math.Abs(TurnAngle(d.Heading - before)) < .08,
                "wall snapped heading to the road tangent");
            double progress = d.TotalProgress;
            d.Step(1, 0, false, false, 25);
            Check(d.Laps == 0 && d.TotalProgress - progress < d.Course.Length / 3,
                "holding throttle at wall drove a lap automatically");
        });
        Test("shortcut centerline and joins stay inside a drivable ribbon", () => {
            var c = RaceCourse.Shared;
            for (int i = 0; i < c.ShortcutPoints.Length; i++) {
                var s = c.Project(c.ShortcutPoints[i]);
                Check(Math.Abs(s.Lateral) <= s.HalfWidth - .8,
                    "shortcut centerline projects into a wall at point " + i);
            }
        });
        Test("kart steers through shortcut joints without walls or free progress", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            var c = d.Course;
            double entry = c.Project(c.ShortcutPoints[0]).Progress;
            double exit = c.Project(c.ShortcutPoints[c.ShortcutPoints.Length - 1]).Progress;
            for (int i = 0; i < 6000 && d.Sample.Progress < entry - 10; i++)
                SteerToward(d, c.Sample(d.Sample.Progress + 8).Position, 8);
            Check(d.Sample.Progress >= entry - 10 && d.WallHits == 0, "approach did not reach shortcut cleanly");
            bool entered = false, merged = false;
            double largestReward = 0;
            for (int i = 0; i < 2000; i++) {
                int nearest = 0;
                double distance = double.PositiveInfinity;
                for (int j = 0; j < c.ShortcutPoints.Length; j++) {
                    double candidate = Distance(d.Position, c.ShortcutPoints[j]);
                    if (candidate < distance) { distance = candidate; nearest = j; }
                }
                RoadPoint target = nearest >= c.ShortcutPoints.Length - 5
                    ? c.Sample(exit + 7).Position
                    : c.ShortcutPoints[Math.Min(c.ShortcutPoints.Length - 1, nearest + 4)];
                SteerToward(d, target, 8);
                if (d.Sample.IsShortcut) entered = true;
                if (d.LastRewardDistance > largestReward) largestReward = d.LastRewardDistance;
                Check(d.WallHits == 0, "kart hit a wall crossing shortcut ribbon");
                if (entered && !d.Sample.IsShortcut && d.Sample.Progress > exit + 3) {
                    merged = true;
                    break;
                }
            }
            Check(entered && merged, "kart failed to enter and merge from shortcut");
            Check(largestReward < .6, "projection switch granted discontinuous progress");
            Check(d.TotalProgress > exit - 4 && d.TotalProgress <= d.Sample.Progress + .1,
                "shortcut traversal earned missing or free progress: total " + d.TotalProgress +
                ", exit " + exit + ", projected " + d.Sample.Progress);
            bool crossedSeam = false;
            for (int i = 0; i < 6500; i++) {
                double before = d.Sample.Progress;
                SteerToward(d, c.Sample(before + 8).Position, 8);
                if (before > c.Length - 2 && d.Sample.Progress < 2) { crossedSeam = true; break; }
            }
            Check(crossedSeam, "shortcut route did not finish a circuit");
            Check(d.Laps == 1 && d.BestLapSeconds > 0,
                "valid seam crossing after shortcut did not complete lap");
        });
        Test("driver can steer away after stopping against a wall", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            for (int i = 0; i < 150 && d.WallHits == 0; i++) d.Step(1, 1, false, false, .02);
            Check(d.WallHits > 0, "setup missed wall");
            d.Stop();
            double heading = d.Heading;
            d.Step(1, -1, false, false, 2);
            Check(Math.Abs(TurnAngle(d.Heading - heading)) > .4,
                "stopped kart cannot steer away from wall");
        });
        Test("course follower earns a lap but reverse oscillation never pays twice", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            Follow(d, 65);
            Check(d.Laps >= 1, "course follower could not finish a lap");
            Check(d.TotalProgress > d.Course.Length && d.BestLapSeconds > 0,
                "lap distance or clock missing");
            double highWater = d.TotalProgress;
            d.Stop();
            for (int i = 0; i < 80; i++) d.Step(0, (i % 20 < 10 ? 1 : -1), true, false, .02);
            Check(d.TotalProgress == highWater && d.LastRewardDistance == 0,
                "stationary oscillation paid again");
        });
        Test("driving back along visited road earns no new progress", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            Follow(d, 4);
            d.Recover();
            int reverseSteps = 0;
            for (int i = 0; i < 1000; i++) {
                double previous = d.Sample.Progress;
                d.Step(.25, 1, false, false, .02);
                double change = d.Sample.Progress - previous;
                if (change < -.001 && change > -d.Course.Length / 2) {
                    reverseSteps++;
                    Check(d.LastRewardDistance == 0, "backward road motion earned reward");
                }
            }
            Check(reverseSteps > 20, "test did not drive backward on the course");
        });
        Test("invalid input cannot move or pay reward", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            var start = d.Position;
            d.Step(double.NaN, 1, false, false, 1);
            d.Step(1, double.PositiveInfinity, false, false, 1);
            d.Step(1, 0, false, false, double.NaN);
            Check(Distance(start, d.Position) == 0 && d.TotalProgress == 0,
                "invalid input moved or rewarded kart");
        });
        Test("large frame uses the same stable driving steps", () => {
            var a = new ArcadeDrive(RaceCourse.Shared);
            var b = new ArcadeDrive(RaceCourse.Shared);
            a.Step(1, .25, false, false, .8);
            for (int i = 0; i < 40; i++) b.Step(1, .25, false, false, .02);
            Near(Distance(a.Position, b.Position), 0, .001, "frame partition changed position");
            Near(a.TotalProgress, b.TotalProgress, .001, "frame partition changed production progress");
        });
        Test("recovery and replay do not pay for already visited road", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            Follow(d, 4);
            double before = d.TotalProgress;
            Check(before > 15, "setup did not drive forward");
            d.Recover();
            Check(d.LastRewardDistance == 0 && d.TotalProgress == before, "recovery changed reward");
            d.Step(0, 0, false, false, .2);
            Check(d.LastRewardDistance == 0 && d.TotalProgress == before, "idle recovered kart earned reward");
        });
        Test("stopping clears the current reward pulse", () => {
            var d = new ArcadeDrive(RaceCourse.Shared);
            d.Step(1, 0, false, false, 1);
            Check(d.LastRewardDistance > 0, "setup earned no reward");
            d.Stop();
            Check(d.LastRewardDistance == 0 && d.LastBoostedRewardDistance == 0,
                "stopped kart retained a production pulse");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
