using System;
using CottonCircuit;

public static class ProgressionMapTests
{
    static int passed, failed;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex.Message); failed++; }
    }
    static double Distance(RoadPoint a, RoadPoint b)
    {
        double x = a.X - b.X, z = a.Z - b.Z;
        return Math.Sqrt(x * x + z * z);
    }
    public static int Main()
    {
        Test("third course is distinct and within the machine ring", () => {
            var third = RaceCourse.ForMap(2);
            Check(third.MapIndex == 2 && third.Length > 600 && third.Length < 850,
                "third course missing or unsuitable length");
            double difference = 0;
            foreach (int other in new[] { 0, 1 })
            {
                var old = RaceCourse.ForMap(other); double sum = 0;
                for (int i = 0; i < 20; i++)
                    sum += Distance(third.Sample(third.Length * i / 20).Position * (1 / third.Length),
                        old.Sample(old.Length * i / 20).Position * (1 / old.Length));
                difference += sum;
                Check(sum > .3, "third course merely duplicates map " + other);
            }
            foreach (RoadPoint point in third.MainPoints)
            {
                double radius = Distance(point, new RoadPoint(0, 0));
                Check(radius - RaceCourse.MainHalfWidth >= 30 &&
                    radius + RaceCourse.MainHalfWidth <= 130, "main road leaves safe ring");
            }
        });
        Test("third course forms a smooth loop and real shortcut", () => {
            var course = RaceCourse.ForMap(2);
            Check(Distance(course.Sample(-.01).Position, course.Sample(.01).Position) < .025,
                "seam disconnected");
            Check(Distance(course.Sample(-.01).Tangent, course.Sample(.01).Tangent) < .06,
                "seam tangent jumped");
            double shortcut = 0;
            for (int i = 1; i < course.ShortcutPoints.Length; i++)
            {
                RoadPoint point = course.ShortcutPoints[i];
                double radius = Distance(point, new RoadPoint(0, 0));
                Check(radius - RaceCourse.ShortcutHalfWidth >= 30 &&
                    radius + RaceCourse.ShortcutHalfWidth <= 130, "shortcut leaves safe ring");
                shortcut += Distance(course.ShortcutPoints[i - 1], point);
            }
            double entry = course.Project(course.ShortcutPoints[0]).Progress;
            double exit = course.Project(course.ShortcutPoints[course.ShortcutPoints.Length - 1]).Progress;
            Check(exit - entry - shortcut > 12, "shortcut saves too little distance");
            Check(course.Project(course.ShortcutPoints[course.ShortcutPoints.Length / 2]).IsShortcut,
                "shortcut does not have a drivable center");
        });
        Test("third course can be driven one lap without hitting a wall", () => {
            var drive = new ArcadeDrive(RaceCourse.ForMap(2));
            for (int step = 0; step < 1900 && drive.Laps == 0; step++)
            {
                double throttle, steering; bool brake;
                CottonCircuit.Tests.CourseTestDriver.Input(drive, .8, out throttle, out steering, out brake);
                drive.Step(throttle, steering, brake, false, .02);
            }
            Check(drive.Laps == 1 && drive.WallHits == 0, "third course blocked an ordinary lap");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
