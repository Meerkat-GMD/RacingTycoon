using System;
using System.Collections.Generic;

namespace CottonCircuit
{
    [Serializable] public class WindingSample { public double Angle; public double Radius; public int Flavor; }
    [Serializable] public class Product
    {
        public string Id;
        public int Grams;
        public int Quality;
        public bool DistanceBased;
        public double DistanceMeters;
        public int FlavorIndex;
        public int SugarGrade = 1;
        public List<WindingSample> Samples = new List<WindingSample>();
    }
    public class Production
    {
        public const double SampleStep = Math.PI / 10;
        public readonly List<WindingSample> Samples = new List<WindingSample>();
        readonly int capacity;
        double angle;
        bool finished;
        public int Grams { get { return Samples.Count * 2; } }
        public bool IsFull { get { return Grams + 2 > capacity; } }
        public double Turns { get { return angle / (2 * Math.PI); } }
        public Production(int capacity) { this.capacity = Math.Max(2, capacity); }

        public int Advance(double radians, double radius, int flavor)
        {
            if (finished || IsFull || double.IsNaN(radians) || double.IsInfinity(radians) || radians <= 0 ||
                double.IsNaN(radius) || double.IsInfinity(radius) || radius < 7 || radius > 13 || flavor < 0 || flavor > 2) return 0;
            angle += radians;
            int previous = Samples.Count;
            int target = (int)Math.Min(capacity / 2, Math.Floor((angle + 1e-8) / SampleStep));
            while (Samples.Count < target)
                Samples.Add(new WindingSample { Angle = (Samples.Count + 1) * SampleStep, Radius = radius, Flavor = flavor });
            return Samples.Count - previous;
        }

        public Product Finish()
        {
            if (finished) return null;
            finished = true;
            if (Samples.Count == 0) return null;
            return new Product { Id = Guid.NewGuid().ToString("N"), Grams = Grams, Samples = new List<WindingSample>(Samples) };
        }
    }
}
