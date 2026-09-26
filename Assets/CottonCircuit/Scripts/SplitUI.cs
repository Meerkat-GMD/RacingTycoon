using UnityEngine;
using UnityEngine.UI;

namespace CottonCircuit
{
    public partial class GameUI
    {
        bool splitLayout;
        Button autoDriveButton, productionTab, upgradesTab;
        GameObject productionSettings, shopUpgrades;
        Text productionState;

        void BuildSplitChrome(RectTransform p)
        {
            if (game.Shift != null) { BuildShiftChrome(p); return; }
            // Leave the live shop camera's viewport uncovered by the overlay canvas.
            Box(p, "Shop header backdrop", 960, 0, 640, 82, Palette.Cream, false);
            Box(p, "Shop backdrop", 960, 208, 640, 692, Palette.Cream, false);
            Box(p, "Shop preview left edge", 960, 82, 20, 126, Palette.Cream, false);
            Box(p, "Shop preview right edge", 1576, 82, 24, 126, Palette.Cream, false);
            Box(p, "Race header", 0, 0, 960, 92, Palette.Cream, false);
            Box(p, "Divider", 960, 0, 2, 900, Palette.Hex("DDD4C4"), false);
            Label(p, "솜사탕 서킷", 24, 14, 270, 38, 28, Palette.Ink, FontStyle.Bold);
            Label(p, "달리는 동안에도, 가게는 계속 열려 있어요", 26, 56, 530, 23, 15, Palette.Muted);
            day = Label(p, "DAY 01", 290, 24, 132, 28, 14, Palette.Muted, FontStyle.Bold);
            autoDriveButton = ButtonAt(p, "자동 주행 ON", 652, 22, 166, 46, Palette.Soda, Palette.Ink, () => game.ToggleAutoDrive(), 17);
            autoDriveButton.name = "Auto drive toggle";
            ButtonAt(p, "도움말", 832, 22, 104, 46, Color.white, Palette.Ink, () => game.TogglePause(), 16);
            Label(p, "우리 솜사탕 가게", 982, 17, 310, 34, 24, Palette.Ink, FontStyle.Bold);
            Label(p, "OPEN  ·  재고 선택 → 손님에게 건네기", 984, 52, 340, 23, 14, Palette.Muted);
            var wallet = Box(p, "Wallet", 1352, 16, 224, 58, Palette.Ink);
            coins = Label(wallet.rectTransform, "80", 12, 7, 144, 44, 25, Color.white, FontStyle.Bold, TextAnchor.MiddleRight);
            Label(wallet.rectTransform, "코인", 164, 17, 48, 28, 14, Palette.Yellow, FontStyle.Bold);
        }

        void BuildSplitShop()
        {
            if (game.Shift != null) { BuildShiftShop(); return; }
            var p = shop.GetComponent<RectTransform>();
            Label(p, "기다리는 손님", 982, 214, 290, 27, 18, Palette.Ink, FontStyle.Bold);
            Label(p, "주문에 맞는 재고를 골라주세요", 1280, 218, 296, 23, 13, Palette.Muted, FontStyle.Normal, TextAnchor.UpperRight);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var card = Box(p, "Customer order " + i, 980 + i * 304, 248, 292, 145, Color.white);
                orderCards[i] = card;
                orderTitles[i] = Label(card.rectTransform, "", 14, 10, 264, 28, 18, Palette.Ink, FontStyle.Bold);
                orderHints[i] = Label(card.rectTransform, "", 14, 43, 264, 43, 13, Palette.Muted);
                orderFills[i] = Progress(card.rectTransform, 14, 89, 264, 5, Palette.Soda);
                orderSelect[i] = ButtonAt(card.rectTransform, "주문 선택", 12, 104, 84, 32, Palette.Cream, Palette.Ink, () => game.SelectOrder(OrderAt(index)?.Id), 13);
                orderMake[i] = ButtonAt(card.rectTransform, "다음 제작", 104, 104, 84, 32, Palette.Ink, Color.white, () => game.MakeOrder(OrderAt(index)?.Id), 13);
                orderServe[i] = ButtonAt(card.rectTransform, "건네기", 196, 104, 84, 32, Palette.Soda, Palette.Ink, () => game.Serve(OrderAt(index)?.Id), 14);
            }
            var stocks = Box(p, "Stock selector", 980, 406, 596, 173, Color.white);
            stockCount = Label(stocks.rectTransform, "", 14, 10, 410, 27, 16, Palette.Ink, FontStyle.Bold);
            discardButton = ButtonAt(stocks.rectTransform, "선택 재고 정리", 430, 7, 152, 29, Palette.Cream, Palette.Muted, () => game.DiscardSelected(), 12);
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                stockButtons[i] = ButtonAt(stocks.rectTransform, "빈 칸", 12 + i % 6 * 96, 46 + i / 6 * 58, 92, 52, Palette.Cream, Palette.Ink,
                    () => { if (index < game.Session.Economy.Inventory.Count) game.SelectProduct(game.Session.Economy.Inventory[index].Id); }, 12);
            }
            productionTab = ButtonAt(p, "다음 생산 설정", 980, 592, 292, 40, Palette.Ink, Color.white, () => ShowShopTab(false), 16);
            upgradesTab = ButtonAt(p, "가게 업그레이드", 1284, 592, 292, 40, Color.white, Palette.Ink, () => ShowShopTab(true), 16);
            productionSettings = Group(p, "Production settings");
            shopUpgrades = Group(p, "Shop upgrades");
            BuildProductionSettings(productionSettings.GetComponent<RectTransform>());
            BuildShopUpgrades(shopUpgrades.GetComponent<RectTransform>());
            status = Label(p, "", 982, 869, 592, 25, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            ShowShopTab(false);
        }

