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
            Screen.SetResolution(1280, 720, false); yield return new WaitForSecondsRealtime(.4f);
            Screen.SetResolution(1600, 900, false); yield return new WaitForSecondsRealtime(.8f);
            Check(game.Session.Mode == GameMode.Shop && game.Orders.Orders.Count == 1, "fresh shop has customer");
            Check(game.PreparedMap == 0 && game.PreparedFlavor == 0, "order auto-selects correct map and flavor");
            Capture("01-shop.png"); yield return null; yield return null; CheckScreenLayout("shop");
            int initialCoins = game.Session.Economy.Coins;
            var firstOrder = game.Orders.Orders[0];
            ClickIn("Customer order 0", "만들러 가기");
            Check(game.Session.RecipeMode && game.RunMap == 0 && game.RunFlavor == 0, "order Make launches configured recipe");
            Check(game.World.CourseRoots[0].gameObject.activeSelf && !game.World.CourseRoots[1].gameObject.activeSelf, "only selected map renders");
            Check(!game.World.GameCamera.orthographic && game.World.GameCamera.rect.width > .95f, "full-width chase camera");
            game.Tick(0, 0, false, 1);
            Check(game.Session.Production.Grams == 0, "stationary run creates no candy");
            double clock = game.Session.Elapsed, patience = firstOrder.Remaining;
            game.TogglePause(); game.Tick(1, 1, false, 2);
            Check(game.Session.Elapsed == clock && firstOrder.Remaining == patience, "pause freezes race and customer");
            Capture("02-help.png"); yield return null; yield return null;
            game.TogglePause();
            yield return DriveMap(0, 0, "03-map-small.png");
            var first = game.Session.Result;
            Check(CandyRecipe.Matches(first, firstOrder), "finished small strawberry matches order");
            Check(game.Session.Economy.Coins == initialCoins + game.Session.ResultBonus && game.Session.ResultBonus > 0,
                "one completion bonus paid");
            int afterBonus = game.Session.Economy.Coins;
            game.FinishRun(); game.Session.TickRecipe(1, 100, 10, 2, 100, 0);
            Check(game.Session.Economy.Coins == afterBonus && game.Session.Economy.Inventory.Count == 1, "repeat finish cannot mint candy or bonus");
            patience = firstOrder.Remaining; game.Tick(0, 0, false, 2);
            Check(firstOrder.Remaining == patience, "results freeze waiting time");
            yield return new WaitForSecondsRealtime(.6f);
            Capture("04-result.png"); yield return null; yield return null; CheckScreenLayout("result");
            Click("가게로 돌아가기"); game.Tick(0, 0, false, 3);
            Check(game.Session.Economy.Inventory.Count == 1 && game.Session.Economy.Coins == afterBonus, "returning never auto-sells");
            ClickIn("Stock selector", "60g"); ClickIn("Customer order 0", "건네기");
            Check(game.Session.Economy.Inventory.Count == 0 && game.Session.Economy.OrdersServed == 1 && game.Session.Economy.TotalTips > 0,
                "manual handover pays sale and tip");
            int paid = game.Session.Economy.Coins;
            Check(!game.Serve(firstOrder.Id) && game.Session.Economy.Coins == paid, "duplicate service cannot pay");
            var saved = new SaveStore(saveDirectory).Load();
            Check(saved.Coins == paid && saved.OrdersServed == 1, "save preserves completion and sale coins");
            for (int map = 0; map < 2; map++) for (int taste = 0; taste < 3; taste++)
            {
                if (map == 0 && taste == 0) continue;
                Click(map == 0 ? "1번 맵" : "2번 맵"); Click(Palette.FlavorName(taste));
                Check(game.PreparedMap == map && game.PreparedFlavor == taste, "map and flavor buttons configure recipe");
                Click("재고 미리 만들기");
                Check(game.World.CourseRoots[map].gameObject.activeSelf && !game.World.CourseRoots[1 - map].gameObject.activeSelf,
                    "switching map changes visible route");
                yield return DriveMap(map, taste, map == 1 && taste == 1 ? "05-map-large.png" : null);
                game.ReturnToShop();
                Check(game.SelectedOrderId == null && game.PreparedMap == map && game.PreparedFlavor == taste,
                    "stock return preserves recipe without highlighting an unrelated order");
            }
            Check(game.Session.Economy.Inventory.Count == 5, "five other configured recipes are stocked");
            for (int map = 0; map < 2; map++)
            {
                Click("다운힐 스타일"); game.SetMap(map); game.SetFlavor(1); game.PrepareStock();
                Check(game.RunStyle == DrivingStyle.Downhill && game.World.Kart.DriveModel.Style == DrivingStyle.Downhill,
                    "downhill button selects a distinct driving model");
                var coupe = game.World.Kart.transform.Find("Downhill coupe visual");
                Check(coupe && coupe.gameObject.activeInHierarchy, "downhill coupe replaces kart visual");
                yield return DriveMap(map, 1, map == 0 ? "07-downhill.png" : null);
                Check(game.World.Kart.DriveModel.BoostCount == 0, "downhill grip driving does not trigger kart boosts");
                game.ReturnToShop(); game.DiscardSelected(); game.DiscardSelected();
                Check(game.Session.Economy.Inventory.Count == 5, "comparison product removed without touching original stock");
            }
            Click("카트 스타일");
            game.SetMap(0); game.PrepareStock();
            game.Tick(1, 0, false, .8f);
            Check(game.World.Kart.Speed >= 23.4f, "kart accelerates past ninety percent within 0.8 seconds");
            var boosterDrive = game.World.Kart.DriveModel;
            Check(boosterDrive.StoredBoosts == 1, "kart starts with one stored booster");
            game.TogglePause(); game.Tick(1, 0, false, .2f, false, true);
            Check(boosterDrive.StoredBoosts == 1 && !boosterDrive.ManualBoostActive, "pause rejects booster input without spending stock");
            game.TogglePause(); game.Tick(1, 0, false, .2f, false, true);
            Check(boosterDrive.StoredBoosts == 0 && boosterDrive.ManualBoostActive && boosterDrive.BoostCount == 1 && boosterDrive.Speed > 33,
                "stored booster input accelerates and is consumed only once across controller substeps");
            yield return new WaitForSecondsRealtime(.5f);
            Capture("10-stored-booster.png"); yield return null; yield return null;
            CheckScreenLayout("stored booster");
            game.Tick(1, 0, true, .02f);
            Check(!boosterDrive.ManualBoostActive && boosterDrive.BoostRemaining == 0, "brake cancels stored booster in player");
            game.World.Kart.Recover(); game.FinishRun(); game.FinishRun(); game.ReturnToShop();
            game.Tick(1, 0, false, .1f, false, true);
            Check(boosterDrive.StoredBoosts == 0 && boosterDrive.BoostCount == 1, "shop rejects booster input");
            game.SetStyle(1); game.SetMap(0); game.PrepareStock();
            var downhillDrive = game.World.Kart.DriveModel;
            game.Tick(1, 0, false, 1);
            double launchSpeed = downhillDrive.Speed;
            game.Tick(1, 0, false, .6f, false, true);
            Check(downhillDrive.Speed > launchSpeed + 3 && downhillDrive.Speed > 29 && downhillDrive.WallHits == 0,
                "downhill keeps accelerating through former 26m/s limit on the starting straight");
            Check(downhillDrive.BoostCount == 0 && downhillDrive.StoredBoosts == 0 && !downhillDrive.ManualBoostActive,
                "downhill ignores the kart booster input");
            game.FinishRun(); game.FinishRun(); game.ReturnToShop();
            foreach (int style in new[] { 0, 1 })
            {
                game.SetStyle(style); game.SetMap(0); game.PrepareStock();
                game.Tick(1, 0, false, .4f);
                for (int i = 0; i < 180 && game.World.Kart.Charge < .8f; i++)
                    game.Tick(0, i % 40 < 20 ? .65f : -.65f, false, .02f, true);
                Check(game.World.Kart.Charge >= .8f && game.World.Kart.DriveModel.WallHits == 0,
                    "controlled sliding charges without wall contact in style " + style);
                game.Tick(1, 0, false, .02f);
                Check(game.World.Kart.DriveModel.SkillCount == 1, "completed driving action contributes to quality");
                Check(style == 0 ? game.World.Kart.DriveModel.BoostTier == 2 : !game.World.Kart.Boosting,
                    "kart releases super boost while downhill retains momentum without boost");
                if (style == 0) Check(game.World.Kart.DriveModel.StoredBoosts == 2, "clean kart drift fills second booster slot");
                game.Tick(1, 0, false, .18f);
                yield return new WaitForSecondsRealtime(.5f);
                var streaks = FindAnyObjectByType<SpeedLinesGraphic>();
                if (style == 0)
                {
                    Check(streaks && streaks.Strength > .4f && game.World.GameCamera.fieldOfView > 76,
                        "boost visibly expands field of view and speed streaks");
                    Capture("08-super-boost.png"); yield return null; yield return null;
                }
                else { Capture("09-downhill-slide.png"); yield return null; yield return null; }
                CheckScreenLayout("driving style " + style);
                game.TogglePause(); var cameraPosition = game.World.GameCamera.transform.position;
                yield return new WaitForSecondsRealtime(.2f);
                Check(streaks.Strength == 0 && (game.World.GameCamera.transform.position - cameraPosition).sqrMagnitude < .00001f,
                    "pause freezes chase camera and removes speed effects");
                bool silent = true;
                foreach (var source in game.Audio.GetComponents<AudioSource>())
                    if (source.isPlaying && source.volume > 0) silent = false;
                Check(silent, "pause silences active driving audio");
                game.TogglePause(); game.ToggleMute();
                foreach (var source in game.Audio.GetComponents<AudioSource>())
                    Check(!source.isPlaying || source.volume == 0, "mute silences wind engine and rush");
                game.ToggleMute(); game.FinishRun(); game.FinishRun(); game.ReturnToShop();
            }
            game.SetStyle(0);
            // Do not lose a customer's identity when stock requests or time change.
            if (game.Orders.Orders.Count == 0) game.Tick(0, 0, false, 16);
            var pending = game.Orders.Orders[0];
            var wrong = game.Session.Economy.Inventory.Find(p => !CandyRecipe.Matches(p, pending));
            game.SelectProduct(wrong.Id); int stockBefore = game.Session.Economy.Inventory.Count; int coinsBefore = game.Session.Economy.Coins;
            Check(!game.Serve(pending.Id) && game.Session.Economy.Inventory.Count == stockBefore && game.Session.Economy.Coins == coinsBefore,
                "wrong flavor or size leaves stock and money intact");
            game.SetMap(1); game.SetFlavor(1); game.PrepareStock();
            game.Tick(1, 0, false, .8f); game.FinishRun();
            Check(game.Session.Mode == GameMode.Racing, "first abort asks for confirmation");
            game.FinishRun();
            Check(game.Session.Mode == GameMode.Results && game.Session.Result == null && game.Session.ResultBonus == 0 &&
                game.Session.Economy.Coins == coinsBefore && game.Session.Economy.Inventory.Count == stockBefore,
                "confirmed abort grants no product or reward");
            game.ReturnToShop(); game.SetMap(0); game.PrepareStock();
            for (int i = 0; i < 19; i++) game.Tick(0, 0, false, 10);
            Check(game.Session.Mode == GameMode.Results && game.Session.Result == null && game.Session.Economy.Inventory.Count == stockBefore,
                "stationary timeout creates no finished candy");
            game.ReturnToShop();
            game.Session.Economy.Coins = Math.Max(game.Session.Economy.Coins, 800); game.UI.Refresh();
            ClickIn("Upgrade 0", "120"); Check(game.Session.Economy.Levels[0] == 1, "motor upgrade purchased");
            ClickIn("Shelf expansion", "160"); ClickIn("Shelf expansion", "300");
            Check(game.Session.Economy.StockCapacity == 12 && game.World.DisplayRacks[2].gameObject.activeSelf, "shelf expands to twelve visible slots");
            var template = game.Session.Economy.Inventory[0];
            while (game.Session.Economy.Inventory.Count < 12)
                Check(game.Session.Economy.CompleteRun(new Product { Id = Guid.NewGuid().ToString("N"), Grams = template.Grams,
                    Quality = template.Quality, Samples = new List<WindingSample>(template.Samples) }), "expanded stock slot accepts product");
            game.World.ShowInventory(game.Session.Economy); game.UI.Refresh(); game.StartRun();
            Check(game.Session.Mode == GameMode.Shop, "full shelf blocks launch");
            Check(game.Store.Save(game.Session.Economy) && new SaveStore(saveDirectory).Load().Inventory[0].Quality == template.Quality,
                "quality stock survives V3 save");
            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 960), new Vector2Int(1920, 820) })
            {
                Screen.SetResolution(resolution.x, resolution.y, false); yield return new WaitForSecondsRealtime(.5f);
                Capture("06-shop-" + resolution.x + "x" + resolution.y + ".png"); yield return null; yield return null;
                CheckScreenLayout("shop " + resolution);
            }
            var discarded = game.Session.Economy.Inventory[11].Id; game.SelectProduct(discarded);
            ClickIn("Stock selector", "선택 재고 정리"); Check(game.Session.Economy.Inventory.Count == 12, "first discard does not delete");
            ClickIn("Stock selector", "선택 재고 정리"); Check(game.Session.Economy.Inventory.Count == 11 && game.Session.Economy.Inventory.TrueForAll(p => p.Id != discarded), "confirmed discard removes only selected item");
            Check(!failed, "no runtime errors through map recipe cycle");
        }
        IEnumerator DriveMap(int map, int taste, string screenshot)
        {
            int steps = 0; bool filledBeforeFinish = false; double minRadius = 100, maxRadius = 0;
            while (game.Session.Mode == GameMode.Racing && steps < 12000)
            {
                FollowMap(); steps++;
                if (game.Session.Production.IsFull && game.Session.Mode == GameMode.Racing)
                    filledBeforeFinish = true;
                if (steps % 16 == 0) yield return null;
                if (screenshot != null && steps == 500)
                {
                    yield return new WaitForSecondsRealtime(.6f); Capture(screenshot); yield return null; yield return null;
                    CheckScreenLayout("race map " + map);
                }
            }
            var p = game.Session.Result;
            string diagnostic = "map=" + map + " flavor=" + taste + " seconds=" + game.Session.Elapsed.ToString("0.00") +
                " laps=" + game.World.Kart.DriveModel.Laps + " hits=" + game.World.Kart.DriveModel.WallHits;
            Check(game.Session.Mode == GameMode.Results && p != null && game.World.Kart.DriveModel.Laps == 1,
                "one complete lap creates product: " + diagnostic);
            Check(game.World.Kart.DriveModel.WallHits == 0, "both styles can follow the course cleanly at base speed");
            Check(filledBeforeFinish, "full weight does not end the race early");
            Check(p.Grams == RaceRecipe.TargetGrams(map) && CandyRecipe.SizeOf(p) == map && p.Samples.TrueForAll(s => s.Flavor == taste),
                "map fixes size and machine setting fixes every flavor sample");
            foreach (var sample in p.Samples) { minRadius = Math.Min(minRadius, sample.Radius); maxRadius = Math.Max(maxRadius, sample.Radius); }
            Check(maxRadius - minRadius > .1, "driving line remains in cotton shape");
            Check(game.Session.Elapsed > 18 && game.Session.Elapsed < 36, "compressed course finishes in a short fast round");
            Check(p.Quality >= 0 && p.Quality <= 100 && game.Session.ResultBonus > 0, "completed race has bounded quality and record bonus");
        }
        void FollowMap()
        {
            double throttle, steering; bool brake;
            CourseTestDriver.Input(game.World.Kart.DriveModel, .8, out throttle, out steering, out brake);
            game.Tick((float)throttle, (float)steering, brake, .02f);
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
