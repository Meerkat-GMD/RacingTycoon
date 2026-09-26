using System;
using CottonCircuit;

public static class AccelerationTests
{
    static int passed, failed;

    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    static void Near(double actual, double expected, double tolerance, string message)
    { Check(Math.Abs(actual - expected) <= tolerance, message + " (got " + actual + ", wanted " + expected + ")"); }

    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }

    static double Run(double seconds, double throttle = 1, bool brake = false,
        double start = 0, double maximumSpeed = 26, double dt = .02, int boostTier = 0,
        DrivingStyle style = DrivingStyle.Downhill, bool drift = false)
    {
        double speed = start;
        int steps = (int)Math.Round(seconds / dt);
        for (int i = 0; i < steps; i++)
            speed = DriveAcceleration.Advance(speed, throttle, brake, style, maximumSpeed, boostTier, dt, drift);
        return speed;
    }

    public static int Main()
    {
        Test("kart launch partial throttle coast and braking retain their existing response", () => {
            Near(DriveAcceleration.Advance(0, 1, false, DrivingStyle.Kart, 26, 0, .02), .726, 1e-12, "launch changed");
            Near(DriveAcceleration.Advance(20, .5, false, DrivingStyle.Kart, 26, 0, .02), 20.302, 1e-12, "partial throttle changed");
            Near(DriveAcceleration.Advance(20, 0, false, DrivingStyle.Kart, 26, 0, .02), 19.922, 1e-12, "coasting changed");
            Near(DriveAcceleration.Advance(20, 1, true, DrivingStyle.Kart, 26, 2, .02), 19.2, 1e-12, "brakes lost priority");
            Near(Run(10, style: DrivingStyle.Kart), 26, 1e-12, "normal kart speed limit changed");
        });
        Test("kart boost tiers keep their acceleration and separate speed limits", () => {
            Near(DriveAcceleration.Advance(26, 1, false, DrivingStyle.Kart, 26, 1, .02), 26.7888, 1e-12, "boost engine changed");
            Near(Run(10, boostTier: 1, style: DrivingStyle.Kart), 35, 1e-12, "mini boost limit changed");
            Near(Run(10, boostTier: 2, style: DrivingStyle.Kart), 40, 1e-12, "super boost limit changed");
        });
        Test("kart boost expiry carries excess speed and decays to the normal limit", () => {
            Near(DriveAcceleration.Advance(40, 1, false, DrivingStyle.Kart, 26, 0, .02), 39.82, 1e-12, "expiry snapped speed");
            Near(DriveAcceleration.Advance(26.05, 1, false, DrivingStyle.Kart, 26, 0, .02), 26, 1e-12, "decay fell below the limit");
        });
        Test("downhill launches strongly and keeps gaining speed throughout twenty seconds", () => {
            double one = Run(1), three = Run(3), six = Run(6), ten = Run(10), twenty = Run(20);
            Console.WriteLine("  DOWNHILL m/s at 1/3/6/10/20 s: " + one.ToString("F3") + " / " + three.ToString("F3") +
                " / " + six.ToString("F3") + " / " + ten.ToString("F3") + " / " + twenty.ToString("F3"));
            Check(one > 18 && one < 35, "launch is too weak or abrupt");
            Check(three > one + 6 && six > three + 4 && ten > six + 3 && twenty > ten + 3,
                "holding throttle reaches an early plateau");
            Check(twenty > Run(19) + .12, "acceleration is no longer perceptible at twenty seconds");
            Check((twenty - ten) / 10 < (ten - six) / 4 && (ten - six) / 4 < (six - three) / 3,
                "high-speed acceleration does not diminish smoothly");
        });
        Test("downhill full throttle is monotonic and bounded over repeated two-minute runs", () => {
            foreach (double maximum in new double[] { 26, 32, 44 })
            {
                double speed = 0;
                for (int i = 0; i < 6000; i++)
                {
                    double next = DriveAcceleration.Advance(speed, 1, false, DrivingStyle.Downhill, maximum, 0, .02);
                    Check(!double.IsNaN(next) && !double.IsInfinity(next), "full throttle became non-finite");
                    Check(next >= speed && next < maximum + 34, "speed fell or exceeded its finite ceiling");
                    speed = next;
                }
                Check(speed > maximum + 29, "the intended high-speed range is unreachable");
            }
        });
        Test("downhill coasting and braking reduce speed without reversing", () => {
            double coast = Run(1, throttle: 0, start: 45);
            double brake = Run(1, brake: true, start: 45);
            Check(coast < 43 && coast > 35, "releasing the accelerator does not coast naturally");
            Near(brake, 20, 1e-10, "downhill brakes changed or throttle overrode braking");
            Near(Run(3, brake: true, start: 45), 0, 1e-12, "braking reversed the car");
            Near(Run(30, throttle: 0, start: 45), 0, 1e-12, "coasting never stopped");
        });
        Test("downhill handbrake slows at low cruising and high speeds regardless of throttle", () => {
            foreach (double entry in new double[] { 3, 26, 45 })
            {
                double slowed = Run(.5, start: entry, drift: true);
                Check(slowed >= 0 && slowed < entry - 1, "handbrake did not slow from " + entry);
                Check(slowed > Run(.5, start: entry, brake: true), "handbrake exceeded the service brake");
                Near(slowed, Run(.5, start: entry, throttle: 0, drift: true), 1e-12,
                    "full throttle overrode the handbrake");
                Near(slowed, Run(.5, start: entry, throttle: .5, drift: true), 1e-12,
                    "partial throttle overrode the handbrake");
            }
            double highSpeed = Run(.5, start: 45, drift: true);
            Console.WriteLine("  DOWNHILL half-second Space from 45 m/s: " + highSpeed.ToString("F3"));
            Check(DriveAcceleration.Advance(80, 1, false, DrivingStyle.Downhill, 26, 0, .02, true) < 79.7,
                "over-limit speed bypassed the handbrake slowdown");
        });
        Test("downhill service braking takes priority over the handbrake without stacking", () => {
            Near(Run(.5, start: 45, brake: true, drift: true), 32.5, 1e-10,
                "handbrake changed the service-brake response");
            Near(Run(3, start: 45, brake: true, drift: true), 0, 0,
                "combined braking reversed the car");
        });
        Test("downhill handbrake reaches zero and release restores engine pull", () => {
            Near(Run(2, start: 3, drift: true), 0, 0, "handbrake retained a minimum rolling speed");
            Near(Run(1, drift: true), 0, 0, "handbrake allowed a launch from rest");
            double slowed = Run(.5, start: 26, drift: true);
            Check(Run(.5, start: slowed) > slowed + 1, "releasing the handbrake retained deceleration");
        });
        Test("downhill handbrake deceleration is stable across supported timesteps", () => {
            Near(Run(.5, start: 45, drift: true), Run(.5, start: 45, dt: .005, drift: true), .02,
                "handbrake deceleration depends on frame timing");
            Near(Run(3, start: 3, drift: true), Run(3, start: 3, dt: .005, drift: true), 0,
                "handbrake stopping depends on frame timing");
        });
        Test("kart drift input preserves normal engine pull and booster behavior", () => {
            foreach (int tier in new int[] { 0, 1, 2 })
                Near(Run(1, boostTier: tier, style: DrivingStyle.Kart, drift: true),
                    Run(1, boostTier: tier, style: DrivingStyle.Kart), 0,
                    "downhill handbrake response leaked into kart engine or boost");
        });
        Test("downhill partial throttle launches more slowly and settles below full throttle", () => {
            double partialLaunch = Run(1, throttle: .5);
            Check(partialLaunch > 5 && partialLaunch < Run(1) - 4, "partial throttle is not distinct from full throttle");
            double partialCruise = Run(120, throttle: .5);
            Check(partialCruise > 20 && partialCruise < Run(120) - 10, "partial throttle cannot hold a lower cruising speed");
            Check(Run(1, throttle: .5, start: 50) < 50, "partial throttle keeps accelerating at high speed");
        });
        Test("downhill engine upgrades improve launch sustained speed and the high-speed ceiling", () => {
            Check(Run(1, maximumSpeed: 32) > Run(1) + 1, "upgrade does not improve acceleration");
            Check(Run(20, maximumSpeed: 32) > Run(20) + 3, "upgrade does not improve sustained speed");
            Check(Run(120, maximumSpeed: 32) > 60, "upgrade never exceeds the stock high-speed ceiling");
        });
        Test("downhill ignores kart boosters", () => {
            Near(Run(20, boostTier: 2), Run(20), 1e-12, "a kart boost leaked into downhill");
        });
        Test("downhill speed changes continuously around the former speed limit", () => {
            double below = DriveAcceleration.Advance(25.999, 1, false, DrivingStyle.Downhill, 26, 0, .02) - 25.999;
            double above = DriveAcceleration.Advance(26.001, 1, false, DrivingStyle.Downhill, 26, 0, .02) - 26.001;
            Check(below > .05 && above > .05, "the old cap still stops acceleration");
            Near(above, below, .001, "acceleration jumps at the old limit");
        });
        Test("downhill motion is consistent across supported timesteps", () => {
            foreach (double duration in new double[] { 1, 3, 10, 20 })
                Near(Run(duration, dt: .02), Run(duration, dt: .005), .15, "full throttle depends on frame timing");
            Near(Run(10, throttle: .4, start: 50, dt: .02), Run(10, throttle: .4, start: 50, dt: .005), .15,
                "partial-throttle deceleration depends on frame timing");
        });
        Test("invalid inputs remain finite and throttle is clamped", () => {
            foreach (DrivingStyle style in new DrivingStyle[] { DrivingStyle.Kart, DrivingStyle.Downhill })
            {
                Near(DriveAcceleration.Advance(20, double.NaN, false, style, 26, 0, .02), 20, 0, "invalid throttle moved the car");
                Near(DriveAcceleration.Advance(20, 1, false, style, 26, 0, double.PositiveInfinity), 20, 0, "invalid time moved the car");
                Near(DriveAcceleration.Advance(20, 1, false, style, 26, 0, 0), 20, 0, "zero time moved the car");
                Near(DriveAcceleration.Advance(20, 1, false, style, 26, 0, -.02), 20, 0, "negative time moved the car");
                Near(DriveAcceleration.Advance(20, -3, false, style, 26, 0, .02),
                    DriveAcceleration.Advance(20, 0, false, style, 26, 0, .02), 0, "negative throttle reverses the car");
                Near(DriveAcceleration.Advance(20, 3, false, style, 26, 0, .02),
                    DriveAcceleration.Advance(20, 1, false, style, 26, 0, .02), 0, "excess throttle adds speed");
                Near(DriveAcceleration.Advance(20, 1, false, style, double.NaN, 0, .02),
                    DriveAcceleration.Advance(20, 1, false, style, 26, 0, .02), 0, "invalid upgrade does not use the default");
                foreach (double invalidSpeed in new double[] { double.NaN, double.PositiveInfinity, -5 })
                {
                    double value = DriveAcceleration.Advance(invalidSpeed, 1, false, style, 26, 0, .02);
                    Check(value >= 0 && value < 1, "invalid starting speed was not recovered");
                }
            }
        });

        Console.WriteLine("Acceleration tests: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}
