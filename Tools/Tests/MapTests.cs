using System;
using CottonCircuit;

public static class MapTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }
    static double Distance(RoadPoint a, RoadPoint b)
    { double x = a.X - b.X, z = a.Z - b.Z; return Math.Sqrt(x * x + z * z); }
    static double Turn(double a)
    { while (a > Math.PI) a -= 2 * Math.PI; while (a < -Math.PI) a += 2 * Math.PI; return a; }
    static void Steer(ArcadeDrive drive, RoadPoint target, double speed)
    {
        double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
        drive.Step(drive.Speed < speed ? .75 : 0,
            Math.Max(-1, Math.Min(1, Turn(desired - drive.Heading) * 2.2)),
            drive.Speed > speed + .8, false, .02);
    }
    static void VerifyShortcut(RaceCourse course, DrivingStyle style, double laneOffset = 0)
    {
        var drive = new ArcadeDrive(course) { Style = style };
        double entry = course.Project(course.ShortcutPoints[0]).Progress;
        double exit = course.Project(course.ShortcutPoints[course.ShortcutPoints.Length - 1]).Progress;
        double shortcutLength = 0;
        for (int i = 1; i < course.ShortcutPoints.Length; i++)
            shortcutLength += Distance(course.ShortcutPoints[i - 1], course.ShortcutPoints[i]);
        Check(exit - entry - shortcutLength > 12, "shortcut saves no meaningful distance");
        for (int step = 0; step < 9000 && drive.Sample.Progress < entry - 10; step++)
            Steer(drive, course.Sample(drive.Sample.Progress + 8).Position, 8);
        Check(drive.Sample.Progress >= entry - 10 && drive.WallHits == 0, "shortcut approach hit a wall");
        bool entered = false, merged = false;
        double largestReward = 0, distanceInNewLane = 0;
        for (int step = 0; step < 9000; step++)
        {
            int nearest = 0; double distance = double.PositiveInfinity;
            for (int i = 0; i < course.ShortcutPoints.Length; i++)
            {
                double candidate = Distance(drive.Position, course.ShortcutPoints[i]);
                if (candidate < distance) { distance = candidate; nearest = i; }
            }
            int ahead = nearest; double lookAhead = 0;
            while (ahead < course.ShortcutPoints.Length - 1 && lookAhead < 8)
            {
                lookAhead += Distance(course.ShortcutPoints[ahead], course.ShortcutPoints[ahead + 1]);
                ahead++;
            }
            RoadPoint target = ahead == course.ShortcutPoints.Length - 1
                ? course.Sample(exit + 6).Position : course.ShortcutPoints[ahead];
            if (ahead < course.ShortcutPoints.Length - 1 && laneOffset != 0)
            {
                var tangent = course.ShortcutPoints[ahead + 1] - course.ShortcutPoints[ahead - 1];
                double length = Distance(tangent, new RoadPoint(0, 0));
                double taper = Math.Min(1, Math.Min(ahead / 18.0, (course.ShortcutPoints.Length - 1 - ahead) / 18.0));
                target += new RoadPoint(tangent.Z, -tangent.X) * (laneOffset * taper / length);
            }
            var before = drive.Position;
            Steer(drive, target, 8);
            entered |= drive.Sample.IsShortcut;
            if (drive.Sample.IsShortcut && laneOffset != 0 && drive.Sample.Lateral * Math.Sign(laneOffset) > 1.6)
                distanceInNewLane += Distance(before, drive.Position);
            largestReward = Math.Max(largestReward, drive.LastRewardDistance);
            Check(drive.WallHits == 0, "shortcut traversal hit a wall at " + drive.Sample.Progress.ToString("F2"));
            if (entered && !drive.Sample.IsShortcut && drive.Sample.Progress > exit + 3) { merged = true; break; }
        }
        Check(entered && merged, "kart failed to enter and merge from shortcut");
        if (laneOffset != 0) Check(distanceInNewLane > 8, "kart did not traverse the newly opened shortcut lane");
        Check(largestReward < .6, "shortcut awarded a discontinuous progress pulse");
        Check(drive.TotalProgress > exit - 4 && drive.TotalProgress <= drive.Sample.Progress + .1,
            "shortcut lost or awarded free progress: " + drive.TotalProgress + " at " + drive.Sample.Progress);
        for (int step = 0; step < 9000 && drive.Laps == 0; step++)
            Steer(drive, course.Sample(drive.Sample.Progress + 8).Position, 8);
        Check(drive.Laps == 1 && drive.BestLapSeconds > 0 && drive.WallHits == 0,
            "shortcut route failed a clean forward lap");
        Console.WriteLine("  shortcut saves " + (exit - entry - shortcutLength).ToString("F1") + "m");
    }
    static void VerifyLap(RaceCourse course, DrivingStyle style, double motor = 26)
    {
        var drive = new ArcadeDrive(course) { MaximumSpeed = motor, Style = style };
        double peak = 0; bool braked = false;
        for (int step = 0; step < 1600 && drive.Laps == 0; step++)
        {
            double throttle, steering; bool brake;
            CottonCircuit.Tests.CourseTestDriver.Input(drive, .8, out throttle, out steering, out brake);
            drive.Step(throttle, steering, brake, false, .02);
            peak = Math.Max(peak, drive.Speed); braked |= brake;
        }
        Check(drive.Laps == 1, "base kart did not finish a lap in 32 seconds");
        Check(drive.BestLapSeconds >= 16 && drive.BestLapSeconds <= 32,
            "lap time outside expected race duration: " + drive.BestLapSeconds);
        Check(drive.WallHits == 0, "main course follower hit " + drive.WallHits + " walls");
        Check(drive.TotalProgress >= drive.Course.Length - 1,
            "main course lost progress before finish: " + drive.TotalProgress.ToString("F2") +
            " of " + drive.Course.Length.ToString("F2") + " at sample " + drive.Sample.Progress.ToString("F2"));
        if (style == DrivingStyle.Downhill) Check(peak > 32 && braked, "downhill did not use sustained acceleration and corner braking");
        Console.WriteLine("  " + style + ", motor " + motor + ", length " + drive.Course.Length.ToString("F1") + "m, lap " + drive.BestLapSeconds.ToString("F2") + "s, peak " + peak.ToString("F1") + "m/s");
    }

    // Independent of RaceCourse.Project: distance from a point to a road centerline.
    static double CenterlineDistance(RoadPoint[] points, bool closed, RoadPoint position)
    {
        double best = double.PositiveInfinity;
        int count = closed ? points.Length : points.Length - 1;
        for (int i = 0; i < count; i++)
        {
            RoadPoint a = points[i], delta = points[(i + 1) % points.Length] - a;
            double span = delta.X * delta.X + delta.Z * delta.Z;
            double t = Math.Max(0, Math.Min(1, ((position.X - a.X) * delta.X + (position.Z - a.Z) * delta.Z) / span));
            best = Math.Min(best, Distance(position, a + delta * t));
        }
        return best;
    }
    // The main road near a junction as an open centerline, so probes skip the rest of the lap.
    static RoadPoint[] NearbyMain(RaceCourse course, RoadPoint around, double range)
    {
        var points = course.MainPoints;
        int count = points.Length, nearest = 0;
        for (int i = 1; i < count; i++)
            if (Distance(points[i], around) < Distance(points[nearest], around)) nearest = i;
        int back = 0, ahead = 0;
        while (back < count / 2 && Distance(points[(nearest - back - 1 + count) % count], around) <= range) back++;
        while (ahead < count / 2 && Distance(points[(nearest + ahead + 1) % count], around) <= range) ahead++;
        var nearby = new RoadPoint[back + ahead + 3];
        for (int i = 0; i < nearby.Length; i++) nearby[i] = points[(nearest - back - 1 + i + count) % count];
        return nearby;
    }
    // Positive inside the grass, negative on painted road.
    static double GrassDepth(RoadPoint[] main, RoadPoint[] shortcut, RoadPoint point)
    {
        return Math.Min(CenterlineDistance(main, false, point) - RaceCourse.MainHalfWidth,
            CenterlineDistance(shortcut, false, point) - RaceCourse.ShortcutHalfWidth);
    }
    // Grass reaches a round body through its rim; 256 rim samples lie 2cm apart,
    // so the true deepest grass is at most 1cm deeper than the sampled value.
    static double DeepestGrass(RoadPoint[] main, RoadPoint[] shortcut, RoadPoint center, double radius)
    {
        double deepest = GrassDepth(main, shortcut, center);
        for (int i = 0; i < 256; i++)
        {
            double angle = Math.PI * 2 * i / 256;
            deepest = Math.Max(deepest, GrassDepth(main, shortcut, center + new RoadPoint(Math.Sin(angle), Math.Cos(angle)) * radius));
        }
        return deepest;
    }
    // The shortcut ribbon overlaps the main ribbon at both ends and leaves a grass
    // island between them. Around each island tip, a kart whose body is on painted
    // road must drive freely, and a kart whose body overlaps the grass must be stopped.
    // Bodies within 2cm of the grass either way are left to rounding.
    static void VerifySeams(RaceCourse course)
    {
        int first = -1, last = -1;
        for (int i = 0; i < course.ShortcutPoints.Length; i++)
            if (CenterlineDistance(course.MainPoints, true, course.ShortcutPoints[i]) >
                RaceCourse.MainHalfWidth + RaceCourse.ShortcutHalfWidth)
            { if (first < 0) first = i; last = i; }
        Check(first > 0 && last > first, "shortcut leaves no grass island");
        int phantom = 0, leaks = 0, seam = 0;
        string example = null;
        foreach (int tip in new[] { first, last })
        {
            var main = NearbyMain(course, course.ShortcutPoints[tip], 40);
            for (double x = -9; x <= 9; x += .3)
                for (double z = -9; z <= 9; z += .3)
                {
                    var point = course.ShortcutPoints[tip] + new RoadPoint(x, z);
                    double center = GrassDepth(main, course.ShortcutPoints, point);
                    double grass = center >= .02 || center <= -.84 ? center : DeepestGrass(main, course.ShortcutPoints, point, .8);
                    bool fits = course.Fits(point, .8);
                    if (!fits && grass <= -.03)
                    {
                        phantom++;
                        if (example == null) example = "(" + point.X.ToString("F1") + ", " + point.Z.ToString("F1") + ")";
                    }
                    if (fits && grass >= .02) leaks++;
                    if (grass <= -.03 && CenterlineDistance(main, false, point) > RaceCourse.MainHalfWidth - .8 &&
                        CenterlineDistance(course.ShortcutPoints, false, point) > RaceCourse.ShortcutHalfWidth - .8) seam++;
                }
        }
        Check(phantom == 0, phantom + " kart positions on painted road hit an invisible wall, e.g. " + example);
        Check(leaks == 0, leaks + " kart positions overlapping grass were not blocked");
        Check(seam > 0, "no kart position straddles the seam between the two ribbons");
    }

    // Mirrors ShiftController: forward meters scaled by speed, normalized to the shop lap.
    static double YieldPerLap(int map, DrivingStyle style, double engine)
    {
        var drive = new ArcadeDrive(RaceCourse.ForMap(map)) { MaximumSpeed = engine, Style = style };
        double grown = 0;
        for (int step = 0; step < 30000 && drive.TotalProgress < drive.Course.Length * 2; step++)
        {
            double throttle, steering; bool brake;
            CottonCircuit.Tests.CourseTestDriver.Input(drive, .8, out throttle, out steering, out brake);
            drive.Step(throttle, steering, brake, false, .02);
            grown += drive.LastRewardDistance * ShopShift.SpeedYield(drive.Speed);
        }
        return grown / drive.TotalProgress;
    }

    public static int Main()
    {
        Test("starter course fits a quick corner and boost race", () => {
            Check(RaceCourse.Shared.Length >= 520 && RaceCourse.Shared.Length <= 620,
                "starter course must be 520..620m, got " + RaceCourse.Shared.Length);
        });
        Test("maps select different full circuits and reject unsupported indices", () => {
            var small = RaceCourse.ForMap(0); var large = RaceCourse.ForMap(1); var third = RaceCourse.ForMap(2);
            Check(ReferenceEquals(small, RaceCourse.Shared), "legacy default changed away from map 1");
            Check(RaceCourse.MapCount == 3 && third.MapIndex == 2, "third course missing");
            Check(large.Length >= 640 && large.Length <= 750,
                "large course must be 640..750m, got " + large.Length);
            double difference = 0;
            for (int i = 0; i < 16; i++)
                difference += Distance(small.Sample(small.Length * i / 16).Position * (1 / small.Length),
                    large.Sample(large.Length * i / 16).Position * (1 / large.Length));
            Check(difference > .3, "second layout is only a scaled first layout");
            foreach (int invalid in new[] { -1, 3, int.MaxValue })
            {
                bool rejected = false;
                try { RaceCourse.ForMap(invalid); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "unsupported map silently used a valid course");
            }
        });
        for (int map = 0; map < RaceCourse.MapCount; map++)
        {
            int selected = map;
            Test("map " + (map + 1) + " road and shortcut form finite, connected drivable ribbons", () => {
                var course = RaceCourse.ForMap(selected);
                Check(Distance(course.Sample(-.01).Position, course.Sample(.01).Position) < .025,
                    "loop seam is disconnected");
                Check(Distance(course.Sample(-.01).Tangent, course.Sample(.01).Tangent) < .06,
                    "loop seam changes heading abruptly");
                Check(Distance(course.Sample(0).Position, course.Sample(course.Length).Position) < .001,
                    "sampling does not wrap");
                foreach (var points in new[] { course.MainPoints, course.ShortcutPoints })
                    foreach (var point in points)
                    {
                        double radius = Distance(point, new RoadPoint(0, 0));
                        double halfWidth = ReferenceEquals(points, course.MainPoints)
                            ? RaceCourse.MainHalfWidth : RaceCourse.ShortcutHalfWidth;
                        Check(!double.IsNaN(radius) && !double.IsInfinity(radius) && radius + halfWidth <= 130,
                            "road extends outside machine radius");
                        Check(radius - halfWidth >= 30, "road overlaps the central stage");
                        Check(point.X >= course.BoundsMin.X && point.X <= course.BoundsMax.X &&
                            point.Z >= course.BoundsMin.Z && point.Z <= course.BoundsMax.Z, "minimap bounds omit road");
                        var projected = course.Project(point, .8);
                        Check(Distance(point, projected.Position) < .001 && Math.Abs(projected.Lateral) < .001,
                            "road waypoint does not project to a drivable centerline");
                    }
                for (int i = 0; i < 500; i++)
                {
                    var sample = course.Sample(course.Length * i / 500);
                    Check(Math.Abs(Distance(sample.Tangent, new RoadPoint(0, 0)) - 1) < .001, "non-unit tangent");
                    var right = new RoadPoint(sample.Tangent.Z, -sample.Tangent.X);
                    var projected = course.Project(sample.Position + right * 2, .8);
                    Check(Distance(sample.Position + right * 2, projected.Position) < 2.05,
                        "valid road lane projects outside ribbon");
                }
                var center = course.Project(course.ShortcutPoints[course.ShortcutPoints.Length / 2]);
                Check(center.IsShortcut, "shortcut has no independent drivable section");
            });
            Test("map " + (map + 1) + " walls at the shortcut junctions follow the painted road",
                () => VerifySeams(RaceCourse.ForMap(selected)));
            foreach (DrivingStyle style in new[] { DrivingStyle.Kart, DrivingStyle.Downhill })
            {
                DrivingStyle selectedStyle = style;
                Test("map " + (map + 1) + " base " + style + " completes a clean lap with normal inputs",
                    () => VerifyLap(RaceCourse.ForMap(selected), selectedStyle));
                Test("map " + (map + 1) + " " + style + " shortcut saves distance and drives a clean counted lap",
                    () => VerifyShortcut(RaceCourse.ForMap(selected), selectedStyle));
                foreach (int side in new[] { -1, 1 })
                {
                    int selectedSide = side;
                    Test("map " + (map + 1) + " " + style + " drives the expanded shortcut lane on side " + side,
                        () => VerifyShortcut(RaceCourse.ForMap(selected), selectedStyle, selectedSide * 2.1));
                }
            }
            Test("map " + (map + 1) + " upgraded downhill handles high speed with corner braking",
                () => VerifyLap(RaceCourse.ForMap(selected), DrivingStyle.Downhill, 38.5));
        }
        for (int map = 0; map < 2; map++)
        {
            int selected = map;
            Test("map " + (map + 1) + " speed-based growth keeps clean base laps and rewards speed", () => {
                double kart = YieldPerLap(selected, DrivingStyle.Kart, 26);
                double fast = YieldPerLap(selected, DrivingStyle.Kart, 26 * 1.24);
                double slow = YieldPerLap(selected, DrivingStyle.Kart, 15);
                double downhill = YieldPerLap(selected, DrivingStyle.Downhill, 26);
                double crawl = YieldPerLap(selected, DrivingStyle.Kart, ShopShift.ReferenceSpeed * ShopShift.WarmupSpeedRatio);
                Console.WriteLine("  growth per lap: kart " + kart.ToString("F2") + ", engine+3 " + fast.ToString("F2") +
                    ", slow " + slow.ToString("F2") + ", downhill " + downhill.ToString("F2"));
                Check(kart > .95 && kart < 1.05, "clean base kart lap should grow about one lap of candy");
                Check(fast > kart * 1.2 && downhill > kart, "faster driving did not grow more candy");
                Check(slow > 0 && slow < .5, "slow driving was not penalized beyond distance");
                Check(crawl == 0, "crawling below warm-up speed grew candy");
            });
        }
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
