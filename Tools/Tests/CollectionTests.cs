using System;
using CottonCircuit;
public static class CollectionTests
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        try {
            var c = RaceCourse.Shared;
            Require(SugarCollection.Flavor(c.Length * .1, -2, false, c.Length) == 0, "inner strawberry strip must collect strawberry");
            Require(SugarCollection.Flavor(c.Length * .5, -2, false, c.Length) == 1, "inner soda strip must collect soda");
            Require(SugarCollection.Flavor(c.Length * .8, -2, false, c.Length) == 2, "inner vanilla strip must collect vanilla");
            Require(SugarCollection.Flavor(20, 2, false, c.Length) == -1, "outside lane must bypass sugar");
            Require(SugarCollection.Flavor(20, -.49, false, c.Length) == -1, "neutral boundary must bypass sugar");
            Require(SugarCollection.Flavor(20, -.5, false, c.Length) == 0, "collection boundary must be deterministic");
            Require(SugarCollection.Flavor(20, -2, true, c.Length) == -1, "shortcut must not contaminate a recipe");
            Require(SugarCollection.Flavor(double.NaN, -2, false, c.Length) == -1, "invalid progress must collect nothing");
            Require(SugarCollection.Flavor(20, double.NaN, false, c.Length) == -1, "invalid lateral must collect nothing");
            Console.WriteLine("RESULT: 9 collection checks passed"); return 0;
        } catch(Exception e) { Console.WriteLine("FAIL " + e.Message); return 1; }
    }
}
