#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace CottonCircuit.Tests
{
    public class RuntimeSmoke : MonoBehaviour
    {
        string output;
        string saveDirectory;
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
            foreach (string arg in args) if (arg.StartsWith("--smoke-dir=")) runner.output = arg.Substring(12);
            Directory.CreateDirectory(runner.output);
            runner.saveDirectory = Path.Combine(runner.output, "isolated-save-" + Guid.NewGuid().ToString("N"));
            runner.game = FindAnyObjectByType<GameController>();
            runner.game.enabled = false;
            runner.game.Initialize(runner.saveDirectory);
            Application.logMessageReceived += runner.Log;
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("RUNTIME_CHECK_FAILED " + message);
            checks++; Debug.Log("RUNTIME_CHECK_PASS " + message);
        }
        IEnumerator Start()
        {
            yield return RunGuarded();
        }
        IEnumerator RunGuarded()
        {
            var run = Scenario();
            while (true)
            {
                object current;
                try { if (!run.MoveNext()) break; current = run.Current; }
                catch (Exception e) { failed = true; Debug.LogException(e); break; }
                yield return current;
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), (failed ? "FAILED" : "PASSED") + "\nChecks: " + checks);
            Debug.Log("COTTON_RUNTIME_" + (failed ? "FAILED" : "PASSED") + " " + checks);
            Application.Quit(failed ? 1 : 0);
        }
        IEnumerator Scenario()
        {
            Application.targetFrameRate = 60;
            // Recreate the hidden startup swapchain before inspecting rendered frames.
            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSecondsRealtime(.4f);
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(1.5f);
            Check(game.Session.Mode == GameMode.Shop, "starts in shop");
            Capture("01-shop.png"); yield return null; yield return null;
            game.StartRun();
            Check(game.Session.Mode == GameMode.Racing, "start button enters race");
            for (int i = 0; i < 20; i++) game.Tick(0, 0, false, .05f);
            Check(game.Session.Production.Grams == 0, "stationary cart produces nothing");
            double beforeHitch = game.Session.Remaining; game.Tick(0, 0, false, 2);
            Check(Math.Abs(game.Session.Remaining - (beforeHitch - 2)) < .0001, "slow frame preserves elapsed race time");
            for (int i = 0; i < 400; i++)
            {
                float steer = i >= 100 && i < 125 ? 1 : i >= 220 && i < 247 ? -1 : i >= 330 && i < 343 ? 1 : 0;
                game.Tick(1, steer, false, .05f);
                if (i % 8 == 0) yield return null;
            }
            Check(game.Session.Production.Grams > 50 && game.World.Kart.Speed > 0, "real kart movement creates cotton");
            var samples = game.Session.Production.Samples;
            var seen = new bool[3]; foreach (var sample in samples) seen[sample.Flavor] = true;
            Check(seen[0] && seen[1] && seen[2], "lane changes wind all three flavors");
            int beforeReset = game.Session.Production.Grams; game.World.Kart.ResetPosition(); game.Tick(0, 0, false, .05f);
            Check(game.Session.Production.Grams == beforeReset, "position reset does not produce sugar");
            for (int i = 0; i < 30; i++) { game.Tick(1, 0, false, .05f); yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            Capture("02-race.png"); yield return null; yield return null;
            double remaining = game.Session.Remaining; int grams = game.Session.Production.Grams;
            game.TogglePause(); game.Tick(1, 1, false, .1f);
            Check(game.Session.Remaining == remaining && game.Session.Production.Grams == grams, "pause freezes time and production");
            Capture("03-help.png"); yield return null; yield return null;
            game.TogglePause(); game.FinishRun(); game.FinishRun();
            Check(game.Session.Mode == GameMode.Results && game.Session.Economy.Inventory.Count == 1, "finish stores exactly one product");
            yield return new WaitForSecondsRealtime(1.5f);
            Capture("04-result.png"); yield return null; yield return null;
            game.ReturnToShop(); int coins = game.Session.Economy.Coins;
            int price = game.Session.Economy.Price(game.Session.Economy.Inventory[0]);
            for (int i = 0; i < 90; i++) game.Tick(0, 0, false, .05f);
            Check(game.Session.Economy.Inventory.Count == 0 && game.Session.Economy.Coins == coins + price, "customer buys once for displayed value");
            game.BuyUpgrade(0);
            Check(game.Session.Economy.Levels[0] == 1 && game.Session.Economy.Coins == coins + price - 120, "upgrade purchases motor with earned coins");
            var restored = new SaveStore(saveDirectory).Load();
            Check(restored.Coins == game.Session.Economy.Coins && restored.Levels[0] == 1 && restored.Inventory.Count == 0, "save reload restores completed cycle");
            game.StartRun();
            for (int i = 0; i < 300; i++) game.Tick(0, 0, false, .2f);
            Check(game.Session.Mode == GameMode.Results && game.Session.Result == null, "five fps still ends a 60 second empty run");
            game.ReturnToShop();
            Screen.SetResolution(1280, 720, false); yield return new WaitForSecondsRealtime(1.5f);
            Capture("05-shop-1280.png"); yield return null; yield return null;
            Screen.SetResolution(1920, 1080, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("06-shop-1920.png"); yield return null; yield return null;
            Screen.SetResolution(1280, 960, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("07-shop-4x3.png"); yield return null; yield return null;
            CheckButtonsVisible();
            Screen.SetResolution(1600, 1000, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("08-shop-16x10.png"); yield return null; yield return null;
            CheckButtonsVisible();
            Screen.SetResolution(1920, 820, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("09-shop-wide.png"); yield return null; yield return null;
            CheckButtonsVisible();
            Check(!failed, "no runtime errors during complete cycle");
        }
        void CheckButtonsVisible()
        {
            var corners = new Vector3[4];
            foreach (var button in FindObjectsByType<Button>())
            {
                if (!button.gameObject.activeInHierarchy) continue;
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach (var corner in corners) Check(corner.x >= -1 && corner.y >= -1 && corner.x <= Screen.width + 1 && corner.y <= Screen.height + 1, "button inside viewport: " + button.name);
            }
        }
        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in FindObjectsByType<Text>())
                if (text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text) && text.preferredHeight > text.rectTransform.rect.height + 2)
                    Debug.LogWarning("UI_TEXT_OVERFLOW " + text.text + " height=" + text.preferredHeight + "/" + text.rectTransform.rect.height);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }
        void Log(string condition, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
