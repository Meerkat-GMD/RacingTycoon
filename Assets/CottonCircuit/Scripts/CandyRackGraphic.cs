using UnityEngine;

namespace CottonCircuit
{
    // Coordinates are measured from the top-left so stock and its metal clip use
    // one layout even though Graphic mesh coordinates run from the bottom-left.
    public sealed class CandyRackGraphic : UnityEngine.UI.MaskableGraphic
    {
        public const float Width = 448;
        public const float Height = 222;
        int capacity = 6;

        public void Configure(int capacity)
        {
            raycastTarget = false;
            int next = NormalizeCapacity(capacity);
            if (this.capacity == next) return;
            this.capacity = next;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        static int NormalizeCapacity(int value) { return value <= 6 ? 6 : value <= 9 ? 9 : 12; }

        public static Rect BagRect(int index, int capacity)
        {
            capacity = NormalizeCapacity(capacity);
            index = Mathf.Clamp(index, 0, capacity - 1);
            if (capacity == 6)
            {
                bool upper = index < 3;
                int column = index % 3;
                float center = upper ? 84 + 140 * column : 72 + 152 * column;
                float top = upper ? (column == 1 ? 0 : 4) : (column == 1 ? 100 : 106);
                return new Rect(center - 42, top, 84, 98);
            }
            if (capacity == 9)
            {
                bool upper = index < 4;
                int column = upper ? index : index - 4;
                float center = upper ? 62 + 108 * column : 40 + 92 * column;
                float top = upper ? (column == 0 || column == 3 ? 4 : 0) : (column % 2 == 0 ? 102 : 97);
                return new Rect(center - 37, top, 74, 92);
            }
            else
            {
                bool upper = index < 6;
                int column = index % 6;
                int distance = Mathf.Min(column, 5 - column);
                float center = upper ? 38 + 74.4f * column : 42 + 72.8f * column;
                float top = upper ? (distance == 0 ? 10 : distance == 1 ? 3 : 0) : (distance == 0 ? 105 : distance == 1 ? 98 : 94);
                return new Rect(center - 31, top, 62, 86);
            }
        }

        public static Vector2 ClipPoint(int index, int capacity)
        {
            Rect bag = BagRect(index, capacity);
            return new Vector2(bag.x + bag.width * .5f, bag.y + bag.height * .86f);
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Color dark = new Color(.41f,.45f,.49f,1);
            Color steel = new Color(.63f,.67f,.70f,1);
            Color silver = new Color(.84f,.87f,.88f,1);
            Color glint = new Color(.96f,.97f,.97f,1);
            Vector2 hub = new Vector2(224, 190);

            Oval(vh, new Vector2(224, 217), 37, 3.6f, new Color(.20f,.25f,.31f,.08f));
            for (int i = 0; i < capacity; i++)
            {
                Vector2 end = ClipPoint(i, capacity);
                Vector2 bend = Vector2.Lerp(hub, end, .46f) + new Vector2(0, -5);
                Wire(vh, hub, bend, dark, silver);
                Wire(vh, bend, end, dark, silver);
            }

            // A shallow elliptical foot and a narrow polished mast keep the
            // silhouette closer to a shop display stand than a shelf or grid.
            Oval(vh, new Vector2(224, 214), 29, 5, dark);
            Oval(vh, new Vector2(224, 212.8f), 28.5f, 4.3f, steel);
            Oval(vh, new Vector2(224, 211.5f), 26.5f, 3.4f, silver);
            Line(vh, new Vector2(206, 211), new Vector2(220, 209.6f), 1, glint);
            Line(vh, new Vector2(224, 186), new Vector2(224, 211), 7, dark);
            Line(vh, new Vector2(223.7f, 186), new Vector2(223.7f, 210.5f), 4.3f, steel);
            Line(vh, new Vector2(222.6f, 187), new Vector2(222.6f, 210), 1.2f, glint);
            Oval(vh, new Vector2(224, 190.5f), 7.5f, 3.6f, dark);
            Oval(vh, new Vector2(224, 189.1f), 7.5f, 2.3f, silver);
            Line(vh, new Vector2(219, 189), new Vector2(225, 188), 1, glint);

            for (int i = 0; i < capacity; i++)
            {
                Vector2 end = ClipPoint(i, capacity);
                Line(vh, end, end + new Vector2(0, 7), 4.2f, dark);
                Line(vh, end + new Vector2(-.35f, .8f), end + new Vector2(-.35f, 6.1f), 2.1f, silver);
                Line(vh, end + new Vector2(-1, .3f), end + new Vector2(-2.8f, -5.2f), 1.7f, dark);
                Line(vh, end + new Vector2(1, .3f), end + new Vector2(2.5f, -5.5f), 1.7f, dark);
                Line(vh, end + new Vector2(-1.2f, -.4f), end + new Vector2(-2.7f, -4.9f), .65f, glint);
                Line(vh, end + new Vector2(.9f, -.4f), end + new Vector2(2.3f, -5.2f), .65f, glint);
            }
        }

        void Wire(UnityEngine.UI.VertexHelper vh, Vector2 from, Vector2 to, Color dark, Color silver)
        {
            Line(vh, from, to, 1.5f, dark);
            Vector2 glint = new Vector2(-.25f, -.2f);
            Line(vh, from + glint, to + glint, .65f, silver);
        }

        Vector2 Point(Vector2 point)
        {
            Rect bounds = rectTransform.rect;
            return new Vector2(bounds.xMin + point.x * bounds.width / Width, bounds.yMax - point.y * bounds.height / Height);
        }

        void Line(UnityEngine.UI.VertexHelper vh, Vector2 from, Vector2 to, float width, Color tint)
        {
            Vector2 a = Point(from), b = Point(to);
            Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized * (rectTransform.rect.width / Width) * width * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - side, tint * color, Vector2.zero);
            vh.AddVert(a + side, tint * color, Vector2.zero);
            vh.AddVert(b + side, tint * color, Vector2.zero);
            vh.AddVert(b - side, tint * color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        void Oval(UnityEngine.UI.VertexHelper vh, Vector2 center, float radiusX, float radiusY, Color tint)
        {
            const int steps = 32;
            int start = vh.currentVertCount;
            vh.AddVert(Point(center), tint * color, Vector2.zero);
            for (int i = 0; i <= steps; i++)
            {
                float angle = i * Mathf.PI * 2 / steps;
                vh.AddVert(Point(center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY)), tint * color, Vector2.zero);
            }
            for (int i = 0; i < steps; i++) vh.AddTriangle(start, start + i + 1, start + i + 2);
        }
    }
}
