namespace CottonCircuit
{
    public static class SugarCollection
    {
        public const double Boundary = -.5;
        public static int Flavor(double progress, double lateral, bool shortcut, double length)
        {
            if (shortcut || double.IsNaN(progress) || double.IsInfinity(progress) || double.IsNaN(lateral) || double.IsInfinity(lateral) ||
                double.IsNaN(length) || double.IsInfinity(length) || length <= 0 || lateral > Boundary || lateral < -RaceCourse.MainHalfWidth) return -1;
            progress = (progress % length + length) % length;
            return System.Math.Min(2, (int)(progress / length * 3));
        }
    }
}
