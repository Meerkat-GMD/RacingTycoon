using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CottonCircuit
{
    public partial class GameUI
    {
        UnityEngine.UI.Text shiftSugar, shiftBatch, shiftTier, shiftNextTier;
        UnityEngine.UI.Text shiftDayCaption, shiftResultStats, shiftResultTitle, shiftGhostCaption, shiftPourHint;
        UnityEngine.UI.Image shiftSugarFill, shiftGrowthFill, shiftTrashCard, shiftRaceHighlight, shiftRaceResumeSurface;
        UnityEngine.UI.Button shiftExtractButton, shiftEmptySugarButton;
        ShopArtGraphic shiftBatchArt, shiftGhostArt;
        ShopDropTarget shiftTrashTarget;
        readonly ShopDragItem[] shiftSugarBags = new ShopDragItem[3];
        RectTransform shiftGhost;
        ShopDragItem shiftDrag;
        readonly SugarShake shiftShake = new SugarShake();
        float shiftPourFlashUntil;
        readonly UnityEngine.UI.Button[] shiftMachineButtons = new UnityEngine.UI.Button[3];
        readonly UnityEngine.UI.Text[] shiftSugarCosts = new UnityEngine.UI.Text[3];
        readonly UnityEngine.UI.Text[] shiftSugarNames = new UnityEngine.UI.Text[3];
        UnityEngine.UI.Button shiftGradeButton;
        UnityEngine.UI.Text shiftSizeLimits, shiftResultCaption, speedYieldLabel;

        public bool ShiftInteractionsAllowed => game && game.Shift != null && game.Shift.IsOpen && !game.Shift.Paused && !game.Session.Paused && game.Store != null && game.Store.CanSave;
        public ShopDragItem ActiveShiftDrag => shiftDrag;
        public RectTransform ShiftDragGhost => shiftGhost;
        public RectTransform ShiftComposition => root;

        void BuildShiftChrome(RectTransform p)
        {
            Box(p, "Shop header backdrop", 960, 0, 640, 82, Palette.Cream, false);
            Box(p, "Shop backdrop", 960, 82, 640, 818, Palette.Cream, false);
            Box(p, "Race header", 0, 0, 960, 92, Palette.Cream, false);
            Box(p, "Divider", 960, 0, 2, 900, Palette.Hex("DDD4C4"), false);
            Label(p, "솜사탕 서킷", 24, 14, 285, 38, 28, Palette.Ink, FontStyle.Bold);
            if (!game.HasProgression) Label(p, "설탕을 흔들고, 달리며 키우는 달콤한 하루", 26, 56, 560, 23, 15, Palette.Muted);
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    int machineIndex = i;
                    var button = ButtonAt(p, "M0" + (i + 1), 24 + i * 170, 57, 158, 27, Color.white, Palette.Ink, () => { CancelShiftDrag(); game.ChooseMachine(machineIndex); }, 12);
                    shiftMachineButtons[i] = button; button.name = "SwitchMachine_" + i;
                    HoverHint.Attach(button.gameObject, () => MachineAvailability(machineIndex), true);
                }
                shiftGradeButton = ButtonAt(p, "설탕 1등급", 534, 57, 107, 27, Palette.Soda, Palette.Ink, () => { }, 12); shiftGradeButton.name = "BusinessSugarGrade";
                shiftGradeButton.interactable = false;
                HoverHint.Attach(shiftGradeButton.gameObject, () => SugarGradeDetails(game.Shift.SelectedMachine), true);
            }
            day = Label(p, "DAY 01", 302, 25, 115, 25, 14, Palette.Muted, FontStyle.Bold);
            autoDriveButton = ButtonAt(p, "자동 주행 ON", 652, 22, 166, 46, Palette.Soda, Palette.Ink, () => game.ToggleAutoDrive(), 17);
            autoDriveButton.name = "Auto drive toggle";
            HoverHint.Attach(autoDriveButton.gameObject, () => "자동 / 수동 운전 전환\nW 가속 · A / D 조향 · S 제동\nSpace 드리프트" + (game.RunStyle == DrivingStyle.Kart ? " · Shift 부스터" : " 감속") + " · R 복귀");
            ButtonAt(p, "도움말", 832, 22, 104, 46, Color.white, Palette.Ink, () => game.TogglePause(), 16);
            Label(p, "우리 솜사탕 가게", 982, 17, 226, 31, 22, Palette.Ink, FontStyle.Bold);
            shiftDayCaption = Label(p, "OPEN  ·  오늘도 달콤하게", 984, 53, 224, 20, 12, Palette.Muted);
            var clock = Box(p, "Shift clock", 1215, 12, 168, 61, Palette.Yellow);
            Label(clock.rectTransform, "남은 영업시간", 10, 5, 148, 17, 11, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            timer = Label(clock.rectTransform, "10:00", 8, 22, 152, 37, 29, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            var wallet = Box(p, "Wallet", 1397, 12, 179, 61, Palette.Ink);
            coins = Label(wallet.rectTransform, "80", 12, 8, 117, 46, 27, Color.white, FontStyle.Bold, TextAnchor.MiddleRight);
            Label(wallet.rectTransform, "코인", 137, 23, 35, 23, 12, Palette.Yellow, FontStyle.Bold);
        }

        void BuildShiftShop()
        {
            var p = shop.GetComponent<RectTransform>();
            BuildShopStreet(p);

            var shelf = Box(p, "Cotton candy shelf", 980, 420, 596, 291, Color.white);
            BuildCandyRackShelf(shelf.rectTransform);
            shiftTrashCard = Box(shelf.rectTransform, "TrashDropTarget", 468, 105, 112, 131, Palette.Hex("EFF3EE"));
            shiftTrashTarget = shiftTrashCard.gameObject.AddComponent<ShopDropTarget>(); shiftTrashTarget.Owner = this; shiftTrashTarget.Kind = ShopDropKind.Trash;
            ShiftArt(shiftTrashCard.rectTransform, "Trash bin", 22, 5, 68, 82, ShopArtKind.Trash);
            Label(shiftTrashCard.rectTransform, "쓰레기통", 4, 87, 104, 23, 14, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (!game.HasProgression)
            {
                Label(shiftTrashCard.rectTransform, "끌어서 버리기", 2, 111, 108, 17, 11, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
                Label(shelf.rectTransform, "1바퀴 미만은\n판매할 수 없어요", 468, 245, 112, 36, 11, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            }
            HoverHint.Attach(shiftTrashCard.gameObject, "제품을 끌어 놓으면 버려요.\n사용한 설탕은 환급되지 않아요.");

            Label(p, "맛별 설탕", 986, 729, 180, 27, 18, Palette.Ink, FontStyle.Bold);
            if (!game.HasProgression) Label(p, "봉지를 왼쪽으로 끌고 위아래로 흔들어요", 1141, 733, 433, 23, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleRight);
            for (int i = 0; i < shiftSugarBags.Length; i++)
            {
                var bag = Box(p, "SugarBag" + i, 980 + i * 202, 769, 192, 115, Color.Lerp(Palette.Flavor(i), Color.white, .74f));
                var item = bag.gameObject.AddComponent<ShopDragItem>(); item.Owner = this; item.Kind = ShopDragKind.Sugar; item.FlavorIndex = i;
                shiftSugarBags[i] = item;
                ShiftArt(bag.rectTransform, "Sugar bag illustration", 6, 4, 82, 100, ShopArtKind.SugarBag, i);
                shiftSugarNames[i] = Label(bag.rectTransform, Palette.FlavorName(i), 88, 20, 99, 30, 21, Palette.Ink, FontStyle.Bold);
                Label(bag.rectTransform, "설탕", 90, 51, 96, 22, 14, Palette.Ink);
                shiftSugarCosts[i] = Label(bag.rectTransform, "무료 · 무제한", 90, 82, 98, 19, 11, Palette.Muted);
                int flavorIndex = i;
                HoverHint.Attach(bag.gameObject, () => SugarBagDetails(flavorIndex));
            }
        }

        void BuildShiftRace()
        {
            var p = race.GetComponent<RectTransform>();
            shiftRaceHighlight = Box(p, "Sugar shake race area", 3, 94, 954, 802, new Color(.48f, .80f, .80f, .10f), false);
            shiftRaceHighlight.gameObject.SetActive(false);
            var edge = Palette.Soda; edge.a = .8f;
            Box(shiftRaceHighlight.rectTransform, "Top shake border", 0, 0, 954, 3, edge, false);
            Box(shiftRaceHighlight.rectTransform, "Bottom shake border", 0, 799, 954, 3, edge, false);
            Box(shiftRaceHighlight.rectTransform, "Left shake border", 0, 0, 3, 802, edge, false);
            Box(shiftRaceHighlight.rectTransform, "Right shake border", 951, 0, 3, 802, edge, false);
            shiftPourHint = Label(shiftRaceHighlight.rectTransform, "↕", 176, 573, 602, 38, 20, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            speedLines = Rect(p, "Speed streaks", 0, 92, 960, 808).gameObject.AddComponent<SpeedLinesGraphic>(); speedLines.raycastTarget = false;
            var mapCard = Box(p, "Course map", 20, 111, 177, 169, new Color(1, .97f, .90f, .92f));
            mapTitle = Label(mapCard.rectTransform, "SUGARWAY", 12, 9, 153, 22, 12, Palette.Ink, FontStyle.Bold);
            var mapRect = Rect(mapCard.rectTransform, "Live course", 10, 39, 157, 120); mapRect.pivot = Vector2.zero; mapRect.anchoredPosition = new Vector2(10, -159);
            map = mapRect.gameObject.AddComponent<RaceMapGraphic>(); map.raycastTarget = false; map.Kart = game.World.Kart;

            var machine = Box(p, "Sugar and batch HUD", 213, 111, 368, 144, new Color(1, .97f, .90f, .94f));
            shiftSugar = Label(machine.rectTransform, "설탕을 먼저 넣어주세요", 14, 10, 340, 25, 15, Palette.Ink, FontStyle.Bold);
            shiftSugarFill = Progress(machine.rectTransform, 14, 40, 340, 5, Palette.Soda);
            shiftBatchArt = ShiftArt(machine.rectTransform, "Growing candy", 9, 51, 77, 88, ShopArtKind.CottonCandy);
            shiftBatch = Label(machine.rectTransform, "0.00 바퀴", 94, 52, 172, 33, 26, Palette.Ink, FontStyle.Bold);
            shiftTier = Label(machine.rectTransform, "생산 준비", 96, 86, 252, 21, 13, Palette.Muted);
            shiftNextTier = Label(machine.rectTransform, "소까지 1.00 바퀴", 96, 111, 252, 20, 12, Palette.Muted);
            shiftGrowthFill = Progress(machine.rectTransform, 96, 135, 258, 4, Palette.Pink);
            shiftExtractButton = ButtonAt(p, "F  꺼내기", 593, 111, 121, 42, Palette.Ink, Color.white, () => game.ExtractCandy(), 15);
            shiftExtractButton.name = "Extract candy";
            shiftEmptySugarButton = ButtonAt(p, "설탕 비우기 · 우클릭", 726, 111, 201, 42, Palette.Yellow, Palette.Ink, () => game.EmptySugar(), 14);
            shiftEmptySugarButton.name = "Empty sugar";
            shiftSizeLimits = Label(p, "소 1바퀴 · 중 1.5바퀴 · 대 2바퀴", 594, 164, 333, 23, 12, Palette.Ink, FontStyle.Bold);
            if (!game.HasProgression) Label(p, "설탕 50g / 바퀴", 594, 190, 333, 20, 12, Palette.Muted);
            machine.raycastTarget = true;
            HoverHint.Attach(machine.gameObject, () => game.HasProgression ? "설탕 봉지를 주행 화면으로 끌고 위아래로 흔들어요.\n같은 맛으로 성장하며, 최대 크기에서는 설탕을 쓰지 않아요.\nF로 꺼내어 진열대에 보관해요." : "설탕 50g / 바퀴.\n같은 맛 설탕을 넣고 달리면 자라요.");
            HoverHint.Attach(shiftExtractButton.gameObject, "F로 현재 제품을 꺼내요.\n1바퀴 이상 감아야 판매할 수 있어요.", true);
            HoverHint.Attach(shiftEmptySugarButton.gameObject, "기계의 남은 설탕을 비워요.\n환급되지 않아요.", true);

            var speedCard = Box(p, "Speedometer", 20, 746, 220, 91, Palette.Ink);
            speedYieldLabel = Label(speedCard.rectTransform, "LIVE RACING", 14, 9, 192, 22, 12, Palette.Soda, FontStyle.Bold);
            speed = Label(speedCard.rectTransform, "0 km/h", 12, 34, 196, 48, 32, Color.white, FontStyle.Bold);
            var driftCard = Box(p, "Drift meter", 258, 778, 444, 59, Palette.Ink);
            driftLabel = Label(driftCard.rectTransform, "", 12, 6, 420, 29, 14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            driftFill = Progress(driftCard.rectTransform, 14, 41, 416, 7, Palette.Soda);
            raceEvent = Label(p, "", 220, 345, 520, 46, 22, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            var controls = Box(p, "Driving controls", 20, 851, 920, 31, new Color(1, .97f, .9f, .94f));
            raceControls = Label(controls.rectTransform, "", 10, 0, 900, 31, 13, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            // Active only while carrying a product, above HUDs so the entire race is a drop area.
            shiftRaceResumeSurface = Box(root, "RaceResumeDropTarget", 0, 92, 960, 808, Color.clear);
            shiftRaceResumeSurface.raycastTarget = true;
            var resumeTarget = shiftRaceResumeSurface.gameObject.AddComponent<ShopDropTarget>();
            resumeTarget.Owner = this; resumeTarget.Kind = ShopDropKind.Race;
            shiftRaceResumeSurface.gameObject.SetActive(false);
        }

        void BuildShiftResults()
        {
            var p = result.GetComponent<RectTransform>();
            var dimmer = Box(p, "Day closed modal", 0, 0, 1600, 900, new Color(.10f, .12f, .19f, .62f), false); dimmer.raycastTarget = true;
            var card = Box(p, "Day summary", 480, 207, 640, 486, Palette.Cream);
            Label(card.rectTransform, "C O T T O N   C I R C U I T", 40, 28, 560, 25, 12, Palette.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            shiftResultTitle = Label(card.rectTransform, "오늘도 수고했어요!", 40, 69, 560, 55, 34, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            shiftResultCaption = Label(card.rectTransform, "10분의 달콤한 영업을 마쳤어요", 40, 132, 560, 28, 17, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            var receipt = Box(card.rectTransform, "Daily receipt", 40, 184, 560, 129, Color.white);
            shiftResultStats = Label(receipt.rectTransform, "", 24, 17, 512, 100, 21, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (!game.HasProgression)
            {
                Label(card.rectTransform, "다음 날에는 진열대 · 설탕 · 제작 중 솜사탕을 모두 비워요", 35, 325, 570, 28, 14, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
                Label(card.rectTransform, "새 손님부터 시작 · 코인과 업그레이드는 유지", 35, 353, 570, 24, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            }
            var next = ButtonAt(card.rectTransform, game.HasProgression ? "가게 정비하기" : "다음 날 시작하기  →", 40, 388, 560, 60, Palette.Pink, Palette.Ink, () => { if (game.HasProgression) game.ReturnToPreparation(); else game.StartNextDay(); }, 22);
            next.name = game.HasProgression ? "ReturnToPreparation" : "Start next shop day";
            HoverHint.Attach(next.gameObject, "성장 지도에서 가게를 확장하고 다음 영업을 준비해요.\n새 영업을 시작하면 재고와 제작 상태가 초기화돼요.");
        }

        void RefreshShift()
        {
            var shift = game.Shift; var state = shift.State; var economy = game.Session.Economy;
            bool allowed = ShiftInteractionsAllowed;
            if (state.Closed && !game.Session.Paused) HoverHint.HideAll();
            if (!allowed || (shiftDrag && shiftDrag.Kind == ShopDragKind.Product && !economy.Inventory.Exists(item => item.Id == shiftDrag.CapturedProductId))) CancelShiftDrag();
            common.SetActive(true); shop.SetActive(true); race.SetActive(true); result.SetActive(state.Closed);
            pause.SetActive(game.Session.Paused); toast.SetActive((!state.Closed || game.Session.Paused) && !string.IsNullOrEmpty(game.Notice)); notice.text = game.Notice ?? "";
            if (game.Session.Paused) toast.transform.SetAsLastSibling();
            toast.GetComponent<RectTransform>().anchoredPosition = game.Session.Paused ? new Vector2(470, -780) : new Vector2(150, -297);
            coins.text = economy.Coins.ToString("N0"); day.text = "DAY " + economy.Day.ToString("00");
            int remaining = Math.Max(0, (int)Math.Ceiling(state.RemainingSeconds)); timer.text = (remaining / 60).ToString("00") + ":" + (remaining % 60).ToString("00");
            shiftDayCaption.text = state.Closed ? "CLOSED  ·  내일 또 만나요" : "OPEN  ·  오늘 판매 " + state.DaySold + "개";
            muteLabel.text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            autoDriveButton.GetComponentInChildren<UnityEngine.UI.Text>().text = game.AutoDrive ? "자동 주행 ON" : "수동 운전 중";
            autoDriveButton.image.color = game.AutoDrive ? Palette.Soda : Palette.Yellow; autoDriveButton.interactable = allowed;

            bool progression = game.HasProgression, sizedProducts = !progression || Progression.MaxSugarGrade(economy) > 1;
            int selectedMachine = shift.SelectedMachine;
            int maxSize = progression ? shift.MaxSize(selectedMachine) : 2;
            if (progression && state.BatchMeters > 0) maxSize = Math.Min(maxSize, Math.Max(0, state.BatchSugarGrade - 1));
            int batchTier = ShopShift.SizeForDistance(state.BatchMeters);
            bool capped = batchTier >= maxSize;
            shiftSugar.text = state.SugarGrams > .00001 ? "설탕 · " + Palette.FlavorName(state.SugarFlavor) + "  " + state.SugarGrams.ToString("0.0") + " / 100 g" : progression ? "설탕  0 / 100 g" : "설탕이 없어요 · 봉지를 흔들어 주세요";
            shiftSugarFill.rectTransform.sizeDelta = new Vector2(340 * Mathf.Clamp01((float)(state.SugarGrams / 100)), 5);
            shiftSugarFill.color = Time.unscaledTime < shiftPourFlashUntil ? Palette.Yellow : state.SugarFlavor < 0 ? Palette.Soda : Palette.Flavor(state.SugarFlavor);
            shiftBatch.text = ShiftDistanceLabel(state.BatchMeters);
            shiftTier.text = state.BatchMeters <= 0 ? "생산 준비" : Palette.FlavorName(state.BatchFlavor) + (sizedProducts ? " · " + ShiftTierName(batchTier) : batchTier < 0 ? " · 미완성" : "") + (batchTier < 0 ? " (판매 불가)" : "");
            double nextMeters = ShopShift.MetersForSize(Math.Min(maxSize, batchTier + 1));
            double previousMeters = batchTier < 0 ? 0 : ShopShift.MetersForSize(batchTier);
            shiftNextTier.text = capped ? (sizedProducts ? "최대 크기  ·  F 꺼내기" : "완성  ·  F 꺼내기") : (sizedProducts && batchTier >= 0 ? ShiftTierName(batchTier) + " 꺼내기 가능 · " : "") + (sizedProducts ? ShiftTierName(batchTier + 1) : "완성") + "까지 " + (Math.Ceiling(Math.Max(0, nextMeters - state.BatchMeters) / ShopShift.LapMeters * 100) / 100).ToString("0.00") + " 바퀴";
            if (!capped && state.BatchMeters > 0 && (state.SugarGrams <= 0 || state.SugarFlavor != state.BatchFlavor)) shiftNextTier.text = Palette.FlavorName(state.BatchFlavor) + " 설탕 대기";
            shiftGrowthFill.rectTransform.sizeDelta = new Vector2(258 * (capped ? 1 : Mathf.Clamp01((float)((state.BatchMeters - previousMeters) / Math.Max(.001, nextMeters - previousMeters)))), 4);
            shiftSizeLimits.text = !progression ? "소 1바퀴 · 중 1.5바퀴 · 대 2바퀴" : sizedProducts ? shift.SizeLimitNote(selectedMachine) + "  /  " + (ShopShift.MetersForSize(maxSize) / ShopShift.LapMeters).ToString("0.##") + " 바퀴" : "";
            shiftBatchArt.Configure(ShopArtKind.CottonCandy, Math.Max(0, state.BatchFlavor), batchTier); shiftBatchArt.SetDistance(state.BatchMeters); shiftBatchArt.color = state.BatchMeters > 0 ? Color.white : new Color(1, 1, 1, .18f);
            shiftExtractButton.interactable = allowed && state.BatchMeters > 0 && economy.Inventory.Count < economy.StockCapacity;
            shiftEmptySugarButton.interactable = allowed && state.SugarGrams > 0;
            RefreshShiftCustomer(allowed);
            RefreshShiftShelf(allowed);
            for (int i = 0; i < shiftSugarBags.Length; i++)
            {
                var bag = shiftSugarBags[i]; bool available = !progression || shift.CanMakeFlavor(selectedMachine, i);
                var image = bag.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = allowed;
                image.color = available ? Color.Lerp(Palette.Flavor(i), Color.white, .74f) : Palette.Hex("E5E5DE");
                shiftSugarNames[i].color = available ? Palette.Ink : Palette.Muted;
                int cost = progression && available ? shift.PourCost(selectedMachine, i) : 0;
                shiftSugarCosts[i].text = !available ? "이 기계에서는 잠김" : cost <= 0 ? "무료" : cost + " C / 10g";
                shiftSugarCosts[i].color = available && cost > economy.Coins ? Palette.Hex("AD6355") : Palette.Muted;
            }
            if (progression) RefreshBusinessMachines(allowed);
            shiftTrashCard.raycastTarget = allowed;
            shiftTrashCard.color = shiftTrashTarget.IsHovered ? Palette.Hex("F5DDD6") : Palette.Hex("EFF3EE");
            shiftResultTitle.text = "DAY " + economy.Day.ToString("00") + "  영업 마감";
            shiftResultCaption.text = progression ? Progression.LocationName(economy.Progression.SelectedLocation) : "10분의 달콤한 영업을 마쳤어요";
            shiftResultStats.fontSize = progression ? 18 : 21;
            shiftResultStats.text = progression ? "판매 " + state.DaySold + "개     매출 " + state.DayRevenue.ToString("N0") + " C\n재료비 " + state.DayMaterialCost.ToString("N0") + " C     순이익 " + (state.DayRevenue - state.DayMaterialCost).ToString("N0") + " C\n오배송 " + state.DayWrong + " · 시간 초과 " + state.DayMissed + " · 폐기 " + state.DayTrashed
                : "판매  " + state.DaySold + "개       매출  " + state.DayRevenue.ToString("N0") + " 코인\n\n오배송 " + state.DayWrong + " · 시간 초과 " + state.DayMissed + " · 폐기 " + state.DayTrashed;

            var kart = game.World.Kart; var drive = kart.DriveModel;
            bool downhill = game.RunStyle == DrivingStyle.Downhill;
            speedLines.SetDriving(kart, shift.IsOpen && !shift.Paused);
            speed.text = Mathf.RoundToInt(kart.Speed * 3.6f) + " km/h";
            double speedYield = ShopShift.SpeedYield(kart.Speed);
            speedYieldLabel.text = speedYield <= 0 ? "예열 중 · 속도를 올려요" : "감기 속도 ×" + speedYield.ToString("0.0");
            speedYieldLabel.color = speedYield <= 0 ? new Color(1, 1, 1, .6f) : speedYield >= 1 ? Palette.Yellow : Palette.Soda; mapTitle.text = progression ? "M0" + (selectedMachine + 1) + "  /  LIVE" : downhill ? "이니셜D  /  LIVE" : "SUGARWAY  /  LIVE";
            float boostFraction = Mathf.Clamp01((float)(drive.BoostRemaining / Math.Max(.01, drive.BoostDuration)));
            driftFill.rectTransform.sizeDelta = new Vector2(416 * (kart.Boosting ? boostFraction : kart.Charge), 7);
            driftFill.color = kart.Boosting ? Palette.Yellow : Palette.Soda;
            driftLabel.text = game.AutoDrive ? "AUTO PILOT  ·  달리는 동안 가게를 돌봐요" : kart.Boosting ? "BOOST!  달콤한 가속 중" : "Space 드리프트  ·  Shift 부스터  [ " + drive.StoredBoosts + " / 2 ]";
            if (downhill) driftLabel.text = kart.Drifting ? "드리프트 감속 중 · 코너 출구에서 다시 가속" : "이니셜D · W 가속 · Space 드리프트 감속";
            if (progression && !downhill) driftLabel.text = kart.Boosting ? "BOOST" : "BOOSTER  " + drive.StoredBoosts + " / 2";
            driftLabel.transform.parent.gameObject.SetActive(!downhill && !game.AutoDrive);
            raceEvent.text = kart.ImpactFlash > 0 ? "벽 충돌!  R로 코스 복귀" : "";
            raceControls.text = game.AutoDrive ? "설탕 봉지를 흔들어 넣기   →   거리만큼 성장   →   F로 꺼내기   →   손님에게 드래그" : "W 가속   A / D 조향   S 제동   Space 드리프트   Shift 부스터   F 꺼내기   R 복귀   Esc 일시정지";
            if (downhill && !game.AutoDrive) raceControls.text = "W 유지 → 계속 가속   A / D 조향   S 제동   Space 드리프트 감속   F 꺼내기   R 복귀   Esc 일시정지";
            if (progression) raceControls.text = "W A S D    ·    Space 드리프트" + (downhill ? "" : "    ·    Shift 부스터") + "    ·    F 꺼내기    ·    Esc";
            raceControls.transform.parent.gameObject.SetActive(!game.AutoDrive);
            map.SetVerticesDirty();
        }

        void RefreshShiftCustomer(bool allowed)
        {
            RefreshShopStreet(allowed);
        }

        void RefreshBusinessMachines(bool allowed)
        {
            var economy = game.Session.Economy; int selected = game.Shift.SelectedMachine;
            for (int i = 0; i < shiftMachineButtons.Length; i++)
            {
                var button = shiftMachineButtons[i]; if (!button) continue;
                bool owned = i < Progression.OwnedMachines(economy); var slot = game.Machine(i);
                button.interactable = allowed && owned; button.image.color = i == selected ? Palette.Soda : Color.white;
                bool worker = slot != null && slot.WorkerAssigned;
                string status = !owned ? "잠김" : i == selected ? (worker ? "운전 · 알바 쉼" : "운전") : worker ? "알바" : "대기";
                button.GetComponentInChildren<UnityEngine.UI.Text>().text = "M0" + (i + 1) + "  ·  " + status;
            }
            if (shiftGradeButton)
            {
                shiftGradeButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "설탕 " + game.Shift.SugarGrade(selected) + "등급";
                shiftGradeButton.interactable = false;
            }
        }

        string SugarGradeDetails(int machine)
        {
            var economy = game.Session.Economy;
            string note = game.Shift.SizeLimitNote(machine);
            return "해금한 최고 등급 설탕을 기계가 쓸 수 있는 만큼 자동으로 써요.\n" +
                Progression.MachineName(machine) + "는 " + Progression.MachineTier(machine) + "등급 설탕까지 쓸 수 있어요." +
                (note.Length > 0 ? "\n" + note : "") +
                (Progression.MaxSugarGrade(economy) < 3 ? "\n성장 지도에서 더 좋은 설탕을 해금할 수 있어요." : "");
        }

        string SugarBagDetails(int flavorIndex)
        {
            if (!game.HasProgression) return "주행 화면으로 끌고 위아래로 흔들어요.\n작게 흔들면 조금, 크게 흔들면 많이 들어가요.";
            var economy = game.Session.Economy; int selected = game.Shift.SelectedMachine;
            if (!Progression.HasFlavor(economy, flavorIndex)) return "성장 지도에서 " + Palette.FlavorName(flavorIndex) + " 맛을 해금하세요.";
            if (!game.Shift.CanMakeFlavor(selected, flavorIndex)) return Palette.FlavorName(flavorIndex) + " 맛에는 " + Progression.FlavorMachineTier(flavorIndex) + "등급 기계가 필요해요.";
            int cost = game.Shift.PourCost(selected, flavorIndex);
            return Palette.FlavorName(flavorIndex) + " 설탕  /  " + game.Shift.SugarGrade(selected) + "등급\n" + (cost == 0 ? "무료" : cost + " C / 10g") + "\n작게 흔들면 조금, 크게 흔들면 많이 들어가요.\n실제 투입량의 재료비를 코인 단위로 올림해요.";
        }

        static string ShiftTierName(int tier) => tier < 0 ? "미완성" : tier == 0 ? "소" : tier == 1 ? "중" : "대";
        static string ShiftTierRange(int tier) => tier == 0 ? "1바퀴 이상 1.5바퀴 미만" : tier == 1 ? "1.5바퀴 이상 2바퀴 미만" : "2바퀴 이상";
        static string ShiftDistanceLabel(double meters) => (Math.Floor(meters / ShopShift.LapMeters * 100) / 100).ToString("0.00") + " 바퀴";

        ShopArtGraphic ShiftArt(RectTransform parent, string name, float x, float y, float width, float height, ShopArtKind kind, int flavorIndex = 0)
        {
            var graphic = Rect(parent, name, x, y, width, height).gameObject.AddComponent<ShopArtGraphic>();
            graphic.raycastTarget = false; graphic.Configure(kind, flavorIndex); return graphic;
        }

        public bool BeginShiftDrag(ShopDragItem item, PointerEventData data)
        {
            if (!ShiftInteractionsAllowed || item.Owner != this) return false;
            if (game.HasProgression && item.Kind == ShopDragKind.Sugar && !game.Shift.CanMakeFlavor(game.Shift.SelectedMachine, item.FlavorIndex)) return false;
            HoverHint.HideAll();
            if (item.Kind == ShopDragKind.Product && !game.Session.Economy.Inventory.Exists(p => p.Id == item.CapturedProductId)) return false;
            CancelShiftDrag(); shiftDrag = item; shiftShake.Reset();
            if (!shiftGhost)
            {
                shiftGhost = Rect(root, "Shop drag ghost", 0, 0, 126, 151);
                var group = shiftGhost.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
                shiftGhostArt = ShiftArt(shiftGhost, "Dragged item illustration", 4, 0, 118, 124, ShopArtKind.CottonCandy);
                var caption = Box(shiftGhost, "Dragged item caption", 0, 124, 126, 25, Palette.Ink);
                shiftGhostCaption = Label(caption.rectTransform, "", 3, 1, 120, 23, 12, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            int tier = 0;
            if (item.Kind == ShopDragKind.Product)
            {
                var product = game.Session.Economy.Inventory.Find(p => p.Id == item.CapturedProductId);
                tier = ShopShift.SizeOf(product); shiftGhostArt.SetDistance(product.DistanceMeters);
            }
            shiftGhostArt.Configure(item.Kind == ShopDragKind.Sugar ? ShopArtKind.SugarBag : ShopArtKind.BaggedCottonCandy, item.FlavorIndex, tier);
            shiftGhostCaption.text = Palette.FlavorName(item.FlavorIndex) + (item.Kind == ShopDragKind.Sugar ? " 설탕" : game.HasProgression && Progression.MaxSugarGrade(game.Session.Economy) == 1 && tier >= 0 ? "" : " · " + ShiftTierName(tier));
            shiftRaceResumeSurface.gameObject.SetActive(item.Kind == ShopDragKind.Product);
            if (item.Kind == ShopDragKind.Product) shiftRaceResumeSurface.transform.SetAsLastSibling();
            shiftGhost.gameObject.SetActive(true); shiftGhost.SetAsLastSibling();
            shiftRaceHighlight.gameObject.SetActive(true);
            shiftPourHint.text = item.Kind == ShopDragKind.Sugar ? "↕"
                : game.Shift.State.BatchMeters > 0 ? "여기에 놓으면 만들던 솜사탕과 교환해요" : "여기에 놓으면 이 솜사탕을 이어서 만들어요";
            MoveShiftDrag(item, data); return true;
        }

        public void MoveShiftDrag(ShopDragItem item, PointerEventData data)
        {
            if (shiftDrag != item || !ShiftInteractionsAllowed || !root) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, data.position, data.pressEventCamera, out Vector2 local)) return;
            float x = local.x + root.rect.width * .5f, y = root.rect.height * .5f - local.y;
            shiftGhost.localScale = item.Kind == ShopDragKind.Product ? Vector3.one * .72f : Vector3.one;
            if (item.Kind == ShopDragKind.Product)
            {
                // Center the scaled illustration, excluding its caption, on the pointer.
                // Allow edge clipping so the held candy never drifts away from the cursor.
                var art = shiftGhostArt.rectTransform;
                Vector3 centerOffset = root.InverseTransformVector(art.TransformPoint(art.rect.center) - shiftGhost.position);
                shiftGhost.anchoredPosition = new Vector2(x - centerOffset.x, -y - centerOffset.y);
            }
            else shiftGhost.anchoredPosition = new Vector2(Mathf.Clamp(x - 63, 0, 1474), -Mathf.Clamp(y - 62, 0, 749));
            if (item.Kind != ShopDragKind.Sugar) return;
            bool inside = x >= 0 && x < 960 && y >= 92 && y <= 900;
            shiftShake.FullStrokePixels = game.SugarShakeFullStrokePixels;
            bool shake = shiftShake.Move(x, y, inside);
            double beforeSugar = game.Shift.State.SugarFlavor == item.FlavorIndex ? game.Shift.State.SugarGrams : 0;
            int beforeCoins = game.Session.Economy.Coins;
            if (shake && game.PourSugar(item.FlavorIndex, shiftShake.Amount))
            {
                shiftPourFlashUntil = Time.unscaledTime + .5f;
                double poured = game.Shift.State.SugarGrams - beforeSugar;
                int cost = beforeCoins - game.Session.Economy.Coins;
                shiftPourHint.text = "+" + poured.ToString("0.#") + " g" + (cost > 0 ? "  ·  -" + cost + " C" : "");
            }
            else if (shake && game.HasProgression)
            {
                shiftPourFlashUntil = Time.unscaledTime + .7f;
                var state = game.Shift.State;
                int cap = game.Shift.MaxSize(game.Shift.SelectedMachine);
                if (state.BatchMeters > 0) cap = Math.Min(cap, Math.Max(0, state.BatchSugarGrade - 1));
                shiftPourHint.text = state.BatchMeters >= ShopShift.MetersForSize(cap) - .001 ? "최대 크기  ·  F 꺼내기"
                    : state.BatchMeters > 0 && state.BatchFlavor != item.FlavorIndex ? "제작 중인 맛과 달라요"
                    : state.SugarGrams >= 100 ? "설탕 가득" : "재료비 부족";
            }
            else if (Time.unscaledTime >= shiftPourFlashUntil) shiftPourHint.text = inside ? "↕" : "←";
        }

        public DeliveryResult CompleteShiftDrop(ShopDragItem item, ShopDropTarget target, out bool consumed)
        {
            consumed = false;
            if (!ShiftInteractionsAllowed || item.Owner != this || target.Owner != this) return DeliveryResult.Rejected;
            if (target.Kind == ShopDropKind.Trash) { consumed = game.TrashCandy(item.CapturedProductId); return DeliveryResult.Rejected; }
            if (target.Kind == ShopDropKind.Race) { consumed = game.ResumeCandy(item.CapturedProductId); return DeliveryResult.Rejected; }
            string customerId = target.CustomerId;
            if (string.IsNullOrEmpty(customerId)) return DeliveryResult.Rejected;
            var delivery = game.DeliverCandy(item.CapturedProductId, customerId); consumed = delivery != DeliveryResult.Rejected; return delivery;
        }

        public void EndShiftDrag(ShopDragItem item)
        {
            if (shiftDrag != item) return;
            shiftDrag = null; shiftShake.Reset();
            if (shiftGhost) shiftGhost.gameObject.SetActive(false);
            if (shiftRaceHighlight) shiftRaceHighlight.gameObject.SetActive(false);
            if (shiftRaceResumeSurface) shiftRaceResumeSurface.gameObject.SetActive(false);
        }

        public void CancelShiftDrag()
        {
            var item = shiftDrag; shiftDrag = null;
            if (item) item.CancelDrag();
            shiftShake.Reset();
            if (shiftGhost) shiftGhost.gameObject.SetActive(false);
            if (shiftRaceHighlight) shiftRaceHighlight.gameObject.SetActive(false);
            if (shiftRaceResumeSurface) shiftRaceResumeSurface.gameObject.SetActive(false);
        }
    }
}
