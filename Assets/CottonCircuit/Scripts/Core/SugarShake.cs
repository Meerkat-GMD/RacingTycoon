using System;

namespace CottonCircuit
{
    public class SugarShake
    {
        public const double LegPixels = 12;
        public const double DefaultFullStrokePixels = 132;
        public const double MinFullStrokePixels = 24;
        public const double MaxFullStrokePixels = 600;
        const double MaximumAmount = 10;
        double fullStrokePixels = DefaultFullStrokePixels;
        bool tracking;
        int direction;
        double anchor;
        double extreme;

        public double Amount { get; private set; }
        public double FullStrokePixels
        {
            get { return fullStrokePixels; }
            set
            {
                double normalized = NormalizeFullStrokePixels(value);
                if (fullStrokePixels == normalized) return;
                fullStrokePixels = normalized;
                Reset();
            }
        }

        public static double NormalizeFullStrokePixels(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return DefaultFullStrokePixels;
            return Math.Max(MinFullStrokePixels, Math.Min(MaxFullStrokePixels, value));
        }

        public void Reset()
        {
            tracking = false;
            direction = 0;
            Amount = 0;
        }

        public bool Move(double x, double y, bool inside)
        {
            Amount = 0;
            if (!inside || double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
            { Reset(); return false; }
            if (!tracking)
            {
                tracking = true;
                anchor = extreme = y;
                return false;
            }
            if (direction == 0)
            {
                if (Math.Abs(y - anchor) < LegPixels) return false;
                direction = y > anchor ? 1 : -1;
                extreme = y;
                return false;
            }
            if ((y - extreme) * direction > 0) extreme = y;
            if ((extreme - y) * direction < LegPixels) return false;

            // Measure the completed stroke, not the size or frequency of pointer events.
            Amount = Math.Min(MaximumAmount, Math.Max(0,
                (Math.Abs(extreme - anchor) - LegPixels) / (FullStrokePixels - LegPixels) * MaximumAmount));
            anchor = extreme;
            extreme = y;
            direction = -direction;
            return Amount > 0;
        }
    }
}
