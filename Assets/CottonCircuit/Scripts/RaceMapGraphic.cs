using UnityEngine;
using UnityEngine.UI;
namespace CottonCircuit
{
    public class RaceMapGraphic : MaskableGraphic
    {
        public KartController Kart;
        Color[] roadColors;
        RaceCourse cachedCourse;
        Vector2 center, extent;
        Vector2 Map(RoadPoint p)
        {
            float scale = Mathf.Min(rectTransform.rect.width / extent.x, rectTransform.rect.height / extent.y);
            return new Vector2(rectTransform.rect.width, rectTransform.rect.height) * .5f + (new Vector2((float)p.X, (float)p.Z) - center) * scale;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var course = Kart && Kart.DriveModel != null ? Kart.DriveModel.Course : RaceCourse.Shared;
            if (cachedCourse != course)
            {
                cachedCourse = course;
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                foreach (var point in course.MainPoints)
                {
                    Vector2 p = new Vector2((float)point.X, (float)point.Z); min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
                center = (min + max) * .5f; extent = max - min + Vector2.one * 25;
                roadColors = new Color[course.MainPoints.Length];
                for (int i = 0; i < roadColors.Length; i++) roadColors[i] = course == RaceCourse.ForMap(0) ? Palette.Pink : Palette.Soda;
            }
            for (int i = 0; i < course.MainPoints.Length; i++)
                Line(vh, Map(course.MainPoints[i]), Map(course.MainPoints[(i + 1) % course.MainPoints.Length]), 7, roadColors[i]);
            for (int i = 0; i < course.ShortcutPoints.Length - 1; i++) Line(vh, Map(course.ShortcutPoints[i]), Map(course.ShortcutPoints[i + 1]), 3, Palette.Ink);
            Vector2 start = Map(course.MainPoints[0]); Line(vh, start + Vector2.down * 6, start + Vector2.up * 6, 3, Color.white);
            if (!Kart || Kart.DriveModel == null) return;
            var model = Kart.DriveModel; Vector2 position = Map(model.Position), facing = new Vector2(Mathf.Sin((float)model.Heading), Mathf.Cos((float)model.Heading));
            Vector2 right = new Vector2(facing.y, -facing.x); int at = vh.currentVertCount;
            Vertex(vh, position + facing * 10, Palette.Ink); Vertex(vh, position - facing * 6 + right * 6, Palette.Ink); Vertex(vh, position - facing * 6 - right * 6, Palette.Ink);
            vh.AddTriangle(at, at + 1, at + 2);
        }
        static void Vertex(VertexHelper vh, Vector2 point, Color color) { var v = UIVertex.simpleVert; v.position = point; v.color = color; vh.AddVert(v); }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 delta = (b - a).normalized, side = new Vector2(-delta.y, delta.x) * width * .5f; int at = vh.currentVertCount;
            Vertex(vh, a - side, color); Vertex(vh, a + side, color); Vertex(vh, b + side, color); Vertex(vh, b - side, color);
            vh.AddTriangle(at, at + 1, at + 2); vh.AddTriangle(at, at + 2, at + 3);
        }
    }
}
