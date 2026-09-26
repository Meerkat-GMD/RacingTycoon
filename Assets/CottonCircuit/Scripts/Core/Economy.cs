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
        public BusinessState Business;
        public ProgressionState Progression;
        public int StockCapacity { get { return Progression == null ? 6 + Math.Max(0, Math.Min(2, ShelfLevel)) * 3 : CottonCircuit.Progression.Capacity(this); } }
        public int ShelfCost { get { return Progression != null ? 0 : ShelfLevel == 0 ? 160 : ShelfLevel == 1 ? 300 : 0; } }
        public int Capacity { get { return 220 + Levels[1] * 80; } }
        public float MaxSpeed { get { return Progression == null ? 26f + Levels[0] * 2.5f : (float)(26 * CottonCircuit.Progression.SpeedMultiplier(this)); } }
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
            if (product.DistanceBased) return DistancePrice(product, ShopShift.Stars(product.Quality));
            var flavors = new HashSet<int>();
            foreach (var sample in product.Samples) flavors.Add(sample.Flavor);
            double qualityMultiplier = 1 + Math.Max(0, Math.Min(100, product.Quality)) * .003;
            double legacyMultiplier = Progression == null ? (1 + Levels[2] * .25) :
                CottonCircuit.Progression.SalesMultiplier(this) * CottonCircuit.Progression.FlavorPriceMultiplier(this, product.FlavorIndex);
            return (int)Math.Round((20 + product.Grams * .75 + flavors.Count * 8) *
                legacyMultiplier * qualityMultiplier);
        }
        // The coins a distance product's stars add on top of its starless price.
        public int StarBonus(Product product)
        {
            if (product == null || product.Grams <= 0 || !product.DistanceBased) return 0;
            return Price(product) - DistancePrice(product, 0);
        }
        int DistancePrice(Product product, int stars)
        {
            int size = ShopShift.SizeOf(product);
            if (size < 0) return 0;
            double multiplier = Progression == null ? (1 + Levels[2] * .25) :
                CottonCircuit.Progression.SalesMultiplier(this) * CottonCircuit.Progression.FlavorPriceMultiplier(this, product.FlavorIndex);
            return (int)Math.Round(ShopShift.TierPrice * (size + 1) * multiplier * (1 + stars * CottonCircuit.Progression.StarBonus(this)));
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
            if (Progression != null || index < 0 || index >= 3 || Levels[index] >= 3) return 0;
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
