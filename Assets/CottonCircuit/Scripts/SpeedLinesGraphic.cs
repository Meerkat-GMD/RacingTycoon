using UnityEngine;
using UnityEngine.UI;
namespace CottonCircuit
{
    // Screen-edge streaks keep the road and steering cues unobscured.
    public sealed class SpeedLinesGraphic : MaskableGraphic
    {
        public float Strength { get; private set; }
        float phase;
        public void SetDriving(KartController kart, bool active)
        {
            bool downhill = kart.DriveModel.Style == DrivingStyle.Downhill;
            Strength = !active ? 0 : downhill ? Mathf.Clamp01((kart.Speed - 13) / 42) * .8f
                : Mathf.Clamp01((kart.Speed - 13) / 20) * (kart.Boosting ? 1 : .38f);
            SetVerticesDirty();
        }
        void Update()
        {
            if (Strength <= 0) return;
            phase = Mathf.Repeat(phase + Time.unscaledDeltaTime * (1.6f + Strength), 1);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (Strength <= 0) return;
            Rect r = rectTransform.rect;
            var center = new Vector2(r.center.x, r.yMin + r.height * .54f);
            for (int i = 0; i < 20; i++)
            {
                float p = Mathf.Repeat(phase + i * .173f, 1);
                float y = .12f + Mathf.Repeat(i * .287f, .67f);
                var edge = new Vector2(i % 2 == 0 ? r.xMin : r.xMax, r.yMin + y * r.height);
                var a = Vector2.Lerp(center, edge, .77f + p * .19f);
                var b = Vector2.Lerp(center, edge, .82f + p * .18f);
                Vector2 direction = (b - a).normalized;
                var side = new Vector2(-direction.y, direction.x) * (1.1f + Strength);
                var tint = new Color(1, .98f, .90f, Strength * Mathf.Sin(p * Mathf.PI) * .65f);
                int n = vh.currentVertCount;
                vh.AddVert(a - side, tint, Vector2.zero); vh.AddVert(a + side, tint, Vector2.zero);
                vh.AddVert(b + side, tint, Vector2.zero); vh.AddVert(b - side, tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
