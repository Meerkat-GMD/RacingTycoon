#if UNITY_EDITOR || DEVELOPMENT_BUILD || COTTON_TESTS
using System;
namespace CottonCircuit.Tests
{
    // Test-only driver supplies the same throttle, steering and brake inputs as a player.
    // The faster Downhill car must anticipate and brake for corners.
    public static class CourseTestDriver
    {
        static double Turn(double angle)
        {
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            return angle;
        }
        public static void Input(ArcadeDrive drive, double weave, out double throttle, out double steering, out bool brake)
        {
            bool downhill = drive.Style == DrivingStyle.Downhill;
            var ahead = drive.Course.Sample(drive.Sample.Progress + (downhill ? 8 + drive.Speed * .18 : 12));
            var target = ahead.Position + new RoadPoint(ahead.Tangent.Z, -ahead.Tangent.X) *
                (weave * Math.Sin(drive.Sample.Progress * .02));
            double desired = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
            steering = Math.Max(-1, Math.Min(1, Turn(desired - drive.Heading) * (downhill ? 2.2 : 1.8)));
            double safe = 55;
            if (downhill) for (double distance = 0; distance <= 80; distance += 4)
            {
                var before = drive.Course.Sample(drive.Sample.Progress + distance - 4).Tangent;
                var after = drive.Course.Sample(drive.Sample.Progress + distance + 4).Tangent;
                double curvature = Math.Abs(Turn(Math.Atan2(after.X, after.Z) - Math.Atan2(before.X, before.Z))) / 8;
                double corner = Math.Sqrt(22 / Math.Max(.001, curvature));
                safe = Math.Min(safe, Math.Sqrt(corner * corner + 28 * Math.Max(0, distance - 10)));
            }
            throttle = !downhill || drive.Speed < safe ? 1 : 0;
            brake = downhill && drive.Speed > safe + .4;
        }
    }
}
#endif
