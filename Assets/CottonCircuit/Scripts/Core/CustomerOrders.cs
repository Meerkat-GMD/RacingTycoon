using System;
using System.Collections.Generic;

namespace CottonCircuit
{
    [Serializable] public class CustomerOrder
    {
        public const double Patience = 300;
        public string Id;
        public int Flavor;
        public int Size;
        public double Remaining;
    }

    public class ServiceReceipt
    {
        public int Price;
        public int Tip;
        public double Satisfaction;
    }

    public static class CandyRecipe
    {
        public static int FlavorOf(Product product)
        {
            if (product == null || product.Grams <= 0 || product.Samples == null || product.Samples.Count == 0)
                return -1;
            var counts = new int[3];
            foreach (var sample in product.Samples)
            {
                if (sample == null || sample.Flavor < 0 || sample.Flavor > 2) return -1;
                counts[sample.Flavor]++;
            }
            int best = -1;
            int highest = 0;
            foreach (var sample in product.Samples)
            {
                if (counts[sample.Flavor] > highest)
                {
                    best = sample.Flavor;
                    highest = counts[best];
                }
            }
            return best;
        }

        public static int SizeOf(Product product)
        {
            if (product == null || product.Grams < 50) return -1;
            return product.Grams < 100 ? 0 : 1;
        }

        public static int TargetGrams(int size) { return size == 0 ? 60 : size == 1 ? 120 : 0; }

        public static bool Matches(Product product, CustomerOrder order)
        {
            return order != null && order.Flavor >= 0 && order.Flavor <= 2 &&
                order.Size >= 0 && order.Size <= 1 &&
                FlavorOf(product) == order.Flavor && SizeOf(product) == order.Size;
        }
    }

    public class OrderManager
    {
        const double ArrivalInterval = 15;
        const double MaximumTick = 3600;
        readonly Economy economy;
        public List<CustomerOrder> Orders { get { return economy.Orders; } }
        public int Revision { get; private set; }

        public OrderManager(Economy economy)
        {
            if (economy == null) throw new ArgumentNullException("economy");
            this.economy = economy;
            if (economy.Orders == null) economy.Orders = new List<CustomerOrder>();
            if (double.IsNaN(economy.NextCustomerIn) || double.IsInfinity(economy.NextCustomerIn) ||
                economy.NextCustomerIn <= 0) economy.NextCustomerIn = ArrivalInterval;
            if (economy.Orders.Count == 0 && economy.OrderSerial == 0) Arrive();
        }

        void Arrive()
        {
            int flavor = economy.OrderSerial % 3;
            int size = (economy.OrderSerial / 3) % 2;
            if (economy.OrderSerial == 0 && economy.Inventory != null)
            {
                foreach (var product in economy.Inventory)
                {
                    int stockedFlavor = CandyRecipe.FlavorOf(product);
                    int stockedSize = CandyRecipe.SizeOf(product);
                    if (stockedFlavor >= 0 && stockedSize >= 0)
                    {
                        flavor = stockedFlavor;
                        size = stockedSize;
                        break;
                    }
                }
            }
            economy.OrderSerial++;
            economy.Orders.Add(new CustomerOrder {
                Id = "order-" + economy.OrderSerial,
                Flavor = flavor,
                Size = size,
                Remaining = CustomerOrder.Patience
            });
            Revision++;
        }

        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            seconds = Math.Min(seconds, MaximumTick);
            while (true)
            {
                for (int i = Orders.Count - 1; i >= 0; i--)
                {
                    if (Orders[i].Remaining > 0) continue;
                    Orders.RemoveAt(i);
                    economy.MissedOrders++;
                    Revision++;
                }
                if (Orders.Count < 2 && economy.NextCustomerIn <= 0)
                {
                    Arrive();
                    economy.NextCustomerIn = ArrivalInterval;
                }
                if (seconds <= 0) break;

                double step = seconds;
                foreach (var order in Orders) step = Math.Min(step, order.Remaining);
                bool hasRoom = Orders.Count < 2;
                if (hasRoom) step = Math.Min(step, economy.NextCustomerIn);
                if (step <= 0) break;
                foreach (var order in Orders) order.Remaining -= step;
                if (hasRoom) economy.NextCustomerIn -= step;
                seconds -= step;
            }
        }

        public ServiceReceipt Serve(string orderId, string productId)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(productId) || economy.Inventory == null)
                return null;
            int orderIndex = Orders.FindIndex(order => order != null && order.Id == orderId && order.Remaining > 0);
            int productIndex = economy.Inventory.FindIndex(product => product != null && product.Id == productId);
            if (orderIndex < 0 || productIndex < 0) return null;
            var orderToServe = Orders[orderIndex];
            var productToServe = economy.Inventory[productIndex];
            if (!CandyRecipe.Matches(productToServe, orderToServe)) return null;
            int price = economy.Price(productToServe);
            if (price <= 0) return null;
            double satisfaction = Math.Max(0, Math.Min(1, orderToServe.Remaining / CustomerOrder.Patience));
            int tip = (int)Math.Round(price * .25 * satisfaction);
            economy.Inventory.RemoveAt(productIndex);
            Orders.RemoveAt(orderIndex);
            economy.Coins += price + tip;
            economy.LifetimeRevenue += price + tip;
            economy.TotalTips += tip;
            economy.TotalSold++;
            economy.OrdersServed++;
            economy.SatisfactionTotal += satisfaction;
            Revision++;
            return new ServiceReceipt { Price = price, Tip = tip, Satisfaction = satisfaction };
        }
    }
}
