#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CottonCircuit.Tests
{
    public class RuntimeSmoke : MonoBehaviour
    {
        string output, saveDirectory;
        GameController game;
        int checks;
        bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--smoke-test") < 0) return;
            var runner = new GameObject("Runtime verification").AddComponent<RuntimeSmoke>();
            runner.output = Path.GetFullPath("CottonSmoke");
            foreach (string arg in args)
                if (arg.StartsWith("--smoke-dir=")) runner.output = arg.Substring(12);
            Directory.CreateDirectory(runner.output);
            runner.saveDirectory = Path.Combine(runner.output,
                "isolated-save-" + Guid.NewGuid().ToString("N"));
            runner.game = FindAnyObjectByType<GameController>();
            runner.game.enabled = false;
            runner.game.Initialize(runner.saveDirectory);
            Application.logMessageReceived += runner.Log;
        }

        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("RUNTIME_CHECK_FAILED " + message);
            checks++;
            Debug.Log("RUNTIME_CHECK_PASS " + message);
        }

        IEnumerator Start() { yield return RunGuarded(); }

        IEnumerator RunGuarded()
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                object current;
                bool advanced;
                try { advanced = stack.Peek().MoveNext();
                    current = advanced ? stack.Peek().Current : null; }
                catch (Exception e) { failed = true; Debug.LogException(e); break; }
                if (!advanced) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            File.WriteAllText(Path.Combine(output, "result.txt"),
                (failed ? "FAILED" : "PASSED") + "\nChecks: " + checks);
            Debug.Log("COTTON_RUNTIME_" + (failed ? "FAILED" : "PASSED") + " " + checks);
            Application.Quit(failed ? 1 : 0);
        }

        IEnumerator Scenario()
        {
            Application.targetFrameRate = 60;
            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSecondsRealtime(.4f);
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(.8f);
            Check(game.Session.Mode == GameMode.Shop && game.Orders.Orders.Count == 1,
                "fresh shop has one waiting customer");
            var firstOrder = game.Orders.Orders[0];
            Check(firstOrder.Flavor == 0 && firstOrder.Size == 0,
                "first request is strawberry small");
            Capture("01-shop.png"); yield return null; yield return null;
            CheckScreenLayout("shop 16:9");

            // The first full sale follows the same buttons as a player.
            ClickIn("Customer order 0", "만들러 가기");
            Check(game.Session.Mode == GameMode.Racing && game.RunTargetGrams == 60 &&
                  game.RunFlavor == 0, "Make button starts selected 60g order");
            for (int i = 0; i < 20; i++) game.Tick(0, 0, false, .05f);
            Check(game.Session.Production.Grams == 0, "stationary kart produces nothing");
            double beforeHitch = game.Session.Remaining;
            game.Tick(0, 0, false, 2);
            Check(Math.Abs(game.Session.Remaining - (beforeHitch - 2)) < .001,
                "long frame preserves elapsed race time");
            double patience = firstOrder.Remaining;
            double pausedRemaining = game.Session.Remaining;
            var pausedPosition = game.World.Kart.transform.position;
            game.TogglePause(); game.Tick(1, 1, false, 1);
            Check(game.Session.Remaining == pausedRemaining &&
                  firstOrder.Remaining == patience &&
                  game.World.Kart.transform.position == pausedPosition,
                "pause freezes run, customer patience, and kart");
            Capture("03-help.png"); yield return null; yield return null;
            game.TogglePause();

            // The outer lane advances the kart without producing stock.
            for (int i = 0; i < 130; i++)
            { FollowFlavor(-1); if (i % 10 == 0) yield return null; }
            Check(game.Session.Mode == GameMode.Racing &&
                  game.Session.Production.Grams == 0 &&
                  game.World.Kart.DriveModel.TotalProgress > 0,
                "outer neutral lane advances without sugar");
            // The outer-lane check ends near a bend; reset to a known center
            // line before measuring drift charge so a wall cannot cancel it.
            game.World.Kart.ResetPosition();
            game.Tick(1, 0, false, 1);
            game.Tick(1, .55f, false, .45f, true);
            Check(game.World.Kart.Charge >= .32f, "corner drift charges meter");
            game.Tick(1, 0, false, .02f);
            Check(game.World.Kart.Boosting && game.World.Kart.DriveModel.BoostCount == 1,
                "releasing drift starts one boost");
            int beforeRecover = game.Session.Production.Grams;
            game.World.Kart.Recover(); game.Tick(0, 0, false, .05f);
            Check(game.Session.Production.Grams == beforeRecover,
                "position recovery grants no sugar");
            Check(!game.World.GameCamera.orthographic &&
                  game.World.GameCamera.rect.width > .95f,
                "race uses full-width chase camera");

            yield return DriveRecipe(0, 0, "02-race.png");
            var firstProduct = game.Session.Result;
            Check(firstProduct != null && firstProduct.Grams == 60 &&
                  CandyRecipe.Matches(firstProduct, firstOrder),
                "real kart makes strawberry small candy");
            double resultPatience = firstOrder.Remaining;
            game.Tick(0, 0, false, 2);
            Check(firstOrder.Remaining == resultPatience,
                "results screen freezes customer patience");
            yield return new WaitForSecondsRealtime(.6f);
            Capture("04-result.png"); yield return null; yield return null;
            Click("가게로 돌아가기");
            Check(game.Session.Mode == GameMode.Shop &&
                  game.Session.Economy.Inventory.Count == 1,
                "result returns product to shop stock");
            int startingCoins = game.Session.Economy.Coins;
            for (int i = 0; i < 60; i++) game.Tick(0, 0, false, .05f);
            Check(game.Session.Economy.Inventory.Count == 1 &&
                  game.Session.Economy.Coins == startingCoins,
                "customer does not buy automatically");
            ClickIn("Stock selector", "60g");
            Check(game.SelectedProductId == firstProduct.Id,
                "stock button selects the made product");
            ClickIn("Customer order 0", "건네기");
            Check(game.Session.Economy.Inventory.Count == 0 &&
                  game.Session.Economy.OrdersServed == 1 &&
                  game.Session.Economy.Coins > startingCoins &&
                  game.Session.Economy.TotalTips > 0,
                "Serve button transfers matching stock once and pays tip");
            int paidCoins = game.Session.Economy.Coins;
            Check(!game.Serve(firstOrder.Id) && game.Session.Economy.Coins == paidCoins,
                "duplicate Serve cannot pay twice");
            Capture("05-hand-off.png"); yield return null; yield return null;
            var saved = new SaveStore(saveDirectory).Load();
            Check(saved.OrdersServed == 1 &&
                  saved.TotalTips == game.Session.Economy.TotalTips &&
                  saved.Inventory.Count == 0 && saved.Coins == paidCoins,
                "served order, tips, and coins survive reload");

            // Five fresh runs cover the remaining flavor and size combinations.
            int[,] recipes = { { 1, 0 }, { 2, 0 }, { 0, 1 }, { 1, 1 }, { 2, 1 } };
            for (int recipe = 0; recipe < recipes.GetLength(0); recipe++)
            {
                int targetFlavor = recipes[recipe, 0], size = recipes[recipe, 1];
                game.SetSize(size);
                Click("재고 미리 만들기");
                Check(game.Session.Mode == GameMode.Racing && game.RunFlavor == -1 &&
                      game.RunTargetGrams == CandyRecipe.TargetGrams(size),
                    "preparation uses chosen size and driven flavor");
                yield return DriveRecipe(targetFlavor, size,
                    recipe == 0 ? "06-race-order.png" : null);
                var made = game.Session.Result;
                Check(made != null && made.Grams == CandyRecipe.TargetGrams(size) &&
                      CandyRecipe.FlavorOf(made) == targetFlavor &&
                      CandyRecipe.SizeOf(made) == size,
                    "kart path makes flavor " + targetFlavor + " size " + size);
                game.ReturnToShop();
            }
            Check(game.Session.Economy.Inventory.Count == 5,
                "five additional recipes remain in stock");

            var pending = game.Orders.Orders[0];
            Product wrong = game.Session.Economy.Inventory.Find(p =>
                !CandyRecipe.Matches(p, pending));
            Check(wrong != null, "wrong stock available for guarded handover");
            game.SelectProduct(wrong.Id);
            int stockBeforeWrong = game.Session.Economy.Inventory.Count;
            int coinsBeforeWrong = game.Session.Economy.Coins;
            Check(!game.Serve(pending.Id) &&
                  game.Session.Economy.Inventory.Count == stockBeforeWrong &&
                  game.Session.Economy.Coins == coinsBeforeWrong,
                "wrong product cannot be served or sold");
            double waiting = pending.Remaining;
            game.TogglePause(); game.Tick(0, 0, false, 10);
            Check(pending.Remaining == waiting, "shop pause freezes patience");
            game.TogglePause();
            pending.Remaining = .05;
            int missed = game.Session.Economy.MissedOrders;
            game.Tick(0, 0, false, .1f);
            Check(game.Session.Economy.MissedOrders == missed + 1 &&
                  game.Session.Economy.Inventory.Count == stockBeforeWrong,
                "expired order leaves made stock untouched");

            // Isolated test funds; both actual purchases still use shop buttons.
            game.Session.Economy.Coins = Math.Max(game.Session.Economy.Coins, 500);
            game.UI.Refresh();
            ClickIn("Shelf expansion", "160");
            Check(game.Session.Economy.ShelfLevel == 1 &&
                  game.Session.Economy.StockCapacity == 9,
                "first shelf purchase opens nine slots");
            ClickIn("Shelf expansion", "300");
            Check(game.Session.Economy.ShelfLevel == 2 &&
                  game.Session.Economy.StockCapacity == 12,
                "second shelf purchase opens twelve slots");
            saved = new SaveStore(saveDirectory).Load();
            Check(saved.ShelfLevel == 2 && saved.StockCapacity == 12,
                "shelf expansion survives reload");
            var template = game.Session.Economy.Inventory[0];
            while (game.Session.Economy.Inventory.Count < 12)
            {
                var copy = new Product { Id = Guid.NewGuid().ToString("N"),
                    Grams = template.Grams,
                    Samples = new List<WindingSample>(template.Samples) };
                Check(game.Session.Economy.CompleteRun(copy),
                    "expanded shelf accepts slot " +
                    game.Session.Economy.Inventory.Count);
            }
            game.World.ShowInventory(game.Session.Economy);
            game.UI.Refresh();
            game.StartRun();
            Check(game.Session.Mode == GameMode.Shop &&
                  game.Session.Economy.Inventory.Count == 12,
                "full twelve-slot shelf blocks another run");
            Check(game.Store.Save(game.Session.Economy) &&
                  new SaveStore(saveDirectory).Load().Inventory.Count == 12,
                "twelve products survive save and reload");

            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSecondsRealtime(.5f);
            Capture("07-shop-1280.png"); yield return null; yield return null;
            CheckScreenLayout("shop 1280x720");
            Screen.SetResolution(1280, 960, false);
            yield return new WaitForSecondsRealtime(.5f);
            Capture("08-shop-4x3.png"); yield return null; yield return null;
            CheckScreenLayout("shop 4:3");
            Screen.SetResolution(1920, 820, false);
            yield return new WaitForSecondsRealtime(.5f);
            Capture("09-shop-wide.png"); yield return null; yield return null;
            CheckScreenLayout("shop ultrawide");
            var stockPanel = GameObject.Find("Stock selector");
            Check(stockPanel != null, "stock selector present at full shelf");
            var stockButtons = stockPanel.GetComponentsInChildren<Button>();
            Check(stockButtons.Length == 13 && stockButtons[12].interactable,
                "twelfth stock button is available");
            string lastProductId = game.Session.Economy.Inventory[11].Id;
            stockButtons[12].onClick.Invoke();
            Check(game.SelectedProductId == lastProductId,
                "twelfth stock button selects the last product");
            ClickIn("Stock selector", "선택 재고 정리");
            Check(game.Session.Economy.Inventory.Count == 12 &&
                  game.SelectedProductId == lastProductId,
                "first discard click preserves full shelf");
            ClickIn("Stock selector", "선택 재고 정리");
            Check(game.Session.Economy.Inventory.Count == 11 &&
                  game.Session.Economy.Inventory.Find(p => p.Id == lastProductId) == null,
                "second discard click removes only selected product");
            Capture("10-shop-after-discard.png"); yield return null; yield return null;
            Click("재고 미리 만들기");
            Check(game.Session.Mode == GameMode.Racing,
                "discarded slot allows another run");
            game.FinishRun(); game.ReturnToShop();
            Check(new SaveStore(saveDirectory).Load().Inventory.Count == 11,
                "eleven products persist after confirmed discard");
            Check(!failed, "no runtime errors during order loop");
        }

        IEnumerator DriveRecipe(int flavor, int size, string screenshot)
        {
            int steps = 0;
            while (game.Session.Mode == GameMode.Racing && steps < 1600)
            {
                FollowFlavor(flavor);
                steps++;
                if (steps % 8 == 0) yield return null;
                if (screenshot != null && steps == 160)
                {
                    yield return new WaitForSecondsRealtime(.6f);
                    Capture(screenshot); yield return null; yield return null;
                    CheckScreenLayout("race");
                }
            }
            var product = game.Session.Result;
            string diagnostic = "flavor=" + flavor + " size=" + size +
                " steps=" + steps + " grams=" +
                (product == null ? game.Session.Production.Grams : product.Grams) +
                " madeFlavor=" + CandyRecipe.FlavorOf(product) +
                " progress=" + game.World.Kart.DriveModel.TotalProgress.ToString("0.0") +
                " lateral=" + game.World.Kart.DriveModel.Sample.Lateral.ToString("0.00") +
                " wallHits=" + game.World.Kart.DriveModel.WallHits;
            Check(game.Session.Mode == GameMode.Results && product != null,
                "run completes with stock within 30 seconds: " + diagnostic);
        }

        // Test-only steering: inner colored lane only in the chosen sector.
        void FollowFlavor(int chosenFlavor)
        {
            var drive = game.World.Kart.DriveModel;
            var ahead = drive.Course.Sample(drive.Sample.Progress + 7);
            int sector = Math.Min(2,
                (int)(ahead.Progress / drive.Course.Length * 3));
            double lateral = sector == chosenFlavor ? -3.0 : 1.2;
            var right = new RoadPoint(ahead.Tangent.Z, -ahead.Tangent.X);
            var target = ahead.Position + right * lateral;
            double angle = Math.Atan2(target.X - drive.Position.X,
                target.Z - drive.Position.Z) - drive.Heading;
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            float steer = Mathf.Clamp((float)angle * 1.8f, -1, 1);
            game.Tick(1, steer, false, .02f);
        }

        void Click(string contains)
        {
            Button match = null;
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.gameObject.activeInHierarchy &&
                    button.name.Contains(contains)) { match = button; break; }
            Check(match != null && match.interactable, "clickable button " + contains);
            match.onClick.Invoke();
        }

        void ClickIn(string parent, string contains)
        {
            var root = GameObject.Find(parent);
            Check(root != null, "UI parent " + parent);
            Button match = null;
            foreach (var button in root.GetComponentsInChildren<Button>())
                if (button.gameObject.activeInHierarchy &&
                    button.GetComponentInChildren<Text>().text.Contains(contains))
                { match = button; break; }
            Check(match != null && match.interactable,
                "clickable " + contains + " in " + parent);
            match.onClick.Invoke();
        }

        void CheckScreenLayout(string label)
        {
            var corners = new Vector3[4];
            int buttons = 0, clipped = 0;
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.gameObject.activeInHierarchy) continue;
                buttons++;
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach (var corner in corners)
                    if (corner.x < -1 || corner.y < -1 ||
                        corner.x > Screen.width + 1 || corner.y > Screen.height + 1)
                    { clipped++; break; }
            }
            Check(buttons > 0 && clipped == 0, label + " buttons inside viewport");
            int overflow = 0;
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (text.gameObject.activeInHierarchy &&
                    !string.IsNullOrEmpty(text.text) &&
                    text.preferredHeight > text.rectTransform.rect.height + 2)
                { overflow++; Debug.LogWarning("UI_TEXT_OVERFLOW " + label + " " + text.text); }
            Check(overflow == 0, label + " has no text overflow");
        }

        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }

        void Log(string condition, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                failed = true;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
