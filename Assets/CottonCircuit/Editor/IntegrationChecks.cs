using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace CottonCircuit.Editor
{
    public static class IntegrationChecks
    {
        [Serializable] class LegacyEnvelope { public int Version = 1; public LegacyEconomy State; }
        [Serializable] class LegacyEconomy
        {
            public int Coins;
            public int Day;
            public int[] Levels;
            public List<Product> Inventory;
            public List<string> CompletedIds;
            public int TotalSold;
            public int LifetimeRevenue;
        }
        static int count;
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception("COTTON_CHECK_FAILED " + message);
            count++; Debug.Log("COTTON_CHECK_PASS " + message);
        }
        static Product MakeStock()
        {
            var production = new Production(220);
            production.Advance(3 * Math.PI, 10, 2);
            return production.Finish();
        }
        static void CheckOrderSaves(string root)
        {
            var stock = MakeStock();
            var legacyDirectory = Path.Combine(root, "legacy");
            Directory.CreateDirectory(legacyDirectory);
            var legacy = new LegacyEnvelope { State = new LegacyEconomy {
                Coins = 413, Day = 8, Levels = new[] { 2, 1, 3 },
                Inventory = new List<Product> { stock }, CompletedIds = new List<string> { stock.Id },
                TotalSold = 6, LifetimeRevenue = 795
            } };
            File.WriteAllText(Path.Combine(legacyDirectory, "cotton-circuit.json"), JsonUtility.ToJson(legacy, true));
            var legacyStore = new SaveStore(legacyDirectory);
            var migrated = legacyStore.Load();
            Check(legacyStore.CanSave && legacyStore.DirectoryPath == legacyDirectory &&
                migrated.Coins == 413 && migrated.Day == 8 && migrated.Levels[0] == 2 &&
                migrated.Levels[1] == 1 && migrated.Levels[2] == 3 &&
                migrated.TotalSold == 6 && migrated.LifetimeRevenue == 795 &&
                migrated.Inventory.Count == 1 && migrated.Inventory[0].Id == stock.Id,
                "V1 save keeps money, stock, and upgrades");
            Check(migrated.Orders != null && migrated.Orders.Count == 0 && migrated.ShelfLevel == 0 &&
                migrated.OrderSerial == 0 && migrated.NextCustomerIn == 15 &&
                migrated.TotalTips == 0 && migrated.OrdersServed == 0 && migrated.MissedOrders == 0 &&
                migrated.SatisfactionTotal == 0, "V1 save initializes order fields");
            Check(legacyStore.Save(migrated) &&
                JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(Path.Combine(legacyDirectory, "cotton-circuit.json"))).Version == 2,
                "migrated save writes V2");

            var expanded = new Economy { Coins = 820, ShelfLevel = 1, OrderSerial = 12,
                NextCustomerIn = 7.5, TotalTips = 42, MissedOrders = 4,
                OrdersServed = 3, TotalSold = 9, LifetimeRevenue = 1024, SatisfactionTotal = 2.25 };
            for (int i = 0; i < 9; i++) Check(expanded.CompleteRun(MakeStock()), "expanded shelf accepts stock " + i);
            expanded.Orders.Add(new CustomerOrder { Id = "order-11", Flavor = 2, Size = 0, Remaining = 85 });
            expanded.Orders.Add(new CustomerOrder { Id = "order-12", Flavor = 0, Size = 1, Remaining = 17.5 });
            var expandedDirectory = Path.Combine(root, "orders");
            var expandedStore = new SaveStore(expandedDirectory);
            Check(expandedStore.Save(expanded), "V2 save accepts expanded shelf and waiting orders");
            var restored = new SaveStore(expandedDirectory).Load();
            Check(restored.ShelfLevel == 1 && restored.StockCapacity == 9 && restored.Inventory.Count == 9 &&
                restored.Coins == 820 && restored.Orders.Count == 2 &&
                restored.Orders[0].Id == "order-11" && restored.Orders[0].Remaining == 85 &&
                restored.Orders[1].Id == "order-12" && restored.Orders[1].Remaining == 17.5 &&
                restored.NextCustomerIn == 7.5 && restored.OrderSerial == 12 &&
                restored.TotalTips == 42 && restored.MissedOrders == 4 &&
                restored.OrdersServed == 3 && restored.SatisfactionTotal == 2.25,
                "V2 round trip retains orders, timing, rewards, and shelf capacity");
            expanded.ShelfLevel = 0;
            Check(!SaveStore.Valid(expanded), "inventory beyond current shelf capacity rejected");
            expanded.ShelfLevel = 1;
            expanded.Orders[1].Id = expanded.Orders[0].Id;
            Check(!SaveStore.Valid(expanded), "duplicate order ID rejected");
            expanded.Orders[1].Id = "order-12";
            expanded.Orders[1].Remaining = double.NaN;
            Check(!SaveStore.Valid(expanded), "nonfinite patience rejected");
            expanded.Orders[1].Remaining = 17.5;
            expanded.OrderSerial = 11;
            Check(!SaveStore.Valid(expanded), "order serial behind a waiting ID rejected");
            expanded.OrderSerial = 12;
            expanded.SatisfactionTotal = 4;
            Check(!SaveStore.Valid(expanded), "satisfaction exceeding served count rejected");
            expanded.SatisfactionTotal = 2.25;
            expanded.OrdersServed = expanded.TotalSold + 1;
            Check(!SaveStore.Valid(expanded), "orders served exceeding sales rejected");
            expanded.OrdersServed = 3;
            expanded.NextCustomerIn = 16;
            Check(!SaveStore.Valid(expanded), "invalid arrival countdown rejected");
            expanded.NextCustomerIn = 7.5;
            expanded.TotalTips = -1;
            Check(!SaveStore.Valid(expanded), "negative tips rejected");
            expanded.TotalTips = 42;
            expanded.Orders[1].Size = 2;
            var priorV2 = File.ReadAllText(Path.Combine(expandedDirectory, "cotton-circuit.json"));
            Check(!expandedStore.Save(expanded) &&
                File.ReadAllText(Path.Combine(expandedDirectory, "cotton-circuit.json")) == priorV2,
                "invalid in-memory V2 does not overwrite valid save");
            File.WriteAllText(Path.Combine(expandedDirectory, "cotton-circuit.json"),
                JsonUtility.ToJson(new SaveStore.Envelope { Version = 2, State = expanded }, true));
            var invalidStore = new SaveStore(expandedDirectory);
            invalidStore.Load();
            Check(!invalidStore.CanSave && !invalidStore.Save(new Economy()),
                "malformed V2 file protected from overwrite");
        }
        public static void Run()
        {
            count = 0;
            if (!UnityEngine.Object.FindAnyObjectByType<GameController>())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            Check(game && game.World && game.UI && game.Audio, "wired playable scene");
            Check(game.World.Kart && game.World.CentralCandy && game.World.Customer && game.World.SugarThread, "world gameplay references");
            Debug.Log("COTTON_MESH_DIAGNOSTIC assets=" + (game.World.Assets ? game.World.Assets.name : "null") + " mesh=" + (game.World.Assets.PuffMesh ? game.World.Assets.PuffMesh.name + " vertices=" + game.World.Assets.PuffMesh.vertexCount : "null"));
            Check(game.World.Assets.PuffMesh && game.World.Assets.PuffMesh.vertexCount > 0, "Blender cotton mesh imported");
            var assets = game.World.Assets;
            Check(assets.Kart && assets.Kiosk && assets.Spinner && assets.Puff && assets.Customer && assets.Crystal && assets.Arch && assets.Tree && assets.Lamp, "all nine Blender assets used");
            Check(assets.Chevron && assets.Barrier && assets.ShortcutGate, "three new Blender racing props imported");
            Check(GameObject.Find("Sugarway circuit") && GameObject.Find("Racing surface") && GameObject.Find("Sugar cut shortcut"), "main course and shortcut rendered from physics geometry");
            Check(game.World.CandyCamera && game.World.CandyPreview, "live cotton preview camera wired");
            foreach (var part in assets.Kart.GetComponentsInChildren<Transform>())
                if (part.name == "Nose") Check(assets.Kart.transform.InverseTransformPoint(part.position).z > .2f, "kart nose faces gameplay +Z");
            var production = new Production(220); production.Advance(Math.PI, 7.5, 0); production.Advance(Math.PI, 12.5, 2);
            var e = new Economy(); e.CompleteRun(production.Finish()); e.Coins = 345;
            var dir = Path.Combine(Path.GetFullPath("Logs"), "save-check-" + Guid.NewGuid().ToString("N"));
            var store = new SaveStore(dir);
            Check(store.Save(e), "first atomic save");
            e.BuyUpgrade(0); Check(store.Save(e), "save replaces prior file");
            Check(File.Exists(Path.Combine(dir, "cotton-circuit.json.bak")), "previous save backed up");
            var loaded = new SaveStore(dir).Load();
            Check(loaded.Coins == 225 && loaded.Levels[0] == 1 && loaded.Inventory.Count == 1 && loaded.Inventory[0].Samples.Count == 20, "save restores economy and winding samples");
            Check(loaded.Inventory[0].Samples[19].Flavor == 2 && loaded.Inventory[0].Samples[0].Radius == 7.5, "save preserves lane history");
            File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), "{broken");
            var broken = new SaveStore(dir); broken.Load();
            Check(!broken.CanSave && !broken.Save(e) && File.ReadAllText(Path.Combine(dir, "cotton-circuit.json")) == "{broken", "corrupt save protected from overwrite");
            Check(broken.ArchiveAndReset() && broken.Save(new Economy()), "explicit reset archives corrupt save");
            e.Levels[1] = 99; Check(!SaveStore.Valid(e), "invalid upgrade level rejected");
            e.Levels[1] = 0; e.Inventory[0].Samples[0].Radius = double.NaN; Check(!SaveStore.Valid(e), "nonfinite product coordinates rejected");
            CheckOrderSaves(dir);
            Debug.Log("COTTON_EDITOR_CHECKS_PASSED " + count);
            File.WriteAllText("Logs/editor-checks.txt", count + " editor checks passed\n" + dir);
        }
    }
}