        void BuildProductionSettings(RectTransform p)
        {
            Label(p, "제품 크기", 982, 650, 100, 27, 14, Palette.Muted);
            smallButton = ButtonAt(p, "작은 60g · 1번 맵", 1086, 644, 236, 35, Color.white, Palette.Ink, () => game.SetSize(0), 15);
            largeButton = ButtonAt(p, "큰 120g · 2번 맵", 1334, 644, 242, 35, Color.white, Palette.Ink, () => game.SetSize(1), 15);
            Label(p, "솜사탕 맛", 982, 696, 100, 27, 14, Palette.Muted);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                flavorButtons[i] = ButtonAt(p, Palette.FlavorName(i), 1086 + i * 166, 690, 158, 35, Color.white, Palette.Ink, () => game.SetFlavor(index), 15);
            }
            Label(p, "주행 스타일", 982, 742, 100, 27, 14, Palette.Muted);
            styleButtons[0] = ButtonAt(p, "카트 스타일", 1086, 736, 236, 35, Color.white, Palette.Ink, () => game.SetStyle(0), 15);
            styleButtons[1] = ButtonAt(p, "다운힐 스타일", 1334, 736, 242, 35, Color.white, Palette.Ink, () => game.SetStyle(1), 15);
            styleHint = Label(p, "", 982, 782, 594, 26, 13, Palette.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            startButton = ButtonAt(p, "선택한 설정으로 계속 만들기  →", 982, 818, 594, 40, Palette.Pink, Palette.Ink, () => game.PrepareStock(), 17);
        }

        void BuildShopUpgrades(RectTransform p)
        {
            string[] names = { "카트 모터", "설탕통", "가게 꾸미기" };
            string[] hints = { "최고 속도 증가", "생산 속도와 품질 증가", "판매가 증가" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var row = Box(p, "Upgrade " + i, 982, 644 + 43 * i, 594, 39, Color.white);
                upgradeTexts[i] = Label(row.rectTransform, names[i], 12, 7, 176, 28, 15, Palette.Ink, FontStyle.Bold);
                Label(row.rectTransform, hints[i], 204, 9, 240, 26, 13, Palette.Muted);
                upgradeButtons[i] = ButtonAt(row.rectTransform, "120", 460, 4, 122, 31, Palette.Yellow, Palette.Ink, () => game.BuyUpgrade(index), 15);
            }
            var shelf = Box(p, "Shelf expansion", 982, 773, 594, 40, Color.white);
            shelfText = Label(shelf.rectTransform, "", 12, 3, 410, 35, 13, Palette.Ink, FontStyle.Bold);
            shelfButton = ButtonAt(shelf.rectTransform, "160", 460, 4, 122, 32, Palette.Soda, Palette.Ink, () => game.BuyShelf(), 15);
            business = Label(p, "", 984, 820, 590, 43, 13, Palette.Muted);
        }

