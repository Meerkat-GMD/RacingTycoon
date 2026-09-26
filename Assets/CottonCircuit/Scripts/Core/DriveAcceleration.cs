using System;

namespace CottonCircuit
{
    public static class DriveAcceleration
    {
        static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        static double Clamp(double value, double min, double max) { return Math.Max(min, Math.Min(max, value)); }

        // ArcadeDrive calls this once per integration step, no longer than .02 seconds.
        public static double Advance(double speed, double throttle, bool brake, DrivingStyle style,
            double maximumSpeed, int boostTier, double dt, bool drift = false)
        {
            speed = Finite(speed) ? Math.Max(0, speed) : 0;
            if (!Finite(throttle) || !Finite(dt) || dt <= 0) return speed;
            throttle = Clamp(throttle, 0, 1);
            double baseLimit = Math.Max(1, Finite(maximumSpeed) ? maximumSpeed : 26);
            bool downhill = style == DrivingStyle.Downhill;
            int tier = Math.Max(0, Math.Min(2, boostTier));
            double limit = baseLimit + (downhill ? 34 : tier == 2 ? 14 : tier == 1 ? 9 : 0);

            if (brake) return Math.Max(0, speed - (downhill ? 25 : 40) * dt);
            // The downhill handbrake overrides engine pull even with throttle held.
            if (downhill && drift) return Math.Max(0, speed - (4 + speed * .18) * dt);
            // Preserve momentum when a boost expires or an engine limit is lowered.
            if (speed > limit) return Math.Max(limit, speed - 9 * dt);

            double drag = 1.7 + speed * .11;
            if (!downhill)
                return Clamp(speed + (throttle * (tier > 0 ? 44 : 38) - drag) * dt, 0, limit);

            // A strong launch fades into sustained high-speed pull. Both fades are
            // smooth: the old base limit has no special transition or hard cap.
            double speedRatio = speed / (baseLimit * .8);
            double ceilingRatio = speed / limit;
            double pull = 38 * Math.Sqrt(baseLimit / 26) / (1 + Math.Pow(speedRatio, 4)) *
                (1 - Math.Pow(ceilingRatio, 4));
            // Full throttle offsets road drag; lifting restores it progressively,
            // so partial throttle finds a lower cruising speed and coasting slows.
            double acceleration = throttle * pull - (1 - throttle) * drag;
            return Clamp(speed + acceleration * dt, 0, limit);
        }
    }
}
