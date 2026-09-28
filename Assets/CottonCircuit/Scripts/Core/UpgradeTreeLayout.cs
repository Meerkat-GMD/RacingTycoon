namespace CottonCircuit
{
    /// <summary>Screen-only positions; purchase prerequisites remain owned by Progression.</summary>
    public sealed class UpgradeTreePosition
    {
        public readonly string Id;
        public readonly float X, Y;
        public UpgradeTreePosition(string id, float x, float y) { Id = id; X = x; Y = y; }
    }

    public sealed class UpgradeTreeTab
    {
        public readonly string Id;
        public readonly UpgradeTreePosition[] Nodes;
        public UpgradeTreeTab(string id, params UpgradeTreePosition[] nodes)
        { Id = id; Nodes = nodes; }
        public string Name => Strings.Get("tab." + Id);
    }

    /// <summary>
    /// Small downward trees replace the non-planar all-in-one map. Cross-tab prerequisites are
    /// rendered as clickable requirement chips beneath the target, never as lines across tabs.
    /// Coordinates are node-disc centers in a 1048 x 550 map.
    /// </summary>
    public static class UpgradeTreeLayout
    {
        static UpgradeTreePosition P(string id, float x, float y) { return new UpgradeTreePosition(id, x, y); }
        public static readonly UpgradeTreeTab[] Tabs = {
            new UpgradeTreeTab("business",
                P("hours", 174, 70), P("patience", 406, 70), P("shelf", 638, 70),
                P("ads", 870, 70), P("group_visit", 638, 300), P("repeat_ads", 870, 300)),
            new UpgradeTreeTab("production",
                P("stick_speed", 355, 60), P("sugar_2", 825, 60),
                P("stick_quality", 155, 228), P("stick_saving", 480, 228), P("sugar_3", 910, 228),
                P("quality_focus", 155, 416), P("sugar_saving", 650, 416)),
            new UpgradeTreeTab("machines",
                P("machine_2", 524, 44), P("machine_3", 524, 228), P("flavor_vanilla", 524, 416)),
            new UpgradeTreeTab("staff",
                P("worker_1", 524, 44), P("worker_speed", 200, 228),
                P("worker_grade_2", 524, 228), P("worker_2", 848, 228), P("worker_grade_3", 524, 416)),
            new UpgradeTreeTab("sales",
                P("sales", 272, 44), P("location_1", 776, 44),
                P("flavor_price", 154, 228), P("location_price", 490, 228),
                P("location_2", 826, 228), P("location_3", 826, 416)),
            new UpgradeTreeTab("kart",
                P("engine", 524, 44), P("handling", 524, 228), P("coupe", 524, 416))
        };

        public static int TabOf(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return -1;
            for (int tab = 0; tab < Tabs.Length; tab++)
                foreach (var position in Tabs[tab].Nodes)
                    if (position.Id == nodeId) return tab;
            return -1;
        }

        public static UpgradeTreePosition Find(string nodeId)
        {
            int tab = TabOf(nodeId);
            if (tab < 0) return null;
            foreach (var position in Tabs[tab].Nodes) if (position.Id == nodeId) return position;
            return null;
        }

        public static int ExternalParentCount(string nodeId)
        {
            var node = Progression.Find(nodeId);
            if (node == null) return 0;
            int count = 0, tab = TabOf(nodeId);
            foreach (string parent in node.Parents) if (TabOf(parent) != tab) count++;
            return count;
        }

        /// <summary>Offset below the disc center occupied by its caption and requirement chips.</summary>
        public static float FootprintBottom(string nodeId) { return 69 + 23 * ExternalParentCount(nodeId); }
    }
}
