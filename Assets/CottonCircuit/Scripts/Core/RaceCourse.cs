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
        public const double MainHalfWidth = 7.2;
        public const double ShortcutHalfWidth = 3.3;
        const int StepsPerAnchor = 24;
        static readonly RaceCourse[] Maps = { new RaceCourse(0), new RaceCourse(1), new RaceCourse(2) };
        public static int MapCount { get { return Maps.Length; } }
        public static readonly RaceCourse Shared = Maps[0];
        public static RaceCourse ForMap(int map)
        {
            if (map < 0 || map >= Maps.Length) throw new ArgumentOutOfRangeException("map");
            return Maps[map];
        }
        public readonly RoadPoint[] MainPoints;
        public readonly RoadPoint[] ShortcutPoints;
        readonly double[] mainDistances;
        readonly double[] shortcutDistances;
        readonly double shortcutStart, shortcutEnd;
        public double Length { get; private set; }
        public int MapIndex { get; private set; }
        public RoadPoint BoundsMin { get; private set; }
        public RoadPoint BoundsMax { get; private set; }

        RaceCourse(int map)
        {
            MapIndex = map;
            // Three separate paths share the same machine ring and road widths.
            // The third path bends across the south and enters a long northeast shortcut.
            RoadPoint[] anchors = map == 0 ? new[] {
                new RoadPoint(0, -66.5), new RoadPoint(49, -66.5), new RoadPoint(81.2, -47.6),
                new RoadPoint(93.1, -15.4), new RoadPoint(83.3, 16.1), new RoadPoint(90, 49),
                new RoadPoint(70, 73.5), new RoadPoint(36.4, 81.2), new RoadPoint(10.5, 67.9),
                new RoadPoint(-16.8, 74.9), new RoadPoint(-60.9, 68.6), new RoadPoint(-86.8, 39.9),
                new RoadPoint(-85.4, 0), new RoadPoint(-58.1, -51.8), new RoadPoint(-29.4, -66.5)
            } : map == 1 ? new[] {
                new RoadPoint(-21, -94.2), new RoadPoint(30, -94.2), new RoadPoint(69, -76.2),
                new RoadPoint(98.4, -37.8), new RoadPoint(96, 12), new RoadPoint(82, 39),
                new RoadPoint(83, 69), new RoadPoint(51.6, 96), new RoadPoint(9.6, 106.8),
                new RoadPoint(-22.2, 87), new RoadPoint(-62.4, 90.6), new RoadPoint(-94.2, 61.2),
                new RoadPoint(-105, 14.4), new RoadPoint(-90, -26.4), new RoadPoint(-81, -42),
                new RoadPoint(-76, -63), new RoadPoint(-55.2, -94.2)
            } : new[] {
                new RoadPoint(0, -105), new RoadPoint(38, -105), new RoadPoint(75, -87),
                new RoadPoint(105, -56), new RoadPoint(118, -20), new RoadPoint(100, 13),
                new RoadPoint(110, 47), new RoadPoint(92, 80), new RoadPoint(61, 104),
                new RoadPoint(25, 95), new RoadPoint(-5, 108), new RoadPoint(-43, 93),
                new RoadPoint(-75, 65), new RoadPoint(-106, 34), new RoadPoint(-115, -10),
                new RoadPoint(-93, -47), new RoadPoint(-64, -79), new RoadPoint(-33, -104)
            };
            MainPoints = new RoadPoint[anchors.Length * StepsPerAnchor];
            for (int i = 0; i < MainPoints.Length; i++)
            {
                int anchor = i / StepsPerAnchor;
                double t = (double)(i % StepsPerAnchor) / StepsPerAnchor;
                MainPoints[i] = Catmull(anchors[(anchor + anchors.Length - 1) % anchors.Length],
                    anchors[anchor], anchors[(anchor + 1) % anchors.Length],
                    anchors[(anchor + 2) % anchors.Length], t);
            }
            mainDistances = new double[MainPoints.Length + 1];
            for (int i = 0; i < MainPoints.Length; i++)
                mainDistances[i + 1] = mainDistances[i] + Distance(MainPoints[i], MainPoints[(i + 1) % MainPoints.Length]);
            Length = mainDistances[MainPoints.Length];
            int entry = map == 0 ? 4 : map == 1 ? 9 : 5;
            int exit = map == 0 ? 7 : map == 1 ? 12 : 9;
            shortcutStart = mainDistances[entry * StepsPerAnchor];
            shortcutEnd = mainDistances[exit * StepsPerAnchor];
            double handle = map == 0 ? 28 : map == 1 ? 33 : 34;
            RoadPoint entryControl = anchors[entry] + Unit(anchors[entry + 1] - anchors[entry - 1]) * handle;
            RoadPoint exitControl = anchors[exit] - Unit(anchors[exit + 1] - anchors[exit - 1]) * handle;
            ShortcutPoints = new RoadPoint[StepsPerAnchor * 3 + 1];
            shortcutDistances = new double[ShortcutPoints.Length];
            for (int i = 0; i < ShortcutPoints.Length; i++)
            {
                double t = (double)i / (ShortcutPoints.Length - 1), u = 1 - t;
                ShortcutPoints[i] = anchors[entry] * (u * u * u) + entryControl * (3 * u * u * t)
                    + exitControl * (3 * u * t * t) + anchors[exit] * (t * t * t);
                if (i > 0) shortcutDistances[i] = shortcutDistances[i - 1] + Distance(ShortcutPoints[i - 1], ShortcutPoints[i]);
            }
            double minX = double.PositiveInfinity, minZ = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxZ = double.NegativeInfinity;
            foreach (var points in new[] { MainPoints, ShortcutPoints })
                foreach (var point in points)
                {
                    minX = Math.Min(minX, point.X); minZ = Math.Min(minZ, point.Z);
                    maxX = Math.Max(maxX, point.X); maxZ = Math.Max(maxZ, point.Z);
                }
            BoundsMin = new RoadPoint(minX - MainHalfWidth, minZ - MainHalfWidth);
            BoundsMax = new RoadPoint(maxX + MainHalfWidth, maxZ + MainHalfWidth);
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
                        : shortcutStart + (shortcutEnd - shortcutStart) *
                            (shortcutDistances[i] + (shortcutDistances[i + 1] - shortcutDistances[i]) * t) /
                            shortcutDistances[shortcutDistances.Length - 1];
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
