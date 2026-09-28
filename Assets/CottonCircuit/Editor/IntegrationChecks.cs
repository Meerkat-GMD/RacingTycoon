using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        static void CheckRoadside(WorldView world)
        {
            for (int map = 0; map < RaceCourse.MapCount; map++)
            {
                var bounds = new List<Bounds>(); int props = 0;
                foreach (Transform child in world.CourseRoots[map])
                {
                    if (!child.name.StartsWith("Roadside ")) continue;
                    props++;
                    foreach (var renderer in child.GetComponentsInChildren<Renderer>(true)) bounds.Add(renderer.bounds);
                }
                Check(props >= 20, "map " + map + " has close roadside landmarks for speed perception");
                var course = RaceCourse.ForMap(map); bool clear = true;
                foreach (var points in new[] { course.MainPoints, course.ShortcutPoints })
                    foreach (var point in points)
                    {
                        var road = course.Project(point);
                        var right = new RoadPoint(road.Tangent.Z, -road.Tangent.X);
                        foreach (int side in new[] { -1, 0, 1 })
                        {
                            var p = point + right * (side * (road.HalfWidth - 1.2));
                            var vehicle = new Bounds(new Vector3((float)p.X, 1.75f, (float)p.Z), new Vector3(1.6f, 1.4f, 1.6f));
                            foreach (var prop in bounds) if (vehicle.Intersects(prop)) clear = false;
                        }
                    }
                Check(clear, "map " + map + " roadside meshes leave drivable lanes clear");
            }
        }
        static string[] Names(int count, Func<int, string> name)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++) names[i] = name(i);
            return names;
        }
        static void CheckSpriteSlots(Sprite[] sprites, string[] expected, string label, List<Sprite> found)
        {
            var problems = new List<string>();
            if (sprites == null || sprites.Length != expected.Length)
                problems.Add("length " + (sprites == null ? "null" : sprites.Length.ToString()) + " instead of " + expected.Length);
            else
                for (int i = 0; i < expected.Length; i++)
                {
                    if (!sprites[i]) problems.Add("[" + i + "] missing");
                    else if (sprites[i].name != expected[i]) problems.Add("[" + i + "] is " + sprites[i].name + " instead of " + expected[i]);
                    else found.Add(sprites[i]);
                }
            Check(problems.Count == 0, label + " holds " + expected.Length + " catalog sprites in order" +
                (problems.Count == 0 ? "" : ": " + string.Join(", ", problems)));
        }
        static void CheckSprites(GameAssets assets)
        {
            Check(assets.TitleBackground && AssetDatabase.GetAssetPath(assets.TitleBackground) ==
                "Assets/CottonCircuit/Sprites/Title/TitleBackground.png", "title screen uses the approved background artwork");
            string[] introNames = { "Intro_01_Dream", "Intro_02_Stopped", "Intro_03_Delivery", "Intro_04_Machine_v2", "Intro_05_RaceAgain" };
            bool introValid = assets.IntroScenes != null && assets.IntroScenes.Length == introNames.Length;
            for (int i = 0; introValid && i < introNames.Length; i++)
                introValid = assets.IntroScenes[i] && AssetDatabase.GetAssetPath(assets.IntroScenes[i]) ==
                    "Assets/CottonCircuit/Sprites/Intro/" + introNames[i] + ".png";
            Check(introValid, "intro uses five approved scenes in order, including the corrected machine illustration");
            string[] flavors = { "Strawberry", "Soda", "Vanilla" }, sizes = { "Small", "Medium", "Large" };
            var found = new List<Sprite>();
            CheckSpriteSlots(assets.CustomerNeutral, Names(3, i => "Customer_V" + i + "_Neutral"), "GameAssets.CustomerNeutral", found);
            CheckSpriteSlots(assets.CustomerAngry, Names(3, i => "Customer_V" + i + "_Angry"), "GameAssets.CustomerAngry", found);
            CheckSpriteSlots(new[] { assets.Storefront, assets.Trash, assets.HeartEmote, assets.AngryEmote, assets.MinaPortrait },
                new[] { "Storefront", "Trash", "Emote_Heart", "Emote_Angry", "Mina_Portrait" },
                "GameAssets Storefront/Trash/HeartEmote/AngryEmote/MinaPortrait", found);
            CheckSpriteSlots(assets.LocationPrep, Names(4, i => "Location_" + i + "_Prep"), "GameAssets.LocationPrep", found);
            CheckSpriteSlots(assets.LocationStreet, Names(4, i => "Location_" + i + "_Street"), "GameAssets.LocationStreet", found);
            CheckSpriteSlots(assets.CottonCandy, Names(9, i => "CottonCandy_" + flavors[i / 3] + "_" + sizes[i % 3]), "GameAssets.CottonCandy", found);
            CheckSpriteSlots(assets.BaggedCandy, Names(9, i => "BaggedCandy_" + flavors[i / 3] + "_" + sizes[i % 3]), "GameAssets.BaggedCandy", found);
            CheckSpriteSlots(assets.SugarBags, Names(3, i => "SugarBag_" + flavors[i]), "GameAssets.SugarBags", found);
            CheckSpriteSlots(assets.Machines, Names(3, i => "Machine_" + i), "GameAssets.Machines", found);
            var icons = assets.TraitIcons ?? new TraitIconSprite[0];
            bool iconIds = icons.Length == TraitIcons.All.Length;
            for (int i = 0; iconIds && i < icons.Length; i++) iconIds = icons[i].Id == TraitIcons.All[i];
            Check(iconIds, "GameAssets.TraitIcons ids follow TraitIcons.All");
            CheckSpriteSlots(Array.ConvertAll(icons, icon => icon.Sprite), TraitIcons.All, "GameAssets.TraitIcons", found);
            var missing = new List<string>();
            foreach (var node in Progression.Nodes) if (assets.TraitIcon(TraitIcons.For(node.Id)) == null) missing.Add(node.Id);
            Check(missing.Count == 0, "all " + Progression.Nodes.Length + " trait nodes resolve to an icon sprite" +
                (missing.Count == 0 ? "" : ": missing " + string.Join(", ", missing)));
            var badImports = new List<string>();
            foreach (var sprite in found)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer || importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.mipmapEnabled || !importer.alphaIsTransparency || importer.textureCompression != TextureImporterCompression.CompressedHQ)
                    badImports.Add(path);
            }
            Check(found.Count == 66 && badImports.Count == 0, found.Count + " UI sprites import as single sprites without mipmaps, with alpha transparency and HQ compression" +
                (badImports.Count == 0 ? "" : ": wrong settings " + string.Join(", ", badImports)));
            Check(!AssetDatabase.IsValidFolder("Assets/CottonCircuit/Resources") && !Directory.Exists("Assets/CottonCircuit/Resources"),
                "Assets/CottonCircuit/Resources is gone; the Mina portrait comes from GameAssets.MinaPortrait");
        }
        static void CheckRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ProjectBuilder.PipelinePath);
            Check(pipeline && GraphicsSettings.defaultRenderPipeline == pipeline && pipeline.rendererDataList[0] is UniversalRendererData,
                "URP asset with a Universal renderer is the default pipeline");
            for (int i = 0; i < QualitySettings.names.Length; i++)
                Check(QualitySettings.GetRenderPipelineAssetAt(i) == pipeline, "quality level " + QualitySettings.names[i] + " renders with URP");
            var shaders = new HashSet<Shader>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                foreach (var material in renderer.sharedMaterials) if (material) shaders.Add(material.shader);
            // Project shaders such as CottonCircuit/SugarFloss qualify by declaring the URP subshader tag.
            foreach (var shader in shaders)
                Check(shader.name.StartsWith("Universal Render Pipeline/") || shader.name == "Sprites/Default" || shader.name == "GUI/Text Shader" ||
                    shader.FindSubshaderTagValue(0, new ShaderTagId("RenderPipeline")).name == "UniversalPipeline",
                    "scene shader " + shader.name + " renders in URP");
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
                JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(Path.Combine(legacyDirectory, "cotton-circuit.json"))).Version == 8,
                "migrated save writes V8");

            var expanded = new Economy { Coins = 820, ShelfLevel = 1, OrderSerial = 12,
                NextCustomerIn = 7.5, TotalTips = 42, MissedOrders = 4,
                OrdersServed = 3, TotalSold = 9, LifetimeRevenue = 1024, SatisfactionTotal = 2.25 };
            for (int i = 0; i < 9; i++) Check(expanded.CompleteRun(MakeStock()), "expanded shelf accepts stock " + i);
            expanded.Orders.Add(new CustomerOrder { Id = "order-11", Flavor = 2, Size = 0, Remaining = 85 });
            expanded.Orders.Add(new CustomerOrder { Id = "order-12", Flavor = 0, Size = 1, Remaining = 17.5 });
            var expandedDirectory = Path.Combine(root, "orders");
            var expandedStore = new SaveStore(expandedDirectory);
            Check(expandedStore.Save(expanded), "V3 save accepts expanded shelf and waiting orders");
            var restored = new SaveStore(expandedDirectory).Load();
            Check(restored.ShelfLevel == 1 && restored.StockCapacity == 9 && restored.Inventory.Count == 9 &&
                restored.Coins == 820 && restored.Orders.Count == 2 &&
                restored.Orders[0].Id == "order-11" && restored.Orders[0].Remaining == 85 &&
                restored.Orders[1].Id == "order-12" && restored.Orders[1].Remaining == 17.5 &&
                restored.NextCustomerIn == 7.5 && restored.OrderSerial == 12 &&
                restored.TotalTips == 42 && restored.MissedOrders == 4 &&
                restored.OrdersServed == 3 && restored.SatisfactionTotal == 2.25,
                "V3 round trip retains orders, timing, rewards, and shelf capacity");
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
                "invalid in-memory V3 does not overwrite valid save");
            File.WriteAllText(Path.Combine(expandedDirectory, "cotton-circuit.json"),
                JsonUtility.ToJson(new SaveStore.Envelope { Version = 2, State = expanded }, true));
            var invalidStore = new SaveStore(expandedDirectory);
            invalidStore.Load();
            Check(!invalidStore.CanSave && !invalidStore.Save(new Economy()),
                "malformed V2 file protected from overwrite");
        }
        static void CheckRecipeSaves(string root)
        {
            var stock = MakeStock();
            var e = new Economy { Coins = 610, ShelfLevel = 1, OrderSerial = 2, OrdersServed = 1,
                TotalSold = 1, TotalTips = 10, LifetimeRevenue = 100, SatisfactionTotal = .5 };
            e.CompleteRun(stock);
            e.Orders.Add(new CustomerOrder { Id = "order-2", Flavor = 1, Size = 1, Remaining = 60 });
            var dir = Path.Combine(root, "v2-migration"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), JsonUtility.ToJson(new SaveStore.Envelope { Version = 2, State = e }));
            var store = new SaveStore(dir); var restored = store.Load();
            Check(store.CanSave && restored.Coins == 610 && restored.Inventory[0].Id == stock.Id && restored.ShelfLevel == 1,
                "V2 migration keeps stock, coins and shelves");
            Check(restored.Orders[0].Remaining == 150 && restored.Inventory[0].Quality == 0 && restored.SatisfactionTotal == .5,
                "V2 migration preserves waiting ratio and legacy candy value");
            restored.Inventory[0].Quality = 75;
            Check(store.Save(restored) && new SaveStore(dir).Load().Inventory[0].Quality == 75,
                "V3 preserves product quality");
            Check(JsonUtility.FromJson<SaveStore.Envelope>(File.ReadAllText(Path.Combine(dir, "cotton-circuit.json"))).Version == 8,
                "V2 migration writes V8");
            restored.Inventory[0].Quality = 101; Check(!SaveStore.Valid(restored), "out of range quality rejected");
            restored.Inventory[0].Quality = -1; Check(!SaveStore.Valid(restored), "negative quality rejected");
        }
        public static void Run()
        {
            count = 0;
            SugarShakeInspectorChecks.Run(Check);
            if (!UnityEngine.Object.FindAnyObjectByType<GameController>())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            Check(game && game.World && game.UI && game.Audio, "wired playable scene");
            Check(game.World.Kart && game.World.CentralCandy && game.World.Customer && game.World.SugarThread, "world gameplay references");
            Check(game.World.SugarThread.sharedMaterial.shader.name == "CottonCircuit/SugarFloss" && game.World.SugarThread.sharedMaterial.mainTexture &&
                game.World.SugarWisps != null && game.World.SugarWisps.Length == 2 && Array.TrueForAll(game.World.SugarWisps, wisp => wisp && !wisp.enabled),
                "sugar floss uses the floss shader and two hidden wisps");
            CheckRenderPipeline();
            Debug.Log("COTTON_MESH_DIAGNOSTIC assets=" + (game.World.Assets ? game.World.Assets.name : "null") + " mesh=" + (game.World.Assets.PuffMesh ? game.World.Assets.PuffMesh.name + " vertices=" + game.World.Assets.PuffMesh.vertexCount : "null"));
            Check(game.World.Assets.PuffMesh && game.World.Assets.PuffMesh.vertexCount > 0, "Blender cotton mesh imported");
            var assets = game.World.Assets;
            Check(assets.Kart && assets.Kiosk && assets.Spinner && assets.Puff && assets.Customer && assets.Crystal && assets.Arch && assets.Tree && assets.Lamp, "all nine Blender assets used");
            Check(assets.Chevron && assets.Barrier && assets.ShortcutGate, "three new Blender racing props imported");
            Check(assets.DisplayRack && assets.OrderBoard && assets.QueuePost && game.World.DisplayRacks.Length == 3, "three Blender shop props and expandable racks wired");
            Check(assets.CandyTunnel && assets.FinishMarker, "two new Blender map landmarks imported");
            Check(assets.DownhillCoupe && assets.DownhillCoupe.GetComponentsInChildren<Renderer>().Length > 0, "Blender downhill coupe imported for style comparison");
            CheckSprites(assets);
            Check(game.World.CourseRoots != null && game.World.CourseRoots.Length == RaceCourse.MapCount &&
                game.World.CourseRoots[0].Find("Racing surface 1") && game.World.CourseRoots[1].Find("Racing surface 2") &&
                game.World.CourseRoots[2].Find("Racing surface 3"), "three maps rendered from their physics courses");
            CheckRoadside(game.World);
            Check(game.World.ShopRoot && game.World.ShopRoot.position.x < -200, "shop is outside the enlarged machine");
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
            var recovered = new SaveStore(dir); var restored = recovered.Load();
            Check(recovered.CanSave && restored.Coins == 345 && restored.Levels[0] == 0,
                "corrupt primary recovers the validated previous save");
            File.WriteAllText(Path.Combine(dir, "cotton-circuit.json"), "{broken");
            File.WriteAllText(Path.Combine(dir, "cotton-circuit.json.bak"), "{broken backup");
            var broken = new SaveStore(dir); broken.Load();
            Check(!broken.CanSave && !broken.Save(e) && File.ReadAllText(Path.Combine(dir, "cotton-circuit.json")) == "{broken", "corrupt save protected from overwrite");
            Check(broken.ArchiveAndReset() && broken.Save(new Economy()), "explicit reset archives corrupt save");
            e.Levels[1] = 99; Check(!SaveStore.Valid(e), "invalid upgrade level rejected");
            e.Levels[1] = 0; e.Inventory[0].Samples[0].Radius = double.NaN; Check(!SaveStore.Valid(e), "nonfinite product coordinates rejected");
            CheckOrderSaves(dir);
            CheckRecipeSaves(dir);
            Debug.Log("COTTON_EDITOR_CHECKS_PASSED " + count);
            File.WriteAllText("Logs/editor-checks.txt", count + " editor checks passed\n" + dir);
        }
    }
}
