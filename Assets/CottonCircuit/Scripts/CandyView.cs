using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CottonCircuit
{
    public class CandyView : MonoBehaviour
    {
        public GameAssets Assets;
        readonly List<Mesh> owned = new List<Mesh>();
        MeshFilter[] filters;
        public void Show(IList<WindingSample> samples)
        {
            if (filters == null)
            {
                filters = new MeshFilter[3];
                for (int i = 0; i < 3; i++)
                {
                    var part = new GameObject("Cotton " + i, typeof(MeshFilter), typeof(MeshRenderer));
                    part.transform.SetParent(transform, false);
                    filters[i] = part.GetComponent<MeshFilter>();
                    var renderer = part.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = Assets.Flavors[i];
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                }
            }
            foreach (var mesh in owned) Destroy(mesh);
            owned.Clear();
            for (int flavor = 0; flavor < 3; flavor++)
            {
                var parts = new List<CombineInstance>();
                if (samples != null)
                {
                    foreach (var sample in samples)
                    {
                        if (sample.Flavor != flavor) continue;
                        float a = (float)sample.Angle;
                        float turns = a / (2 * Mathf.PI);
                        // Radius comes from the chosen lane; height follows actual winding turns.
                        float r = .52f + ((float)sample.Radius - 7.5f) * .10f;
                        float y = 2.25f + turns * .45f;
                        var position = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                        parts.Add(new CombineInstance { mesh = Assets.PuffMesh,
                            transform = Matrix4x4.TRS(position, Quaternion.Euler(0, a * Mathf.Rad2Deg, 0), new Vector3(1.4f, 1.35f, 1.4f)) });
                        // Overlapping center tufts keep wide-lane cotton fluffy around the stick,
                        // while every tuft still inherits the player's recorded color and height.
                        if (((int)System.Math.Round(sample.Angle / Production.SampleStep)) % 4 == 0)
                            parts.Add(new CombineInstance { mesh = Assets.PuffMesh,
                                transform = Matrix4x4.TRS(new Vector3(0, y, 0), Quaternion.identity, Vector3.one * 1.45f) });
                    }
                }
                var combined = new Mesh { name = "Wound " + flavor, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(parts.ToArray(), true, true);
                filters[flavor].sharedMesh = combined;
                owned.Add(combined);
            }
        }
        void OnDestroy() { foreach (var mesh in owned) if (mesh) Destroy(mesh); }
    }
}
