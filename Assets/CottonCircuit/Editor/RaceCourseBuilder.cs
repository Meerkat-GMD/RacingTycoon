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
            world.CourseRoots = new Transform[2];
            for (int map = 0; map < world.CourseRoots.Length; map++)
            {
                var root = new GameObject("Sugarway circuit " + (map + 1)).transform;
                root.SetParent(world.transform);
                world.CourseRoots[map] = root;
                BuildCourse(world, materials, RaceCourse.ForMap(map), root);
                root.gameObject.SetActive(map == 0);
            }
        }
        static void BuildCourse(WorldView world, Dictionary<string, Material> materials, RaceCourse course, Transform root)
        {
            string suffix = " " + (course.MapIndex + 1);
            Ribbon("Racing surface" + suffix, course.MainPoints, true, -(float)RaceCourse.MainHalfWidth,
                (float)RaceCourse.MainHalfWidth, 1.04f, materials[course.MapIndex == 0 ? "InnerLane" : "MiddleLane"], root);
            Ribbon("Sugar cut shortcut" + suffix, course.ShortcutPoints, false, -(float)RaceCourse.ShortcutHalfWidth,
                (float)RaceCourse.ShortcutHalfWidth, 1.06f, materials["RoadNeutral"], root);
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
                var dash = ProjectBuilder.Cube("Route dash", V(s.Position, 1.068f), new Vector3(.12f, .016f, .85f), materials["White"], root);
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
                var board = PlaceRoadside(world.Assets.Chevron, "Corner chevron", V(s.Position) + right * 6.8f,
                    Quaternion.LookRotation(-tangent), 1, course, root);
                if (board == null) continue;
                Vector3 future = V(course.Sample(course.Length * fraction + 10).Tangent, 0);
                if (Vector3.SignedAngle(tangent, future, Vector3.up) < 0) board.transform.localScale = new Vector3(-1, 1, 1);
            }
            // Existing Blender props pass close to the camera every half-second
            // at base speed. Test their entire footprint against both road ribbons.
            for (int marker = 0; marker * 12 + 14 < course.Length - 12; marker++)
            {
                var s = course.Sample(marker * 12 + 14);
                Vector3 tangent = V(s.Tangent, 0), right = new Vector3(tangent.z, 0, -tangent.x);
                int side = marker % 2 == 0 ? -1 : 1;
                PlaceRoadside(world.Assets.Crystal, "Roadside crystal", V(s.Position) + right * (side * 7.1f),
                    Quaternion.Euler(0, marker * 47, 0), 2.4f, course, root);
                if (marker % 3 == 1)
                    PlaceRoadside(world.Assets.Tree, "Roadside tree", V(s.Position) - right * (side * 8.1f),
                        Quaternion.Euler(0, marker * 31, 0), .95f, course, root);
            }
            var start = course.Sample(0); var direction = V(start.Tangent, 0); var across = new Vector3(direction.z, 0, -direction.x);
            var arch = ProjectBuilder.Place(world.Assets.Arch, V(start.Position), Quaternion.LookRotation(direction), root); arch.transform.localScale = new Vector3(2.75f, 1.4f, 1);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 16; col++)
            {
                var tile = ProjectBuilder.Cube("Start checker", V(start.Position, 1.062f) + across * ((col - 7.5f) * .6f) + direction * ((row - .5f) * .6f), new Vector3(.6f, .018f, .6f), materials[(row + col) % 2 == 0 ? "White" : "Navy"], root);
                tile.transform.rotation = Quaternion.LookRotation(direction);
            }
            // The narrower gate belongs after the split, not across the main road.
            int gatePoint = course.ShortcutPoints.Length / 3;
            var entrance = course.ShortcutPoints[gatePoint];
            var toward = V(course.ShortcutPoints[gatePoint + 1]) - V(course.ShortcutPoints[gatePoint - 1]);
            var gate = ProjectBuilder.Place(world.Assets.ShortcutGate, V(entrance), Quaternion.LookRotation(-toward), root);
            gate.transform.localScale = new Vector3(1.4f, 1, 1);
            if (world.Assets.CandyTunnel != null)
            {
                // Keep the 8m-long, 11m-wide opening wholly on the first straight.
                var tunnel = course.Sample(course.MapIndex == 0 ? 24 : 26);
                ProjectBuilder.Place(world.Assets.CandyTunnel, V(tunnel.Position), Quaternion.LookRotation(V(tunnel.Tangent, 0)), root);
            }
            if (world.Assets.FinishMarker != null)
                PlaceRoadside(world.Assets.FinishMarker, "Finish landmark", V(start.Position) + across * 8 + direction * 3,
                    Quaternion.LookRotation(-direction), 1, course, root);
        }
        static GameObject PlaceRoadside(GameObject prefab, string name, Vector3 position, Quaternion rotation,
            float scale, RaceCourse course, Transform root)
        {
            var prop = ProjectBuilder.Place(prefab, position, rotation, root);
            prop.name = name;
            prop.transform.localScale = Vector3.one * scale;
            float radius = 0;
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                float x = Mathf.Max(Mathf.Abs(bounds.min.x - position.x), Mathf.Abs(bounds.max.x - position.x));
                float z = Mathf.Max(Mathf.Abs(bounds.min.z - position.z), Mathf.Abs(bounds.max.z - position.z));
                radius = Mathf.Max(radius, Mathf.Sqrt(x * x + z * z));
            }
            var projected = course.Project(new RoadPoint(position.x, position.z));
            Vector3 offset = position - V(projected.Position, position.y);
            float fromCenter = new Vector2(position.x, position.z).magnitude;
            if (offset.magnitude < projected.HalfWidth + radius + .55f ||
                fromCenter - radius < 30 || fromCenter + radius > 137)
            {
                Object.DestroyImmediate(prop);
                return null;
            }
            return prop;
        }
        static void Ribbon(string name, RoadPoint[] points, bool closed, float left, float rightEdge, float height, Material material, Transform parent)
        {
            int segments = closed ? points.Length : points.Length - 1; var vertices = new List<Vector3>(); var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                int at = i % points.Length, previous = closed ? (at - 1 + points.Length) % points.Length : Mathf.Max(0, at - 1), next = closed ? (at + 1) % points.Length : Mathf.Min(points.Length - 1, at + 1);
                Vector3 tangent = (V(points[next]) - V(points[previous])).normalized; Vector3 right = new Vector3(tangent.z, 0, -tangent.x);
                vertices.Add(V(points[at], height) + right * left); vertices.Add(V(points[at], height) + right * rightEdge);
                uvs.Add(new Vector2(0, i)); uvs.Add(new Vector2(1, i));
                if (i == segments) continue;
                int n = i * 2;
                triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); string path = "Assets/CottonCircuit/Meshes/" + name.Replace(" ", "") + ".asset";
            ProjectBuilder.ReplaceAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
