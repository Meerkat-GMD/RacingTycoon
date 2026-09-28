using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CottonCircuit
{
    public partial class GameUI
    {
        readonly string[] businessStockIds = new string[12];
        readonly VisualElement[] businessStock = new VisualElement[12];
        readonly VisualElement[] businessCustomers = new VisualElement[3];
        readonly VisualElement[] businessSugarBags = new VisualElement[3];
        readonly SugarShake businessShake = new SugarShake();
        VisualElement businessScreen, businessRace, businessGhost, businessDragSource;
        bool businessDraggingSugar;
        int businessDragFlavor, businessPointerId;
        string businessDragProductId, businessHoveredProductId;
        float businessPourMessageUntil;

        public bool ShiftInteractionsAllowed => game && game.Shift != null && game.Shift.IsOpen &&
            !game.Shift.Paused && !game.Session.Paused && game.Store != null && game.Store.CanSave;
        public bool TutorialIsSugarDragging => businessDragSource != null && businessDraggingSugar && businessDragFlavor == 0;
        public bool TutorialSugarOverRace => TutorialIsSugarDragging && businessRace.ClassListContains("drop-hover");
        public VisualElement TutorialSugarTarget => businessSugarBags[0];
        public VisualElement TutorialExtractTarget => Q<Button>("businessExtract");
        public string ShiftCustomerIdAt(int slot) => game && game.Shift != null ? game.Shift.CustomerAt(slot)?.Id : null;
        public bool BusinessDragActive => businessDragSource != null;

        void BindBusiness()
        {
            businessScreen = Q<VisualElement>("businessScreen");
            businessRace = Q<VisualElement>("raceSurface");
            businessGhost = Q<VisualElement>("businessDragGhost");
            Q<Button>("businessExtract").clicked += () => game.ExtractCandy();
            Q<Button>("businessEmptySugar").clicked += () => game.EmptySugar();
            Q<Button>("businessNextDay").clicked += () =>
            {
                if (game.HasProgression) game.ReturnToPreparation(); else game.StartNextDay();
            };
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                Q<Button>("businessMachine" + i).clicked += () => { CancelShiftDrag(); game.ChooseMachine(slot); };
                businessSugarBags[i] = Q<VisualElement>("sugar" + i);
                businessSugarBags[i].AddManipulator(new ToolkitDragController(this, true, i));
                businessCustomers[i] = Q<VisualElement>("customer" + i);
                SetArt(Q<VisualElement>("sugarArt" + i), UiArt.SugarBag(i));
            }
            for (int i = 0; i < businessStock.Length; i++)
            {
                int slot = i;
                businessStock[i] = Q<VisualElement>("stock" + i);
                businessStock[i].AddManipulator(new ToolkitDragController(this, false, i));
                businessStock[i].RegisterCallback<PointerEnterEvent>(_ => businessHoveredProductId = businessStockIds[slot]);
                businessStock[i].RegisterCallback<PointerLeaveEvent>(_ => { if (businessHoveredProductId == businessStockIds[slot]) businessHoveredProductId = null; });
            }
            SetArt(Q<VisualElement>("businessStoreArt"), UiArt.Storefront);
            SetArt(Q<VisualElement>("businessTrashArt"), UiArt.Trash);
        }

        void RefreshBusiness()
        {
            if (game.Shift == null) { Show(businessScreen, false); CancelShiftDrag(); return; }
            Show(businessScreen, !game.InPreparation);
            var shift = game.Shift; var state = shift.State; var economy = game.Session.Economy;
            bool allowed = ShiftInteractionsAllowed, worker = game.SelectedMachineHasWorker;
            bool manual = allowed && !worker, progression = game.HasProgression;
            if (!allowed || businessDragSource != null && (businessDraggingSugar && worker ||
                !businessDraggingSugar && BusinessProduct(businessDragProductId) == null)) CancelShiftDrag();

            SetText("businessDay", "DAY " + economy.Day.ToString("00"));
            SetText("businessWallet", economy.Coins.ToString("N0") + " C");
            int seconds = Math.Max(0, (int)Math.Ceiling(state.RemainingSeconds));
            SetText("businessClock", (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"));
            SetText("businessDayCaption", state.Closed ? Strings.Get("business.day.closed") : Strings.Format("business.day.open", state.DaySold));
            Show(Q<VisualElement>("businessMachines"), progression);
            for (int i = 0; i < 3; i++)
            {
                bool owned = !progression ? i == 0 : i < Progression.OwnedMachines(economy);
                bool assigned = shift.HasWorker(i), selected = i == shift.SelectedMachine;
                var button = Q<Button>("businessMachine" + i);
                button.text = Strings.Format("business.machine.button", i + 1, Strings.Get(!owned ? "business.machine.locked" :
                    assigned ? shift.WorkerCanOperate(i) ? "business.machine.helper" : "business.machine.helper.idle" :
                    selected ? "business.machine.manual" : "business.machine.idle"));
                button.SetEnabled(allowed && owned && !game.TutorialActive);
                button.EnableInClassList("selected", selected);
                button.tooltip = Strings.Get(assigned ? "business.machine.tooltip.assigned" : "business.machine.tooltip.manual");
            }
            SetText("businessGrade", Strings.Format("business.grade", shift.SugarGrade(shift.SelectedMachine)));
            SetText("businessMinimapTitle", progression ? "M0" + (shift.SelectedMachine + 1) + " / LIVE" : "SUGARWAY / LIVE");
            Q<Image>("businessMinimap").image = game.World.MinimapTexture;

            SetText("businessSugar", state.SugarGrams > .00001
                ? Strings.Format("business.sugar.loaded", Palette.FlavorName(state.SugarFlavor), state.SugarGrams.ToString("0.0"))
                : Strings.Get("business.sugar.empty"));
            Q<ProgressBar>("businessSugarMeter").value = (float)state.SugarGrams;
            int tier = ShopShift.SizeForDistance(state.BatchMeters);
            int maxSize = progression ? shift.MaxSize(shift.SelectedMachine) : 2;
            if (progression && state.BatchMeters > 0) maxSize = Math.Min(maxSize, Math.Max(0, state.BatchSugarGrade - 1));
            bool capped = tier >= maxSize, sized = !progression || Progression.MaxSugarGrade(economy) > 1;
            SetText("businessBatchStage", BusinessTierName(tier));
            SetText("businessBatchStars", state.BatchMeters > 0 ? ShopShift.StarText(state.BatchQuality) : "");
            SetText("businessBatchName", state.BatchMeters <= 0 ? Strings.Get("business.batch.ready")
                : tier < 0 ? Strings.Format("business.batch.unsellable", Palette.FlavorName(state.BatchFlavor))
                : Palette.FlavorName(state.BatchFlavor));
            double next = ShopShift.MetersForSize(Math.Min(maxSize, tier + 1));
            double previous = tier < 0 ? 0 : ShopShift.MetersForSize(tier);
            string laps = (Math.Ceiling(Math.Max(0, next - state.BatchMeters) / ShopShift.LapMeters * 100) / 100).ToString("0.00");
            string nextText = capped ? Strings.Get(sized ? "business.next.max" : "business.next.done")
                : sized ? Strings.Format("business.next.size", Progression.SizeName(tier + 1), laps)
                : Strings.Format("business.next.finish", laps);
            if (worker) nextText = economy.Inventory.Count >= economy.StockCapacity ? Strings.Get("business.next.shelf.full")
                : game.WorkerDriving ? Strings.Get("business.next.worker.making") : Strings.Get("business.next.worker.idle");
            SetText("businessNextSize", nextText);
            Q<ProgressBar>("businessGrowthMeter").value = capped ? 100 : Mathf.Clamp01((float)((state.BatchMeters - previous) / Math.Max(.001, next - previous))) * 100;
            var batchArt = Q<VisualElement>("businessBatchArt");
            SetArt(batchArt, UiArt.CottonCandy(Math.Max(0, state.BatchFlavor), Math.Max(0, tier)));
            batchArt.EnableInClassList("biz-faint", state.BatchMeters <= 0);
            SetText("businessExtractLabel", Strings.Get(worker ? "business.extract.worker" : "business.extract.label"));
            Show(Q<VisualElement>("businessExtractKey"), !worker);
            Q<Button>("businessExtract").SetEnabled(manual && state.BatchMeters > 0 && economy.Inventory.Count < economy.StockCapacity);
            SetText("businessEmptySugarLabel", Strings.Get(worker ? "business.empty.worker" : "business.empty.label"));
            Show(Q<VisualElement>("businessEmptySugarKey"), !worker);
            Q<Button>("businessEmptySugar").SetEnabled(manual && state.SugarGrams > 0);
            for (int i = 0; i < 3; i++)
            {
                bool available = !progression || shift.CanMakeFlavor(shift.SelectedMachine, i);
                businessSugarBags[i].SetEnabled(manual && available);
                int cost = available && progression ? shift.PourCost(shift.SelectedMachine, i) : 0;
                SetText("sugarCost" + i, !available ? Strings.Get(shift.MachineMakesFlavor(shift.SelectedMachine, i) ? "business.sugar.tutorial" : "business.sugar.locked")
                    : worker ? Strings.Get("business.sugar.worker") : cost > 0 ? cost + " C / 10g" : Strings.Get("business.sugar.free"));
                businessSugarBags[i].tooltip = Strings.Get("business.sugar.tooltip");
            }
            RefreshBusinessCustomers(allowed);
            RefreshBusinessShelf(allowed);
            RefreshBusinessDriving(worker);
            Show(Q<VisualElement>("businessResults"), state.Closed);
            SetText("businessResultTitle", Strings.Format("business.result.title", economy.Day.ToString("00")));
            SetText("businessResultCaption", progression ? Progression.LocationName(economy.Progression.SelectedLocation) : Strings.Get("business.result.caption.default"));
            SetText("businessResultStats", Strings.Format("business.result.stats", state.DaySold, state.DayRevenue.ToString("N0"),
                state.DayMaterialCost.ToString("N0"), (state.DayRevenue - state.DayMaterialCost).ToString("N0"),
                state.DayWrong, state.DayMissed, state.DayTrashed));
            Q<Button>("businessNextDay").text = Strings.Get(progression ? "business.nextday.progression" : "business.nextday.simple");
        }

        void RefreshBusinessDriving(bool worker)
        {
            var kart = game.World.Kart; var drive = kart.DriveModel;
            bool downhill = game.RunStyle == DrivingStyle.Downhill;
            SetText("businessSpeed", Mathf.RoundToInt(kart.Speed * 3.6f) + " km/h");
            double speedYield = ShopShift.SpeedYield(kart.Speed);
            SetText("businessSpeedYield", worker ? Strings.Get(game.WorkerDriving ? "business.speed.worker.driving" : "business.speed.worker.idle")
                : speedYield <= 0 ? Strings.Get("business.speed.warmup") : Strings.Format("business.speed.yield", speedYield.ToString("0.0")));
            Show(Q<VisualElement>("businessBoostCard"), !downhill && !worker);
            SetText("businessBoost", kart.Boosting ? Strings.Get("business.boost.active") : "BOOSTER  " + drive.StoredBoosts + " / 2");
            var meter = Q<ProgressBar>("businessBoostMeter");
            meter.value = 100 * (kart.Boosting ? Mathf.Clamp01((float)(drive.BoostRemaining / Math.Max(.01, drive.BoostDuration))) : kart.Charge);
            meter.EnableInClassList("boosting", kart.Boosting);
            bool impact = kart.ImpactFlash > 0;
            Show(Q<Label>("businessRaceEvent"), impact);
            SetText("businessRaceEvent", impact ? Strings.Get("business.race.impact") + "  " +
                (game.Shift.State.BatchMeters > 0 ? ShopShift.StarText(game.Shift.State.BatchQuality) : "") +
                (worker ? "" : " · " + Strings.Get("business.race.recover")) : "");
        }

        void RefreshBusinessCustomers(bool allowed)
        {
            int location = game.HasProgression ? game.Session.Economy.Progression.SelectedLocation : 0;
            SetArt(Q<VisualElement>("businessStreetArt"), UiArt.LocationStreet(location));
            bool sized = !game.HasProgression || Progression.MaxSugarGrade(game.Session.Economy) > 1;
            for (int i = 0; i < businessCustomers.Length; i++)
            {
                var customer = game.Shift.CustomerAt(i);
                bool exists = customer != null;
                // Keep each queue position's authored layout reserved while no customer is waiting.
                businessCustomers[i].style.visibility = exists ? Visibility.Visible : Visibility.Hidden;
                if (!exists) continue;
                bool reacting = customer.Angry || customer.Happy;
                SetArt(Q<VisualElement>("customerArt" + i), UiArt.Customer((i + location + customer.Flavor) % 3, customer.Angry));
                SetArt(Q<VisualElement>("customerOrderArt" + i), reacting ? customer.Happy ? UiArt.HeartEmote : UiArt.AngryEmote : UiArt.CottonCandy(customer.Flavor, customer.Size));
                SetText("customerOrderText" + i, reacting ? Strings.Get(customer.Happy ? "business.customer.thanks" : customer.TimedOut ? "business.customer.slow" : "business.customer.wrong")
                    : sized ? Strings.Format("business.customer.order", Palette.FlavorName(customer.Flavor), BusinessTierName(customer.Size)) : Palette.FlavorName(customer.Flavor));
                var patience = Q<ProgressBar>("customerPatience" + i);
                Show(patience, !reacting);
                float fraction = Mathf.Clamp01((float)(customer.PatienceRemaining / game.Shift.PatienceLimit));
                patience.value = fraction * 100;
                patience.EnableInClassList("warning", fraction < .25f);
                businessCustomers[i].tooltip = reacting ? "" : Strings.Get("business.customer.tooltip");
            }
        }

        void RefreshBusinessShelf(bool allowed)
        {
            var economy = game.Session.Economy;
            int capacity = Math.Min(economy.StockCapacity, businessStock.Length);
            SetText("businessShelfCount", Strings.Format("business.shelf.count", economy.Inventory.Count, economy.StockCapacity));
            var rack = Q<VisualElement>("businessStockGrid");
            rack.EnableInClassList("rack-six", capacity <= 6);
            rack.EnableInClassList("rack-nine", capacity > 6 && capacity <= 9);
            rack.EnableInClassList("rack-twelve", capacity > 9);
            Show(Q<Label>("businessRackEmpty"), economy.Inventory.Count == 0);
            for (int i = 0; i < businessStockIds.Length; i++)
                if (i >= capacity || BusinessProduct(businessStockIds[i]) == null) businessStockIds[i] = null;
            foreach (var product in economy.Inventory)
            {
                int empty = -1; bool bound = false;
                for (int i = 0; i < capacity; i++)
                {
                    if (businessStockIds[i] == product.Id) { bound = true; break; }
                    if (empty < 0 && businessStockIds[i] == null) empty = i;
                }
                if (!bound && empty >= 0) businessStockIds[empty] = product.Id;
            }
            bool sized = !game.HasProgression || Progression.MaxSugarGrade(economy) > 1;
            for (int i = 0; i < businessStock.Length; i++)
            {
                Show(businessStock[i], i < capacity);
                if (i >= capacity) continue;
                var product = BusinessProduct(businessStockIds[i]);
                businessStock[i].pickingMode = product == null ? PickingMode.Ignore : PickingMode.Position;
                var art = Q<VisualElement>("stockArt" + i);
                Show(art, product != null);
                businessStock[i].EnableInClassList("occupied", product != null);
                businessStock[i].EnableInClassList("selected", product != null && (product.Id == game.SelectedProductId || product.Id == businessHoveredProductId));
                if (product == null) { SetText("stockGrade" + i, ""); businessStock[i].tooltip = Strings.Get("business.shelf.slot.empty"); continue; }
                int tier = ShopShift.SizeOf(product);
                SetArt(art, UiArt.BaggedCandy(product.FlavorIndex, Math.Max(0, tier)));
                SetText("stockGrade" + i, tier < 0 ? Strings.Get("business.unfinished") : (sized ? BusinessTierName(tier) + " " : "") + ShopShift.StarText(product.Quality));
                businessStock[i].tooltip = tier < 0
                    ? Strings.Format("business.shelf.tooltip.grow", Palette.FlavorName(product.FlavorIndex), BusinessDistance(product.DistanceMeters))
                    : Strings.Format("business.shelf.tooltip.sell", Palette.FlavorName(product.FlavorIndex), BusinessDistance(product.DistanceMeters), economy.Price(product).ToString("N0"));
            }
        }

        Product BusinessProduct(string id) => string.IsNullOrEmpty(id) || !game || game.Session == null ? null : game.Session.Economy.Inventory.Find(p => p.Id == id);
        static string BusinessTierName(int tier) => tier < 0 ? Strings.Get("business.unfinished") : Progression.SizeName(tier);
        static string BusinessDistance(double meters) => Strings.Format("business.laps", (Math.Floor(meters / ShopShift.LapMeters * 100) / 100).ToString("0.00"));

        public VisualElement TutorialProductTarget(string productId = null)
        {
            for (int i = 0; i < businessStockIds.Length; i++)
                if (BusinessProduct(businessStockIds[i]) != null && (string.IsNullOrEmpty(productId) || businessStockIds[i] == productId)) return businessStock[i];
            return null;
        }

        public VisualElement TutorialCustomerTarget(string customerId = null)
        {
            if (!game || game.Shift == null) return null;
            for (int i = 0; i < businessCustomers.Length; i++)
            {
                var customer = game.Shift.CustomerAt(i);
                if (customer != null && !customer.Angry && !customer.Happy &&
                    (string.IsNullOrEmpty(customerId) ? customer.Flavor == 0 : customer.Id == customerId)) return businessCustomers[i];
            }
            return null;
        }
    }
}
