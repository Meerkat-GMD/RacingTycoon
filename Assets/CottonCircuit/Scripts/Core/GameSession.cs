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
        public bool Paused;
        public GameSession(Economy economy) { Economy = economy; }
        public bool StartRun()
        {
            if (Mode != GameMode.Shop || Economy.Inventory.Count >= Economy.InventoryLimit) return false;
            Production = new Production(Economy.Capacity);
            Result = null;
            Remaining = 60;
            Paused = false;
            Mode = GameMode.Racing;
            return true;
        }
        public void Tick(double deltaTime, double radians, double radius, int flavor)
        {
            if (Mode != GameMode.Racing || Paused || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime <= 0) return;
            double elapsed = Math.Min(deltaTime, Remaining);
            Production.Advance(radians * elapsed / deltaTime, radius, flavor);
            Remaining = Math.Max(0, Remaining - elapsed);
            if (Remaining <= 0 || Production.IsFull) FinishRun();
        }
        public void FinishRun()
        {
            if (Mode != GameMode.Racing || Paused) return;
            Result = Production.Finish();
            if (Result != null) Economy.CompleteRun(Result);
            Mode = GameMode.Results;
        }
        public void ReturnToShop() { if (Mode == GameMode.Results) { Mode = GameMode.Shop; Paused = false; } }
    }
}
