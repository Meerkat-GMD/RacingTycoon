using System;

namespace CottonCircuit
{
    // Road-following inputs for hired workers. Player-controlled machines never
    // invoke this helper; their steering and pedals come only from player input.
    public static class AutoDrive
    {
        static double Turn(double angle)
        {
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            return angle;
        }
        public static void Input(ArcadeDrive drive, out double throttle, out double steering, out bool brake)
        {
            if (drive == null) throw new ArgumentNullException("drive");
            bool downhill = drive.Style == DrivingStyle.Downhill;
            double lookAhead = downhill ? 8 + drive.Speed * .18 : 12;
            var ahead = drive.Course.Sample(drive.Sample.Progress + lookAhead);
            double desired = Math.Atan2(ahead.Position.X - drive.Position.X, ahead.Position.Z - drive.Position.Z);
            steering = Math.Max(-1, Math.Min(1, Turn(desired - drive.Heading) * (downhill ? 2.2 : 1.8)));

            // Anticipate the tightest nearby bend, allowing braking distance before
            // reaching it. This also keeps upgraded karts safely inside the road.
            double safeSpeed = downhill ? 55 : drive.MaximumSpeed;
            for (double distance = 0; distance <= 80; distance += 4)
            {
                var before = drive.Course.Sample(drive.Sample.Progress + distance - 4).Tangent;
                var after = drive.Course.Sample(drive.Sample.Progress + distance + 4).Tangent;
                double curvature = Math.Abs(Turn(Math.Atan2(after.X, after.Z) - Math.Atan2(before.X, before.Z))) / 8;
                double cornerSpeed = Math.Sqrt(22 / Math.Max(.001, curvature));
                safeSpeed = Math.Min(safeSpeed, Math.Sqrt(cornerSpeed * cornerSpeed + 28 * Math.Max(0, distance - 10)));
            }
            throttle = drive.Speed < safeSpeed ? 1 : 0;
            brake = drive.Speed > safeSpeed + .4;
        }
    }
}
