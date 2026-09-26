using UnityEngine;

namespace CottonCircuit
{
    /// <summary>Only local prerequisites are drawn; external requirements have named navigation controls.</summary>
    public sealed class UpgradeGraphGraphic : UnityEngine.UI.MaskableGraphic
    {
        public Economy Economy;
        public int TabIndex;
        public string PinnedNode;
        string focused;
        public void Focus(string nodeId) { if (focused == nodeId) return; focused = nodeId; SetVerticesDirty(); }
        public void RefreshEdges() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear(); if (Economy == null) return;
            foreach (var node in Progression.Nodes)
            {
                if (UpgradeTreeLayout.TabOf(node.Id) != TabIndex) continue;
                foreach (string parent in node.Parents)
                {
                    if (UpgradeTreeLayout.TabOf(parent) != TabIndex) continue;
                    var source = UpgradeTreeLayout.Find(parent); var target = UpgradeTreeLayout.Find(node.Id);
                    Vector2 a = Point(source.X, source.Y + UpgradeTreeLayout.FootprintBottom(parent) + 5);
                    Vector2 b = Point(target.X, target.Y - 38), direction = (b - a).normalized;
                    bool unlocked = Progression.Level(Economy, parent) > 0;
                    string focus = focused ?? PinnedNode;
                    bool highlighted = focus == parent || focus == node.Id;
                    Color tint = highlighted ? Palette.Hex("50796D") : unlocked ? Palette.Hex("82AC9D") : Palette.Hex("B4BDB2");
                    Line(vh, a, b, highlighted ? 3.5f : 2.5f, tint);
                    Vector2 side = new Vector2(-direction.y, direction.x);
                    Line(vh, b - direction * 8 + side * 5, b, 2.5f, tint);
                    Line(vh, b - direction * 8 - side * 5, b, 2.5f, tint);
                }
            }
        }
        Vector2 Point(float x, float y) => new Vector2(rectTransform.rect.xMin + x, rectTransform.rect.yMax - y);
        static void Line(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 d = (b - a).normalized, side = new Vector2(-d.y, d.x) * width * .5f; int at = vh.currentVertCount;
            Add(vh, a - side, tint); Add(vh, a + side, tint); Add(vh, b + side, tint); Add(vh, b - side, tint);
            vh.AddTriangle(at, at + 1, at + 2); vh.AddTriangle(at, at + 2, at + 3);
        }
        static void Add(UnityEngine.UI.VertexHelper vh, Vector2 p, Color tint) { var v = UIVertex.simpleVert; v.position = p; v.color = tint; vh.AddVert(v); }
    }
}
