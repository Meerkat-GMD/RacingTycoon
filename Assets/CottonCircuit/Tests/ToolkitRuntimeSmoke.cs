#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit.Tests
{
    // A real UIDocument/panel input smoke test. Never reads or writes the player's save folder.
    public sealed class ToolkitRuntimeSmoke : MonoBehaviour
    {
        static readonly string[] Flags = { "--uitk-smoke", "--title-smoke", "--tutorial-smoke", "--progression-smoke", "--smoke-test", "--shop-shift-smoke" };
        GameController game;
        string output, saveDirectory, scenario = "full", failure;
        int width = 1600, height = 900, checks;
        bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            bool requested = false;
            foreach (var flag in Flags) requested |= Array.IndexOf(args, flag) >= 0;
            if (!requested) return;
            var runner = new GameObject("UI Toolkit verification").AddComponent<ToolkitRuntimeSmoke>();
            // An EventSystem without an input module takes UI Toolkit input over from its default event system, so a
            // real cursor resting on or crossing the player window cannot move drags or end hovers. Synthetic pointer
            // events are still dispatched through the real panels.
            runner.gameObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            runner.output = Path.GetFullPath("CottonToolkitSmoke");
            foreach (var arg in args)
            {
                if (arg.StartsWith("--smoke-dir=")) runner.output = Path.GetFullPath(arg.Substring(12));
                else if (arg.StartsWith("--uitk-case=")) runner.scenario = arg.Substring(12);
                else if (arg.StartsWith("--uitk-width=") || arg.StartsWith("--title-width=") || arg.StartsWith("--tutorial-width="))
                    int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out runner.width);
                else if (arg.StartsWith("--uitk-height=") || arg.StartsWith("--title-height=") || arg.StartsWith("--tutorial-height="))
                    int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out runner.height);
            }
            runner.saveDirectory = Path.Combine(runner.output, "isolated-uitk-save-" + Guid.NewGuid().ToString("N"));
            runner.game = FindAnyObjectByType<GameController>();
            if (runner.game) runner.game.enabled = false;
            Application.logMessageReceived += runner.Log;
        }

        IEnumerator Start()
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                object current;
                bool advanced;
                try { advanced = stack.Peek().MoveNext(); current = advanced ? stack.Peek().Current : null; }
                catch (Exception e) { failed = true; failure = e.ToString(); Debug.LogException(e); break; }
                if (!advanced) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            try { Check(Strings.Missing.Count == 0, "no missing localization keys were requested: " + string.Join(", ", Strings.Missing)); }
            catch (Exception e) { failed = true; if (string.IsNullOrEmpty(failure)) failure = e.ToString(); Debug.LogException(e); }
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "result.txt"), (failed ? "FAILED" : "PASSED") +
                "\nCase: " + scenario + "\nChecks: " + checks + "\nResolution: " + Screen.width + "x" + Screen.height +
                "\nLanguage: " + Strings.Code(Strings.Current) +
                "\nSave: " + saveDirectory + (string.IsNullOrEmpty(failure) ? "" : "\n" + failure));
            Debug.Log("COTTON_UITK_RUNTIME_" + (failed ? "FAILED" : "PASSED") + " " + checks);
            Application.Quit(failed ? 1 : 0);
        }

        IEnumerator Scenario()
        {
            Check(game && game.Session == null, "runner starts before gameplay without touching the user's save");
            Check(scenario == "full" || scenario == "legacy" || scenario == "rack" || scenario == "hud" || scenario == "music", "known UI Toolkit scenario");
            Directory.CreateDirectory(saveDirectory);
            Screen.SetResolution(width, height, false);
            yield return new WaitForSecondsRealtime(.6f);
            Check(Screen.width == width && Screen.height == height, "requested framebuffer size is active");
            Check(Strings.Get("language.self") == (Strings.Current == Language.Korean ? "한국어" : "English") && Strings.Get("title.start") != "title.start",
                "the string table loaded in the player (" + Strings.Code(Strings.Current) + ")");
            if (scenario == "legacy") { yield return Legacy(); yield break; }
            if (scenario == "rack") { yield return Rack(); yield break; }
            if (scenario == "hud") { yield return Hud(); yield break; }
            if (scenario == "music") { yield return MusicLoop(); yield break; }

            game.ShowTitle(saveDirectory);
            yield return Settle();
            CheckScreenBounds("TitleScreen");
            CheckTitleText();
            Check(game.Audio.Music == MusicCue.Title, "title plays the title theme");
            Check(Element<Button>("TitlePrimaryButton").text == Strings.Get("title.start"), "fresh title exposes game start");
            Check(!Visible(Find("TitleNewGameButton")), "fresh title does not show redundant new game");
            CheckNoGeneratedUI();
            yield return Capture("01-title.png");
            int titleClicks = game.Audio.Played(Sound.UiClick);
            yield return Click("TitlePrimaryButton");
            Check(game.Session == null && Visible(Find("IntroScreen")), "new game opens the story before creating a save");
            Check(game.Audio.Music == MusicCue.Story && game.Audio.Played(Sound.UiClick) > titleClicks, "the story theme follows a clicked start button");
            CheckScreenBounds("IntroScreen");
            Check(!new SaveStore(saveDirectory).HasSave, "reading the story has not created or replaced a save");
            yield return Capture("02-intro-first.png");
            for (int page = 0; page < 6; page++)
            {
                Check(!string.IsNullOrEmpty(Element<Label>("IntroDialogue").text), "story page " + page + " has dialogue");
                string expectedCounter = new[] { "01", "02", "03", "04", "04", "05" }[page] + " / 05";
                string actualCounter = Element<Label>("IntroPageCounter").text;
                Check(actualCounter == expectedCounter, "story page " + page + " expects " + expectedCounter + ", got " + actualCounter);
                if (page == 3) yield return Capture("02-intro.png");
                yield return Click("IntroNextButton");
            }
            Check(game.TutorialStep == TutorialStep.PourSugar && game.TutorialActive, "story completion starts the real tutorial");
            Check(game.Audio.Music == MusicCue.Tutorial && game.Audio.Played(Sound.TutorialPopup) > 0, "the tutorial plays its theme and the bubble sound");
            Check(game.Session.Economy.Coins == 80 && game.Session.Economy.Day == 1, "new-game money and day are preserved");
            yield return Tutorial();
            yield return PreparationAndWorkers();
            yield return NewGameSkip();
            yield return Legacy();
            Check(!failed, "entire migrated flow has no runtime error logs");
        }

        IEnumerator Tutorial()
        {
            yield return Settle();
            var state = game.Shift.State;
            var customer = game.Shift.CustomerAt(0);
            double clock = state.RemainingSeconds, patience = customer.PatienceRemaining;
            Check(customer.Flavor == 0 && customer.Size == 0, "first customer can be served by the starting machine");
            var tutorialSkip = Element<Button>("TutorialSkipButton");
            Check(!tutorialSkip.focusable, "driving keys cannot activate tutorial skip through keyboard focus");
            Check(!tutorialSkip.worldBound.Overlaps(Element<Label>("businessClock").worldBound) &&
                !tutorialSkip.worldBound.Overlaps(Element<Label>("businessWallet").worldBound), "tutorial skip has its own header slot without covering the clock or wallet");
            var skipSlot = Element<VisualElement>("businessTutorialSlot");
            int skipSlots = 0;
            Element<VisualElement>("businessMachines").Query<VisualElement>(className: "biz-tutorial-slot").ForEach(_ => skipSlots++);
            Check(skipSlots == 1, "business header reserves exactly one tutorial skip slot");
            float roundingTolerance = 2f * tutorialSkip.panel.visualTree.worldBound.width / Screen.width;
            Check(Mathf.Abs(tutorialSkip.worldBound.xMin - skipSlot.worldBound.xMin) <= roundingTolerance &&
                Mathf.Abs(tutorialSkip.worldBound.xMax - skipSlot.worldBound.xMax) <= roundingTolerance &&
                Mathf.Abs(tutorialSkip.worldBound.yMin - skipSlot.worldBound.yMin) <= roundingTolerance &&
                Mathf.Abs(tutorialSkip.worldBound.yMax - skipSlot.worldBound.yMax) <= roundingTolerance,
                "tutorial skip matches its reserved header slot: button " + tutorialSkip.worldBound + ", slot " + skipSlot.worldBound);
            var composition = Element<VisualElement>("TutorialComposition");
            var gameComposition = Element<VisualElement>("gameComposition");
            Check((composition.worldBound.center - gameComposition.worldBound.center).sqrMagnitude < .1f,
                "tutorial coaching stays aligned with the centered gameplay composition");
            Check(!game.EmptySugar() && game.ExtractCandy() == null, "tutorial rejects premature destructive production actions");
            var sodaBag = Element<VisualElement>("sugar1");
            Check(!sodaBag.enabledInHierarchy && Element<Label>("sugarCost1").text == "튜토리얼 중 잠김" &&
                Element<Label>("sugarCost2").text == "이 기계에서는 잠김", "the starting soda bag waits for the strawberry tutorial to finish");
            CheckNoPlayerAuto();
            yield return SugarGuideBeforeGrab();
            yield return Capture("03-tutorial-sugar.png");
            yield return PourSugar();
            Check(game.TutorialStep == TutorialStep.Drive && state.SugarGrams > 0, "real sugar pointer gestures unlock driving");
            Check(!Visible(Find("TutorialSugarGuide")), "sugar markers leave the screen once the sugar is filled");
            game.Tick(0, 0, false, 2);
            Check(game.World.Kart.Speed < .01f && state.BatchMeters == 0, "filled unstaffed machine stays still without user input");
            yield return Settle();
            Check(Visible(Find("TutorialCoach")), "stationary driving instruction is visible");
            game.Tick(1, 0, false, 1.5f);
            yield return Settle();
            Check(state.BatchMeters > 0 && game.World.Kart.Speed > 0, "manual acceleration physically produces candy");
            Check(!Visible(Find("TutorialCoach")), "driving hides the obstructing tutorial coach");
            yield return Capture("04-driving-clear.png");
            game.World.Kart.Stop();
            yield return Settle();
            Check(!Visible(Find("TutorialCoach")), "stopping does not bring back the driving popup");
            yield return PauseTitleResume();
            state = game.Shift.State;
            for (int guard = 0; game.TutorialStep == TutorialStep.Drive && guard < 2400; guard++)
            {
                ManualDriving(.1f);
                if (guard % 40 == 0) yield return null;
            }
            yield return Settle();
            Check(game.TutorialStep == TutorialStep.Extract, "manual inputs reach a finished first candy");
            Check(Visible(Find("TutorialCoach")), "extraction brings the instruction back");
            yield return Capture("04-tutorial-extract.png");
            int pops = game.Audio.Played(Sound.CandyExtract), extractClicks = game.Audio.Played(Sound.UiClick);
            yield return Click("businessExtract");
            Check(game.Audio.Played(Sound.CandyExtract) == pops + 1 && game.Audio.Played(Sound.UiClick) == extractClicks,
                "extracting plays the pop without the button click");
            Check(game.TutorialStep == TutorialStep.Deliver && game.Session.Economy.Inventory.Count == 1, "extract button places the real product on the shelf");
            Check(!Visible(Find("TutorialSugarGuide")), "sugar markers stay hidden during delivery");
            yield return Capture("05-tutorial-delivery.png");
            string productId = game.Session.Economy.Inventory[0].Id;
            int coins = game.Session.Economy.Coins;
            var item = game.UI.TutorialProductTarget(productId);
            var destination = game.UI.TutorialCustomerTarget();
            Check(item != null && destination != null, "authored product and customer targets are bound to live data");
            int sales = game.Audio.Played(Sound.DeliverSuccess) + game.Audio.Played(Sound.StarBonus);
            Pick(item, item.worldBound.center);
            Pointer(item, EventType.MouseDown, item.worldBound.center);
            yield return null;
            Check(game.UI.BusinessDragActive, "product pointer down starts the real captured drag");
            Pointer(item, EventType.MouseDrag, destination.worldBound.center);
            yield return null;
            Pick(destination, destination.worldBound.center);
            Pointer(item, EventType.MouseUp, destination.worldBound.center);
            yield return Settle();
            Check(!game.UI.BusinessDragActive && game.TutorialStep == TutorialStep.Success, "customer pointer drop advances to first sale success");
            Check(game.Audio.Played(Sound.DeliverSuccess) + game.Audio.Played(Sound.StarBonus) == sales + 1, "a sale plays one success chime");
            Check(game.Session.Economy.TotalSold == 1 && game.Session.Economy.Coins > coins && game.Session.Economy.Inventory.Count == 0, "delivery charges exactly once and consumes the product");
            Check(game.DeliverCandy(productId, game.Shift.CustomerAt(0).Id) == DeliveryResult.Rejected, "repeating a completed delivery is rejected");
            Check(Near(clock, state.RemainingSeconds) && Near(patience, game.Shift.CustomerAt(0).PatienceRemaining), "tutorial actions hold the business and customer clocks");
            yield return Click("TutorialCompleteButton");
            Check(!game.TutorialActive && game.TutorialStep == TutorialStep.Complete, "success action ends the tutorial");
            Check(sodaBag.enabledInHierarchy && Element<Label>("sugarCost1").text == "2 C / 10g" &&
                Element<Label>("sugarCost2").text == "이 기계에서는 잠김", "after the tutorial the basic machine offers paid soda while vanilla stays locked");
            Check(game.Audio.Music == MusicCue.Machine1, "the first machine song follows the tutorial");
            Check(new SaveStore(saveDirectory).Load().TutorialStep == TutorialStep.Complete, "tutorial completion is saved immediately");
            int bells = game.Audio.Played(Sound.ClosingBell);
            yield return CloseDay();
            Check(game.Audio.Music == MusicCue.None && game.Audio.Played(Sound.ClosingBell) == bells + 1, "closing rings once and settlement is quiet");
            Check(!game.GrowthHintVisible, "growth hint waits until preparation after settlement");
            yield return Click("businessNextDay");
            Check(game.InPreparation && game.GrowthHintVisible, "first settlement opens the one-time growth hint");
            Check(Element<Label>("TutorialModalDialogue").text == "번 돈으로 가게를 성장시킬 수 있어요.", "growth hint uses the exact approved sentence");
            Check(new SaveStore(saveDirectory).Load().GrowthHintShown, "one-time flag is saved when the hint appears");
            var blockedLocations = Element<Button>("OpenLocations");
            var blockedBusiness = Element<Button>("BeginBusiness");
            Check(!blockedLocations.enabledInHierarchy && !blockedBusiness.enabledInHierarchy,
                "growth modal disables underlying preparation navigation and business start");
            var growthClose = Element<Button>("TutorialGrowthCloseButton");
            growthClose.Focus();
            blockedLocations.Focus();
            // Unity clears focus when Focus() is called on an element that cannot grab it.
            Check(!blockedLocations.canGrabFocus && !blockedBusiness.canGrabFocus &&
                growthClose.panel.focusController.focusedElement != blockedLocations &&
                growthClose.panel.focusController.focusedElement != blockedBusiness,
                "disabled preparation controls cannot acquire keyboard focus during the growth modal");
            growthClose.Focus();
            using (var navigation = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next))
            {
                navigation.target = growthClose;
                growthClose.SendEvent(navigation);
            }
            yield return Settle();
            var navigatedFocus = growthClose.panel.focusController.focusedElement as VisualElement;
            Check(navigatedFocus == null || !Element<VisualElement>("PreparationScreen").Contains(navigatedFocus),
                "native keyboard navigation cannot enter the disabled preparation screen behind the growth modal");
            growthClose.Focus();
            yield return Capture("06-growth-hint.png");
            yield return Click("TutorialGrowthCloseButton");
            Check(!game.GrowthHintVisible && Visible(Find("PreparationScreen")), "confirm closes growth guidance and exposes preparation");
            Check(game.Audio.Music == MusicCue.Preparation, "preparation plays its theme");
            Check(blockedLocations.enabledInHierarchy && blockedBusiness.enabledInHierarchy,
                "closing the growth modal immediately restores preparation controls");
        }

        IEnumerator PauseTitleResume()
        {
            double meters = game.Shift.State.BatchMeters, sugar = game.Shift.State.SugarGrams;
            int coins = game.Session.Economy.Coins;
            game.TogglePause();
            yield return Settle();
            Check(game.Session.Paused && Visible(Find("PauseScreen")), "Esc pause action opens the authored pause menu");
            Check(Element<Button>("PauseTitleButton").text == "타이틀 화면으로", "pause offers title navigation rather than a new shop");
            bool wasMuted = game.Audio.Muted;
            yield return Click("PauseMuteButton");
            Check(game.Audio.Muted != wasMuted, "pause mute button changes the actual audio state");
            yield return Click("PauseMuteButton");
            Check(game.Audio.Muted == wasMuted, "pause mute toggles back to its original state");
            yield return Capture("pause.png");
            yield return Click("PauseTitleButton");
            Check(game.Session == null && Element<Button>("TitlePrimaryButton").text == Strings.Get("title.continue"), "pause return saves and opens the saved title");
            Check(game.Audio.Music == MusicCue.Title, "returning to the title restores the title theme");
            var saved = new SaveStore(saveDirectory).Load();
            Check(saved.Coins == coins && saved.TutorialStep == TutorialStep.Drive, "returning to title retains money and tutorial progress");
            yield return Click("TitleNewGameButton");
            Check(Visible(Find("TitleConfirmation")), "new-game selection requires the existing confirmation");
            yield return Click("TitleCancelNewGameButton");
            Check(!Visible(Find("TitleConfirmation")) && new SaveStore(saveDirectory).Load().Coins == coins, "cancel retains the current save");
            yield return Click("TitlePrimaryButton");
            Check(game.TutorialStep == TutorialStep.Drive && game.Session.Economy.Coins == coins &&
                Near(game.Shift.State.BatchMeters, meters) && Near(game.Shift.State.SugarGrams, sugar), "continue restores in-progress candy and the tutorial");
        }

        IEnumerator PreparationAndWorkers()
        {
            yield return UpgradeGraph();
            var economy = game.Session.Economy;
            yield return Click("TraitTab_production");
            int clicks = game.Audio.Played(Sound.UiClick);
            yield return Click("UpgradeNode_sugar_3");
            Check(Progression.Level(economy, "sugar_3") == 0 && game.Audio.Played(Sound.UiClick) == clicks,
                "clicking a trait that cannot be bought neither buys it nor plays a click");
            economy.Coins = 10000;
            game.UI.Refresh();
            foreach (string node in new[] { "sugar_2", "machine_2", "worker_1", "worker_grade_2", "shelf" })
            {
                yield return Click("OpenUpgradeGraph");
                yield return Click("TraitTab_" + UpgradeTreeLayout.Tabs[UpgradeTreeLayout.TabOf(node)].Id);
                var button = Element<Button>("UpgradeNode_" + node);
                int before = economy.Coins, purchases = game.Audio.Played(Sound.Purchase);
                yield return Click(button.name);
                Check(Progression.Level(economy, node) == 1 && economy.Coins < before, "authored upgrade button buys " + node);
                Check(game.Audio.Played(Sound.Purchase) == purchases + 1, "buying " + node + " plays the purchase sound");
            }
            Check(Element<VisualElement>("TraitEdge_sugar_2_sugar_3").ClassListContains("trait-edge-unlocked"),
                "arrows leaving a bought trait switch to the unlocked color");
            var machineChip = Element<Button>("TraitRequirement_worker_1_machine_2");
            Check(machineChip.ClassListContains("trait-chip-met") && machineChip.text.StartsWith("✓"), "a bought prerequisite marks its chip complete");
            Check(Find("TraitNode_flavor_soda") == null && Find("TraitRequirement_flavor_price_flavor_soda") == null &&
                Find("TraitRequirement_quality_focus_flavor_soda") == null, "the growth map no longer offers the starting soda flavor");
            yield return Capture("07-preparation-traits.png");
            yield return Click("OpenEquipment");
            Check(Visible(Find("EquipmentPage")) && !game.Machine(0).WorkerAssigned, "equipment page shows the hired worker without silently assigning it");
            yield return Click("AssignWorker_1");
            Check(game.Machine(1).WorkerAssigned, "trained worker can be assigned to the second machine");
            yield return Click("RecipeFlavor_1");
            yield return Click("RecipeSize_1");
            Check(game.Machine(1).RecipeFlavor == 1 && game.Machine(1).RecipeSize == 1, "recipe buttons change the assigned machine's flavor and size");
            yield return Click("AssignWorker_1");
            yield return Click("AssignWorker_0");
            Check(game.Machine(0).WorkerAssigned && !game.Machine(1).WorkerAssigned, "worker assignment moves explicitly between machines");
            yield return Capture("08-preparation-equipment.png");
            yield return Click("OpenLocations");
            Check(Visible(Find("LocationsPage")), "location navigation opens authored scenery and options");
            yield return Click("BeginBusiness");
            Check(game.InBusiness && economy.Day == 2 && !game.GrowthHintVisible, "preparation starts day two without repeating guidance");
            yield return Settle();
            CheckShelfDisplay(9);
            Check(game.Audio.Music == MusicCue.Machine1, "business opens on the first machine song");
            double beforeMeters = game.Machine(0).BatchMeters;
            game.Tick(0, 0, false, 5);
            double selectedGrowth = game.Machine(0).BatchMeters - beforeMeters;
            Check(game.SelectedMachineHasWorker && game.WorkerDriving && game.World.Kart.Speed > 0 && selectedGrowth > 0, "assigned selected worker alone drives and produces automatically");
            Check(!game.PourSugar(0) && game.ExtractCandy() == null && !game.EmptySugar(), "worker machine rejects manual production conflicts");
            yield return Capture("09-worker-driving.png");
            Check(!Visible(Find("businessExtractKey")) && !Visible(Find("businessEmptySugarKey")),
                "worker machine hides the manual F and right-click icons");
            yield return Click("businessMachine1");
            Check(game.Audio.Music == MusicCue.Machine2, "switching to the soda machine switches its song");
            beforeMeters = game.Machine(0).BatchMeters;
            game.Tick(0, 0, false, 5);
            Check(!game.SelectedMachineHasWorker && !game.WorkerDriving && game.World.Kart.Speed < .01f, "unstaffed selected machine remains manual and stationary");
            Check(Math.Abs(game.Machine(0).BatchMeters - beforeMeters - selectedGrowth) < .001, "offscreen worker produces at the same rate as when selected");
            yield return Capture("10-unstaffed-manual.png");
            CheckNoPlayerAuto();
            yield return CloseDay();
            yield return Click("businessNextDay");
            Check(!game.GrowthHintVisible, "second settlement does not repeat the growth hint");
        }

        IEnumerator NewGameSkip()
        {
            game.TogglePause();
            yield return Settle();
            yield return Click("PauseTitleButton");
            int savedCoins = new SaveStore(saveDirectory).Load().Coins;
            yield return Click("TitleNewGameButton");
            yield return Click("TitleConfirmNewGameButton");
            Check(game.Session == null && Visible(Find("IntroScreen")), "confirmed new game opens the introduction");
            Check(new SaveStore(saveDirectory).Load().Coins == savedCoins, "prior save stays intact while the new introduction is being read");
            yield return Click("IntroSkipButton");
            Check(game.TutorialStep == TutorialStep.PourSugar && game.Session.Economy.Day == 1 && game.Session.Economy.Coins == 80, "intro skip starts a fresh first-day tutorial exactly once");
            yield return Click("TutorialSkipButton");
            Check(!game.TutorialActive && game.InBusiness, "tutorial skip remains available through its real button");
        }

        // The authored map must reproduce UpgradeTreeLayout: disc centers, same-tab arrows and cross-tab chips.
        IEnumerator UpgradeGraph()
        {
            yield return Click("OpenUpgradeGraph");
            var area = Element<VisualElement>("TraitGraphArea");
            foreach (var tab in UpgradeTreeLayout.Tabs)
            {
                yield return Click("TraitTab_" + tab.Id);
                var graph = Element<VisualElement>("TraitGraph_" + tab.Id);
                foreach (var other in UpgradeTreeLayout.Tabs)
                    Check(Visible(Find("TraitGraph_" + other.Id)) == (other == tab), "the " + tab.Id + " tab shows only its own map");
                Check(Inside(area.worldBound, graph.worldBound), tab.Id + " map fits the growth map area without scrolling: " + graph.worldBound + " in " + area.worldBound);
                foreach (var position in tab.Nodes)
                {
                    var node = Element<VisualElement>("TraitNode_" + position.Id);
                    var button = graph.WorldToLocal(Element<Button>("UpgradeNode_" + position.Id).worldBound);
                    Check(node.parent == graph && Close(button.center, new Vector2(position.X, position.Y)), position.Id + " disc center matches UpgradeTreeLayout: " + button.center);
                    Check(button.width <= 76 && button.height <= 76, position.Id + " reacts only on its disc, like the old graph: " + button.size);
                    float bottom = graph.WorldToLocal(node.worldBound).yMax;
                    Check(bottom <= position.Y + UpgradeTreeLayout.FootprintBottom(position.Id) + 2, position.Id + " name and chips end above where its arrows start: " + bottom);
                    foreach (string parent in Progression.Find(position.Id).Parents)
                    {
                        if (UpgradeTreeLayout.TabOf(parent) != UpgradeTreeLayout.TabOf(position.Id))
                        {
                            var chip = Element<Button>("TraitRequirement_" + position.Id + "_" + parent);
                            Check(node.Contains(chip) && Visible(chip), "cross-tab prerequisite " + parent + " is a chip under " + position.Id);
                            continue;
                        }
                        var from = UpgradeTreeLayout.Find(parent);
                        var edge = Element<VisualElement>("TraitEdge_" + parent + "_" + position.Id);
                        float middle = edge.resolvedStyle.height * .5f;
                        var start = graph.WorldToLocal(edge.LocalToWorld(new Vector2(0, middle)));
                        var tip = graph.WorldToLocal(edge.LocalToWorld(new Vector2(edge.resolvedStyle.width, middle)));
                        Check(Close(start, new Vector2(from.X, from.Y + UpgradeTreeLayout.FootprintBottom(parent) + 5)) &&
                            Close(tip, new Vector2(position.X, position.Y - 38)), "arrow " + parent + " -> " + position.Id + " runs from " + start + " to " + tip);
                    }
                }
                yield return Capture("graph-" + tab.Id + ".png");
            }
            var lockedDisc = Element<VisualElement>("TraitNode_sugar_3").Q(className: "trait-disc").resolvedStyle.backgroundColor;
            var openDisc = Element<VisualElement>("TraitNode_hours").Q(className: "trait-disc").resolvedStyle.backgroundColor;
            Check(Element<VisualElement>("TraitNode_sugar_3").ClassListContains("trait-locked") && lockedDisc == Palette.Hex("E7E9E2") &&
                !Element<VisualElement>("TraitNode_hours").ClassListContains("trait-locked") && openDisc != lockedDisc,
                "locked traits have grey discs and reachable ones keep their category color: " + lockedDisc + " / " + openDisc);

            yield return Click("TraitTab_production");
            var speed = Element<Button>("UpgradeNode_stick_speed");
            var details = Element<VisualElement>("TraitDetails");
            var arrow = Element<VisualElement>("TraitEdge_stick_speed_stick_saving");
            Pointer(speed, EventType.MouseMove, speed.worldBound.center);
            Check(!details.ClassListContains("hidden") && Element<Label>("TraitDetailsTitle").text == "젓가락 회전" &&
                Element<Label>("TraitDetailsBody").text.Contains(" C"), "hovering a trait opens its details with the cost");
            Check(arrow.ClassListContains("trait-edge-focus"), "hovering a trait focuses its arrows");
            yield return null;
            Check(Visible(details) && Inside(area.worldBound, details.worldBound) && details.worldBound.xMin > speed.worldBound.xMax,
                "trait details open to the right of an upper-left trait inside the map area: " + details.worldBound);
            yield return Capture("graph-hover.png");
            Pointer(area, EventType.MouseMove, area.worldBound.min + new Vector2(4, 4));
            Check(details.ClassListContains("hidden") && !arrow.ClassListContains("trait-edge-focus"), "leaving the trait closes its details and focus");
            yield return Click("TraitTab_sales");
            var square = Element<Button>("UpgradeNode_location_3");
            Pointer(square, EventType.MouseMove, square.worldBound.center);
            Check(!details.ClassListContains("hidden") && details.ClassListContains("trait-details-left") && details.ClassListContains("trait-details-up"),
                "a lower-right trait flips its details to the left and upward");
            yield return null;
            Check(Visible(details) && Inside(area.worldBound, details.worldBound) && details.worldBound.xMax < square.worldBound.xMin &&
                Math.Abs(details.worldBound.yMax - square.worldBound.yMax) <= 2, "flipped details end beside the trait inside the map area: " + details.worldBound);
            Pointer(area, EventType.MouseMove, area.worldBound.min + new Vector2(4, 4));
            Check(details.ClassListContains("hidden"), "leaving the lower-right trait closes its details");

            yield return Click("TraitTab_business");
            yield return Click("TraitRequirement_repeat_ads_location_1");
            Check(Visible(Find("TraitGraph_sales")) && Element<Button>("TraitTab_sales").ClassListContains("prep-selected") &&
                Element<VisualElement>("TraitNode_location_1").ClassListContains("trait-highlight"), "a prerequisite chip opens its tab and highlights the prerequisite");
        }

        IEnumerator Legacy()
        {
            game.Initialize(Path.Combine(saveDirectory, "legacy-continuous"), true, false, false);
            yield return Settle();
            Check(game.Shift == null && game.ContinuousMode && game.Session.Mode == GameMode.Racing, "legacy continuous mode still initializes");
            game.Tick(0, 0, false, 2);
            Check(game.World.Kart.Speed < .01f, "legacy car does not automatically drive");
            game.Tick(1, 0, false, 1);
            Check(game.World.Kart.Speed > 0, "legacy car accepts manual acceleration");
            CheckNoGeneratedUI();
            CheckNoPlayerAuto();
            yield return Capture("11-legacy-continuous.png");
            game.Initialize(Path.Combine(saveDirectory, "legacy-session"), false, false, false);
            yield return Settle();
            Check(!game.ContinuousMode && game.Shift == null && game.Session.Mode == GameMode.Shop, "legacy session shop still initializes");
            yield return Capture("12-legacy-shop.png");
        }

        // The authored-ending loop is only reached after about two minutes, so this case moves the
        // playing deck near its loop end and checks the restart the audio clock scheduled.
        IEnumerator MusicLoop()
        {
            game.Initialize(Path.Combine(saveDirectory, "music"));
            var economy = game.Session.Economy;
            economy.Coins = 10000;
            foreach (string node in new[] { "sugar_2", "machine_2" }) Check(Progression.Buy(economy, node), "music case owns " + node);
            game.BeginBusiness();
            game.ChooseMachine(1);
            yield return new WaitForSecondsRealtime(.3f);
            Check(game.Audio.Music == MusicCue.Machine2, "the soda machine plays its song");
            Check(game.Audio.Sounds.TryGet(MusicCue.Machine2, out var song) && song.LoopEnd > 0, "the soda song has an authored-ending loop window");
            var player = Private<MusicPlayer>(game.Audio, "music");
            var decks = Private<AudioSource[]>(player, "decks");
            var first = decks[Private<int>(player, "active")];
            Check(first.isPlaying && first.clip == song.Clip, "the active deck plays the soda song");
            first.timeSamples = (int)((song.LoopEnd - .8f) * song.Clip.frequency);
            yield return new WaitForSecondsRealtime(1.6f);
            var second = decks[Private<int>(player, "active")];
            double position = second.timeSamples / (double)second.clip.frequency - song.LoopStart;
            Check(second != first && second.isPlaying && second.clip == song.Clip && position > .5 && position < 1.2,
                "the song restarts from its loop start at the loop end (" + position.ToString("0.00") + " s in)");
            Check(!first.isPlaying, "the previous deck stops after its short tail");
            second.Stop();
            yield return new WaitForSecondsRealtime(1.6f);
            var third = decks[Private<int>(player, "active")];
            Check(third.isPlaying && third.clip == song.Clip, "a stopped song restarts instead of leaving the business silent");
        }

        static T Private<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);

        IEnumerator Hud()
        {
            game.Initialize(Path.Combine(saveDirectory, "hud"), true, true, false);
            game.World.Kart.Stop();
            for (int i = 0; i < 6; i++) AddRackProduct(i % 3, i % 3, 50);
            game.UI.Refresh();
            yield return Settle();
            CheckNoGeneratedUI();
            CheckScreenBounds("businessScreen");
            foreach (string name in new[] { "businessDriver", "businessPause", "businessSizeLimits", "businessStreetStatus", "businessShelfDetails", "businessControls", "businessBatchDistance" })
                Check(Find(name) == null, "clean gameplay HUD omits the removed " + name + " element");
            Check(Element<VisualElement>("businessShelf").Q<Label>(className: "biz-shelf-tip") == null,
                "rack heading no longer repeats drag instructions");
            foreach (string action in new[] { "businessExtract", "businessEmptySugar" })
                Check(Visible(Find(action + "Key")) && Inside(Element<Button>(action).worldBound, Find(action + "Key").worldBound),
                    action + " shows its input icon inside the button");

            var state = game.Shift.State;
            state.BatchFlavor = 0;
            state.BatchQuality = 50;
            state.SugarFlavor = 0;
            state.SugarGrams = 50;
            double[] laps = { 0, .5, .999, 1, 1.499, 1.5, 1.999, 2, 2.5 };
            string[] stages = { "미완성", "미완성", "미완성", "소", "소", "중", "중", "대", "대" };
            string[] remaining = {
                "소까지 1.00 바퀴", "소까지 0.50 바퀴", "소까지 0.01 바퀴",
                "중까지 0.50 바퀴", "중까지 0.01 바퀴", "대까지 0.50 바퀴", "대까지 0.01 바퀴",
                "최대 크기 · F로 꺼내기", "최대 크기 · F로 꺼내기"
            };
            string[] screenshots = { "hud-ready.png", "hud-incomplete.png", null, "hud-small.png", null, "hud-medium.png", null, "hud-large.png", null };
            for (int i = 0; i < laps.Length; i++)
            {
                state.BatchMeters = ShopShift.LapMeters * laps[i];
                state.BatchOverflowMeters = 0;
                game.UI.Refresh();
                yield return Settle();
                var stage = Element<Label>("businessBatchStage");
                Check(Visible(stage) && stage.text == stages[i],
                    laps[i] + " laps displays current candy stage " + stages[i] + ", got " + stage.text);
                Check(Element<Label>("businessNextSize").text == remaining[i],
                    laps[i] + " laps preserves next-stage distance or extraction guidance");
                if (screenshots[i] != null) yield return Capture(screenshots[i]);
            }

            game.Initialize(Path.Combine(saveDirectory, "hud-capped"), true, true, true);
            game.BeginBusiness();
            state = game.Shift.State;
            state.BatchFlavor = 0;
            state.BatchMeters = ShopShift.LapMeters;
            state.BatchOverflowMeters = ShopShift.LapMeters * 2;
            state.BatchSugarGrade = 1;
            game.UI.Refresh();
            yield return Settle();
            Check(game.HasProgression && game.Shift.MaxSize(0) == 0 && state.BatchOverflowMeters > state.BatchMeters,
                "capped fixture has a small first-machine batch and excess winding distance");
            Check(Element<Label>("businessBatchStage").text == "소",
                "overflow winding cannot mislabel a capped small candy as medium or large");
            Check(Element<Label>("businessNextSize").text == "완성 · F로 꺼내기",
                "capped first-machine batch retains extraction guidance");
            yield return Capture("hud-capped-small.png");
            game.TogglePause();
            yield return Settle();
            Check(game.Session.Paused && Visible(Find("PauseScreen")), "Esc pause action remains available without the HUD pause button");
            yield return Click("PauseContinueButton");
            Check(!game.Session.Paused && Visible(Find("businessScreen")) && !Visible(Find("PauseScreen")),
                "pause continue returns to the cleaned gameplay HUD");
            Check(!failed, "HUD scenario has no runtime error logs");
        }

        IEnumerator Rack()
        {
            game.Initialize(Path.Combine(saveDirectory, "rack"), true, true, false);
            game.World.Kart.Stop();
            var economy = game.Session.Economy;
            Check(game.Shift.IsOpen && !game.HasProgression && !game.SelectedMachineHasWorker,
                "rack fixture opens an isolated business day with manual production");
            economy.Inventory.Clear();
            economy.ShelfLevel = 0;
            game.World.ShowInventory(economy);
            game.UI.Refresh();
            yield return Settle();
            CheckNoGeneratedUI();
            CheckShelfDisplay(6);
            var rackArt = Element<VisualElement>("businessRackArt");
            var emptyLabel = Element<Label>("businessRackEmpty");
            Check(Visible(rackArt) && rackArt.pickingMode == PickingMode.Ignore && emptyLabel.pickingMode == PickingMode.Ignore,
                "rack decoration and empty hint do not intercept product input");
            Check(Visible(emptyLabel), "empty rack displays its authored hint");
            CheckRackProducts(6);
            var emptySlot = Element<VisualElement>("stock0");
            Pointer(emptySlot, EventType.MouseDown, emptySlot.worldBound.center);
            yield return null;
            Check(!game.UI.BusinessDragActive, "empty rack clip cannot start a product drag");
            yield return Capture("rack-empty.png");

            string[] capacityNames = { "six", "nine", "twelve" };
            for (int level = 0; level < 3; level++)
            {
                economy.Inventory.Clear();
                economy.ShelfLevel = level;
                int capacity = 6 + level * 3;
                for (int i = 0; i < capacity; i++)
                    AddRackProduct(i % 3, level == 2 ? 2 : i / 3, (i % 3) * 50);
                game.World.ShowInventory(economy);
                game.UI.Refresh();
                yield return Settle();
                Check(!Visible(emptyLabel), "occupied rack hides its empty hint at capacity " + capacity);
                CheckRackProducts(capacity);
                CheckShelfDisplay(capacity);
                yield return Capture("rack-" + capacityNames[level] + ".png");
            }

            economy.Inventory.Clear();
            economy.ShelfLevel = 1;
            var neighbor = AddRackProduct(0, 0, 50);
            var incomplete = AddRackProduct(1, -1, 50);
            var retained = AddRackProduct(2, 2, 100);
            game.UI.Refresh();
            yield return Settle();
            var source = game.UI.TutorialProductTarget(retained.Id);
            Check(source != null, "retained product has an authored rack source");
            Rect originalBounds = source.worldBound;
            yield return StartRackDrag(source);
            var cancelPoint = Element<Label>("businessShelfCount").worldBound.center;
            Pointer(source, EventType.MouseDrag, cancelPoint);
            yield return null;
            Check((Element<VisualElement>("businessDragArt").worldBound.center - cancelPoint).sqrMagnitude < 4,
                "carried bag follows the pointer in panel coordinates");
            Pointer(source, EventType.MouseUp, cancelPoint);
            yield return Settle();
            CheckRackReleased(source);
            Check(economy.Inventory.Contains(retained) && economy.Inventory.Count == 3,
                "canceling a rack drag retains its exact product");

            int coins = economy.Coins, trashed = game.Shift.State.DayTrashed;
            var neighborSource = game.UI.TutorialProductTarget(neighbor.Id);
            yield return StartRackDrag(neighborSource);
            var trash = Element<VisualElement>("businessTrash");
            Pick(trash, trash.worldBound.center);
            Pointer(neighborSource, EventType.MouseDrag, trash.worldBound.center);
            Pointer(neighborSource, EventType.MouseUp, trash.worldBound.center);
            yield return Settle();
            Check(!game.UI.BusinessDragActive && !economy.Inventory.Contains(neighbor) &&
                economy.Inventory.Contains(incomplete) && economy.Inventory.Contains(retained) &&
                economy.Coins == coins && game.Shift.State.DayTrashed == trashed + 1,
                "actual trash drop removes only its captured product without paying coins");
            CheckRackStable(retained, source, originalBounds);
            Check(game.UI.TutorialProductTarget(incomplete.Id) == Element<VisualElement>("stock1") &&
                source == Element<VisualElement>("stock2") && Element<VisualElement>("stock0").pickingMode == PickingMode.Ignore,
                "sparse rack keeps product IDs on their original clips after inventory compaction");
            Check(Element<Label>("stockGrade1").text.Contains("미완성"), "incomplete bag keeps its visible unsellable tag");
            CheckRackProducts(9);
            yield return Capture("rack-sparse.png");

            yield return StartRackDrag(source);
            Check(game.TrashCandy(incomplete.Id), "neighbor can be removed while another bag is held");
            yield return Settle();
            Check(game.UI.BusinessDragActive && source.HasPointerCapture(PointerId.mousePointerId) &&
                source.resolvedStyle.opacity < .001f, "inventory refresh preserves the captured and hidden source bag");
            CheckRackStable(retained, source, originalBounds);
            Pointer(source, EventType.MouseUp, cancelPoint);
            yield return Settle();
            CheckRackReleased(source);
            Check(economy.Inventory.Count == 1 && economy.Inventory[0].Id == retained.Id,
                "cancel after a neighbor removal preserves the held product identity");

            yield return StartRackDrag(source);
            var race = Element<VisualElement>("raceSurface");
            Pointer(source, EventType.MouseDrag, race.worldBound.center);
            Pointer(source, EventType.MouseUp, race.worldBound.center);
            yield return Settle();
            Check(!game.UI.BusinessDragActive && !source.HasPointerCapture(PointerId.mousePointerId) &&
                !Visible(Element<VisualElement>("businessDragGhost")), "resume drop releases capture and hides the carried bag");
            Check(economy.Inventory.Count == 0 && game.Shift.State.BatchProductId == retained.Id &&
                game.Shift.State.BatchFlavor == 2 && Near(game.Shift.State.BatchMeters, retained.DistanceMeters) &&
                game.Shift.State.BatchQuality == 100 && economy.Coins == coins,
                "rack drop onto the manual race resumes the exact product and its quality without payment");
            Check(Visible(emptyLabel), "resuming the last product restores the empty rack hint");
            CheckRackProducts(9);
            Check(!failed, "rack scenario has no runtime error logs");
        }

        Product AddRackProduct(int flavor, int tier, int quality)
        {
            var product = ShopShift.Preview(tier < 0 ? ShopShift.LapMeters * .2 : ShopShift.MetersForSize(tier), flavor);
            product.Id = Guid.NewGuid().ToString("N");
            product.Quality = quality;
            game.Session.Economy.Inventory.Add(product);
            game.Session.Economy.CompletedIds.Add(product.Id);
            return product;
        }

        void CheckRackProducts(int capacity)
        {
            var economy = game.Session.Economy;
            var rack = Element<VisualElement>("businessStockGrid");
            Check(economy.StockCapacity == capacity &&
                Element<Label>("businessShelfCount").text.Contains(economy.Inventory.Count + " / " + capacity),
                "rack count reports live inventory and capacity " + capacity);
            var sources = new HashSet<VisualElement>();
            foreach (var product in economy.Inventory)
            {
                var source = game.UI.TutorialProductTarget(product.Id);
                Check(source != null && sources.Add(source) && Visible(source) && source.pickingMode == PickingMode.Position,
                    "each inventory ID owns one visible, pickable rack source");
                Rect bounds = source.worldBound;
                Check(bounds.xMin >= rack.worldBound.xMin - .5f && bounds.xMax <= rack.worldBound.xMax + .5f &&
                    bounds.yMin >= rack.worldBound.yMin - .5f && bounds.yMax <= rack.worldBound.yMax + .5f,
                    source.name + " stays inside its rack footprint");
                Pick(source, bounds.center);
                foreach (var sample in new[] { new Vector2(.25f, .4f), new Vector2(.75f, .4f), new Vector2(.25f, .7f), new Vector2(.75f, .7f) })
                    Pick(source, new Vector2(bounds.xMin + bounds.width * sample.x, bounds.yMin + bounds.height * sample.y));
                int slot = int.Parse(source.name.Substring("stock".Length));
                var art = Element<VisualElement>("stockArt" + slot);
                var sprite = art.resolvedStyle.backgroundImage.sprite;
                string expectedSprite = "BaggedCandy_" + new[] { "Strawberry", "Soda", "Vanilla" }[product.FlavorIndex] + "_" +
                    new[] { "Small", "Medium", "Large" }[Math.Max(0, ShopShift.SizeOf(product))];
                Check(Visible(art) && sprite && sprite.name == expectedSprite && art.pickingMode == PickingMode.Ignore,
                    source.name + " renders the actual bagged product sprite " + expectedSprite);
            }
            for (int i = 0; i < 12; i++)
            {
                var source = Element<VisualElement>("stock" + i);
                Check(Visible(source) == (i < capacity), source.name + " follows the live rack capacity");
                if (i < capacity && !sources.Contains(source))
                    Check(source.pickingMode == PickingMode.Ignore && !Visible(Element<VisualElement>("stockArt" + i)),
                        source.name + " leaves an empty, non-interactive clip");
            }
        }

        // The rack art and the shop's 3D racks both follow the owned slots: 6 on one rack, then 3 more per rack.
        void CheckShelfDisplay(int capacity)
        {
            int racks = 0;
            foreach (var rack in game.World.DisplayRacks) if (rack.gameObject.activeSelf) racks++;
            Check(game.Session.Economy.StockCapacity == capacity && racks == 1 + (capacity - 6) / 3,
                "shop shows " + racks + " display racks for capacity " + capacity);
            var image = Element<VisualElement>("businessRackArt").resolvedStyle.backgroundImage;
            string art = image.texture ? image.texture.name : image.sprite ? image.sprite.name : "";
            string expected = capacity == 12 ? "CottonCandyFanStand" : "CottonCandyFanStand" + capacity;
            Check(art == expected, "rack art " + art + " shows one clip per slot at capacity " + capacity);
        }

        IEnumerator StartRackDrag(VisualElement source)
        {
            Check(source != null, "rack drag has a live source");
            Pick(source, source.worldBound.center);
            Pointer(source, EventType.MouseDown, source.worldBound.center);
            int startedFrame = Time.frameCount;
            var ghost = Element<VisualElement>("businessDragGhost");
            Check(source.ClassListContains("drag-source") && !ghost.ClassListContains("hidden"),
                "rack pointer down immediately requests the lifted source and visible ghost");
            yield return null;
            float firstFrameOpacity = source.resolvedStyle.opacity;
            bool firstFrameGhostVisible = Visible(ghost);
            // Capture resumes at EndOfFrame, after the panel style pass. Its next
            // pointer action needs two frame boundaries before reading resolved styles.
            yield return null;
            Check(game.UI.BusinessDragActive && source.HasPointerCapture(PointerId.mousePointerId),
                "real rack pointer down captures the product source");
            Check(source.resolvedStyle.opacity < .001f && Visible(ghost),
                "lifting a bag leaves one carried visual and no duplicate on its clip; source=" + source.name +
                ", frames=" + (Time.frameCount - startedFrame) + ", afterFirstFrameOpacity=" + firstFrameOpacity +
                ", afterFirstFrameGhostVisible=" + firstFrameGhostVisible + ", finalOpacity=" + source.resolvedStyle.opacity +
                ", finalGhostVisible=" + Visible(ghost) + ", ghostDisplay=" + ghost.resolvedStyle.display +
                ", dragClass=" + source.ClassListContains("drag-source") + ", ghostHiddenClass=" + ghost.ClassListContains("hidden"));
        }

        void CheckRackReleased(VisualElement source)
        {
            Check(!game.UI.BusinessDragActive && !source.HasPointerCapture(PointerId.mousePointerId) &&
                !Visible(Element<VisualElement>("businessDragGhost")) && source.resolvedStyle.opacity > .999f,
                "cancel restores the source bag and clears ghost and pointer capture");
        }

        void CheckRackStable(Product product, VisualElement source, Rect originalBounds)
        {
            Check(game.UI.TutorialProductTarget(product.Id) == source &&
                (source.worldBound.position - originalBounds.position).sqrMagnitude < .01f &&
                (source.worldBound.size - originalBounds.size).sqrMagnitude < .01f,
                "retained product keeps its authored clip and pickup bounds when a neighbor is removed");
        }

        IEnumerator SugarGuideBeforeGrab()
        {
            var bag = game.UI.TutorialSugarTarget;
            var race = Element<VisualElement>("raceSurface");
            var press = Element<VisualElement>("TutorialPickMarker");
            var arrow = Element<VisualElement>("TutorialPickArrow");
            var hand = Element<VisualElement>("TutorialPickHand");
            var zone = Element<VisualElement>("TutorialShakeZone");
            Check(Visible(press) && Visible(hand) && Visible(Find("TutorialDragTrail")) && Visible(zone) && Visible(Find("TutorialShakeDemo")),
                "sugar step shows the press, drag and shake markers before the bag is grabbed");
            float pixel = press.panel.visualTree.worldBound.width / Screen.width;
            Check(Mathf.Abs(arrow.worldBound.center.x - bag.worldBound.center.x) <= 3f * pixel &&
                Mathf.Abs(arrow.worldBound.yMax - bag.worldBound.yMin) <= 16f,
                "press arrow points at the top of the strawberry sugar bag: arrow " + arrow.worldBound + ", bag " + bag.worldBound);
            Check(bag.worldBound.Contains(hand.worldBound.center), "pointing hand rests on the strawberry sugar bag");
            Pick(bag, hand.worldBound.center);
            Check(race.worldBound.Contains(zone.worldBound.min) && race.worldBound.Contains(zone.worldBound.max),
                "shake zone lies inside the driving screen: zone " + zone.worldBound + ", race " + race.worldBound);
            Check(!zone.worldBound.Overlaps(Element<VisualElement>("TutorialCoach").worldBound) &&
                !zone.worldBound.Overlaps(Element<VisualElement>("businessProduction").worldBound) &&
                !zone.worldBound.Overlaps(Element<Button>("businessExtract").worldBound),
                "shake zone leaves the coach and the production panel readable");
            float low = float.MaxValue, high = float.MinValue;
            for (float until = Time.realtimeSinceStartup + 1.2f; Time.realtimeSinceStartup < until;)
            {
                low = Mathf.Min(low, arrow.worldBound.y);
                high = Mathf.Max(high, arrow.worldBound.y);
                yield return null;
            }
            Check(high - low > 3f, "press arrow keeps moving to draw the eye: moved " + (high - low));
        }

        IEnumerator PourSugar()
        {
            var bag = game.UI.TutorialSugarTarget;
            var race = Element<VisualElement>("raceSurface");
            var zone = Element<VisualElement>("TutorialShakeZone");
            Pick(bag, bag.worldBound.center);
            Pointer(bag, EventType.MouseDown, bag.worldBound.center);
            yield return null;
            Check(game.UI.TutorialIsSugarDragging && bag.HasPointerCapture(PointerId.mousePointerId), "real sugar pointer down captures the draggable authored element");
            yield return null;
            Check(!Visible(Find("TutorialPickMarker")) && !Visible(Find("TutorialPickHand")) && !Visible(Find("TutorialShakeDemo")),
                "grabbing the bag removes the press marker and the demonstration bag");
            Check(Visible(Find("TutorialDragTrail")) && Visible(zone), "drag route and shake zone stay while the bag is outside the driving screen");
            // Shake where the marker tells the player to shake.
            var center = zone.worldBound.center;
            float stroke = (float)game.SugarShakeFullStrokePixels;
            for (int i = 0; game.TutorialStep == TutorialStep.PourSugar && i < 18; i++)
            {
                var point = center + new Vector2(0, (i % 2 == 0 ? -.5f : .5f) * stroke);
                Pointer(bag, EventType.MouseDrag, point);
                yield return null;
                if (i > 0) continue;
                yield return null;
                Check(!Visible(Find("TutorialDragTrail")) && Visible(zone) && !Visible(Find("TutorialShakeDemo")),
                    "inside the driving screen only the shake marker remains");
                yield return Capture("03-tutorial-shake.png");
            }
            Pointer(bag, EventType.MouseUp, center);
            yield return Settle();
            Check(!game.UI.BusinessDragActive, "sugar pointer release clears capture and the drag visual");
        }

        void ManualDriving(float seconds)
        {
            // Synthetic inputs exist only in this development test, never as a player feature.
            float remaining = seconds;
            while (remaining > .00001f)
            {
                float dt = Mathf.Min(.05f, remaining);
                AutoDrive.Input(game.World.Kart.DriveModel, out double throttle, out double steering, out bool brake);
                game.Tick((float)throttle, (float)steering, brake, dt);
                remaining -= dt;
            }
        }

        IEnumerator CloseDay()
        {
            game.World.Kart.Stop();
            for (int guard = 0; game.Shift.IsOpen && guard < 30; guard++) { game.Tick(0, 0, false, 60); yield return null; }
            yield return Settle();
            Check(game.Shift.State.Closed && game.Session.Economy.Progression.Phase == BusinessPhase.Results, "real business clock reaches settlement");
        }

        IEnumerator Click(string name)
        {
            var button = Element<Button>(name);
            Check(Visible(button) && button.enabledInHierarchy, name + " is visible and enabled");
            var point = button.worldBound.center;
            Pick(button, point);
            Pointer(button, EventType.MouseDown, point);
            yield return null;
            Pointer(button, EventType.MouseUp, point);
            yield return Settle();
        }

        void Pick(VisualElement expected, Vector2 position)
        {
            Check(expected != null && expected.panel != null && expected.worldBound.width > 0 && expected.worldBound.height > 0, expected?.name + " has a live laid-out panel target");
            Check(expected.panel.visualTree.worldBound.Contains(position), expected.name + " hit point is inside the panel viewport: " + position);
            var picked = expected.panel.Pick(position);
            Check(picked == expected || picked != null && expected.Contains(picked), expected.name + " is reachable through real panel hit testing (picked " + picked?.name + ")");
        }

        static void Pointer(VisualElement target, EventType type, Vector2 position)
        {
            var mouse = new Event { type = type, button = 0, mousePosition = position };
            if (type == EventType.MouseDown) { using (var evt = PointerDownEvent.GetPooled(mouse)) { evt.target = target; target.SendEvent(evt); } }
            else if (type == EventType.MouseUp) { using (var evt = PointerUpEvent.GetPooled(mouse)) { evt.target = target; target.SendEvent(evt); } }
            else { using (var evt = PointerMoveEvent.GetPooled(mouse)) { evt.target = target; target.SendEvent(evt); } }
        }

        VisualElement Find(string name)
        {
            foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var element = document.rootVisualElement?.Q<VisualElement>(name);
                if (element != null) return element;
            }
            return null;
        }

        T Element<T>(string name) where T : VisualElement
        {
            var result = Find(name) as T;
            Check(result != null, name + " exists in authored UXML as " + typeof(T).Name);
            return result;
        }

        static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null) return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }

        void CheckNoPlayerAuto()
        {
            Check(typeof(GameController).GetMethod("ToggleAutoDrive") == null && typeof(GameController).GetMethod("TutorialAutoDrive") == null, "player automatic-driving APIs remain removed");
            Check(Find("TutorialAutoDriveButton") == null && Find("AutoDriveButton") == null, "authored screens contain no player automatic-driving button");
        }

        void CheckNoGeneratedUI()
        {
            Check(FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == 0, "rendered UI contains no legacy Canvas");
            foreach (string name in new[] { "TutorialGuideGraphic", "TitleScreenShade", "UpgradeTreeGraphic", "ShopOrderBubble" })
                Check(typeof(GameUI).Assembly.GetType("CottonCircuit." + name) == null, "old procedural UI type is removed: " + name);
            Check(FindObjectsByType<UIDocument>(FindObjectsSortMode.None).Length > 0, "runtime UI is backed by UIDocument");
        }

        void CheckTitleText()
        {
            var words = Element<VisualElement>("TitleMenu").Query<Label>(className: "title-word").ToList();
            Check(words.Count == 2 && words[0].text == Strings.Get("title.word.first") && words[1].text == Strings.Get("title.word.second"),
                "title words come from the table through LocalizedText in " + Strings.Code(Strings.Current));
        }

        void CheckScreenBounds(string name)
        {
            var screen = Element<VisualElement>(name);
            Check(screen.worldBound.width > 600 && screen.worldBound.height > 400,
                name + " occupies a real screen area: " + screen.worldBound);
            var viewport = screen.panel.visualTree.worldBound;
            Check(viewport.width > 600 && viewport.height > 400 && viewport.Contains(screen.worldBound.center),
                name + " is centered inside the live panel viewport");
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Check(texture && texture.width == width && texture.height == height, name + " captures the requested framebuffer");
                int visible = 0;
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                {
                    var pixel = texture.GetPixel((2 * x + 1) * texture.width / 32, (2 * y + 1) * texture.height / 32);
                    if (pixel.r > .1f || pixel.g > .1f || pixel.b > .1f) visible++;
                }
                Check(visible >= 16, name + " contains rendered game content");
                File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
            }
            finally { if (texture) Destroy(texture); }
        }

        static IEnumerator Settle() { yield return null; yield return null; }
        static bool Near(double a, double b) => Math.Abs(a - b) < .00001;
        // Layout positions are rounded to the device pixel grid, which can move them by up to two pixels.
        static bool Close(Vector2 a, Vector2 b) => (a - b).magnitude <= 2;
        static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - 1 && inner.yMin >= outer.yMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMax <= outer.yMax + 1;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("UITK_CHECK_FAILED " + message);
            checks++;
            Debug.Log("UITK_CHECK_PASS " + message);
        }
        void Log(string condition, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            failed = true;
            if (string.IsNullOrEmpty(failure)) failure = condition + "\n" + trace;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
