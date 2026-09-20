using System;

namespace CottonCircuit
{
    public struct RoadPoint
    {
        public double X, Z;
        public RoadPoint(double x, double z) { X = x; Z = z; }
        public static RoadPoint operator +(RoadPoint a, RoadPoint b) { return new RoadPoint(a.X + b.X, a.Z + b.Z); }
        public static RoadPoint operator -(RoadPoint a, RoadPoint b) { return new RoadPoint(a.X - b.X, a.Z - b.Z); }
        public static RoadPoint operator *(RoadPoint a, double scale) { return new RoadPoint(a.X * scale, a.Z * scale); }
    }

    public struct CourseSample
    {
        public RoadPoint Position, Tangent;
        public double Progress, HalfWidth, Lateral;
        public bool IsShortcut;
    }

    public sealed class RaceCourse
    {
        public const double MainHalfWidth = 4.8;
        public const double ShortcutHalfWidth = 2.2;
        const int StepsPerAnchor = 24;
        static readonly RoadPoint[] Anchors = {
            new RoadPoint(0, -25), new RoadPoint(24, -25), new RoadPoint(34, -14),
            new RoadPoint(29, 1), new RoadPoint(35, 15), new RoadPoint(24, 27),
            new RoadPoint(7, 30), new RoadPoint(-5, 21), new RoadPoint(-22, 24),
            new RoadPoint(-33, 12), new RoadPoint(-31, -7), new RoadPoint(-22, -24)
        };
        public static readonly RaceCourse Shared = new RaceCourse();
        public readonly RoadPoint[] MainPoints;
        public readonly RoadPoint[] ShortcutPoints;
        readonly double[] mainDistances;
        readonly double shortcutStart, shortcutEnd;
        public double Length { get; private set; }

        RaceCourse()
        {
            MainPoints = new RoadPoint[Anchors.Length * StepsPerAnchor];
            for (int i = 0; i < MainPoints.Length; i++)
            {
                int anchor = i / StepsPerAnchor;
                double t = (double)(i % StepsPerAnchor) / StepsPerAnchor;
                MainPoints[i] = Catmull(Anchors[(anchor + Anchors.Length - 1) % Anchors.Length],
                    Anchors[anchor], Anchors[(anchor + 1) % Anchors.Length],
                    Anchors[(anchor + 2) % Anchors.Length], t);
            }
            mainDistances = new double[MainPoints.Length + 1];
            for (int i = 0; i < MainPoints.Length; i++)
                mainDistances[i + 1] = mainDistances[i] + Distance(MainPoints[i], MainPoints[(i + 1) % MainPoints.Length]);
            Length = mainDistances[MainPoints.Length];
            shortcutStart = mainDistances[3 * StepsPerAnchor];
            shortcutEnd = mainDistances[5 * StepsPerAnchor];
            ShortcutPoints = new RoadPoint[StepsPerAnchor + 1];
            for (int i = 0; i < ShortcutPoints.Length; i++)
            {
                double t = (double)i / StepsPerAnchor, u = 1 - t;
                ShortcutPoints[i] = Anchors[3] * (u * u * u) + new RoadPoint(21, 8) * (3 * u * u * t)
                    + new RoadPoint(18, 21) * (3 * u * t * t) + Anchors[5] * (t * t * t);
            }
        }

        static RoadPoint Catmull(RoadPoint a, RoadPoint b, RoadPoint c, RoadPoint d, double t)
        {
            double t2 = t * t, t3 = t2 * t;
            return (b * 2 + (c - a) * t + (a * 2 - b * 5 + c * 4 - d) * t2
                + (b * 3 - a - c * 3 + d) * t3) * 0.5;
        }
        static double Distance(RoadPoint a, RoadPoint b)
        { double x = a.X - b.X, z = a.Z - b.Z; return Math.Sqrt(x * x + z * z); }
        static double Dot(RoadPoint a, RoadPoint b) { return a.X * b.X + a.Z * b.Z; }
        static RoadPoint Unit(RoadPoint a)
        { double length = Math.Sqrt(Dot(a, a)); return length > 1e-9 ? a * (1 / length) : new RoadPoint(0, 1); }
        static double Wrap(double value, double length)
        { value %= length; return value < 0 ? value + length : value; }

        public CourseSample Sample(double progress)
        {
            if (double.IsNaN(progress) || double.IsInfinity(progress)) progress = 0;
            double p = Wrap(progress, Length);
            int low = 0, high = MainPoints.Length - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (mainDistances[middle] <= p) low = middle;
                else high = middle - 1;
            }
            int next = (low + 1) % MainPoints.Length;
            double span = mainDistances[low + 1] - mainDistances[low];
            double ratio = span > 0 ? (p - mainDistances[low]) / span : 0;
            return new CourseSample {
                Position = MainPoints[low] + (MainPoints[next] - MainPoints[low]) * ratio,
                Tangent = Unit(MainPoints[next] - MainPoints[low]), Progress = p,
                HalfWidth = MainHalfWidth
            };
        }

        public CourseSample Project(RoadPoint position)
        { return Project(position, 0); }

        // Clearance reduces each ribbon by the kart's half-width before route selection.
        // The returned HalfWidth remains the full road width for rendering and HUD use.
        public CourseSample Project(RoadPoint position, double clearance)
        {
            if (double.IsNaN(clearance) || double.IsInfinity(clearance)) clearance = 0;
            clearance = Math.Max(0, clearance);
            CourseSample best = new CourseSample();
            double bestOutside = double.PositiveInfinity, bestCenter = double.PositiveInfinity;
            for (int road = 0; road < 2; road++)
            {
                RoadPoint[] points = road == 0 ? MainPoints : ShortcutPoints;
                int count = road == 0 ? points.Length : points.Length - 1;
                double width = road == 0 ? MainHalfWidth : ShortcutHalfWidth;
                for (int i = 0; i < count; i++)
                {
                    RoadPoint a = points[i], delta = points[(i + 1) % points.Length] - a;
                    double spanSquared = Dot(delta, delta);
                    if (spanSquared < 1e-10) continue;
                    double t = Math.Max(0, Math.Min(1, Dot(position - a, delta) / spanSquared));
                    RoadPoint center = a + delta * t, tangent = Unit(delta), offset = position - center;
                    double lateral = tangent.Z * offset.X - tangent.X * offset.Z;
                    double centerDistance = Math.Sqrt(Dot(offset, offset));
                    double outside = Math.Max(0, centerDistance - Math.Max(.01, width - clearance));
                    if (outside > bestOutside + 1e-8 ||
                        (Math.Abs(outside - bestOutside) <= 1e-8 && centerDistance >= bestCenter)) continue;
                    double progress = road == 0
                        ? mainDistances[i] + (mainDistances[i + 1] - mainDistances[i]) * t
                        : shortcutStart + (shortcutEnd - shortcutStart) * (i + t) / count;
                    best = new CourseSample { Position = center, Tangent = tangent,
                        Progress = Wrap(progress, Length), HalfWidth = width,
                        Lateral = lateral, IsShortcut = road == 1 };
                    bestOutside = outside;
                    bestCenter = centerDistance;
                }
            }
            return best;
        }
    }
}
