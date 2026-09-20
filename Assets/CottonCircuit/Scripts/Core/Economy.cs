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
        public int ShelfLevel;
        public List<CustomerOrder> Orders = new List<CustomerOrder>();
        public int OrderSerial;
        public int TotalTips;
        public int MissedOrders;
        public double NextCustomerIn = 15;
        public double SatisfactionTotal;
        public int OrdersServed;
        public int TotalSold;
        public int LifetimeRevenue;
        public int StockCapacity { get { return 6 + Math.Max(0, Math.Min(2, ShelfLevel)) * 3; } }
        public int ShelfCost { get { return ShelfLevel == 0 ? 160 : ShelfLevel == 1 ? 300 : 0; } }
        public int Capacity { get { return 220 + Levels[1] * 80; } }
        public float MaxSpeed { get { return 18f + Levels[0] * 2.5f; } }
        public bool CompleteRun(Product product)
        {
            if (product == null || product.Grams <= 0 || string.IsNullOrEmpty(product.Id) ||
                Inventory.Count >= StockCapacity || CompletedIds.Contains(product.Id)) return false;
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
        public bool BuyShelf()
        {
            int cost = ShelfCost;
            if (cost == 0 || Coins < cost) return false;
            Coins -= cost;
            ShelfLevel++;
            return true;
        }
        public bool Discard(string productId)
        {
            if (string.IsNullOrEmpty(productId) || Inventory == null) return false;
            int index = Inventory.FindIndex(product => product != null && product.Id == productId);
            if (index < 0) return false;
            Inventory.RemoveAt(index);
            return true;
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
