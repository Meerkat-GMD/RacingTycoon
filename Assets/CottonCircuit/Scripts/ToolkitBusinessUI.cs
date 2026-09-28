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
            SetText("businessDayCaption", state.Closed ? "CLOSED · 내일 또 만나요" : "OPEN · 오늘 판매 " + state.DaySold + "개");
            Show(Q<VisualElement>("businessMachines"), progression);
            for (int i = 0; i < 3; i++)
            {
                bool owned = !progression ? i == 0 : i < Progression.OwnedMachines(economy);
                bool assigned = shift.HasWorker(i), selected = i == shift.SelectedMachine;
                var button = Q<Button>("businessMachine" + i);
                button.text = "M0" + (i + 1) + " · " + (!owned ? "잠김" : assigned ? shift.WorkerCanOperate(i) ? "알바 운전" : "알바 대기" : selected ? "직접 운전" : "대기");
                button.SetEnabled(allowed && owned && !game.TutorialActive);
                button.EnableInClassList("selected", selected);
                button.tooltip = assigned ? "배치한 알바가 운전과 제작을 맡아요." : "이 기계는 직접 운전해요.";
            }
            SetText("businessGrade", "설탕 " + shift.SugarGrade(shift.SelectedMachine) + "등급");
            SetText("businessMinimapTitle", progression ? "M0" + (shift.SelectedMachine + 1) + " / LIVE" : "SUGARWAY / LIVE");
            Q<Image>("businessMinimap").image = game.World.MinimapTexture;

            SetText("businessSugar", state.SugarGrams > .00001 ? Palette.FlavorName(state.SugarFlavor) + " 설탕  " + state.SugarGrams.ToString("0.0") + " / 100 g" : "설탕  0 / 100 g");
            Q<ProgressBar>("businessSugarMeter").value = (float)state.SugarGrams;
            int tier = ShopShift.SizeForDistance(state.BatchMeters);
            int maxSize = progression ? shift.MaxSize(shift.SelectedMachine) : 2;
            if (progression && state.BatchMeters > 0) maxSize = Math.Min(maxSize, Math.Max(0, state.BatchSugarGrade - 1));
            bool capped = tier >= maxSize, sized = !progression || Progression.MaxSugarGrade(economy) > 1;
            SetText("businessBatchStage", BusinessTierName(tier));
            SetText("businessBatchStars", state.BatchMeters > 0 ? ShopShift.StarText(state.BatchQuality) : "");
            SetText("businessBatchName", state.BatchMeters <= 0 ? "생산 준비" : Palette.FlavorName(state.BatchFlavor) + (tier < 0 ? " · 판매 불가" : ""));
            double next = ShopShift.MetersForSize(Math.Min(maxSize, tier + 1));
            double previous = tier < 0 ? 0 : ShopShift.MetersForSize(tier);
            string nextText = capped ? (sized ? "최대 크기" : "완성") + " · F로 꺼내기" :
                (sized ? BusinessTierName(tier + 1) : "완성") + "까지 " + (Math.Ceiling(Math.Max(0, next - state.BatchMeters) / ShopShift.LapMeters * 100) / 100).ToString("0.00") + " 바퀴";
            if (worker) nextText = economy.Inventory.Count >= economy.StockCapacity ? "진열대가 가득 차 대기" : game.WorkerDriving ? "알바가 만들고 꺼내요" : "알바 대기";
            SetText("businessNextSize", nextText);
            Q<ProgressBar>("businessGrowthMeter").value = capped ? 100 : Mathf.Clamp01((float)((state.BatchMeters - previous) / Math.Max(.001, next - previous))) * 100;
            var batchArt = Q<VisualElement>("businessBatchArt");
            SetArt(batchArt, UiArt.CottonCandy(Math.Max(0, state.BatchFlavor), Math.Max(0, tier)));
            batchArt.EnableInClassList("biz-faint", state.BatchMeters <= 0);
            SetText("businessExtractLabel", worker ? "알바가 꺼내요" : "꺼내기");
            Show(Q<VisualElement>("businessExtractKey"), !worker);
            Q<Button>("businessExtract").SetEnabled(manual && state.BatchMeters > 0 && economy.Inventory.Count < economy.StockCapacity);
            SetText("businessEmptySugarLabel", worker ? "알바가 설탕 관리" : "설탕 비우기");
            Show(Q<VisualElement>("businessEmptySugarKey"), !worker);
            Q<Button>("businessEmptySugar").SetEnabled(manual && state.SugarGrams > 0);
            for (int i = 0; i < 3; i++)
            {
                bool available = !progression || shift.CanMakeFlavor(shift.SelectedMachine, i);
                businessSugarBags[i].SetEnabled(manual && available);
                int cost = available && progression ? shift.PourCost(shift.SelectedMachine, i) : 0;
                SetText("sugarCost" + i, !available ? shift.MachineMakesFlavor(shift.SelectedMachine, i) ? "튜토리얼 중 잠김" : "이 기계에서는 잠김"
                    : worker ? "알바 담당" : cost > 0 ? cost + " C / 10g" : "무료");
                businessSugarBags[i].tooltip = "주행 화면으로 끌어 위아래로 흔들어 주세요.\n작게 흔들면 조금, 크게 흔들면 많이 들어가요.";
            }
            RefreshBusinessCustomers(allowed);
            RefreshBusinessShelf(allowed);
            RefreshBusinessDriving(worker);
            Show(Q<VisualElement>("businessResults"), state.Closed);
            SetText("businessResultTitle", "DAY " + economy.Day.ToString("00") + "  영업 마감");
            SetText("businessResultCaption", progression ? Progression.LocationName(economy.Progression.SelectedLocation) : "오늘도 달콤한 하루였어요");
            SetText("businessResultStats", "판매 " + state.DaySold + "개   ·   매출 " + state.DayRevenue.ToString("N0") + " C\n\n재료비 " + state.DayMaterialCost.ToString("N0") + " C   ·   순이익 " + (state.DayRevenue - state.DayMaterialCost).ToString("N0") + " C\n\n오배송 " + state.DayWrong + "   ·   시간 초과 " + state.DayMissed + "   ·   폐기 " + state.DayTrashed);
            Q<Button>("businessNextDay").text = progression ? "가게 정비하기" : "다음 날 시작하기";
        }

        void RefreshBusinessDriving(bool worker)
        {
            var kart = game.World.Kart; var drive = kart.DriveModel;
            bool downhill = game.RunStyle == DrivingStyle.Downhill;
            SetText("businessSpeed", Mathf.RoundToInt(kart.Speed * 3.6f) + " km/h");
            double speedYield = ShopShift.SpeedYield(kart.Speed);
            SetText("businessSpeedYield", worker ? game.WorkerDriving ? "알바 운전 중" : "알바 대기" : speedYield <= 0 ? "예열 중 · 속도를 올려요" : "감기 속도 ×" + speedYield.ToString("0.0"));
            Show(Q<VisualElement>("businessBoostCard"), !downhill && !worker);
            SetText("businessBoost", kart.Boosting ? "BOOST · 달콤한 가속" : "BOOSTER  " + drive.StoredBoosts + " / 2");
            var meter = Q<ProgressBar>("businessBoostMeter");
            meter.value = 100 * (kart.Boosting ? Mathf.Clamp01((float)(drive.BoostRemaining / Math.Max(.01, drive.BoostDuration))) : kart.Charge);
            meter.EnableInClassList("boosting", kart.Boosting);
            bool impact = kart.ImpactFlash > 0;
            Show(Q<Label>("businessRaceEvent"), impact);
            SetText("businessRaceEvent", impact ? "벽 충돌!  " + (game.Shift.State.BatchMeters > 0 ? ShopShift.StarText(game.Shift.State.BatchQuality) : "") + (worker ? "" : " · R로 코스 복귀") : "");
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
                SetText("customerOrderText" + i, reacting ? customer.Happy ? "고마워요!" : customer.TimedOut ? "오래 걸려요!" : "주문이 달라요!" : Palette.FlavorName(customer.Flavor) + (sized ? " · " + BusinessTierName(customer.Size) : ""));
                var patience = Q<ProgressBar>("customerPatience" + i);
                Show(patience, !reacting);
                float fraction = Mathf.Clamp01((float)(customer.PatienceRemaining / game.Shift.PatienceLimit));
                patience.value = fraction * 100;
                patience.EnableInClassList("warning", fraction < .25f);
                businessCustomers[i].tooltip = reacting ? "" : "주문에 맞는 솜사탕을 끌어다 주세요.";
            }
        }

        void RefreshBusinessShelf(bool allowed)
        {
            var economy = game.Session.Economy;
            int capacity = Math.Min(economy.StockCapacity, businessStock.Length);
            SetText("businessShelfCount", "내 진열대  " + economy.Inventory.Count + " / " + economy.StockCapacity);
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
                if (product == null) { SetText("stockGrade" + i, ""); businessStock[i].tooltip = "빈 진열 공간"; continue; }
                int tier = ShopShift.SizeOf(product);
                SetArt(art, UiArt.BaggedCandy(product.FlavorIndex, Math.Max(0, tier)));
                SetText("stockGrade" + i, tier < 0 ? "미완성" : (sized ? BusinessTierName(tier) + " " : "") + ShopShift.StarText(product.Quality));
                businessStock[i].tooltip = Palette.FlavorName(product.FlavorIndex) + " · " + BusinessDistance(product.DistanceMeters) + "\n" +
                    (tier < 0 ? "주행 화면에서 더 키우세요." : economy.Price(product).ToString("N0") + " C · 손님에게 끌어다 주세요.");
            }
        }

        Product BusinessProduct(string id) => string.IsNullOrEmpty(id) || !game || game.Session == null ? null : game.Session.Economy.Inventory.Find(p => p.Id == id);
        static string BusinessTierName(int tier) => tier < 0 ? "미완성" : tier == 0 ? "소" : tier == 1 ? "중" : "대";
        static string BusinessDistance(double meters) => (Math.Floor(meters / ShopShift.LapMeters * 100) / 100).ToString("0.00") + " 바퀴";

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