        void ShowShopTab(bool upgrades)
        {
            productionSettings.SetActive(!upgrades); shopUpgrades.SetActive(upgrades);
            productionTab.image.color = upgrades ? Color.white : Palette.Ink;
            productionTab.GetComponentInChildren<Text>().color = upgrades ? Palette.Ink : Color.white;
            upgradesTab.image.color = upgrades ? Palette.Ink : Color.white;
            upgradesTab.GetComponentInChildren<Text>().color = upgrades ? Color.white : Palette.Ink;
        }

        void BuildSplitRace()
        {
            if (game.Shift != null) { BuildShiftRace(); return; }
            var p = race.GetComponent<RectTransform>();
            speedLines = Rect(p, "Speed streaks", 0, 92, 960, 808).gameObject.AddComponent<SpeedLinesGraphic>();
            speedLines.raycastTarget = false;
            var mapCard = Box(p, "Course map", 20, 112, 180, 182, new Color(1, .97f, .90f, .93f));
            mapTitle = Label(mapCard.rectTransform, "", 12, 10, 156, 25, 14, Palette.Ink, FontStyle.Bold);
            var mapRect = Rect(mapCard.rectTransform, "Live course", 10, 42, 160, 128);
            mapRect.pivot = Vector2.zero; mapRect.anchoredPosition = new Vector2(10, -170);
            map = mapRect.gameObject.AddComponent<RaceMapGraphic>(); map.raycastTarget = false; map.Kart = game.World.Kart;
            var lapCard = Box(p, "Lap times", 216, 112, 218, 95, Palette.Ink);
            laps = Label(lapCard.rectTransform, "", 14, 9, 190, 35, 23, Color.white, FontStyle.Bold);
            bestLap = Label(lapCard.rectTransform, "", 14, 47, 192, 38, 13, Palette.Cream);
            var clock = Box(p, "Race clock", 448, 112, 144, 95, Palette.Yellow);
            Label(clock.rectTransform, "이번 생산", 0, 10, 144, 23, 13, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            timer = Label(clock.rectTransform, "0:00", 0, 32, 144, 52, 32, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            raceOrder = Label(p, "", 216, 219, 475, 57, 14, Palette.Ink, FontStyle.Bold);
            var product = Box(p, "Live cotton", 716, 112, 224, 194, new Color(1, .97f, .90f, .94f));
            productionState = Label(product.rectTransform, "주행하며 만드는 중", 12, 10, 200, 26, 15, Palette.Ink, FontStyle.Bold);
            var preview = Rect(product.rectTransform, "Cotton preview", 6, 43, 96, 108).gameObject.AddComponent<RawImage>();
            preview.texture = game.World.CandyPreview; preview.raycastTarget = false;
            grams = Label(product.rectTransform, "0 g", 108, 43, 104, 36, 23, Palette.Ink, FontStyle.Bold);
            flavor = Label(product.rectTransform, "", 108, 84, 104, 44, 13, Palette.Muted);
            raceQuality = Label(product.rectTransform, "", 12, 151, 200, 24, 12, Palette.Muted);
            productionFill = Progress(product.rectTransform, 12, 179, 200, 6, Palette.Pink);
            var speedCard = Box(p, "Speedometer", 20, 746, 220, 91, Palette.Ink);
            Label(speedCard.rectTransform, "LIVE RACING", 14, 9, 192, 22, 12, Palette.Soda, FontStyle.Bold);
            speed = Label(speedCard.rectTransform, "0 km/h", 12, 34, 196, 48, 32, Color.white, FontStyle.Bold);
            var driftCard = Box(p, "Drift meter", 258, 778, 444, 59, Palette.Ink);
            driftLabel = Label(driftCard.rectTransform, "", 12, 6, 420, 29, 14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            driftFill = Progress(driftCard.rectTransform, 14, 41, 416, 7, Palette.Soda);
            raceEvent = Label(p, "", 220, 371, 520, 46, 23, Palette.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            var controls = Box(p, "Driving controls", 20, 851, 920, 31, new Color(1, .97f, .9f, .95f));
            raceControls = Label(controls.rectTransform, "", 10, 0, 900, 31, 13, Palette.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        void RefreshSplit()
        {
            if (game.Shift != null) { RefreshShift(); return; }
            var s = game.Session; var e = s.Economy; var kart = game.World.Kart; var drive = kart.DriveModel;
            common.SetActive(true); shop.SetActive(true); race.SetActive(true); result.SetActive(false);
            pause.SetActive(s.Paused); toast.SetActive(!string.IsNullOrEmpty(game.Notice)); notice.text = game.Notice ?? "";
            toast.GetComponent<RectTransform>().anchoredPosition = s.Paused ? new Vector2(470, -780) : new Vector2(150, -319);
            coins.text = e.Coins.ToString("N0"); day.text = "DAY " + e.Day.ToString("00");
            muteLabel.text = game.Audio.Muted ? "소리 꺼짐" : "소리 켜짐";
            autoDriveButton.GetComponentInChildren<Text>().text = game.AutoDrive ? "자동 주행 ON" : "수동 운전 중";
            autoDriveButton.image.color = game.AutoDrive ? Palette.Soda : Palette.Yellow;
            RefreshShop();
            speedLines.SetDriving(kart, game.RaceVisible && !s.Paused);
            speed.text = Mathf.RoundToInt(kart.Speed * 3.6f) + " km/h";
            timer.text = ((int)s.Elapsed / 60) + ":" + ((int)s.Elapsed % 60).ToString("00");
            mapTitle.text = RaceRecipe.Name(game.RunMap);
            bool waiting = s.ProductionWaiting;
            int weight = s.Production == null || waiting ? 0 : s.Production.Grams;
            grams.text = weight + " g";
            productionFill.rectTransform.sizeDelta = new Vector2(200f * weight / game.RunTargetGrams, 6);
            productionState.text = waiting ? "생산 대기 · 주행 중" : "주행하며 만드는 중";
            flavor.text = Palette.FlavorName(game.RunFlavor) + "맛\n목표 " + game.RunTargetGrams + "g";
            raceQuality.text = waiting ? "빈 칸이 생기면 다음 바퀴부터 생산" : "한 바퀴마다 완성 → 진열대에 보관";
            float progress = Mathf.Clamp01((float)game.RunProgress);
            laps.text = waiting ? "계속 달리는 중" : "이번 바퀴 " + progress.ToString("P0");
            bestLap.text = "이번 실행 생산 " + s.CompletedRecipes + "개\n" + (waiting ? "판매 후 다음 바퀴부터 생산" : "완주 후에도 계속 달려요");
            raceOrder.text = (game.RunStyle == DrivingStyle.Kart ? "카트" : "다운힐") + " · " + Palette.FlavorName(game.RunFlavor) + " " + game.RunTargetGrams + "g 생산\n" +
                (game.AutoDrive ? "자동 운전 중 · 오른쪽에서 손님을 응대하세요" : "직접 운전 중 · 상단에서 자동 주행으로 전환");
            float boostFraction = Mathf.Clamp01((float)(drive.BoostRemaining / System.Math.Max(.01, drive.BoostDuration)));
            driftFill.rectTransform.sizeDelta = new Vector2(416 * (kart.Boosting ? boostFraction : kart.Charge), 7);
            driftFill.color = game.RunStyle == DrivingStyle.Downhill ? Palette.Soda : kart.Boosting
                ? (drive.BoostTier == 2 ? Palette.Yellow : Palette.Soda) : kart.Charge >= .75f ? Palette.Yellow : Palette.Pink;
            driftLabel.text = game.AutoDrive ? "AUTO PILOT  ·  생산과 판매를 동시에" : game.RunStyle == DrivingStyle.Downhill
                ? "W 유지하면 가속 · 코너 전 S 감속" : kart.Boosting ? "BOOST!  달콤한 가속 중"
                : kart.Charge >= .32f ? "Space를 놓아 부스트!  ·  보관 " + drive.StoredBoosts + "/2"
                : "Space 드리프트  ·  Shift 부스터  [ " + drive.StoredBoosts + " / 2 ]";
            driftLabel.transform.parent.gameObject.SetActive(game.RunStyle == DrivingStyle.Kart);
            raceEvent.text = kart.ImpactFlash > 0 ? "벽 충돌!  R로 코스 복귀" : kart.Boosting ? "달콤한 가속!" : "";
            raceControls.text = game.AutoDrive ? "자동으로 달리고 한 바퀴마다 솜사탕을 만들어요  ·  오른쪽에서 판매와 다음 생산을 관리하세요" : "W 가속   A / D 조향   S 제동   Space 드리프트   Shift 부스터   R 복귀   Esc 일시정지";
            if (!game.AutoDrive && game.RunStyle == DrivingStyle.Downhill) raceControls.text = "W 가속   A / D 조향   S 제동   Space 드리프트 감속   R 복귀   Esc 일시정지";
            map.SetVerticesDirty();
        }
    }
}
