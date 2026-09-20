using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace CottonCircuit.Editor
{
    public static class RaceCourseBuilder
    {
        static Vector3 V(RoadPoint p, float y = 1.04f) => new Vector3((float)p.X, y, (float)p.Z);
        public static void Build(WorldView world, Dictionary<string, Material> materials)
        {
            var course = RaceCourse.Shared; var root = new GameObject("Sugarway circuit").transform; root.SetParent(world.transform);
            Ribbon("Racing surface", course.MainPoints, true, (float)RaceCourse.MainHalfWidth, new[] { materials["InnerLane"], materials["MiddleLane"], materials["OuterLane"] }, root);
            Ribbon("Sugar cut shortcut", course.ShortcutPoints, false, (float)RaceCourse.ShortcutHalfWidth, new[] { materials["InnerLane"], materials["MiddleLane"], materials["OuterLane"] }, root);
            for (double p = 0; p < course.Length; p += 3.5)
            {
                var s = course.Sample(p); Vector3 tangent = V(s.Tangent, 0), right = new Vector3(tangent.z, 0, -tangent.x);
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 point = V(s.Position) + right * (float)(side * (s.HalfWidth + .25));
                    // Keep both shortcut junctions physically and visually open.
                    var projected = course.Project(new RoadPoint(point.x, point.z));
                    if (projected.IsShortcut && Mathf.Abs((float)projected.Lateral) < RaceCourse.ShortcutHalfWidth + .8) continue;
                    var rail = ProjectBuilder.Place(world.Assets.Barrier, point, Quaternion.LookRotation(-right * side), root);
                    rail.transform.localScale = new Vector3(1.7f, 1, 1);
                }
                // Short dashed line makes speed and the direction of travel readable.
                var dash = ProjectBuilder.Cube("Center dash", V(s.Position, 1.055f), new Vector3(.12f, .016f, .85f), materials["White"], root);
                dash.transform.rotation = Quaternion.LookRotation(tangent);
            }
            for (int i = 2; i < course.ShortcutPoints.Length - 2; i += 2)
            {
                Vector3 tangent = (V(course.ShortcutPoints[i + 1]) - V(course.ShortcutPoints[i - 1])).normalized;
                Vector3 right = new Vector3(tangent.z, 0, -tangent.x);
                foreach (int side in new[] { -1, 1 })
                {
                    var point = V(course.ShortcutPoints[i]) + right * (float)(side * (RaceCourse.ShortcutHalfWidth + .25));
                    var main = course.Project(new RoadPoint(point.x, point.z));
                    if (!main.IsShortcut) continue;
                    ProjectBuilder.Place(world.Assets.Barrier, point, Quaternion.LookRotation(-right * side), root);
                }
            }
            foreach (double fraction in new[] { .09, .19, .28, .39, .49, .61, .72, .86 })
            {
                var s = course.Sample(course.Length * fraction); Vector3 tangent = V(s.Tangent, 0), right = new Vector3(tangent.z, 0, -tangent.x);
                var board = ProjectBuilder.Place(world.Assets.Chevron, V(s.Position) + right * 6.2f, Quaternion.LookRotation(-tangent), root);
                Vector3 future = V(course.Sample(course.Length * fraction + 10).Tangent, 0);
                if (Vector3.SignedAngle(tangent, future, Vector3.up) < 0) board.transform.localScale = new Vector3(-1, 1, 1);
            }
            var start = course.Sample(0); var direction = V(start.Tangent, 0); var across = new Vector3(direction.z, 0, -direction.x);
            var arch = ProjectBuilder.Place(world.Assets.Arch, V(start.Position), Quaternion.LookRotation(direction), root); arch.transform.localScale = new Vector3(2.2f, 1.4f, 1);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 16; col++)
            {
                var tile = ProjectBuilder.Cube("Start checker", V(start.Position, 1.062f) + across * ((col - 7.5f) * .6f) + direction * ((row - .5f) * .6f), new Vector3(.6f, .018f, .6f), materials[(row + col) % 2 == 0 ? "White" : "Navy"], root);
                tile.transform.rotation = Quaternion.LookRotation(direction);
            }
            var entrance = course.ShortcutPoints[3]; var toward = V(course.ShortcutPoints[4]) - V(course.ShortcutPoints[2]);
            ProjectBuilder.Place(world.Assets.ShortcutGate, V(entrance), Quaternion.LookRotation(-toward), root);
        }
        static void Ribbon(string name, RoadPoint[] points, bool closed, float halfWidth, Material[] materials, Transform parent)
        {
            int segments = closed ? points.Length : points.Length - 1; var vertices = new List<Vector3>(); var uvs = new List<Vector2>();
            var triangles = new List<int>[materials.Length]; for (int i = 0; i < triangles.Length; i++) triangles[i] = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                int at = i % points.Length, previous = closed ? (at - 1 + points.Length) % points.Length : Mathf.Max(0, at - 1), next = closed ? (at + 1) % points.Length : Mathf.Min(points.Length - 1, at + 1);
                Vector3 tangent = (V(points[next]) - V(points[previous])).normalized; Vector3 right = new Vector3(tangent.z, 0, -tangent.x);
                vertices.Add(V(points[at], closed ? 1.04f : 1.045f) - right * halfWidth); vertices.Add(V(points[at], closed ? 1.04f : 1.045f) + right * halfWidth);
                uvs.Add(new Vector2(0, i)); uvs.Add(new Vector2(1, i));
                if (i == segments) continue;
                int n = i * 2;
                int sector = Mathf.Min(materials.Length - 1, (int)(RaceCourse.Shared.Project(points[at]).Progress / RaceCourse.Shared.Length * materials.Length));
                triangles[sector].AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 });
            }
            var mesh = new Mesh { name = name, subMeshCount = materials.Length }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs);
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); string path = "Assets/CottonCircuit/Meshes/" + name.Replace(" ", "") + ".asset";
            ProjectBuilder.ReplaceAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); go.GetComponent<MeshRenderer>().sharedMaterials = materials;
        }
    }
}
