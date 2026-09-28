using System;

namespace CottonCircuit
{
    public static class RaceRecipe
    {
        static void ValidateMap(int map)
        {
            if (map < 0 || map > 1) throw new ArgumentOutOfRangeException("map");
        }

        public static int TargetGrams(int map) { ValidateMap(map); return map == 0 ? 60 : 120; }
        public static double Timeout(int map) { ValidateMap(map); return map == 0 ? 180 : 240; }
        public static double ParSeconds(int map) { ValidateMap(map); return map == 0 ? 24 : 30; }
        public static string Name(int map) { ValidateMap(map); return Strings.Get("course." + map); }

        public static int Quality(int boosts, int hits, int tankLevel)
        {
            long score = 50 + Math.Max(0, Math.Min(10, boosts)) * 5 +
                Math.Max(0, Math.Min(3, tankLevel)) * 5 - Math.Max(0L, hits) * 10;
            return (int)Math.Max(0, Math.Min(100, score));
        }
    }
}
