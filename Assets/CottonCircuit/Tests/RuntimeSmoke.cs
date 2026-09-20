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
            game.Tick(1, 0, false, 1);
            game.Tick(1, .55f, false, .45f, true);
            Check(game.World.Kart.Charge >= .32f, "corner drift charges meter");
            game.Tick(1, 0, false, .02f);
            Check(game.World.Kart.Boosting && game.World.Kart.DriveModel.BoostCount == 1, "releasing Space input produces one boost");
            game.World.Kart.Recover();
            bool boostCaptured = false;
            for (int i = 0; i < 1500; i++)
            {
                FollowCourse(.02f);
                if (i % 4 == 0) yield return null;
                if (!boostCaptured && game.Session.Production.Grams > 40 && game.World.Kart.DriveModel.BoostRemaining > .7)
                {
                    boostCaptured = true;
                    for (int frame = 0; frame < 18; frame++) { FollowCourse(1f / 60); yield return null; }
                    Capture("02a-boost.png"); yield return null; yield return null;
                }
            }
            Check(game.Session.Production.Grams > 50 && game.World.Kart.Speed > 0, "real kart movement creates cotton");
            Check(game.World.Kart.DriveModel.Laps >= 1 && game.World.Kart.DriveModel.BestLapSeconds > 0, "direct steering completes a timed lap");
            Check(!game.World.GameCamera.orthographic && game.World.GameCamera.rect.width > .95f, "race uses full-width perspective chase camera");
            var samples = game.Session.Production.Samples;
            var seen = new bool[3]; foreach (var sample in samples) seen[sample.Flavor] = true;
            Check(seen[0] && seen[1] && seen[2], "course sectors wind all three flavors");
            int beforeReset = game.Session.Production.Grams; game.World.Kart.Recover(); game.Tick(0, 0, false, .05f);
            Check(game.Session.Production.Grams == beforeReset, "position reset does not produce sugar");
            for (int i = 0; i < 100; i++) { FollowCourse(.02f); yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            Capture("02-race.png"); yield return null; yield return null;
            CheckButtonsVisible();
            Screen.SetResolution(1280, 960, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("02b-race-4x3.png"); yield return null; yield return null; CheckButtonsVisible();
            Screen.SetResolution(1920, 820, false); yield return new WaitForSecondsRealtime(.7f);
            Capture("02c-race-wide.png"); yield return null; yield return null; CheckButtonsVisible();
            Screen.SetResolution(1600, 900, false); yield return new WaitForSecondsRealtime(.5f);
            int wallHits = game.World.Kart.DriveModel.WallHits;
            for (int i = 0; i < 250 && game.World.Kart.DriveModel.WallHits == wallHits; i++) game.Tick(1, 1, false, .02f, true);
            Check(game.World.Kart.DriveModel.WallHits > wallHits && !game.World.Kart.Boosting, "wall impact slows and cancels boost");
            Check(game.Session.Production.Grams >= beforeReset, "wall impact preserves made cotton");
            double remaining = game.Session.Remaining; int grams = game.Session.Production.Grams;
            var pausedPosition = game.World.Kart.transform.position;
            game.TogglePause(); game.Tick(1, 1, false, .1f);
            Check(game.Session.Remaining == remaining && game.Session.Production.Grams == grams, "pause freezes time and production");
            Check(game.World.Kart.transform.position == pausedPosition, "pause freezes kart movement");
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
            game.StartRun();
            var course = RaceCourse.Shared;
            for (int i = 0; i < 700 && game.World.Kart.DriveModel.Sample.Progress < 44; i++)
            {
                SteerTo(course.Sample(game.World.Kart.DriveModel.Sample.Progress + 8).Position);
                if (i % 4 == 0) yield return null;
            }
            bool enteredShortcut = false, mergedShortcut = false, shortcutCaptured = false;
            double shortcutExit = course.Project(course.ShortcutPoints[course.ShortcutPoints.Length - 1]).Progress;
            for (int i = 0; i < 700; i++)
            {
                var drive = game.World.Kart.DriveModel; int nearest = 0; double distance = double.MaxValue;
                for (int j = 0; j < course.ShortcutPoints.Length; j++)
                {
                    var difference = drive.Position - course.ShortcutPoints[j]; double candidate = difference.X * difference.X + difference.Z * difference.Z;
                    if (candidate < distance) { distance = candidate; nearest = j; }
                }
                SteerTo(nearest >= course.ShortcutPoints.Length - 5 ? course.Sample(shortcutExit + 7).Position : course.ShortcutPoints[Math.Min(course.ShortcutPoints.Length - 1, nearest + 4)]);
                enteredShortcut |= drive.Sample.IsShortcut;
                if (i % 4 == 0) yield return null;
                if (enteredShortcut && !shortcutCaptured)
                {
                    shortcutCaptured = true; yield return new WaitForSecondsRealtime(.4f);
                    Capture("10-shortcut.png"); yield return null; yield return null;
                }
                if (enteredShortcut && !drive.Sample.IsShortcut && drive.Sample.Progress > shortcutExit + 3) { mergedShortcut = true; break; }
            }
            Check(enteredShortcut && mergedShortcut && game.World.Kart.DriveModel.WallHits == 0, "actual kart drives through shortcut and rejoins without false walls");
            game.FinishRun(); game.ReturnToShop();
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
        // Test-only controller. Production gameplay never steers for the player.
        void SteerTo(RoadPoint target)
        {
            var d = game.World.Kart.DriveModel;
            double angle = Math.Atan2(target.X - d.Position.X, target.Z - d.Position.Z) - d.Heading;
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            game.Tick(d.Speed < 8 ? .7f : 0, Mathf.Clamp((float)angle * 1.8f, -1, 1), false, .02f);
        }
        void FollowCourse(float dt)
        {
            var drive = game.World.Kart.DriveModel;
            var target = drive.Course.Sample(drive.Sample.Progress + 10).Position;
            double heading = Math.Atan2(target.X - drive.Position.X, target.Z - drive.Position.Z);
            double difference = heading - drive.Heading;
            while (difference > Math.PI) difference -= Math.PI * 2;
            while (difference < -Math.PI) difference += Math.PI * 2;
            float steer = Mathf.Clamp((float)difference * 1.8f, -1, 1);
            bool drift = Math.Abs(steer) > .24 && drive.DriftCharge < .5 && drive.BoostRemaining <= 0;
            game.Tick(1, steer, false, dt, drift);
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
