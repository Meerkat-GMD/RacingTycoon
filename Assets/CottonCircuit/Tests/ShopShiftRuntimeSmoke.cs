#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CottonCircuit.Tests
{
    public partial class RuntimeSmoke
    {
        IEnumerator ShiftScenario()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--candy-rack-smoke") >= 0)
            {
                yield return CandyRackScenario();
                yield break;
            }
            Application.targetFrameRate = 60;
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.Shift != null && game.Shift.State.RemainingSeconds == 600, "default game opens a ten-minute business day");
            Check(game.Session.Economy.Inventory.Count == 0, "fresh day has empty stock");
            Check(game.Shift.CustomerAt(0) != null, "first street customer is available");
            CheckDefaultShiftDriving("fresh day");
            CheckShiftRoadWidths();
            checks += ShopShiftSaveChecks.Run(Path.Combine(output, "save-checks"));
            CaptureShift("01-open.png");
            int coinsBefore = game.Session.Economy.Coins;
            DriveShiftSeconds(59.9);
            Check(game.Shift.State.Customers.Count == 1, "only the first customer waits before the sixty-second arrival");
            DriveShiftSeconds(.2);
            Check(game.Shift.State.Customers.Count == 2, "second customer arrives after sixty seconds");
            Check(game.World.Kart.DriveModel.Laps > 0, "actual auto driver completes a lap");
            Check(game.Shift.State.BatchMeters == 0 && game.Session.Economy.Inventory.Count == 0, "sugarless laps do not create candy");
            Check(game.Session.Economy.Coins == coinsBefore && game.Session.Economy.Day == 1, "lap grants no coins or day change");

            var sugar = GameObject.Find("SugarBag0").GetComponent<ShopDragItem>();
            var data = PointerAtSource(sugar.gameObject);
            CheckRaycast(data, sugar.gameObject, false);
            ExecuteEvents.Execute(sugar.gameObject, data, ExecuteEvents.beginDragHandler);
            MoveDrag(sugar, data, 340, 280);
            MoveDrag(sugar, data, 390, 280); MoveDrag(sugar, data, 440, 280);
            Check(game.Shift.State.SugarGrams == 0, "horizontal sugar drag does not pour");
            ExecuteEvents.Execute(sugar.gameObject, data, ExecuteEvents.endDragHandler);
            Check(game.Shift.State.SugarGrams == 0, "ordinary drop does not pour");
            data = PointerAtSource(sugar.gameObject);
            ExecuteEvents.Execute(sugar.gameObject, data, ExecuteEvents.beginDragHandler);
            MoveDrag(sugar, data, 1050, 420); MoveDrag(sugar, data, 1050, 480); MoveDrag(sugar, data, 1050, 420);
            ExecuteEvents.Execute(sugar.gameObject, data, ExecuteEvents.endDragHandler);
            Check(game.Shift.State.SugarGrams == 0, "shaking on shop side does not pour");
            PourGesture(0, 1);
            Check(Math.Abs(game.Shift.State.SugarGrams - 10) < .001, "vertical round trip pours ten grams");
            double lapMeters = game.World.Kart.DriveModel.Course.Length;
            double sugarStartGrowth = speedGrownMeters;
            DriveShiftUnfueledGrowth(lapMeters * .05, "first growing-candy sample");
            var growth = GameObject.Find("Growing candy").GetComponent<ShopArtGraphic>().GrowthScale;
            DriveShiftUnfueledGrowth(lapMeters * .05, "second growing-candy sample");
            Check(GameObject.Find("Growing candy").GetComponent<ShopArtGraphic>().GrowthScale > growth, "cotton visibly grows before reaching the next tier");
            double grown = speedGrownMeters - sugarStartGrowth;
            Check(Math.Abs(game.Shift.State.SugarGrams - (10 - grown / lapMeters * 50)) < .00001 &&
                Math.Abs(game.Shift.State.BatchMeters - grown) < .00001,
                "speed-scaled driving grows candy and consumes fifty grams per grown lap without helper refills" + ShiftProductionDetails());
            CaptureShift("02-growing.png");
            DriveShiftUnfueledGrowth(lapMeters * .15, "single-pour depletion");
            Check(game.Shift.State.SugarGrams == 0 && Math.Abs(game.Shift.State.BatchMeters - lapMeters * .2) < .00001,
                "one ten-gram pour runs out after one fifth of a lap" + ShiftProductionDetails());
            double exhaustedBatch = game.Shift.State.BatchMeters;
            DriveShiftUnfueledMeters(lapMeters * .1, "driving after sugar depletion");
            Check(game.Shift.State.SugarGrams == 0 && game.Shift.State.BatchMeters == exhaustedBatch,
                "actual travel after depletion cannot grow the batch" + ShiftProductionDetails());
            var small = game.ExtractCandy();
            Check(small != null && ShopShift.SizeOf(small) == -1, "F action extracts undersize cotton");
            Check(game.Shift.State.BatchMeters == 0, "extraction clears batch without resetting race");
            DragStock(0, "CustomerDropTarget0");
            Check(game.Session.Economy.Inventory.Count == 1 && game.Session.Economy.Coins == coinsBefore, "undersize cannot be sold");
            DragStock(0, "TrashDropTarget");
            Check(game.Session.Economy.Inventory.Count == 0 && game.Shift.State.DayTrashed == 1, "trash drop removes exact product once");
            KeepDeliveryFixturesWaiting();
            CheckLapBasedProduction();
            CheckResumeCandy();

            var order = game.Shift.CustomerAt(0);
            KeepDeliveryFixturesWaiting();
            PourGesture(order.Flavor, 8);
            DriveShiftMeters(ShopShift.MetersForSize(order.Size) + lapMeters * .01);
            var correct = game.ExtractCandy();
            Check(correct != null && ShopShift.SizeOf(correct) == order.Size, "actual driving reaches ordered tier");
            CaptureShift("03-stock-ready.png");
            int salePrice = game.Session.Economy.Price(correct);
            var soldId = correct.Id;
            DragStock(0, "CustomerDropTarget0");
            Check(game.Session.Economy.Inventory.Count == 0 && game.Session.Economy.Coins == coinsBefore + salePrice, "matching drag sale pays exact price");
            Check(game.Shift.CustomerAt(0) == order && order.Happy && !order.Angry &&
                Math.Abs(order.ReactionRemaining - ShopShift.ReactionDuration) < .001,
                "matching sale keeps its customer for the heart reaction");
            Check(GameObject.Find("Customer emote 0").activeInHierarchy &&
                GameObject.Find("Customer emote 0").GetComponent<ShopStreetGraphic>().Kind == ShopStreetArtKind.HeartEmote,
                "correct sale displays a heart emote");
            AddRackStock(1, 1); AddRackStock(2, 2);
            game.UI.Refresh();
            CaptureShift("12-customer-heart.png");
            game.Session.Economy.Inventory.Clear(); game.UI.Refresh();
            Check(game.DeliverCandy(soldId, order.Id) == DeliveryResult.Rejected, "repeat delivery cannot pay twice");
            game.ToggleAutoDrive(); game.World.Kart.Stop();
            game.Tick(0, 0, false, 1.6f);
            Check(game.Shift.CustomerAt(0) == null, "happy customer leaves after the reaction delay");
            game.Tick(0, 0, false, 60.1f);

            // Wrong flavor remains sellable but must trigger the angry phase, not an immediate replacement.
            order = game.Shift.CustomerAt(0);
            Check(order != null, "next customer arrives after sale");
            KeepDeliveryFixturesWaiting();
            int wrongFlavor = (order.Flavor + 1) % 3;
            PourGesture(wrongFlavor, 9);
            game.ToggleAutoDrive();
            DriveShiftMeters(ShopShift.MetersForSize(0) + lapMeters * .01);
            var wrong = game.ExtractCandy();
            Check(wrong != null, "wrong-flavor batch can be extracted");
            int beforeWrong = game.Session.Economy.Coins;
            DragStock(0, "CustomerDropTarget0");
            Check(game.Shift.CustomerAt(0) == order && order.Angry && game.Shift.State.DayWrong == 1, "wrong delivery shows angry existing customer");
            Check(!order.TimedOut && GameObject.Find("Customer emote 0").activeInHierarchy &&
                GameObject.Find("Customer emote 0").GetComponent<ShopStreetGraphic>().Kind == ShopStreetArtKind.AngryEmote,
                "wrong order displays the angry emote without timeout");
            Check(game.Session.Economy.Coins == beforeWrong && game.Session.Economy.Inventory.Count == 0, "wrong product consumed without payment");
            CaptureShift("04-angry.png");
            game.ToggleAutoDrive(); game.World.Kart.Stop();
            double reaction = order.ReactionRemaining, remaining = game.Shift.State.RemainingSeconds;
            game.TogglePause(); game.Tick(0, 0, false, 5);
            Check(game.Shift.State.RemainingSeconds == remaining && order.ReactionRemaining == reaction, "pause freezes both business and angry timers");
            Check(!game.PourSugar(0) && game.ExtractCandy() == null, "paused actions are refused");
            game.TogglePause(); game.Tick(0, 0, false, .7f);
            Check(game.Shift.CustomerAt(0) == order, "angry customer stays during reaction delay");
            game.Tick(0, 0, false, .9f);
            Check(game.Shift.CustomerAt(0) == null, "angry customer leaves after delay");

            game.ToggleAutoDrive();
            PourGesture(1, 5); DriveShiftMeters(ShopShift.MetersForSize(0) + lapMeters * .1); game.ExtractCandy();
            PourGesture(1, 2); DriveShiftMeters(lapMeters * .2);
            double keptMeters = game.Shift.State.BatchMeters;
            double keptSugar = game.Shift.State.SugarGrams;
            int keptStock = game.Session.Economy.Inventory.Count;
            int trashedBeforeClose = game.Shift.State.DayTrashed;
            Check(keptSugar > 0 && keptStock > 0 && keptMeters > 0, "closing fixture has sugar shelf stock and active candy to reset");
            // Stop the car so closing tests isolate the clock from additional production.
            if (game.AutoDrive) game.ToggleAutoDrive(); game.World.Kart.Stop();
            while (game.Shift.IsOpen) game.Tick(0, 0, false, 60);
            Check(game.Shift.State.RemainingSeconds == 0 && game.Shift.State.Closed, "ten-minute day closes at zero");
            Check(!game.PourSugar(0) && game.ExtractCandy() == null, "closed day refuses production actions");
            Check(game.Session.Economy.Inventory.Count == 0 && game.Shift.State.BatchMeters == 0 &&
                game.Shift.State.DayTrashed == trashedBeforeClose + keptStock + 1, "closing counts leftover stock and the unfinished candy as disposals");
            Check(!game.ResumeCandy("missing-after-close"), "closed day refuses a candy resume");
            CaptureShift("05-closed.png");
            game.Initialize(saveDirectory, true, true, false);
            Check(game.Shift.State.Closed && game.Shift.State.BatchMeters == 0 && game.Shift.State.DayTrashed == trashedBeforeClose + keptStock + 1,
                "closed day and its disposal count survive reload");
            CheckDefaultShiftDriving("closed-day reload");
            Check(Mathf.Abs(game.World.GameCamera.transform.position.y - game.World.Kart.transform.position.y - 3.2f) < .001f,
                "closed-day reload immediately places the frozen camera at downhill height");
            game.StartNextDay();
            Check(game.Session.Economy.Day == 2 && game.Shift.State.RemainingSeconds == 600, "next day restarts the business clock");
            Check(game.Session.Economy.Inventory.Count == 0 && game.Shift.State.SugarGrams == 0 && game.Shift.State.SugarFlavor == -1,
                "next day clears shelf stock and loaded sugar");
            Check(game.Shift.State.BatchMeters == 0 && game.Shift.State.BatchFlavor == -1 && game.Shift.State.BatchProductId == null &&
                game.Shift.State.BatchQuality == 50 && game.SelectedProductId == null, "next day clears candy and product selection");
            Check(game.World.Kart.DriveModel.Laps == 0 && game.World.Kart.Speed == 0 && !game.World.SugarThread.enabled,
                "next day starts a fresh drive without the previous sugar thread");
            Check(game.Shift.State.Customers.Count == 1 && game.Shift.CustomerAt(0).PatienceRemaining == 90 &&
                !game.Shift.CustomerAt(0).Angry && !game.Shift.CustomerAt(0).Happy, "next day creates a fresh waiting customer");
            var nextDaySaved = new SaveStore(saveDirectory).Load();
            Check(nextDaySaved.Day == 2 && nextDaySaved.Inventory.Count == 0 && nextDaySaved.Business.SugarGrams == 0 &&
                nextDaySaved.Business.BatchMeters == 0, "next-day reset is saved immediately");
            CaptureShift("day-reset-fresh-day.png");
            game.StartNextDay(); Check(game.Session.Economy.Day == 2, "next-day button cannot increment open day twice");
            foreach (var resolution in new[] { new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(1280, 960), new Vector2Int(1920, 820) })
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return new WaitForSecondsRealtime(.25f);
                game.UI.Refresh(); Canvas.ForceUpdateCanvases();
                CheckShiftBounds();
                // Check scaled pointer coordinates and pause cancellation using the real UI handlers.
                var bag = GameObject.Find("SugarBag1").GetComponent<ShopDragItem>();
                var drag = PointerAtSource(bag.gameObject); CheckRaycast(drag, bag.gameObject, false);
                ExecuteEvents.Execute(bag.gameObject, drag, ExecuteEvents.beginDragHandler);
                MoveDrag(bag, drag, 420, 400);
                Check(game.UI.ActiveShiftDrag == bag && game.UI.ShiftDragGhost.gameObject.activeSelf, "drag ghost follows an active pointer");
                game.TogglePause();
                Check(game.UI.ActiveShiftDrag == null && !game.UI.ShiftDragGhost.gameObject.activeSelf, "pause cancels the held item and ghost");
                game.TogglePause();
                CaptureShift("06-shop-" + resolution.x + "x" + resolution.y + ".png");
            }
            // Reload only after the production and new-day reset assertions, keeping this
            // manual-input check independent of their measured travel and sugar use.
            game.Initialize(saveDirectory, true, true, false);
            CheckDefaultShiftDriving("open-day reload");
            game.ToggleAutoDrive();
            var manualDrive = game.World.Kart.DriveModel;
            game.Tick(1, 0, false, 1);
            double launchSpeed = manualDrive.Speed;
            game.Tick(1, 0, false, .6f);
            Check(manualDrive.Speed > launchSpeed + 3 && manualDrive.Speed > 29 && manualDrive.WallHits == 0,
                "manual W keeps accelerating beyond the kart limit on the starting straight");
            game.Tick(1, 0, false, .02f, false, true);
            Check(manualDrive.StoredBoosts == 0 && manualDrive.BoostCount == 0 &&
                manualDrive.BoostRemaining == 0 && !manualDrive.ManualBoostActive,
                "manual Shift cannot create a kart boost in the default business day");
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureShift("07-manual-downhill.png");
            double beforeDriftSpeed = manualDrive.Speed;
            double beforeDriftMotion = Math.Sqrt(manualDrive.Velocity.X * manualDrive.Velocity.X + manualDrive.Velocity.Z * manualDrive.Velocity.Z);
            game.Tick(1, .45f, false, .35f, true);
            double driftingMotion = Math.Sqrt(manualDrive.Velocity.X * manualDrive.Velocity.X + manualDrive.Velocity.Z * manualDrive.Velocity.Z);
            Check(manualDrive.IsDrifting && manualDrive.WallHits == 0 && manualDrive.Speed < beforeDriftSpeed - 2,
                "W plus Space visibly slows the downhill car without a wall collision");
            Check(driftingMotion < beforeDriftMotion - 1,
                "Space drift reduces actual velocity as well as the speedometer");
            Debug.Log("COTTON_DRIFT_SPEED before=" + (beforeDriftSpeed * 3.6).ToString("F2") +
                "km/h after=" + (manualDrive.Speed * 3.6).ToString("F2") + "km/h in0.35s");
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureShift("08-drift-slowdown.png");
            double beforeRelease = manualDrive.Speed;
            game.Tick(1, 0, false, .12f, false);
            Check(!manualDrive.IsDrifting && manualDrive.WallHits == 0 && manualDrive.Speed > beforeRelease,
                "releasing Space with W held resumes acceleration");
            Check(manualDrive.BoostCount == 0 && manualDrive.LastBoostedRewardDistance == 0,
                "drift release accelerates normally without kart boost rewards");
            CheckStreetCustomers();
            yield return CandyRackScenario();
            CheckEmptySugarButton();
        }

        void CheckEmptySugarButton()
        {
            game.Initialize(Path.Combine(output, "empty-sugar-" + Guid.NewGuid().ToString("N")), true, true, false);
            game.ToggleAutoDrive();
            game.PourSugar(0); game.PourSugar(0);
            game.Shift.Advance(1, ShopShift.LapMeters * .1);
            AddRackStock(1, 1);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var buttonObject = GameObject.Find("Empty sugar");
            Check(buttonObject != null, "sugar-empty action is visible beside production controls");
            var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            var state = game.Shift.State;
            double meters = state.BatchMeters, clock = state.RemainingSeconds;
            int coins = game.Session.Economy.Coins;
            string customerId = game.Shift.CustomerAt(0).Id;
            string stockId = game.Session.Economy.Inventory[0].Id;
            Check(button.interactable, "loaded sugar enables the empty button");
            CaptureShift("day-reset-sugar-loaded.png");
            var pointer = PointerAtSource(buttonObject);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == buttonObject,
                "empty button receives its real pointer raycast");
            ExecuteEvents.Execute(buttonObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(state.SugarGrams == 0 && state.SugarFlavor == -1 && !button.interactable && !game.World.SugarThread.enabled,
                "empty button clears sugar and disables further empty actions");
            Check(state.BatchMeters == meters && state.BatchFlavor == 0 && state.RemainingSeconds == clock &&
                game.Session.Economy.Inventory[0].Id == stockId && game.Session.Economy.Coins == coins &&
                game.Shift.CustomerAt(0).Id == customerId, "empty button preserves candy shelf customer clock and coins");
            var saved = new SaveStore(game.Store.DirectoryPath).Load();
            Check(saved.Business.SugarGrams == 0 && saved.Business.SugarFlavor == -1 && saved.Business.BatchMeters == meters,
                "empty-button result is saved immediately without losing current candy");
            CaptureShift("day-reset-sugar-empty.png");
            game.PourSugar(0);
            var bag = GameObject.Find("SugarBag0").GetComponent<ShopDragItem>();
            var drag = PointerAtSource(bag.gameObject);
            ExecuteEvents.Execute(bag.gameObject, drag, ExecuteEvents.beginDragHandler);
            MoveDrag(bag, drag, 430, 340);
            ExecuteEvents.Execute(buttonObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(game.UI.ActiveShiftDrag == null && !bag.IsDragging && !game.UI.ShiftDragGhost.gameObject.activeSelf,
                "empty action cancels an active bag drag");
            MoveDrag(bag, drag, 430, 395); MoveDrag(bag, drag, 430, 340);
            Check(state.SugarGrams == 0, "cancelled drag cannot immediately refill emptied sugar");
            game.PourSugar(0); game.TogglePause();
            ExecuteEvents.Execute(buttonObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(!button.interactable && !game.EmptySugar() && state.SugarGrams == 10, "pause disables button and shortcut action");
            game.TogglePause();
            game.Shift.Advance(600, 0); game.UI.Refresh();
            ExecuteEvents.Execute(buttonObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(!button.interactable && !game.EmptySugar() && state.SugarGrams == 10, "closed day disables button and shortcut action");
            game.Initialize(saveDirectory, true, true, false);
        }

        void CheckStreetCustomers()
        {
            if (game.AutoDrive) game.ToggleAutoDrive();
            game.World.Kart.Stop(); game.Tick(0, 0, false, 5);
            var economy = game.Session.Economy;
            // Isolate delivery fixtures after the production/carry-over scenarios.
            economy.Inventory.Clear();
            EnsureStreetFixture(1);
            EnsureStreetFixture(2);
            var first = game.Shift.CustomerAt(0);
            var second = game.Shift.CustomerAt(1);
            var third = game.Shift.CustomerAt(2);
            Check(first != null && second != null && third != null, "street has three independently addressable customers");
            KeepDeliveryFixturesWaiting();
            var firstPosition = GameObject.Find("CustomerDropTarget0").transform.position;
            var thirdPosition = GameObject.Find("CustomerDropTarget2").transform.position;
            for (int slot = 0; slot < 3; slot++)
            {
                var customer = game.Shift.CustomerAt(slot);
                customer.Flavor = slot; customer.Size = slot;
                StockForStreetCustomer(customer);
            }
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            for (int slot = 0; slot < 3; slot++)
            {
                var customer = game.Shift.CustomerAt(slot);
                var candy = GameObject.Find("Requested cotton " + slot).GetComponent<ShopArtGraphic>();
                Check(candy.FlavorIndex == customer.Flavor && candy.SizeTier == customer.Size,
                    "speech pictogram matches flavor and size for slot " + slot);
                var target = GameObject.Find("CustomerDropTarget" + slot);
                foreach (string artName in new[] { "Order bubble ", "Street customer " })
                {
                    var pointer = PointerAtSource(GameObject.Find(artName + slot));
                    CheckRaycast(pointer, target, true);
                }
            }
            CaptureShift("09-shop-street.png");
            int coins = economy.Coins, secondPrice = economy.Price(economy.Inventory[1]);
            string soldId = economy.Inventory[1].Id;
            DragStock(1, "CustomerDropTarget1");
            Check(economy.Coins == coins + secondPrice && game.Shift.CustomerAt(1) == second && second.Happy,
                "middle customer's drop pays their own order and shows a heart");
            Check(game.Shift.CustomerAt(0) == first && game.Shift.CustomerAt(2) == third &&
                GameObject.Find("CustomerDropTarget0").transform.position == firstPosition &&
                GameObject.Find("CustomerDropTarget2").transform.position == thirdPosition,
                "neighboring customers and target positions remain unchanged after a sale");
            Check(game.DeliverCandy(soldId, second.Id) == DeliveryResult.Rejected, "sold street order cannot pay twice");
            int thirdPrice = economy.Price(economy.Inventory[1]);
            DragStock(1, "CustomerDropTarget2");
            Check(game.Shift.CustomerAt(2) == third && third.Happy && game.Shift.CustomerAt(0) == first &&
                economy.Coins == coins + secondPrice + thirdPrice, "rightmost customer's drop selects the rightmost order");
            game.Tick(0, 0, false, 1.6f);
            Check(game.Shift.CustomerAt(1) == null && game.Shift.CustomerAt(2) == null,
                "both happy customers leave their own slots after the heart phase");
            EnsureStreetFixture(1);
            EnsureStreetFixture(2);
            second = game.Shift.CustomerAt(1); third = game.Shift.CustomerAt(2);
            Check(second != null && third != null && second.Id != third.Id,
                "three independently addressed street slots remain usable with fixture customers");
            KeepDeliveryFixturesWaiting();

            // Carry one product while the target is served by another product.
            economy.Inventory.Clear();
            var held = StockForStreetCustomer(second);
            var other = StockForStreetCustomer(second);
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var source = RackItemFor(held.Id);
            var data = PointerAtSource(source.gameObject);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.beginDragHandler);
            Check(game.DeliverCandy(other.Id, second.Id) == DeliveryResult.Sold, "replacement fixture serves the captured customer");
            game.Tick(0, 0, false, 1.6f);
            Check(game.Shift.CustomerAt(1) == null, "captured sold customer leaves after heart phase");
            EnsureStreetFixture(1);
            var replacement = game.Shift.CustomerAt(1);
            Check(replacement != null && replacement.Id != second.Id, "new customer occupies the same physical position");
            replacement.Flavor = held.FlavorIndex; replacement.Size = ShopShift.SizeOf(held);
            game.UI.Refresh();
            var drop = GameObject.Find("CustomerDropTarget1");
            data.position = PointerAtSource(drop).position;
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.dragHandler);
            CheckRaycast(data, drop, true);
            int beforeHandOff = economy.Coins, heldPrice = economy.Price(held);
            ExecuteEvents.Execute(drop, data, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.endDragHandler);
            Check(economy.Coins == beforeHandOff + heldPrice && !economy.Inventory.Contains(held) && replacement.Happy &&
                source.LastDeliveryResult == DeliveryResult.Sold,
                "candy held before a customer arrives can be handed to that new customer");
            game.Tick(0, 0, false, 1.6f);
            EnsureStreetFixture(1);
            replacement = game.Shift.CustomerAt(1);
            KeepDeliveryFixturesWaiting();
            held = StockForStreetCustomer(replacement);

            // A deliberately wrong target reacts without changing its neighbors.
            replacement.Flavor = (held.FlavorIndex + 1) % 3;
            DragStock(0, "CustomerDropTarget1");
            Check(replacement.Angry && !first.Angry && !third.Angry && game.Shift.CustomerAt(0) == first &&
                game.Shift.CustomerAt(2) == third, "wrong target alone shows the angry response");
            Check(BubbleContainsText(1, "주문이 달라요"),
                "angry customer displays their own reaction text");
            CaptureShift("10-shop-street-angry.png");
            game.Tick(0, 0, false, 1.6f);
            Check(game.Shift.CustomerAt(1) == null && game.Shift.CustomerAt(0) == first && game.Shift.CustomerAt(2) == third,
                "angry customer leaves alone after the reaction delay");
            CheckCustomerTimeout(first, third);
        }

        void KeepDeliveryFixturesWaiting()
        {
            foreach (var customer in game.Shift.State.Customers)
                if (customer != null && !customer.Angry && !customer.Happy)
                    customer.PatienceRemaining = ShopShift.CustomerPatience;
        }

        bool BubbleContainsText(int slot, string expected)
        {
            foreach (var label in GameObject.Find("Order bubble " + slot).GetComponentsInChildren<Text>())
                if (label.text.Contains(expected)) return true;
            return false;
        }

        void EnsureStreetFixture(int slot)
        {
            if (game.Shift.CustomerAt(slot) != null) return;
            // SaveStore accepts only issued shop-N orders. A fixture must reserve
            // its serial just like a natural arrival, including in accounting.
            int serial = ++game.Session.Economy.OrderSerial;
            game.Shift.State.Customers.Add(new ShopCustomer
            {
                Id = "shop-" + serial,
                Slot = slot,
                Flavor = slot % 3,
                Size = slot % 3,
                PatienceRemaining = ShopShift.CustomerPatience
            });
            if (game.Shift.State.Customers.Count == ShopShift.CustomerCapacity)
                game.Shift.State.NextCustomerIn = ShopShift.ArrivalDelay;
            game.UI.Refresh();
            Check(SaveStore.Valid(game.Session.Economy), "issued street fixture remains valid for saving");
        }

        void CheckResumeCandy()
        {
            var economy = game.Session.Economy;
            double lap = game.World.Kart.DriveModel.Course.Length;
            Check(economy.Inventory.Count == 0 && game.Shift.State.BatchMeters == 0,
                "resume fixture starts with an empty shelf and no growing batch");
            var resumed = ShopShift.Preview(lap * 1.2, 2);
            resumed.Id = Guid.NewGuid().ToString("N"); resumed.Quality = 73;
            economy.Inventory.Add(resumed); economy.CompletedIds.Add(resumed.Id);
            for (int i = 1; i < economy.StockCapacity; i++) AddRackStock(i % 3, 0);
            PourGesture(1, 2);
            DriveShiftMeters(lap * .12);
            string formerBatchId = game.Shift.State.BatchProductId;
            int exchangeSlot = economy.Inventory.IndexOf(resumed);
            double formerMeters = game.Shift.State.BatchMeters;
            int formerFlavor = game.Shift.State.BatchFlavor;
            int formerQuality = game.Shift.State.BatchQuality;
            double formerSugar = game.Shift.State.SugarGrams;
            Check(formerSugar > 0 && game.Shift.State.SugarFlavor == 1,
                "mismatched sugar remains loaded with the growing batch");
            int coins = economy.Coins;
            DragStockToRace(resumed.Id);
            var swapped = economy.Inventory[exchangeSlot];
            if (string.IsNullOrEmpty(formerBatchId)) formerBatchId = swapped.Id;
            Check(economy.Inventory.Count == economy.StockCapacity &&
                !string.IsNullOrEmpty(formerBatchId) && swapped.Id == formerBatchId &&
                economy.Inventory.Exists(product => product.Id == formerBatchId) &&
                !economy.Inventory.Exists(product => product.Id == resumed.Id),
                "full shelf atomically exchanges the growing batch for the selected candy");
            Check(swapped != null && Math.Abs(swapped.DistanceMeters - formerMeters) < .001 &&
                swapped.FlavorIndex == formerFlavor && swapped.Quality == formerQuality,
                "swapped candy preserves the previous batch identity, meters, flavor and quality");
            Check(game.Shift.State.BatchProductId == resumed.Id &&
                Math.Abs(game.Shift.State.BatchMeters - resumed.DistanceMeters) < .001 &&
                game.Shift.State.BatchFlavor == resumed.FlavorIndex && game.Shift.State.BatchQuality == resumed.Quality &&
                economy.Coins == coins && Math.Abs(game.Shift.State.SugarGrams - formerSugar) < .001,
                "resumed batch preserves product identity, distance, flavor and quality without payment or sugar loss");
            CaptureShift("11-resumed-candy.png");
            Check(!game.ResumeCandy(resumed.Id), "already resumed candy cannot be resumed again from the shelf");
            game.TogglePause();
            Check(!game.ResumeCandy(formerBatchId), "paused shift refuses another resume");
            game.TogglePause();
            double frozenMeters = game.Shift.State.BatchMeters;
            double frozenSugar = game.Shift.State.SugarGrams;
            DriveShiftUnfueledMeters(lap * .08, "mismatched sugar driving sample");
            Check(game.Shift.State.BatchMeters == frozenMeters && game.Shift.State.SugarGrams == frozenSugar,
                "mismatched loaded sugar cannot grow or consume the resumed candy");
            PourGesture(2, 1);
            Check(game.Shift.State.SugarFlavor == 2, "matching sugar pour explicitly replaces the mismatched fuel");
            DriveShiftMeters(lap * .06);
            Check(game.Shift.State.BatchProductId == resumed.Id && game.Shift.State.BatchMeters > frozenMeters &&
                game.Shift.State.BatchFlavor == resumed.FlavorIndex && game.Shift.State.BatchQuality >= 0,
                "actual kart travel continues the exact resumed product after matching sugar is poured");
            Check(game.TrashCandy(economy.Inventory[0].Id), "one shelf slot can be cleared to extract the resumed batch");
            double grownMeters = game.Shift.State.BatchMeters;
            int grownQuality = game.Shift.State.BatchQuality;
            var extracted = game.ExtractCandy();
            Check(extracted != null && extracted.Id == resumed.Id &&
                Math.Abs(extracted.DistanceMeters - grownMeters) < .001 &&
                extracted.FlavorIndex == resumed.FlavorIndex && extracted.Quality == grownQuality,
                "extracting resumed candy returns its original identity and current batch properties");
            economy.Inventory.Clear();
            game.UI.Refresh();
        }

        void CheckCustomerTimeout(ShopCustomer first, ShopCustomer third)
        {
            KeepDeliveryFixturesWaiting();
            var economy = game.Session.Economy;
            var retained = StockForStreetCustomer(first);
            int missed = game.Shift.State.DayMissed;
            int stock = economy.Inventory.Count, coins = economy.Coins;
            first.PatienceRemaining = ShopShift.CustomerPatience;
            Check(SaveStore.Valid(economy), "timeout fixture is valid before its patience run");
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var patienceBar = GameObject.Find("Customer patience 0");
            Check(patienceBar && patienceBar.activeInHierarchy,
                "waiting customer displays a patience gauge");
            game.Tick(0, 0, false, 45);
            Check(Math.Abs(first.PatienceRemaining - 45) < .001 && !first.Angry,
                "customer retains half of the ninety-second waiting allowance");
            foreach (var customer in game.Shift.State.Customers)
                if (customer != first && !customer.Angry && !customer.Happy)
                    customer.PatienceRemaining = ShopShift.CustomerPatience;
            game.Tick(0, 0, false, 44.9f);
            Check(game.Shift.CustomerAt(0) == first && !first.Angry && first.PatienceRemaining > 0,
                "customer keeps waiting just before the ninety-second deadline");
            double patience = first.PatienceRemaining;
            Check(SaveStore.Valid(economy), "timeout fixture remains valid immediately before pause");
            game.TogglePause(); game.Tick(0, 0, false, 5);
            Check(first.PatienceRemaining == patience && game.Shift.State.DayMissed == missed,
                "pause freezes patience without recording a missed order");
            game.TogglePause(); game.Tick(0, 0, false, .2f);
            Check(game.Shift.CustomerAt(0) == first && first.Angry && first.TimedOut &&
                game.Shift.State.DayMissed == missed + 1 &&
                economy.Inventory.Count == stock && economy.Coins == coins,
                "timeout records one miss, preserves stock and shows an angry reaction");
            Check(SaveStore.Valid(economy), "timed-out customer state is valid for saving");
            Check(GameObject.Find("Customer emote 0").activeInHierarchy &&
                GameObject.Find("Customer emote 0").GetComponent<ShopStreetGraphic>().Kind == ShopStreetArtKind.AngryEmote &&
                !patienceBar.activeInHierarchy,
                "timed-out customer changes from patience gauge to angry emote");
            Check(game.DeliverCandy(retained.Id, first.Id) == DeliveryResult.Rejected && economy.Inventory.Contains(retained),
                "timed-out customer rejects further delivery");
            CaptureShift("13-customer-timeout.png");
            game.Tick(0, 0, false, 1.6f);
            Check(game.Shift.CustomerAt(0) == null && game.Shift.CustomerAt(2) == third &&
                game.Shift.State.DayMissed == missed + 1,
                "timeout reaction leaves its own slot without a second missed count");
            EnsureStreetFixture(0);
        }

        Product StockForStreetCustomer(ShopCustomer customer)
        {
            var product = ShopShift.Preview(ShopShift.MetersForSize(customer.Size), customer.Flavor);
            product.Id = Guid.NewGuid().ToString("N");
            game.Session.Economy.Inventory.Add(product);
            game.Session.Economy.CompletedIds.Add(product.Id);
            return product;
        }

        void CheckDefaultShiftDriving(string context)
        {
            var kart = game.World.Kart;
            Check(game.AutoDrive, context + " opens with auto driving enabled");
            Check(game.PreparedStyle == DrivingStyle.Downhill && game.RunStyle == DrivingStyle.Downhill &&
                kart.DriveModel.Style == DrivingStyle.Downhill, context + " uses the downhill driving model");
            Check(kart.DriveModel.StoredBoosts == 0, context + " has no inherited kart booster");
            var coupe = kart.transform.Find("Downhill coupe visual");
            Check(coupe && coupe.gameObject.activeInHierarchy, context + " activates the downhill coupe");
            bool visibleCoupe = false;
            foreach (var renderer in coupe.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && renderer.bounds.size.sqrMagnitude > 0) visibleCoupe = true;
            Check(visibleCoupe, context + " has an enabled coupe renderer");
            var originalKart = kart.transform.GetChild(0);
            Check(originalKart != coupe && !originalKart.gameObject.activeInHierarchy,
                context + " hides the original kart visual");
        }

        void CheckShiftRoadWidths()
        {
            Check(game.World.CourseRoots != null && game.World.CourseRoots.Length == RaceCourse.MapCount,
                "all road courses are present in the built scene");
            for (int map = 0; map < game.World.CourseRoots.Length; map++)
            {
                var root = game.World.CourseRoots[map];
                CheckShiftRoadWidth(root.Find("Racing surface " + (map + 1)), 14.4f,
                    (float)RaceCourse.MainHalfWidth * 2);
                CheckShiftRoadWidth(root.Find("Sugar cut shortcut " + (map + 1)), 6.6f,
                    (float)RaceCourse.ShortcutHalfWidth * 2);
            }
        }

        void CheckShiftRoadWidth(Transform road, float expectedWidth, float drivingWidth)
        {
            Check(road && road.GetComponent<MeshFilter>() && road.GetComponent<Renderer>(),
                "road ribbon has a mesh and renderer");
            var mesh = road.GetComponent<MeshFilter>().sharedMesh;
            Check(mesh && mesh.isReadable && mesh.vertexCount >= 4 && mesh.vertexCount % 2 == 0,
                road.name + " has readable paired road edges");
            var vertices = mesh.vertices;
            float minWidth = float.MaxValue, maxWidth = 0;
            for (int i = 0; i < vertices.Length; i += 2)
            {
                float width = Vector3.Distance(road.TransformPoint(vertices[i]), road.TransformPoint(vertices[i + 1]));
                minWidth = Mathf.Min(minWidth, width); maxWidth = Mathf.Max(maxWidth, width);
            }
            Check(Mathf.Abs(minWidth - expectedWidth) < .002f && Mathf.Abs(maxWidth - expectedWidth) < .002f &&
                Mathf.Abs(drivingWidth - expectedWidth) < .002f,
                road.name + " renders the widened " + expectedWidth.ToString("F1") + "m driving surface" +
                " (mesh=" + minWidth.ToString("F3") + ".." + maxWidth.ToString("F3") + "m, driving=" + drivingWidth.ToString("F3") + "m)");
        }

        void DriveShiftSeconds(double seconds)
        {
            for (double remaining = seconds; remaining > 1e-8;)
            {
                float step = (float)Math.Min(.05, remaining);
                game.Tick(0, 0, false, step);
                remaining -= step;
            }
        }

        void CheckLapBasedProduction()
        {
            var drive = game.World.Kart.DriveModel;
            double lapMeters = drive.Course.Length, startGrowth = speedGrownMeters;
            PourGesture(0, 10);
            Check(game.Shift.State.SugarGrams == 100, "ten pours fill the hundred-gram tank");
            for (int tier = 0; tier < 3; tier++)
            {
                double threshold = lapMeters * (1 + tier * .5);
                string size = new[] { "소", "중", "대" }[tier];
                DriveShiftUnfueledGrowth(threshold - lapMeters * .02 - (speedGrownMeters - startGrowth),
                    "approaching size " + size);
                Check(game.Shift.State.BatchMeters < threshold && ShopShift.SizeForDistance(game.Shift.State.BatchMeters) == tier - 1,
                    "actual driving stays below size " + size + " before " + (1 + tier * .5) + " grown laps" + ShiftProductionDetails());
                DriveShiftUnfueledGrowth(threshold + lapMeters * .005 - (speedGrownMeters - startGrowth),
                    "crossing size " + size);
                double traveled = speedGrownMeters - startGrowth;
                Check(ShopShift.SizeForDistance(game.Shift.State.BatchMeters) == tier,
                    "actual driving reaches size " + size + " at " + (1 + tier * .5) + " laps" + ShiftProductionDetails());
                Check(Math.Abs(game.Shift.State.BatchMeters - Math.Min(traveled, lapMeters * 2)) < .00001 &&
                    Math.Abs(game.Shift.State.SugarGrams - Math.Max(0, 100 - traveled / lapMeters * 50)) < .00001,
                    "size " + size + " production and sugar match speed-scaled forward progress without refills" + ShiftProductionDetails());
            }
            Check(game.Shift.State.SugarGrams == 0 && Math.Abs(game.Shift.State.BatchMeters - lapMeters * 2) < .00001,
                "a full tank makes two laps of candy before running out" + ShiftProductionDetails());
            var largest = game.ExtractCandy();
            Check(largest != null && ShopShift.SizeOf(largest) == 2, "the two-lap batch extracts as size 대");
            Check(game.TrashCandy(largest.Id), "size calibration stock is removed before the delivery scenario");
        }

        // Mirrors ShiftController: each .05s tick drives once, and growth is the forward
        // distance of that step scaled by the kart's speed yield.
        double speedGrownMeters;
        void TickShiftGrowth()
        {
            var drive = game.World.Kart.DriveModel;
            game.Tick(0, 0, false, .05f);
            speedGrownMeters += drive.LastRewardDistance * ShopShift.SpeedYield(drive.Speed);
        }

        void DriveShiftUnfueledGrowth(double meters, string purpose)
        {
            double initial = speedGrownMeters;
            int guard = 20000;
            while (speedGrownMeters - initial < meters && guard-- > 0 && game.Shift.IsOpen) TickShiftGrowth();
            Check(guard > 0 && game.Shift.IsOpen && speedGrownMeters - initial >= meters,
                purpose + " reaches speed-scaled growth " + meters.ToString("F3") + "m" + ShiftProductionDetails());
            game.UI.Refresh();
        }

        void DriveShiftUnfueledMeters(double meters, string purpose)
        {
            var drive = game.World.Kart.DriveModel;
            double initial = drive.TotalProgress;
            int guard = 10000;
            while (drive.TotalProgress - initial < meters && guard-- > 0 && game.Shift.IsOpen)
                TickShiftGrowth();
            Check(guard > 0 && game.Shift.IsOpen && drive.TotalProgress - initial >= meters,
                purpose + " reaches actual forward distance " + meters.ToString("F3") + "m" + ShiftProductionDetails());
            game.UI.Refresh();
        }

        string ShiftProductionDetails()
        {
            return " (progress=" + game.World.Kart.DriveModel.TotalProgress.ToString("F6") +
                "m, batch=" + game.Shift.State.BatchMeters.ToString("F6") +
                "m, sugar=" + game.Shift.State.SugarGrams.ToString("F6") +
                "g, lap=" + game.World.Kart.DriveModel.Course.Length.ToString("F6") + "m)";
        }

        void DriveShiftMeters(double meters)
        {
            double initial = game.Shift.State.BatchMeters;
            int guard = 10000;
            while (game.Shift.State.BatchMeters - initial < meters && guard-- > 0 && game.Shift.IsOpen)
            {
                if (game.Shift.State.SugarGrams < 2) game.PourSugar(Math.Max(0, game.Shift.State.BatchFlavor));
                game.Tick(0, 0, false, .05f);
            }
            Check(guard > 0 && game.Shift.IsOpen, "actual kart reaches production distance before closing" + ShiftProductionDetails());
            game.UI.Refresh();
        }

        Vector2 ScreenPoint(float x, float y)
        {
            var root = GameObject.Find("Centered game composition").GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(null, root.TransformPoint(new Vector3(x - 800, 450 - y, 0)));
        }
        PointerEventData PointerAtSource(GameObject source)
        {
            Canvas.ForceUpdateCanvases();
            var rect = source.GetComponent<RectTransform>();
            return new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)), pointerDrag = source, button = PointerEventData.InputButton.Left };
        }
        void CheckRaycast(PointerEventData data, GameObject expected, bool drop)
        {
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            GameObject found = hits.Count == 0 ? null : drop ? ExecuteEvents.GetEventHandler<IDropHandler>(hits[0].gameObject) : ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
            if (found != expected)
            {
                var graphic = expected.GetComponent<Graphic>();
                var rect = expected.GetComponent<RectTransform>();
                string detail = " pointer=" + data.position + " found=" + (found ? found.name : "<none>") +
                    " expectedActive=" + expected.activeInHierarchy +
                    " graphic=" + (graphic ? graphic.GetType().Name : "<none>") +
                    " raycastTarget=" + (graphic && graphic.raycastTarget) +
                    " depth=" + (graphic ? graphic.depth.ToString() : "<none>") +
                    " rectContains=" + (rect && RectTransformUtility.RectangleContainsScreenPoint(rect, data.position, null));
                for (int i = 0; i < Math.Min(5, hits.Count); i++)
                    detail += " hit" + i + "=" + hits[i].gameObject.name + "/" + hits[i].module.GetType().Name +
                        "/depth" + hits[i].depth;
                Check(false, expected.name + " receives the actual pointer raycast;" + detail);
                return;
            }
            Check(true, expected.name + " receives the actual pointer raycast");
        }
        void CaptureShift(string name)
        {
            // Simulated ticks advance the kart in one frame, before WorldView.LateUpdate.
            // Align the existing chase camera before rendering that simulated state.
            game.World.SetMode(GameMode.Racing); CaptureOffscreen(name);
        }
        void MoveDrag(ShopDragItem item, PointerEventData data, float x, float y)
        { data.position = ScreenPoint(x, y); ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.dragHandler); }
        void PourGesture(int flavor, int cycles)
        {
            var bag = GameObject.Find("SugarBag" + flavor).GetComponent<ShopDragItem>();
            for (int i = 0; i < cycles; i++)
            {
                // A strong stroke is ten grams; release between fixture pours so
                // the returning stroke cannot become the next confirmed pour.
                var data = PointerAtSource(bag.gameObject);
                if (i == 0) CheckRaycast(data, bag.gameObject, false);
                ExecuteEvents.Execute(bag.gameObject, data, ExecuteEvents.beginDragHandler);
                MoveDrag(bag, data, 430, 340);
                MoveDrag(bag, data, 430, 472);
                MoveDrag(bag, data, 430, 340);
                ExecuteEvents.Execute(bag.gameObject, data, ExecuteEvents.endDragHandler);
            }
        }
        void DragStock(int index, string targetName)
        {
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var item = RackItemFor(game.Session.Economy.Inventory[index].Id);
            var target = GameObject.Find(targetName);
            var data = PointerAtSource(item.gameObject);
            CheckRaycast(data, item.gameObject, false);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.beginDragHandler);
            data.position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center));
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.dragHandler);
            CheckRaycast(data, target, true);
            ExecuteEvents.Execute(target, data, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.endDragHandler);
        }
        void DragStockToRace(string productId)
        {
            game.UI.Refresh(); Canvas.ForceUpdateCanvases();
            var item = RackItemFor(productId);
            var data = PointerAtSource(item.gameObject);
            CheckRaycast(data, item.gameObject, false);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.beginDragHandler);
            var target = GameObject.Find("RaceResumeDropTarget");
            Check(target && target.activeInHierarchy, "race resume target appears while a shelf candy is held");
            // The target becomes active in BeginDrag. Probe before rebuilding the canvas
            // so a same-frame GraphicRaycaster registration delay is visible in the log.
            data.position = ScreenPoint(400, 150);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.dragHandler);
            var immediateHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, immediateHits);
            bool immediateTarget = immediateHits.Count > 0 &&
                ExecuteEvents.GetEventHandler<IDropHandler>(immediateHits[0].gameObject) == target;
            Debug.Log("RUNTIME_RACE_RAYCAST immediate=" + immediateTarget +
                " top=" + (immediateHits.Count > 0 ? immediateHits[0].gameObject.name : "<none>") +
                " depth=" + target.GetComponent<Graphic>().depth);
            // A hidden player needs an explicit render to assign native Graphic depth;
            // ForceUpdateCanvases alone only rebuilds the newly enabled mesh.
            CaptureShift("14-resume-drag.png");
            CheckRaycast(data, target, true);
            data.position = ScreenPoint(650, 500);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.dragHandler);
            CheckRaycast(data, target, true);
            ExecuteEvents.Execute(target, data, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(item.gameObject, data, ExecuteEvents.endDragHandler);
            Check(!target.activeInHierarchy, "race resume target hides after the product drop");
        }
        void CheckShiftBounds()
        {
            foreach (string name in new[] { "SugarBag0", "SugarBag1", "SugarBag2", "CustomerDropTarget0", "CustomerDropTarget1", "CustomerDropTarget2", "Shop street", "TrashDropTarget", "Empty sugar", "Extract candy" })
            {
                var rect = GameObject.Find(name).GetComponent<RectTransform>(); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
                    Check(point.x >= -.5 && point.x <= Screen.width + .5 && point.y >= -.5 && point.y <= Screen.height + .5, name + " remains within viewport");
                }
            }
            Check(game.World.GameCamera.rect.xMax <= game.World.ShopCamera.rect.xMin + .001, "race and shop cameras do not overlap");
        }
    }
}
#endif
