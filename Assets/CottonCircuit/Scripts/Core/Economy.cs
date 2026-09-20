using System;
using System.Collections.Generic;
namespace CottonCircuit
{
    [Serializable] public class Economy
    {
        public const int InventoryLimit = 6;
        public int Coins = 80;
        public int Day = 1;
        public int[] Levels = new int[3];
        public List<Product> Inventory = new List<Product>();
        public List<string> CompletedIds = new List<string>();
        public int TotalSold;
        public int LifetimeRevenue;
        public int Capacity { get { return 220 + Levels[1] * 80; } }
        public float MaxSpeed { get { return 18f + Levels[0] * 2.5f; } }
        public bool CompleteRun(Product product)
        {
            if (product == null || product.Grams <= 0 || string.IsNullOrEmpty(product.Id) ||
                Inventory.Count >= InventoryLimit || CompletedIds.Contains(product.Id)) return false;
            Inventory.Add(product);
            CompletedIds.Add(product.Id);
            Day++;
            return true;
        }
        public int Price(Product product)
        {
            if (product == null || product.Grams <= 0) return 0;
            var flavors = new HashSet<int>();
            foreach (var sample in product.Samples) flavors.Add(sample.Flavor);
            return (int)Math.Round((20 + product.Grams * .75 + flavors.Count * 8) * (1 + Levels[2] * .25));
        }
        public int SellNext()
        {
            if (Inventory.Count == 0) return 0;
            int price = Price(Inventory[0]);
            Inventory.RemoveAt(0);
            Coins += price;
            LifetimeRevenue += price;
            TotalSold++;
            return price;
        }
        public int UpgradeCost(int index)
        {
            if (index < 0 || index >= 3 || Levels[index] >= 3) return 0;
            return new[] { 120, 250, 450 }[Levels[index]];
        }
        public bool BuyUpgrade(int index)
        {
            int cost = UpgradeCost(index);
            if (cost == 0 || Coins < cost) return false;
            Coins -= cost;
            Levels[index]++;
            return true;
        }
    }
}
