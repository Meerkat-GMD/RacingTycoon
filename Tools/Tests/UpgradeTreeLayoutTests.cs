using System;
using System.Collections.Generic;
using CottonCircuit;

public static class UpgradeTreeLayoutTests
{
    static int passed, failed;
    const double Epsilon = .001;
    struct Point { public double X, Y; public Point(double x, double y) { X = x; Y = y; } }
    struct Box { public double Left, Top, Right, Bottom; }
    sealed class Edge { public string Parent, Child; public Point A, B; }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); passed++; }
        catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex.Message); failed++; }
    }
    static double Cross(Point a, Point b, Point c)
    { return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X); }
    static bool SegmentsConflict(Point a, Point b, Point c, Point d)
    {
        double abC = Cross(a, b, c), abD = Cross(a, b, d), cdA = Cross(c, d, a), cdB = Cross(c, d, b);
        if (abC * abD < -Epsilon && cdA * cdB < -Epsilon) return true;
        if (Math.Abs(abC) > Epsilon || Math.Abs(abD) > Epsilon) return false;
        bool useX = Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y);
        double a0 = useX ? a.X : a.Y, a1 = useX ? b.X : b.Y;
        double b0 = useX ? c.X : c.Y, b1 = useX ? d.X : d.Y;
        return Math.Min(Math.Max(a0, a1), Math.Max(b0, b1)) -
            Math.Max(Math.Min(a0, a1), Math.Min(b0, b1)) > Epsilon;
    }
    static bool Within(Point p, Box r)
    { return p.X >= r.Left && p.X <= r.Right && p.Y >= r.Top && p.Y <= r.Bottom; }
    static bool SegmentHitsBox(Point a, Point b, Box r)
    {
        if (Within(a, r) || Within(b, r)) return true;
        Point tl = new Point(r.Left, r.Top), tr = new Point(r.Right, r.Top);
        Point bl = new Point(r.Left, r.Bottom), br = new Point(r.Right, r.Bottom);
        return SegmentsConflict(a, b, tl, tr) || SegmentsConflict(a, b, tr, br) ||
            SegmentsConflict(a, b, br, bl) || SegmentsConflict(a, b, bl, tl);
    }
    static Box Footprint(UpgradeTreePosition p)
    {
        return new Box { Left = p.X - 88, Top = p.Y - 32,
            Right = p.X + 88, Bottom = p.Y + UpgradeTreeLayout.FootprintBottom(p.Id) };
    }
    static List<Edge> Edges(int tab)
    {
        var result = new List<Edge>();
        foreach (var child in UpgradeTreeLayout.Tabs[tab].Nodes)
            foreach (string parentId in Progression.Find(child.Id).Parents)
            {
                if (UpgradeTreeLayout.TabOf(parentId) != tab) continue;
                var parent = UpgradeTreeLayout.Find(parentId);
                // Must match the renderer: clear the source text/chips, then stop outside the child disc.
                result.Add(new Edge { Parent = parentId, Child = child.Id,
                    A = new Point(parent.X, parent.Y + UpgradeTreeLayout.FootprintBottom(parentId) + 5),
                    B = new Point(child.X, child.Y - 38) });
            }
        return result;
    }
    public static int Main()
    {
        Test("geometry recognizes crossings and overlapping lines", () => {
            Check(SegmentsConflict(new Point(0, 0), new Point(10, 10), new Point(0, 10), new Point(10, 0)), "cross missed");
            Check(SegmentsConflict(new Point(0, 0), new Point(10, 0), new Point(5, 0), new Point(15, 0)), "overlap missed");
            Check(!SegmentsConflict(new Point(0, 0), new Point(10, 0), new Point(10, 0), new Point(20, 0)), "shared endpoint is not overlap");
            Check(SegmentHitsBox(new Point(-5, 5), new Point(15, 5), new Box { Left = 0, Top = 0, Right = 10, Bottom = 10 }), "box hit missed");
        });
        Test("six tabs cover each actual progression node exactly once", () => {
            Check(UpgradeTreeLayout.Tabs.Length == 6, "expected six tabs");
            var seen = new HashSet<string>(); var tabs = new HashSet<string>();
            for (int t = 0; t < UpgradeTreeLayout.Tabs.Length; t++)
            {
                var tab = UpgradeTreeLayout.Tabs[t];
                Check(tabs.Add(tab.Id) && !string.IsNullOrEmpty(tab.Name), "duplicate/unnamed tab");
                foreach (var p in tab.Nodes)
                {
                    Check(Progression.Find(p.Id) != null && seen.Add(p.Id), "missing or duplicated " + p.Id);
                    Check(UpgradeTreeLayout.TabOf(p.Id) == t && object.ReferenceEquals(UpgradeTreeLayout.Find(p.Id), p), "lookup mismatch " + p.Id);
                }
            }
            Check(seen.Count == Progression.Nodes.Length && seen.Count == 31, "incomplete catalog");
            foreach (var n in Progression.Nodes) Check(seen.Contains(n.Id), "missing " + n.Id);
            Check(UpgradeTreeLayout.Find(null) == null && UpgradeTreeLayout.Find("missing") == null &&
                UpgradeTreeLayout.TabOf("missing") == -1, "unknown node lookup");
        });
        Test("all real prerequisites are represented once as a local arrow or external chip", () => {
            int local = 0, external = 0;
            foreach (var child in Progression.Nodes)
            {
                int externalCount = 0, tab = UpgradeTreeLayout.TabOf(child.Id);
                foreach (string parent in child.Parents)
                {
                    Check(UpgradeTreeLayout.TabOf(parent) >= 0, "missing parent " + parent);
                    if (UpgradeTreeLayout.TabOf(parent) == tab) local++;
                    else { external++; externalCount++; }
                }
                Check(UpgradeTreeLayout.ExternalParentCount(child.Id) == externalCount, "chip count " + child.Id);
            }
            Check(local + external == 37, "prerequisite catalog changed");
            Console.WriteLine("  Prerequisites: " + local + " local arrows, " + external + " external chips");
        });
        for (int index = 0; index < UpgradeTreeLayout.Tabs.Length; index++)
        {
            int t = index;
            Test(UpgradeTreeLayout.Tabs[t].Id + ": bounds, footprints and downward arrows", () => {
                var nodes = UpgradeTreeLayout.Tabs[t].Nodes;
                foreach (var p in nodes)
                {
                    var a = Footprint(p);
                    Check(p.X >= 100 && p.X <= 948 && p.Y >= 44 && p.Y <= 440, "center out of bounds " + p.Id);
                    Check(a.Left >= 0 && a.Top >= 0 && a.Right <= 1048 && a.Bottom <= 550, "footprint out of bounds " + p.Id);
                    foreach (var other in nodes)
                    {
                        if (other.Id == p.Id) continue;
                        var b = Footprint(other);
                        Check(a.Right <= b.Left || b.Right <= a.Left || a.Bottom <= b.Top || b.Bottom <= a.Top,
                            "footprint overlap " + p.Id + " / " + other.Id);
                    }
                }
                foreach (var edge in Edges(t))
                {
                    Check(edge.B.Y - edge.A.Y >= 14, "no visible downward arrow " + edge.Parent + " -> " + edge.Child);
                    foreach (var p in nodes)
                        if (p.Id != edge.Parent && p.Id != edge.Child)
                        {
                            var footprint = Footprint(p);
                            // Include a six-pixel radius for stroke/arrowhead geometry around the center line.
                            footprint.Left -= 6; footprint.Right += 6; footprint.Top -= 6; footprint.Bottom += 6;
                            Check(!SegmentHitsBox(edge.A, edge.B, footprint), "arrow hits " + p.Id + " on " + edge.Parent + " -> " + edge.Child);
                        }
                }
            });
            Test(UpgradeTreeLayout.Tabs[t].Id + ": zero line crossings or overlaps", () => {
                var edges = Edges(t);
                for (int a = 0; a < edges.Count; a++)
                    for (int b = a + 1; b < edges.Count; b++)
                        Check(!SegmentsConflict(edges[a].A, edges[a].B, edges[b].A, edges[b].B),
                            edges[a].Parent + " -> " + edges[a].Child + " crosses " + edges[b].Parent + " -> " + edges[b].Child);
            });
        }
        Console.WriteLine("Upgrade tree layout: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
