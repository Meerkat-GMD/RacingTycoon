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
    static void VerifyShortcut(RaceCourse course)
    {
        var drive = new ArcadeDrive(course);
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
        double largestReward = 0;
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
            Steer(drive, target, 8);
            entered |= drive.Sample.IsShortcut;
            largestReward = Math.Max(largestReward, drive.LastRewardDistance);
            Check(drive.WallHits == 0, "shortcut traversal hit a wall at " + drive.Sample.Progress.ToString("F2"));
            if (entered && !drive.Sample.IsShortcut && drive.Sample.Progress > exit + 3) { merged = true; break; }
        }
        Check(entered && merged, "kart failed to enter and merge from shortcut");
        Check(largestReward < .6, "shortcut awarded a discontinuous progress pulse");
        Check(drive.TotalProgress > exit - 4 && drive.TotalProgress <= drive.Sample.Progress + .1,
            "shortcut lost or awarded free progress: " + drive.TotalProgress + " at " + drive.Sample.Progress);
        for (int step = 0; step < 9000 && drive.Laps == 0; step++)
            Steer(drive, course.Sample(drive.Sample.Progress + 8).Position, 8);
        Check(drive.Laps == 1 && drive.BestLapSeconds > 0 && drive.WallHits == 0,
            "shortcut route failed a clean forward lap");
        Console.WriteLine("  shortcut saves " + (exit - entry - shortcutLength).ToString("F1") + "m");
    }

    public static int Main()
    {
        Test("starter course supports a full length race", () => {
            Check(RaceCourse.Shared.Length >= 750 && RaceCourse.Shared.Length <= 850,
                "starter course must be 750..850m, got " + RaceCourse.Shared.Length);
        });
        Test("maps select different full circuits and reject unsupported indices", () => {
            var small = RaceCourse.ForMap(0); var large = RaceCourse.ForMap(1);
            Check(ReferenceEquals(small, RaceCourse.Shared), "legacy default changed away from map 1");
            Check(large.Length >= 1050 && large.Length <= 1200, "large course must be 1050..1200m");
            double difference = 0;
            for (int i = 0; i < 16; i++)
                difference += Distance(small.Sample(small.Length * i / 16).Position * (1 / small.Length),
                    large.Sample(large.Length * i / 16).Position * (1 / large.Length));
            Check(difference > .3, "second layout is only a scaled first layout");
            foreach (int invalid in new[] { -1, 2, int.MaxValue })
            {
                bool rejected = false;
                try { RaceCourse.ForMap(invalid); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "unsupported map silently used a valid course");
            }
        });
        for (int map = 0; map < 2; map++)
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
                        Check(!double.IsNaN(radius) && !double.IsInfinity(radius) && radius + RaceCourse.MainHalfWidth <= 200,
                            "road extends outside machine radius");
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
            Test("map " + (map + 1) + " base kart completes one clean lap in 45..90 seconds", () => {
                var drive = new ArcadeDrive(RaceCourse.ForMap(selected));
                for (int step = 0; step < 4500 && drive.Laps == 0; step++)
                {
                    var target = drive.Course.Sample(drive.Sample.Progress + 10).Position;
                    double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
                    drive.Step(1, Math.Max(-1, Math.Min(1, Turn(desired - drive.Heading) * 1.8)), false, false, .02);
                }
                Check(drive.Laps == 1, "base kart did not finish a lap in 90 seconds");
                Check(drive.BestLapSeconds >= 45 && drive.BestLapSeconds <= 90,
                    "lap time outside expected race duration: " + drive.BestLapSeconds);
                Check(drive.WallHits == 0, "main course follower hit " + drive.WallHits + " walls");
                Check(drive.TotalProgress >= drive.Course.Length - 1, "main course lost progress before finish");
                Console.WriteLine("  length " + drive.Course.Length.ToString("F1") + "m, lap " + drive.BestLapSeconds.ToString("F2") + "s");
            });
            Test("map " + (map + 1) + " shortcut saves distance and drives a clean counted lap", () => VerifyShortcut(RaceCourse.ForMap(selected)));
        }
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
