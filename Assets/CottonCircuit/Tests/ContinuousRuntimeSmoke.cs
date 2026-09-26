#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        IEnumerator ContinuousScenario()
        {
            saveDirectory = Path.Combine(output, "continuous-save-" + Guid.NewGuid().ToString("N"));
            game.Initialize(saveDirectory, true, false);
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(.5f);
            Check(game.ContinuousMode && !game.AutoDrive && game.RaceVisible &&
                game.Session.Mode == GameMode.Racing && game.Session.RecipeMode,
                "default game starts continuous production with manual driving");
            Check(game.Orders.Orders.Count == 1 && game.RunMap == 0 && game.RunFlavor == 0,
                "continuous fresh game starts the first customer's recipe");

            game.Tick(0, 0, false, .5f);
            Check(game.Session.Production.Grams == 0 && game.World.Kart.Speed < .01f,
                "manual driving waits for input rather than producing while stationary");
            ClickObject("Auto drive toggle");
            Check(game.AutoDrive, "manual driving can be switched to automatic with its button");
            ClickObject("Auto drive toggle");
            Check(!game.AutoDrive, "automatic driving can be switched back to manual with its button");
            ClickObject("Auto drive toggle");
            Check(game.AutoDrive, "automatic driving can be resumed with its button");
            game.Tick(0, 0, false, 3);
            Check(game.World.Kart.Speed > 5 && game.Session.Production.Grams > 0,
                "automatic driving moves and produces without steering input");
            yield return CaptureContinuous("11-split-initial.png");
            CheckSplitLayout("continuous initial");

            var firstOrder = game.Orders.Orders[0];
            yield return AutoUntil(() => game.Session.CompletedRecipes >= 2, "two automatic laps");
            Check(game.Session.Economy.Inventory.Count == 2 && game.World.Kart.DriveModel.Laps >= 2,
                "successive laps automatically stock products without opening results");
            var productIds = new HashSet<string>();
            foreach (var product in game.Session.Economy.Inventory) productIds.Add(product.Id);
            Check(productIds.Count == 2 && game.Session.Economy.Inventory.TrueForAll(p => CandyRecipe.Matches(p, firstOrder)),
                "each automatic lap creates one distinct correctly configured product");

            ClickIn("Stock selector", "60g");
            var production = game.Session.Production;
            var carPosition = game.World.Kart.transform.position;
            float carSpeed = game.World.Kart.Speed;
            int stockBefore = game.Session.Economy.Inventory.Count;
            int coinsBefore = game.Session.Economy.Coins;
            ClickIn("Customer order 0", "건네기");
            Check(game.Session.Mode == GameMode.Racing && !game.Session.Paused &&
                game.Session.Economy.Inventory.Count == stockBefore - 1 &&
                game.Session.Economy.Coins > coinsBefore && game.Session.Economy.OrdersServed == 1,
                "right-side handover sells a selected product while the race stays active");
            Check(ReferenceEquals(production, game.Session.Production) &&
                game.World.Kart.transform.position == carPosition && game.World.Kart.Speed == carSpeed,
                "serving does not reset production, car position, or speed");
            coinsBefore = game.Session.Economy.Coins;
            Check(!game.Serve(firstOrder.Id) && game.Session.Economy.Coins == coinsBefore,
                "continuous duplicate handover cannot pay twice");
            game.Tick(0, 0, false, .5f);
            Check((game.World.Kart.transform.position - carPosition).sqrMagnitude > 1 && game.World.Kart.Speed > 5,
                "car keeps moving immediately after a sale");
            var saved = new SaveStore(saveDirectory).Load();
            Check(saved.OrdersServed == 1 && saved.Coins == coinsBefore && saved.Inventory.Count == 1,
                "continuous production and sale are saved");
            yield return CaptureContinuous("12-split-sale.png");
            CheckSplitLayout("continuous sale");

            var pending = game.Orders.Orders[0];
            double patience = pending.Remaining, elapsed = game.Session.Elapsed;
            int grams = game.Session.Production.Grams;
            int preparedMap = game.PreparedMap, preparedFlavor = game.PreparedFlavor;
            int level = game.Session.Economy.Levels[0], capacity = game.Session.Economy.StockCapacity;
            var selection = game.SelectedProductId;
            stockBefore = game.Session.Economy.Inventory.Count;
            carPosition = game.World.Kart.transform.position;
            game.TogglePause();
            game.Tick(1, 1, false, 2);
            game.SelectProduct(game.Session.Economy.Inventory[0].Id);
            game.SetMap(1 - preparedMap); game.SetFlavor((preparedFlavor + 1) % 3);
            game.BuyUpgrade(0); game.BuyShelf(); game.DiscardSelected(); game.DiscardSelected();
            Check(!game.Serve(pending.Id), "pause rejects serving in the continuous shop");
            Check(game.Session.Elapsed == elapsed && pending.Remaining == patience &&
                game.Session.Production.Grams == grams && game.World.Kart.transform.position == carPosition,
                "pause freezes the continuous race, production, and customer patience together");
            Check(game.PreparedMap == preparedMap && game.PreparedFlavor == preparedFlavor &&
                game.SelectedProductId == selection && game.Session.Economy.Inventory.Count == stockBefore &&
                game.Session.Economy.Coins == coinsBefore && game.Session.Economy.Levels[0] == level &&
                game.Session.Economy.StockCapacity == capacity,
                "pause blocks recipe, selection, stock, and upgrade mutations");
            game.TogglePause();

            Click("1번 맵"); Click(Palette.FlavorName(1));
            Check(game.PreparedFlavor == 1 && game.RunFlavor == 0 && game.Session.RecipeFlavor == 0,
                "flavor choice is queued without altering the cotton currently being made");
            int completed = game.Session.CompletedRecipes;
            yield return AutoUntil(() => game.Session.CompletedRecipes > completed, "queued flavor boundary");
            Check(CandyRecipe.FlavorOf(LastStock()) == 0 && game.RunFlavor == 1,
                "old flavor finishes before the next lap adopts the queued flavor");
            completed = game.Session.CompletedRecipes;
            yield return AutoUntil(() => game.Session.CompletedRecipes > completed, "queued flavor product");
            Check(CandyRecipe.FlavorOf(LastStock()) == 1 && LastStock().Samples.TrueForAll(s => s.Flavor == 1),
                "the following whole lap produces only the newly selected flavor");

            Click("2번 맵");
            Check(game.PreparedMap == 1 && game.RunMap == 0,
                "map choice remains queued while the current course is in progress");
            completed = game.Session.CompletedRecipes;
            yield return AutoUntil(() => game.Session.CompletedRecipes > completed, "queued map boundary");
            Check(CandyRecipe.SizeOf(LastStock()) == 0 && game.RunMap == 1 &&
                game.World.CourseRoots[1].gameObject.activeSelf && !game.World.CourseRoots[0].gameObject.activeSelf,
                "old map completes its product before switching to the queued course");
            completed = game.Session.CompletedRecipes;
            yield return AutoUntil(() => game.Session.CompletedRecipes > completed, "queued map product");
            Check(CandyRecipe.SizeOf(LastStock()) == 1 && LastStock().Grams == 120 && CandyRecipe.FlavorOf(LastStock()) == 1,
                "new map creates its configured large product after a complete lap");
            game.Tick(0, 0, false, 2);
            yield return CaptureContinuous("13-split-large.png");
            CheckSplitLayout("continuous large map");

            yield return AutoUntil(() => game.Session.Economy.Inventory.Count == game.Session.Economy.StockCapacity,
                "automatic shelf fill");
            Check(game.Session.ProductionWaiting && game.Session.Mode == GameMode.Racing,
                "full shelf pauses new production while the race stays active");
            completed = game.Session.CompletedRecipes;
            coinsBefore = game.Session.Economy.Coins;
            int fullLaps = game.World.Kart.DriveModel.Laps;
            yield return AutoUntil(() => game.World.Kart.DriveModel.Laps > fullLaps, "full-shelf driving lap");
            Check(game.Session.ProductionWaiting && game.Session.CompletedRecipes == completed &&
                game.Session.Economy.Inventory.Count == game.Session.Economy.StockCapacity &&
                game.Session.Economy.Coins == coinsBefore && game.World.Kart.Speed > 5,
                "a whole lap with a full shelf keeps driving without duplicate stock or reward");
            yield return CaptureContinuous("14-split-full.png");
            CheckSplitLayout("continuous full shelf");

            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 960), new Vector2Int(1920, 820) })
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return new WaitForSecondsRealtime(.5f);
                yield return CaptureContinuous("15-split-" + resolution.x + "x" + resolution.y + ".png");
                CheckSplitLayout("continuous " + resolution);
            }
            Screen.SetResolution(1600, 900, false); yield return new WaitForSecondsRealtime(.4f);

            int matchingSlot = game.Orders.Orders.FindIndex(order =>
                game.Session.Economy.Inventory.Exists(product => CandyRecipe.Matches(product, order)));
            Check(matchingSlot >= 0, "a waiting customer's recipe is present on the full shelf");
            var matchingOrder = game.Orders.Orders[matchingSlot];
            var matchingProduct = game.Session.Economy.Inventory.Find(product => CandyRecipe.Matches(product, matchingOrder));
            game.SelectProduct(matchingProduct.Id);
            ClickIn("Customer order " + matchingSlot, "건네기");
            Check(game.Session.Economy.Inventory.Count == game.Session.Economy.StockCapacity - 1 &&
                game.Session.Mode == GameMode.Racing && game.World.Kart.Speed > 5,
                "selling from a full shelf opens a production slot without stopping the car");
            yield return AutoUntil(() => !game.Session.ProductionWaiting, "production resumes at the finish line");
            double restartProgress = game.World.Kart.DriveModel.TotalProgress;
            double requiredDistance = game.World.Kart.DriveModel.Course.Length;
            Check(game.Session.CompletedRecipes == completed && game.Session.Production.Grams < game.RunTargetGrams / 4,
                "resuming production starts fresh rather than minting a partial-lap product");
            yield return AutoUntil(() => game.Session.CompletedRecipes > completed, "new full lap after sale");
            Check(game.World.Kart.DriveModel.TotalProgress - restartProgress > requiredDistance * .9 &&
                game.Session.Economy.Inventory.Count == game.Session.Economy.StockCapacity,
                "the free shelf slot is filled only after a new full production lap");

            Click("가게 업그레이드");
            carPosition = game.World.Kart.transform.position;
            Check(game.Session.Economy.Coins >= 280, "continuous sales and lap rewards fund motor and shelf upgrades");
            ClickIn("Upgrade 0", "120");
            Check(game.Session.Economy.Levels[0] == 1 && game.Session.Mode == GameMode.Racing &&
                game.World.Kart.transform.position == carPosition,
                "motor upgrade can be bought while the race continues");
            ClickIn("Shelf expansion", "160");
            Check(game.Session.Economy.StockCapacity == 9 && game.Session.Mode == GameMode.Racing,
                "shelf can be expanded from the continuous shop without leaving the race");
            yield return CaptureContinuous("16-split-upgrades.png");
            CheckSplitLayout("continuous upgrades");
            Check(!failed, "no runtime errors through continuous racing and shop cycle");
        }

        IEnumerator CaptureContinuous(string name)
        {
            // The accelerated scenario advances many drive steps between frames.
            // Let the normal chase camera catch the stationary test car before QA.
            yield return new WaitForSecondsRealtime(.75f);
            Capture(name);
            yield return null;
        }

        Product LastStock() => game.Session.Economy.Inventory[game.Session.Economy.Inventory.Count - 1];

        IEnumerator AutoUntil(Func<bool> condition, string label)
        {
            int steps = 0;
            while (!condition() && steps++ < 8000)
            {
                int completed = game.Session.CompletedRecipes, mapBefore = game.RunMap;
                var previousPosition = game.World.Kart.transform.position;
                float previousSpeed = game.World.Kart.Speed;
                game.Tick(0, 0, false, .05f);
                if (game.Session.Mode != GameMode.Racing || game.Session.Paused || !game.AutoDrive || !game.RaceVisible)
                    Check(false, label + " keeps the continuous race active at step " + steps);
                if (game.Session.CompletedRecipes > completed && game.RunMap == mapBefore && previousSpeed > 5)
                    Check((game.World.Kart.transform.position - previousPosition).magnitude < 5 &&
                        game.World.Kart.Speed > previousSpeed * .75f,
                        label + " lap completion preserves car position and speed");
                if (steps % 20 == 0) yield return null;
            }
            Check(condition(), label + " completes automatically within 400 simulated seconds" +
                " (recipes " + game.Session.CompletedRecipes + ", laps " + game.World.Kart.DriveModel.Laps +
                ", stock " + game.Session.Economy.Inventory.Count + ", waiting " + game.Session.ProductionWaiting + ")");
            Check(game.Session.Mode == GameMode.Racing && !game.Session.Paused,
                label + " has no result-screen transition");
        }

        void ClickObject(string objectName)
        {
            var found = GameObject.Find(objectName);
            var button = found ? found.GetComponent<Button>() : null;
            Check(button != null && button.interactable, "clickable button object " + objectName);
            button.onClick.Invoke();
        }

        void CheckSplitLayout(string label)
        {
            Canvas.ForceUpdateCanvases();
            CheckScreenLayout(label);
            var shopPanel = GameObject.Find("Shop panel");
            var racePanel = GameObject.Find("Race panel");
            Check(shopPanel && shopPanel.activeInHierarchy && racePanel && racePanel.activeInHierarchy,
                label + " shows race and shop panels simultaneously");
            Check(!GameObject.Find("Results panel"), label + " keeps results hidden");
            var composition = GameObject.Find("Centered game composition").GetComponent<RectTransform>();
            var leftViewport = DesignScreenRect(composition, new Rect(0, 92, 960, 808));
            var shopViewport = DesignScreenRect(composition, new Rect(980, 82, 596, 126));
            Check(game.World.GameCamera.enabled && game.World.ShopCamera && game.World.ShopCamera.enabled,
                label + " has two enabled live world cameras");
            Check(NearRect(game.World.GameCamera.pixelRect, leftViewport, 2),
                label + " keeps the race camera in the left viewport: " + game.World.GameCamera.pixelRect);
            Check(NearRect(game.World.ShopCamera.pixelRect, shopViewport, 2),
                label + " keeps the shop camera inside its right-side preview: " + game.World.ShopCamera.pixelRect);
            Check(game.World.GameCamera.pixelRect.xMax <= game.World.ShopCamera.pixelRect.xMin,
                label + " camera viewports do not overlap");
            var corners = new Vector3[4];
            int shopButtons = 0;
            foreach (var button in shopPanel.GetComponentsInChildren<Button>())
            {
                if (!button.gameObject.activeInHierarchy) continue;
                shopButtons++;
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                bool inShopColumn = true;
                foreach (var corner in corners)
                    if (composition.InverseTransformPoint(corner).x + composition.rect.width * .5f < 979)
                        inShopColumn = false;
                Check(inShopColumn, label + " shop control stays in right column: " + button.name);
            }
            Check(shopButtons >= 10, label + " provides active sales and stock controls in the right column");
            foreach (string objectName in new[] { "Customer order 0", "Customer order 1", "Stock selector" })
            {
                var panel = GameObject.Find(objectName).GetComponent<RectTransform>();
                panel.GetWorldCorners(corners);
                bool inShopColumn = true;
                foreach (var corner in corners)
                    if (composition.InverseTransformPoint(corner).x + composition.rect.width * .5f < 979)
                        inShopColumn = false;
                Check(inShopColumn, label + " places " + objectName + " entirely in right column");
            }
            foreach (var streaks in racePanel.GetComponentsInChildren<SpeedLinesGraphic>())
            {
                streaks.GetComponent<RectTransform>().GetWorldCorners(corners);
                bool insideRace = true;
                foreach (var corner in corners)
                    if (corner.x > leftViewport.xMax + 2) insideRace = false;
                Check(insideRace, label + " speed effects stay out of the shop column");
            }
        }

        static Rect DesignScreenRect(RectTransform composition, Rect design)
        {
            var topLeft = composition.TransformPoint(new Vector3(design.x - 800, 450 - design.y, 0));
            var bottomRight = composition.TransformPoint(new Vector3(design.xMax - 800, 450 - design.yMax, 0));
            return Rect.MinMaxRect(topLeft.x, bottomRight.y, bottomRight.x, topLeft.y);
        }

        static bool NearRect(Rect actual, Rect expected, float tolerance) =>
            Mathf.Abs(actual.x - expected.x) <= tolerance && Mathf.Abs(actual.y - expected.y) <= tolerance &&
            Mathf.Abs(actual.width - expected.width) <= tolerance && Mathf.Abs(actual.height - expected.height) <= tolerance;
    }
}
#endif

