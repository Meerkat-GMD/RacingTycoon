using System;
namespace CottonCircuit
{
    public enum GameMode { Shop, Racing, Results }
    public class GameSession
    {
        public readonly Economy Economy;
        public GameMode Mode { get; private set; }
        public Production Production { get; private set; }
        public Product Result { get; private set; }
        public double Remaining { get; private set; }
        public bool RecipeMode { get; private set; }
        public int RecipeMap { get; private set; }
        public int RecipeFlavor { get; private set; }
        public int ResultBonus { get; private set; }
        public double Elapsed { get; private set; }
        public bool Paused;
        public GameSession(Economy economy) { Economy = economy; }
        public bool StartRun()
        {
            return StartRun(Economy.Capacity, 60);
        }
        public bool StartRun(int targetGrams, double duration)
        {
            if (Mode != GameMode.Shop || Economy.Inventory.Count >= Economy.StockCapacity ||
                targetGrams <= 0 || double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0) return false;
            Production = new Production(Math.Min(targetGrams, Economy.Capacity));
            Result = null;
            RecipeMode = false;
            ResultBonus = 0;
            Elapsed = 0;
            Remaining = duration;
            Paused = false;
            Mode = GameMode.Racing;
            return true;
        }
        public void Tick(double deltaTime, double radians, double radius, int flavor)
        {
            if (RecipeMode || Mode != GameMode.Racing || Paused || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime <= 0) return;
            double elapsed = Math.Min(deltaTime, Remaining);
            Production.Advance(radians * elapsed / deltaTime, radius, flavor);
            Remaining = Math.Max(0, Remaining - elapsed);
            Elapsed += elapsed;
            if (Remaining <= 0 || Production.IsFull) FinishRun();
        }
        public bool StartRecipe(int map, int flavor)
        {
            if (map < 0 || map > 1 || flavor < 0 || flavor > 2 ||
                !StartRun(RaceRecipe.TargetGrams(map), RaceRecipe.Timeout(map))) return false;
            RecipeMode = true;
            RecipeMap = map;
            RecipeFlavor = flavor;
            return true;
        }
        public void TickRecipe(double dt, double radians, double radius, int completedLaps, int boosts, int wallHits)
        {
            if (!RecipeMode || Mode != GameMode.Racing || Paused ||
                double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0) return;
            double step = Math.Min(dt, Remaining);
            Production.Advance(radians * step / dt, radius, RecipeFlavor);
            Elapsed += step;
            Remaining = Math.Max(0, Remaining - step);
            if (Remaining <= 0) { FinishRun(); return; }
            if (completedLaps < 1) return;

            // Lap tracking is authoritative; finish any samples missed by frame boundaries.
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 7 || radius > 13)
                radius = Production.Samples.Count == 0 ? 10 : Production.Samples[Production.Samples.Count - 1].Radius;
            Production.Advance(RaceRecipe.TargetGrams(RecipeMap) * Production.SampleStep, radius, RecipeFlavor);
            Result = Production.Finish();
            Result.Quality = RaceRecipe.Quality(boosts, wallHits, Economy.Levels[1]);
            if (Economy.CompleteRun(Result))
            {
                int maximumBonus = RecipeMap == 0 ? 20 : 40;
                ResultBonus = (int)Math.Round(maximumBonus * Math.Min(1, RaceRecipe.ParSeconds(RecipeMap) / Elapsed));
                Economy.Coins += ResultBonus;
            }
            Mode = GameMode.Results;
        }
        public void FinishRun()
        {
            if (Mode != GameMode.Racing || Paused) return;
            if (RecipeMode)
            {
                Result = null;
                ResultBonus = 0;
                Mode = GameMode.Results;
                return;
            }
            Result = Production.Finish();
            if (Result != null) Economy.CompleteRun(Result);
            Mode = GameMode.Results;
        }
        public void ReturnToShop() { if (Mode == GameMode.Results) { Mode = GameMode.Shop; Paused = false; } }
    }
}
