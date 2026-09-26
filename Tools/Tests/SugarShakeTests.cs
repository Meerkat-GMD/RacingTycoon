using System;
using CottonCircuit;

public static class SugarShakeTests
{
    static int passed, failed;

    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    static void Near(double actual, double expected, string message)
    {
        Check(Math.Abs(actual - expected) < .000001,
            message + ": expected " + expected + ", got " + actual);
    }

    static double Amount(SugarShake shake)
    {
        return shake.Amount;
    }

    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception error) { Console.WriteLine("FAIL " + name + ": " + error.Message); failed++; }
    }

    static double Stroke(double amplitude, double fullStrokePixels = 132)
    {
        var shake = new SugarShake { FullStrokePixels = fullStrokePixels };
        Check(!shake.Move(0, 0, true), "pickup poured");
        Check(!shake.Move(0, amplitude, true), "initial one-way motion poured");
        Check(shake.Move(0, amplitude - 12, true), "confirmed reversal did not pour");
        return Amount(shake);
    }

    static double FollowPath(int step)
    {
        var shake = new SugarShake();
        double total = 0;
        shake.Move(0, 0, true);
        for (int leg = 0; leg < 4; leg++)
        {
            for (int distance = step; distance <= 60; distance += step)
            {
                double y = leg % 2 == 0 ? distance : 60 - distance;
                if (shake.Move(0, y, true)) total += Amount(shake);
                else Near(Amount(shake), 0, "no-event move retained a previous amount");
            }
        }
        return total;
    }

    public static int Main()
    {
        Test("a reversal must travel twelve pixels before pouring", () => {
            var shake = new SugarShake();
            Check(!shake.Move(0, 0, true) && !shake.Move(0, 24, true), "one-way motion poured");
            Check(!shake.Move(0, 13, true), "an eleven pixel reversal poured");
            Check(shake.Move(0, 12, true), "a twelve pixel reversal did not pour");
            Near(Amount(shake), 1, "weak stroke amount");
        });
        Test("weak normal and strong strokes produce increasing amounts", () => {
            Near(Stroke(24), 1, "weak stroke must trickle sugar");
            Near(Stroke(60), 4, "normal stroke must produce a moderate amount");
            Near(Stroke(132), 10, "strong stroke must produce the full amount");
        });
        Test("a small but valid stroke preserves fractional sugar", () => {
            Near(Stroke(18), .5, "small stroke rounded up to a full pour");
        });
        Test("oversized strokes cannot produce an oversized pour", () => {
            Near(Stroke(600), 10, "oversized stroke exceeded the pour cap");
        });
        Test("the same physical stroke responds to the selected full stroke width", () => {
            Near(Stroke(72, 72), 10, "shorter setting did not allow a full pour");
            Near(Stroke(72, 132), 5, "default setting changed its response");
            Near(Stroke(72, 252), 2.5, "longer setting did not reduce sugar");
        });
        Test("minimum and maximum full stroke settings reach ten grams at their endpoints", () => {
            Near(Stroke(18, 24), 5, "minimum setting discarded a fractional stroke");
            Near(Stroke(24, 24), 10, "minimum full stroke did not reach ten grams");
            Near(Stroke(306, 600), 5, "maximum setting did not preserve proportional pouring");
            Near(Stroke(600, 600), 10, "maximum full stroke did not reach ten grams");
            Near(Stroke(900, 600), 10, "maximum setting allowed an oversized pour");
        });
        Test("out of range settings are clamped before they affect pouring", () => {
            var shake = new SugarShake { FullStrokePixels = -100 };
            Near(shake.FullStrokePixels, 24, "too-small setting was not clamped");
            Near(Stroke(24, -100), 10, "clamped minimum did not produce a full pour");
            shake.FullStrokePixels = 900;
            Near(shake.FullStrokePixels, 600, "too-large setting was not clamped");
            Near(Stroke(306, 900), 5, "clamped maximum did not use the maximum response");
        });
        Test("nonfinite settings restore the default response", () => {
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var shake = new SugarShake { FullStrokePixels = 24 };
                shake.FullStrokePixels = invalid;
                Near(shake.FullStrokePixels, 132, "nonfinite setting did not restore the default");
                Near(Stroke(72, invalid), 5, "nonfinite setting changed default pouring");
            }
        });
        Test("changing the setting discards the unfinished stroke", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            shake.Move(0, 72, true);
            shake.FullStrokePixels = 252;
            Check(!shake.Move(0, 60, true), "changing settings retained the old stroke");
            Check(!shake.Move(0, 132, true), "a fresh one-way stroke poured");
            Check(shake.Move(0, 120, true), "a fresh reversal did not pour");
            Near(Amount(shake), 2.5, "fresh stroke did not use the changed setting");
            shake.FullStrokePixels = 72;
            Near(Amount(shake), 0, "changing settings retained the last pour amount");
        });
        Test("reapplying the same effective setting preserves an unfinished stroke", () => {
            foreach (double repeated in new[] { 24, 0.0 })
            {
                var shake = new SugarShake { FullStrokePixels = 24 };
                shake.Move(0, 0, true);
                shake.Move(0, 18, true);
                shake.FullStrokePixels = repeated;
                Check(shake.Move(0, 6, true), "same setting erased an unfinished stroke");
                Near(Amount(shake), 5, "same setting changed the stroke amount");
            }
        });
        Test("the adjustable setting keeps the twelve pixel dead zone", () => {
            foreach (double width in new[] { 24, 600.0 })
            {
                var shake = new SugarShake { FullStrokePixels = width };
                shake.Move(0, 0, true);
                for (int i = 0; i < 20; i++)
                {
                    Check(!shake.Move(0, i % 2 == 0 ? 12 : 0, true), "dead-zone stroke poured");
                    Near(Amount(shake), 0, "setting removed the dead zone");
                }
            }
        });
        Test("initial one-way motion never pours", () => {
            var shake = new SugarShake();
            foreach (double y in new[] { 0, 10, 24, 60, 180, 500.0 })
            {
                Check(!shake.Move(0, y, true), "one-way movement poured");
                Near(Amount(shake), 0, "one-way movement produced sugar");
            }
        });
        Test("sub-threshold jitter does not accumulate sugar", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            for (int i = 0; i < 100; i++)
            {
                Check(!shake.Move(0, i % 2 == 0 ? 10 : 0, true), "jitter poured");
                Near(Amount(shake), 0, "jitter accumulated sugar");
            }
        });
        Test("twelve pixel oscillation stays inside the dead zone", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            for (int i = 0; i < 20; i++)
            {
                Check(!shake.Move(0, i % 2 == 0 ? 12 : 0, true), "dead-zone stroke poured");
                Near(Amount(shake), 0, "dead-zone stroke produced sugar");
            }
        });
        Test("horizontal motion never contributes to sugar", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            foreach (double x in new[] { 30, -30, 300, -300.0 })
                Check(!shake.Move(x, 0, true), "horizontal movement poured");
            shake.Move(0, 60, true);
            Check(!shake.Move(300, 60, true) && !shake.Move(-300, 60, true), "horizontal motion reversed a vertical stroke");
            Near(Amount(shake), 0, "horizontal movement produced sugar");
        });
        Test("upward and downward strokes have equal strength", () => {
            var shake = new SugarShake();
            Check(!shake.Move(0, 100, true) && !shake.Move(0, 40, true), "initial downward motion poured");
            Check(shake.Move(0, 52, true), "downward stroke did not pour on reversal");
            Near(Amount(shake), 4, "downward stroke amount");
        });
        Test("continuous oscillation measures every completed stroke", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            shake.Move(0, 60, true);
            foreach (double y in new[] { 0, 60, 0, 60.0 })
            {
                Check(shake.Move(0, y, true), "completed stroke was skipped");
                Near(Amount(shake), 4, "stroke amount drifted during oscillation");
            }
        });
        Test("sub-threshold backtracking preserves the true stroke extreme", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            Check(!shake.Move(0, 60, true) && !shake.Move(0, 55, true) && !shake.Move(0, 72, true), "small backtracking poured");
            Check(shake.Move(0, 60, true), "confirmed reversal did not pour");
            Near(Amount(shake), 5, "backtracking changed the completed stroke amplitude");
        });
        Test("coarse and dense pointer sampling produce the same sugar", () => {
            Near(FollowPath(60), 12, "coarse pointer samples lost a stroke");
            Near(FollowPath(3), 12, "dense pointer samples changed production");
        });
        Test("leaving and re-entering discards the unfinished stroke", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            shake.Move(0, 60, true);
            Check(!shake.Move(0, 0, false), "leaving the machine poured");
            Near(Amount(shake), 0, "outside movement retained sugar");
            Check(!shake.Move(0, 60, true) && !shake.Move(0, 0, true), "re-entry reused an outside stroke");
            Check(shake.Move(0, 12, true), "fresh inside stroke did not pour");
            Near(Amount(shake), 4, "fresh inside stroke amount");
        });
        Test("nonfinite pointer values reset the unfinished stroke", () => {
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                foreach (bool invalidX in new[] { false, true })
                {
                    var shake = new SugarShake();
                    shake.Move(0, 0, true);
                    shake.Move(0, 60, true);
                    Check(!shake.Move(invalidX ? invalid : 0, invalidX ? 0 : invalid, true), "nonfinite pointer poured");
                    Near(Amount(shake), 0, "nonfinite pointer retained sugar");
                    Check(!shake.Move(0, 0, true) && !shake.Move(0, 60, true), "invalid pointer did not clear tracking");
                }
            }
        });
        Test("a non-pouring move clears the previous amount", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            shake.Move(0, 60, true);
            Check(shake.Move(0, 48, true), "setup reversal did not pour");
            Near(Amount(shake), 4, "setup amount");
            Check(!shake.Move(0, 30, true), "continuing a stroke poured twice");
            Near(Amount(shake), 0, "previous amount leaked into another pointer event");
        });
        Test("reset clears amount and requires a fresh stroke", () => {
            var shake = new SugarShake();
            shake.Move(0, 0, true);
            shake.Move(0, 60, true);
            shake.Move(0, 48, true);
            shake.Reset();
            Near(Amount(shake), 0, "reset retained sugar");
            Check(!shake.Move(0, 60, true) && !shake.Move(0, 0, true), "reset retained the previous stroke");
            Check(shake.Move(0, 12, true), "fresh stroke after reset did not pour");
            Near(Amount(shake), 4, "fresh stroke after reset amount");
        });
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
