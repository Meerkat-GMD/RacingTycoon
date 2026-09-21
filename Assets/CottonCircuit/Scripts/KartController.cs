using UnityEngine;
namespace CottonCircuit
{
    public class KartController : MonoBehaviour
    {
        public ArcadeDrive DriveModel { get; private set; }
        public float Speed => DriveModel == null ? 0 : (float)DriveModel.Speed;
        public float Radius => DriveModel == null ? 10 : Mathf.Clamp(10 + (float)DriveModel.Sample.Lateral * .5f, 7.5f, 12.5f);
        public int ConfiguredFlavor;
        public int Flavor => ConfiguredFlavor;
        public int CollectionFlavor => ConfiguredFlavor;
        public bool IsCollecting => CollectionFlavor >= 0 && DriveModel.LastRewardDistance > 0;
        public float MaximumSpeed = 26;
        public bool Boosting => DriveModel != null && DriveModel.BoostRemaining > 0;
        public bool Drifting => DriveModel != null && DriveModel.IsDrifting;
        public float Charge => DriveModel == null ? 0 : (float)DriveModel.DriftCharge;
        public float Steering { get; private set; }
        public float ImpactFlash { get; private set; }
        Transform[] wheels;
        Transform visual;
        Transform kartVisual, coupeRoot;
        TrailRenderer[] skids, jets;
        Material skidMaterial, jetMaterial;
        void Awake()
        {
            DriveModel = new ArcadeDrive();
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            kartVisual = visual;
            wheels = System.Array.FindAll(GetComponentsInChildren<Transform>(), t => t.name.StartsWith("Wheel"));
            skidMaterial = new Material(Shader.Find("Sprites/Default"));
            jetMaterial = new Material(Shader.Find("Sprites/Default"));
            skids = new TrailRenderer[2]; jets = new TrailRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -.63f : .63f;
                skids[i] = Trail("Tire mark", new Vector3(side, .045f, -.65f), skidMaterial, new Color(.20f, .20f, .29f, .65f), .17f, 3);
                jets[i] = Trail("Sugar boost", new Vector3(side, .5f, -1.1f), jetMaterial, Palette.Soda, .32f, .3f);
            }
        }
        TrailRenderer Trail(string label, Vector3 position, Material material, Color color, float width, float lifetime)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false); go.transform.localPosition = position;
            var trail = go.AddComponent<TrailRenderer>(); trail.sharedMaterial = material; trail.time = lifetime;
            trail.startWidth = width; trail.endWidth = width * .15f; trail.minVertexDistance = .12f;
            trail.startColor = color; trail.endColor = new Color(color.r, color.g, color.b, 0);
            trail.emitting = false; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return trail;
        }
        public void ResetPosition()
        {
            if (DriveModel == null) DriveModel = new ArcadeDrive();
            DriveModel.Reset(); ClearTrails(); ApplyPose(0);
        }
        public void SetCourse(RaceCourse course)
        {
            DriveModel = new ArcadeDrive(course); ResetPosition();
        }
        public void SetStyle(DrivingStyle style, GameObject coupePrefab)
        {
            DriveModel.Style = style;
            bool downhill = style == DrivingStyle.Downhill && coupePrefab;
            if (downhill && !coupeRoot)
            {
                coupeRoot = Instantiate(coupePrefab, transform).transform;
                coupeRoot.name = "Downhill coupe visual";
            }
            kartVisual.gameObject.SetActive(!downhill);
            if (coupeRoot) coupeRoot.gameObject.SetActive(downhill);
            visual = downhill ? coupeRoot.GetChild(0) : kartVisual;
            wheels = System.Array.FindAll(visual.GetComponentsInChildren<Transform>(), t => t.name.StartsWith("Wheel"));
            ClearTrails(); ApplyPose(0);
        }
        public void Recover()
        {
            DriveModel.Recover(); ImpactFlash = 0; ClearTrails(); ApplyPose(0);
        }
        void ClearTrails()
        {
            if (skids != null) foreach (var t in skids) { t.emitting = false; t.Clear(); }
            if (jets != null) foreach (var t in jets) { t.emitting = false; t.Clear(); }
        }
        public double Drive(float throttle, float steering, bool brake, float deltaTime, bool drift = false, bool boost = false)
        {
            DriveModel.MaximumSpeed = MaximumSpeed;
            int hits = DriveModel.WallHits;
            DriveModel.Step(throttle, steering, brake, drift, deltaTime, boost);
            Steering = (float)DriveModel.SteeringInput;
            ImpactFlash = hits != DriveModel.WallHits ? .35f : Mathf.Max(0, ImpactFlash - deltaTime);
            ApplyPose(deltaTime);
            if (wheels != null) foreach (var wheel in wheels) wheel.Rotate(Vector3.right, Speed * deltaTime * 150, Space.Self);
            SetEffects(true);
            return (DriveModel.LastRewardDistance + .25 * DriveModel.LastBoostedRewardDistance) / DriveModel.Course.Length * System.Math.PI * 2;
        }
        public void SetEffects(bool active)
        {
            if (skids != null) foreach (var t in skids) t.emitting = active && Drifting && Speed > 5;
            if (jets != null) foreach (var t in jets)
            {
                t.emitting = active && DriveModel.Style == DrivingStyle.Kart && (Boosting || Charge > .32f);
                t.startColor = Boosting ? (DriveModel.BoostTier == 2 ? Palette.Yellow : Palette.Soda) : Charge >= .75f ? Palette.Yellow : Palette.Pink;
                t.time = Boosting ? .5f : .22f; t.startWidth = Boosting ? .55f : .25f;
            }
        }
        public void Stop() { DriveModel.Stop(); SetEffects(false); }
        void ApplyPose(float dt)
        {
            var p = DriveModel.Position;
            transform.SetPositionAndRotation(new Vector3((float)p.X, 1.05f, (float)p.Z), Quaternion.Euler(0, (float)DriveModel.Heading * Mathf.Rad2Deg, 0));
            if (visual)
            {
                var target = Quaternion.Euler(Boosting ? -3 : 0, 180, -Steering * Mathf.Clamp01(Speed / 12) * (Drifting ? 9 : 4));
                visual.localRotation = dt <= 0 ? target : Quaternion.Slerp(visual.localRotation, target, 1 - Mathf.Exp(-dt * 10));
            }
        }
        void OnDestroy() { if (skidMaterial) Destroy(skidMaterial); if (jetMaterial) Destroy(jetMaterial); }
    }
}
