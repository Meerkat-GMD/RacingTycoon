using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CottonCircuit
{
    public class SaveStore
    {
        [Serializable] public class Envelope { public int Version = 2; public Economy State; }
        readonly string directory;
        public string DirectoryPath { get { return directory; } }
        string FilePath => Path.Combine(directory, "cotton-circuit.json");
        public string Error { get; private set; }
        public bool CanSave { get; private set; } = true;
        public SaveStore(string directory) { this.directory = directory; }
        public Economy Load()
        {
            if (!File.Exists(FilePath)) return new Economy();
            try
            {
                if (new FileInfo(FilePath).Length > 8 * 1024 * 1024) throw new InvalidDataException();
                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(FilePath));
                if (envelope == null || envelope.State == null) throw new InvalidDataException();
                if (envelope.Version == 1)
                {
                    if (!ValidLegacy(envelope.State)) throw new InvalidDataException();
                    MigrateLegacy(envelope.State);
                }
                else if (envelope.Version != 2) throw new InvalidDataException();
                if (!Valid(envelope.State)) throw new InvalidDataException();
                return envelope.State;
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
            { Error = "저장 파일을 읽지 못했어요. 원본을 보관하고 새로 시작할 수 있어요."; CanSave = false; return new Economy(); }
        }
        public bool Save(Economy state)
        {
            if (!CanSave) return false;
            if (!Valid(state))
            { Error = "저장 데이터가 올바르지 않아 저장하지 못했어요."; return false; }
            try
            {
                Directory.CreateDirectory(directory);
                var temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new Envelope { State = state }, true));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = "저장하지 못했어요. 저장 폴더의 공간과 접근 권한을 확인해주세요."; return false; }
        }
        public bool ArchiveAndReset()
        {
            try
            {
                if (File.Exists(FilePath)) File.Move(FilePath, FilePath + ".archived-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
                CanSave = true; Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = "기존 저장 파일을 보관하지 못했어요."; return false; }
        }
        public static bool Valid(Economy e)
        {
            if (e == null || e.ShelfLevel < 0 || e.ShelfLevel > 2 ||
                e.Orders == null || e.Orders.Count > 2 || e.OrderSerial < 0 ||
                e.TotalTips < 0 || e.MissedOrders < 0 || e.OrdersServed < 0 ||
                e.OrdersServed > e.TotalSold || e.OrdersServed > e.OrderSerial ||
                (long)e.OrdersServed + e.MissedOrders + e.Orders.Count > e.OrderSerial ||
                e.TotalTips > e.LifetimeRevenue ||
                !Finite(e.NextCustomerIn) || e.NextCustomerIn <= 0 || e.NextCustomerIn > 15 ||
                !Finite(e.SatisfactionTotal) || e.SatisfactionTotal < 0 ||
                e.SatisfactionTotal > e.OrdersServed || !ValidBase(e, e.StockCapacity)) return false;
            var orderIds = new HashSet<string>();
            foreach (var order in e.Orders)
            {
                if (order == null || string.IsNullOrEmpty(order.Id) || !orderIds.Add(order.Id) ||
                    order.Flavor < 0 || order.Flavor > 2 || order.Size < 0 || order.Size > 1 ||
                    !Finite(order.Remaining) || order.Remaining <= 0 ||
                    order.Remaining > CustomerOrder.Patience) return false;
                int serial;
                if (!order.Id.StartsWith("order-", StringComparison.Ordinal) ||
                    !int.TryParse(order.Id.Substring(6), out serial) || serial <= 0 ||
                    order.Id != "order-" + serial || serial > e.OrderSerial) return false;
            }
            return true;
        }
        static bool ValidLegacy(Economy e) { return ValidBase(e, Economy.InventoryLimit); }
        static void MigrateLegacy(Economy e)
        {
            e.ShelfLevel = 0;
            e.Orders = new List<CustomerOrder>();
            e.OrderSerial = 0;
            e.TotalTips = 0;
            e.MissedOrders = 0;
            e.NextCustomerIn = 15;
            e.SatisfactionTotal = 0;
            e.OrdersServed = 0;
        }
        static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        static bool ValidBase(Economy e, int stockCapacity)
        {
            if (e == null || e.Coins < 0 || e.Coins > 1000000000 || e.Day < 1 || e.Levels == null || e.Levels.Length != 3 ||
                e.Inventory == null || e.Inventory.Count > stockCapacity || e.CompletedIds == null || e.TotalSold < 0 || e.LifetimeRevenue < 0) return false;
            foreach (int level in e.Levels) if (level < 0 || level > 3) return false;
            var ids = new HashSet<string>();
            foreach (var product in e.Inventory)
            {
                if (product == null || string.IsNullOrEmpty(product.Id) || !ids.Add(product.Id) || product.Samples == null ||
                    product.Samples.Count == 0 || product.Samples.Count > 230 || product.Grams != product.Samples.Count * 2) return false;
                double previous = 0;
                foreach (var sample in product.Samples)
                {
                    if (sample == null || sample.Flavor < 0 || sample.Flavor > 2 || !Finite(sample.Radius) || sample.Radius < 7 || sample.Radius > 13 ||
                        !Finite(sample.Angle) || sample.Angle <= previous || sample.Angle > 100) return false;
                    previous = sample.Angle;
                }
                if (!e.CompletedIds.Contains(product.Id)) e.CompletedIds.Add(product.Id);
            }
            return true;
        }
    }
}
