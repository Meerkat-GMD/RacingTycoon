using UnityEngine;
namespace CottonCircuit
{
    public class KartController : MonoBehaviour
    {
        public float Speed { get; private set; }
        public float Radius { get; private set; } = 10;
        public double Angle { get; private set; }
        public int Flavor => Radius < 8.75f ? 0 : Radius < 11.25f ? 1 : 2;
        public float MaximumSpeed = 11;
        Transform[] wheels;
        void Awake()
        {
            var all = GetComponentsInChildren<Transform>();
            wheels = System.Array.FindAll(all, t => t.name.StartsWith("Wheel"));
        }
        public void ResetPosition()
        {
            Speed = 0; Radius = 10; Angle = Mathf.PI * .5;
            ApplyPose();
        }
        public double Drive(float throttle, float steering, bool brake, float deltaTime)
        {
            float dt = Mathf.Clamp(deltaTime, 0, .1f);
            float target = brake ? 0 : throttle > .1f ? MaximumSpeed : 0;
            Speed = Mathf.MoveTowards(Speed, target, dt * (brake ? 14 : throttle > .1f ? 5 : 1.5f));
            Radius = Mathf.Clamp(Radius + steering * dt * 3.8f, 7.5f, 12.5f);
            double delta = Speed / Radius * dt;
            Angle += delta;
            ApplyPose();
            if (wheels != null) foreach (var wheel in wheels) wheel.Rotate(Vector3.right, Speed * dt * 100, Space.Self);
            return delta;
        }
        public void Stop() { Speed = 0; }
        void ApplyPose()
        {
            float a = (float)Angle;
            transform.position = new Vector3(Mathf.Cos(a) * Radius, 1.05f, Mathf.Sin(a) * Radius);
            transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
        }
    }
}
